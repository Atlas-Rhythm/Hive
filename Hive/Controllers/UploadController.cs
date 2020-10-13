using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using MathExpr.Syntax;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Serilog;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hive.Controllers
{
    [Aggregable]
    public interface IUploadPlugin
    {
        [return: StopIfReturns(false)]
        bool ValidateAndPopulateKnownMetadata(Mod mod, Stream data, [ReturnLast] out object? validationFailureInfo);

        [return: StopIfReturns(false)]
        bool ValidateAndPopulateKnownMetadata(Mod mod, Stream data, 
            [TakesOutValue(2)] [In, Out] ref object? dataContext,
            [ReturnLast] out object? validationFailureInfo)
            => ValidateAndPopulateKnownMetadata(mod, data, out validationFailureInfo);

        // TODO: maybe do another validation after confirmation?
    }

    internal class HiveDefaultUploadPlugin : IUploadPlugin
    {
        [return: StopIfReturns(false)]
        public bool ValidateAndPopulateKnownMetadata(Mod mod, Stream data, [ReturnLast] out object? validationFailureInfo)
        {
            validationFailureInfo = null;
            return true;
        }
    }

    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly IUploadPlugin plugins;
        private readonly IProxyAuthenticationService authService;
        private readonly ICdnProvider cdn;
        private readonly HiveContext database;

        public UploadController(ILogger log,
            PermissionsManager<PermissionContext> perms,
            IAggregate<IUploadPlugin> plugins,
            IProxyAuthenticationService auth,
            ICdnProvider cdn,
            HiveContext db)
        {
            if (plugins is null)
                throw new ArgumentNullException(nameof(plugins));

            logger = log;
            permissions = perms;
            this.plugins = plugins.Instance;
            this.cdn = cdn;
            authService = auth;
            database = db;
        }

        public enum ResultType
        {
            Success,
            Error
        }

        public struct UploadResult
        {
            [JsonPropertyName("type")]
            public ResultType Type { get; init; }

            [JsonPropertyName("error")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public object? ErrorContext { get; init; }


            [JsonPropertyName("data")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public JsonElement ExtractedData { get; init; }

            [JsonPropertyName("actionCookie")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ActionCookie { get; init; }

            internal static UploadResult ErrNoFile()
                => new UploadResult
                {
                    Type = ResultType.Error,
                    ErrorContext = "No file was given"
                };

            internal static UploadResult ErrValidationFailed(object? context)
                => new UploadResult
                {
                    Type = ResultType.Error,
                    ErrorContext = context
                };

            // TODO: this should take encryption keys
            internal static UploadResult Ok(Mod data, CdnObject cdnObj)
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    IgnoreNullValues = true
                };

                var abw = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(abw))
                    JsonSerializer.Serialize(writer, data, options);
                var doc = JsonDocument.Parse(abw.WrittenMemory);

                // TODO: figure out actual encryption stuffs
                string encData;
                using (var rij = Rijndael.Create()) // i just picked one lmao, need to keep the key and IV around (probably constant for the lifetime of the app) for the actual impl
                {
                    var enc = rij.CreateEncryptor();

                    using var mStream = new MemoryStream();
                    using (var encStream = new CryptoStream(mStream, enc, CryptoStreamMode.Write))
                    {
                        using var writer = new Utf8JsonWriter(encStream);

                        JsonSerializer.Serialize(writer,
                            new EncryptedUploadPayload
                            {
                                ModData = doc.RootElement,
                                CdnObject = cdnObj
                            });
                    }

                    if (!mStream.TryGetBuffer(out var buffer))
                        throw new InvalidOperationException(); // panic! this should never happen

                    encData = Convert.ToBase64String(buffer);
                }

                return new UploadResult
                {
                    Type = ResultType.Success,
                    ActionCookie = encData,
                    ExtractedData = doc.RootElement.Clone()
                };
            }
        }
        
        private struct EncryptedUploadPayload
        {
            public JsonElement ModData { get; init; }
            public CdnObject CdnObject { get; init; }
        }

        [ThreadStatic]
        private static PermissionActionParseState BaseUploadParseState;
        private const string BaseUploadAction = "hive.mods.upload";

        [ThreadStatic]
        private static PermissionActionParseState UploadWithDataParseState;
        private const string UploadWithDataAction = "hive.mods.upload.with_data";

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UploadResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
        {
            if (file is null)
                return BadRequest(UploadResult.ErrNoFile()); // TODO: add error information

            var user = await authService.GetUser(Request).ConfigureAwait(false);

            if (user is null)
                return Unauthorized();

            // check if the user is allowed to upload at all
            if (!permissions.CanDo(BaseUploadAction, new PermissionContext { User = user }, ref BaseUploadParseState))
                return Forbid();

            // TODO: check that the file is not too large

            // we'll start by copying the file into an in-memory stream
            using var memStream = new MemoryStream((int)file.Length);

            await file.CopyToAsync(memStream).ConfigureAwait(false);

            // go back to the beginning
            memStream.Seek(0, SeekOrigin.Begin);

            // TODO: figure out what the default channel should be
            var modData = new Mod
            {
                UploadedAt = SystemClock.Instance.GetCurrentInstant(), // TODO: DI clock instance?
                Uploader = user
            };

            // the dataContext ref param allows the plugins to pass data around to avoid re-parsing, when possible
            // For example, most mods will be ZIP files. The first plugin to get called would load it into a ZipFile,
            //   then put that object into the dataContext variable. Later plugins can then check that context to 
            //   see if it is a ZipFile, and avoid having to re-parse and re-create that information.
            object? dataContext = null;
            var result = plugins.ValidateAndPopulateKnownMetadata(modData, memStream, ref dataContext, out var valFailCtx);
            // We try to dispose the context if possible to help clean up resources persisted in dataContext more quickly.
            if (dataContext is IAsyncDisposable adisp)
                await adisp.DisposeAsync().ConfigureAwait(false);
            else if (dataContext is IDisposable disp)
                disp.Dispose();

            if (!result)
                return BadRequest(UploadResult.ErrValidationFailed(valFailCtx));

            if (!permissions.CanDo(UploadWithDataAction, new PermissionContext { User = user, Mod = modData }, ref UploadWithDataParseState))
                return Forbid();

            // we've gotten the OK based on all of our other checks, lets upload the file to the actual CDN
            memStream.Seek(0, SeekOrigin.Begin);
            var cdnObject = await cdn.UploadObject(file.FileName, memStream).ConfigureAwait(false);

            // TODO: encrypt/sign extracted mod data and CDN link; return it

            // this method encrypts the extracted data into a cookie in the resulting object that is sent along
            return UploadResult.Ok(modData, cdnObject);
        }

    }
}
