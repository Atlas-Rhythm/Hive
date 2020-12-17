using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Utilities;
using MathExpr.Syntax;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
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
using System.Security.Policy;
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
            ref object? dataContext,
            [ReturnLast] out object? validationFailureInfo)
            => ValidateAndPopulateKnownMetadata(mod, data, out validationFailureInfo);

        void LatePopulateKnownMetadata(Mod mod, Stream data) { }

        void LatePopulateKnownMetadata(Mod mod, Stream data,
            ref object? dataContext)
            => LatePopulateKnownMetadata(mod, data);

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// <see cref="Mod.DownloadLink"/> may not be set on <paramref name="mod"/> when this is called.
        /// </remarks>
        /// <param name="mod"></param>
        /// <param name="originalAdditionalData"></param>
        /// <param name="validationFailureInfo"></param>
        /// <returns></returns>
        [return: StopIfReturns(false)]
        bool ValidateAndFixUploadedData(Mod mod, JsonElement originalAdditionalData, [ReturnLast] out object? validationFailureInfo);
    }

    internal class HiveDefaultUploadPlugin : IUploadPlugin
    {
        [return: StopIfReturns(false)]
        public bool ValidateAndFixUploadedData(Mod mod, JsonElement origAdditionalData, [ReturnLast] out object? validationFailureInfo)
        {
            validationFailureInfo = null;
            return true;
        }

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
        private readonly SymmetricAlgorithm tokenAlgorithm;
        private readonly HiveContext database;
        private readonly IClock nodaClock;
        private readonly long maxFileSize;

        public UploadController(ILogger log,
            PermissionsManager<PermissionContext> perms,
            IAggregate<IUploadPlugin> plugins,
            IProxyAuthenticationService auth,
            ICdnProvider cdn,
            SymmetricAlgorithm tokenAlgo,
            HiveContext db,
            IClock clock,
            IConfiguration config)
        {
            if (plugins is null)
                throw new ArgumentNullException(nameof(plugins));
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            logger = log;
            permissions = perms;
            this.plugins = plugins.Instance;
            this.cdn = cdn;
            authService = auth;
            tokenAlgorithm = tokenAlgo;
            database = db;
            nodaClock = clock;
            maxFileSize = config.GetSection("Uploads:MaxFileSize").Get<long>();
            if (maxFileSize == 0) maxFileSize = 32 * 1024 * 1024;
        }

        public enum ResultType
        {
            Success,
            Confirm,
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
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public SerializedMod? ExtractedData { get; init; }

            [JsonPropertyName("actionCookie")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ActionCookie { get; init; }

            internal static UploadResult ErrNoFile()
                => new UploadResult
                {
                    Type = ResultType.Error,
                    ErrorContext = "No file was given"
                };

            internal static UploadResult ErrTooBig()
                => new UploadResult
                {
                    Type = ResultType.Error,
                    ErrorContext = "Uploaded file was too large"
                };

            internal static UploadResult ErrValidationFailed(object? context)
                => new UploadResult
                {
                    Type = ResultType.Error,
                    ErrorContext = context
                };

            internal static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                IgnoreNullValues = true
            }.ConfigureForNodaTime(DateTimeZoneProviders.Bcl); // BCL is (I think) the best thing to use here

            internal static UploadResult Confirm(SymmetricAlgorithm algo, Mod data, CdnObject cdnObj)
            {
                var serialized = SerializedMod.Serialize(data, data.Localizations.FirstOrDefault());

                using var mStream = new MemoryStream();
                using (var encStream = new CryptoStream(mStream, algo.CreateEncryptor(), CryptoStreamMode.Write, true))
                {
                    using var writer = new Utf8JsonWriter(encStream);

                    JsonSerializer.Serialize(writer,
                        new EncryptedUploadPayload
                        {
                            ModData = serialized,
                            CdnObject = cdnObj
                        }, Options);
                }

                if (!mStream.TryGetBuffer(out var buffer))
                    throw new InvalidOperationException(); // panic! this should never happen

                var encData = Convert.ToBase64String(buffer);

                return new UploadResult
                {
                    Type = ResultType.Confirm,
                    ActionCookie = encData,
                    ExtractedData = serialized
                };
            }

            internal static UploadResult Finish()
                => new UploadResult
                {
                    Type = ResultType.Success
                };
        }
        
        private struct EncryptedUploadPayload
        {
            public SerializedMod ModData { get; init; }
            public CdnObject CdnObject { get; init; }

            internal static async ValueTask<EncryptedUploadPayload> ExtractFromCookie(SymmetricAlgorithm algo, string cookie)
            {
                var data = Convert.FromBase64String(cookie);

                using var mStream = new MemoryStream(data);
                using var decStream = new CryptoStream(mStream, algo.CreateDecryptor(), CryptoStreamMode.Read);

                return await JsonSerializer.DeserializeAsync<EncryptedUploadPayload>(decStream, UploadResult.Options).ConfigureAwait(false);
            }
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
            var user = await authService.GetUser(Request).ConfigureAwait(false);

            if (user is null)
                return Unauthorized();

            if (file is null)
                return BadRequest(UploadResult.ErrNoFile());

            if (file.Length > maxFileSize)
                return BadRequest(UploadResult.ErrTooBig());

            // check if the user is allowed to upload at all
            if (!permissions.CanDo(BaseUploadAction, new PermissionContext { User = user }, ref BaseUploadParseState))
                return Forbid();

            logger.Information("Began mod upload by user {User}", user.Username);

            // we'll start by copying the file into an in-memory stream
            using var memStream = new MemoryStream((int)file.Length);

            await file.CopyToAsync(memStream).ConfigureAwait(false);

            // go back to the beginning
            memStream.Seek(0, SeekOrigin.Begin);

            // TODO: figure out what the default channel should be
            var modData = new Mod
            {
                UploadedAt = nodaClock.GetCurrentInstant(),
                Uploader = user,
                AdditionalData = JsonDocument.Parse("{}").RootElement.Clone()
            };

            // the dataContext ref param allows the plugins to pass data around to avoid re-parsing, when possible
            // For example, most mods will be ZIP files. The first plugin to get called would load it into a ZipFile,
            //   then put that object into the dataContext variable. Later plugins can then check that context to 
            //   see if it is a ZipFile, and avoid having to re-parse and re-create that information.
            object? dataContext = null;
            var result = plugins.ValidateAndPopulateKnownMetadata(modData, memStream, ref dataContext, out var valFailCtx);
            if (result) plugins.LatePopulateKnownMetadata(modData, memStream, ref dataContext);

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
            var cdnObject = await cdn.UploadObject(file.FileName, memStream, nodaClock.GetCurrentInstant() + Duration.FromHours(1)).ConfigureAwait(false);

            var uploadResult = UploadResult.Confirm(tokenAlgorithm, modData, cdnObject);
            logger.Information("First stage of mod upload complete (cookie {ID})", uploadResult.ActionCookie!.Substring(0, 16));

            // this method encrypts the extracted data into a cookie in the resulting object that is sent along
            return uploadResult;
        }


        [HttpPost("finish")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status410Gone)] 
        // ^^^ returned when the object associated with the upload was deleted automatically. Basically, "You took too long"
        public async Task<ActionResult<UploadResult>> CompleteUpload([FromForm] SerializedMod finalMetadata, [FromForm] string cookie)
        {
            if (finalMetadata is null || cookie is null)
                return BadRequest();

            var user = await authService.GetUser(Request).ConfigureAwait(false);

            if (user is null)
                return Unauthorized();

            if (finalMetadata.ID is null
             || finalMetadata.Version is null
             || finalMetadata.ChannelName is null
             || finalMetadata.LocalizedModInfo is null
             || finalMetadata.LocalizedModInfo.Language is null
             || finalMetadata.LocalizedModInfo.Name is null
             || finalMetadata.LocalizedModInfo.Description is null
             || finalMetadata.SupportedGameVersions is null || finalMetadata.SupportedGameVersions.Count < 1)
                return BadRequest("Missing metadata keys");

            logger.Information("Completing upload {ID}", cookie.Substring(0, 16));

            // decrypt the token
            var payload = await EncryptedUploadPayload.ExtractFromCookie(tokenAlgorithm, cookie).ConfigureAwait(false);

            var cdnObject = payload.CdnObject;

            #region Create modObject
            var modObject = new Mod
            {
                ReadableID = finalMetadata.ID,
                Version = finalMetadata.Version,
                UploadedAt = payload.ModData.UploadedAt,
                EditedAt = null,
                Uploader = user,
                Dependencies = finalMetadata.Dependencies?.ToList() ?? new(), // if deps is null, default to empty list (it's allowed)
                Conflicts = finalMetadata.ConflictsWith?.ToList() ?? new(),   // same as above
                AdditionalData = finalMetadata.AdditionalData,
                Links = finalMetadata.Links?.Select(t => (t.Item1, new Uri(t.Item2))).ToList() ?? new(), // defaults to empty list
                Authors = new List<User>(), // both of these default to empty lists
                Contributors = new List<User>(),
            };

            // REVIEW: should nonexistent users be an error?

            if (finalMetadata.Authors is not null)
            {
                modObject.Authors = await finalMetadata.Authors
                    .Select(n => authService.GetUser(n))
                    .FlattenToAsyncEnumerable()
                    .WhereNotNull()
                    .ToListAsync()
                    .ConfigureAwait(false);
            }

            if (finalMetadata.Contributors is not null)
            {
                modObject.Contributors = await finalMetadata.Contributors
                    .Select(n => authService.GetUser(n))
                    .FlattenToAsyncEnumerable()
                    .WhereNotNull()
                    .ToListAsync()
                    .ConfigureAwait(false);
            }

            var channel = await database.Channels.FirstOrDefaultAsync(c => c.Name == finalMetadata.ChannelName).ConfigureAwait(false);
            if (channel is null)
                return BadRequest($"Missing channel '{finalMetadata.ChannelName}'");
            modObject.Channel = channel;

            var versions = finalMetadata.SupportedGameVersions
                .Select(name => (name, version: database.GameVersions.FirstOrDefault(v => v.Name == name)))
                .ToList();

            foreach (var (name, version) in versions)
            {
                if (version is null)
                    return BadRequest($"Missing game version '{name}'");
            }

            modObject.SupportedVersions = versions.Select(t => t.version!).ToList();

            var localization = new LocalizedModInfo
            {
                Language = finalMetadata.LocalizedModInfo.Language,
                Name = finalMetadata.LocalizedModInfo.Name,
                Description = finalMetadata.LocalizedModInfo.Description,
                Credits = finalMetadata.LocalizedModInfo.Credits,
                Changelog = finalMetadata.LocalizedModInfo.Changelog,
                OwningMod = modObject // this adds the localization to the mod object
            };
            #endregion

            var result = plugins.ValidateAndFixUploadedData(modObject, payload.ModData.AdditionalData, out var validationFailureInfo);
            if (!result)
                return BadRequest(UploadResult.ErrValidationFailed(validationFailureInfo));

            if (!await cdn.RemoveExpiry(cdnObject).ConfigureAwait(false))
                return StatusCode(StatusCodes.Status410Gone); // the object no longer exists, tell the client as much

            modObject.DownloadLink = await cdn.GetObjectActualUrl(cdnObject).ConfigureAwait(false);

            // do one final permission check
            if (!permissions.CanDo(UploadWithDataAction, new PermissionContext { User = user, Mod = modObject }, ref UploadWithDataParseState))
            {
                // the user doesn't have permissions, so we don't want to keep the object on the CDN
                await cdn.TryDeleteObject(cdnObject).ConfigureAwait(false);
                return Forbid();
            }

            // ok, we're good to just go ahead and insert it into the database
            await using (var transaction = await database.Database.BeginTransactionAsync().ConfigureAwait(false))
            {
                database.ModLocalizations.Add(localization);
                database.Mods.Add(modObject);
                await database.SaveChangesAsync().ConfigureAwait(false);

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            logger.Information("Upload {ID} complete: {Name} by {Author}", cookie.Substring(0, 16), localization.Name, modObject.Uploader.Username);

            return UploadResult.Finish();
        }

    }
}
