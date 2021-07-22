using Hive.Extensions;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins.Aggregates;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hive.Controllers
{
    /// <summary>
    /// A plugin for the mod upload flow.
    /// </summary>
    [Aggregable]
    public interface IUploadPlugin
    {
        /// <summary>
        /// Validates an upload and populates the object to be added from the uploaded file.
        /// </summary>
        /// <remarks>
        /// <para><paramref name="data"/> may not be at the beginning of the stream. It is recommended to seek to the beginning of the stream before reading.</para>
        /// <para>If you can, you should use <see cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/> and check the third parameter
        /// for a deserialized representation of the uploaded file to avoid reparsing it.</para>
        /// </remarks>
        /// <param name="mod">The mod object to populate.</param>
        /// <param name="data">The uploaded file.</param>
        /// <param name="validationFailureInfo">An object containing information about the rejection, if any.</param>
        /// <returns><see langword="true"/> if the uploaded file is valid, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/>
        [return: StopIfReturns(false)]
        bool ValidateAndPopulateKnownMetadata(Mod mod, Stream data, [ReturnLast] out object? validationFailureInfo);

        /// <summary>
        /// Validates an upload and populates the object to be added from the uploaded file.
        /// </summary>
        /// <remarks>
        /// <para><paramref name="data"/> may not be at the beginning of the stream. It is recommended to seek to the beginning of the stream before reading.</para>
        /// <para>The <paramref name="dataContext"/> parameter should be used to persist deserialized information about the file to avoid reparsing it. For example,
        /// if the upload is a zip file, a plugin may ensure that it is a <see cref="System.IO.Compression.ZipFile"/>, and in the case that it is already, it would
        /// completely avoid reparsing the entire file.</para>
        /// </remarks>
        /// <param name="mod">The mod object to populate.</param>
        /// <param name="data">The uploaded file.</param>
        /// <param name="dataContext">The context persisted between plugins for sharing data instead of reparsing it.</param>
        /// <param name="validationFailureInfo">An object containing information about the rejection, if any.</param>
        /// <returns><see langword="true"/> if the uploaded file is valid, <see langword="false"/> otherwise.</returns>
        [return: StopIfReturns(false)]
        bool ValidateAndPopulateKnownMetadata(Mod mod, Stream data,
            ref object? dataContext,
            [ReturnLast] out object? validationFailureInfo)
            => ValidateAndPopulateKnownMetadata(mod, data, out validationFailureInfo);

        /// <summary>
        /// Attempts to populate mod metadata from an uploaded file after it has been validated.
        /// </summary>
        /// <remarks>
        /// This is invoked after every plugins' <see cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/> is called, and only if
        /// they all return <see langword="true"/>.
        /// </remarks>
        /// <param name="mod">The mod object to populate.</param>
        /// <param name="data">The uploaded file.</param>
        /// <seealso cref="LatePopulateKnownMetadata(Mod, Stream, ref object?)"/>
        void LatePopulateKnownMetadata(Mod mod, Stream data) { }

        /// <summary>
        /// Attempts to populate mod metadata from an uploaded file after it has been validated.
        /// </summary>
        /// <remarks>
        /// <para>This is invoked after every plugins' <see cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/> is called, and only if
        /// they all return <see langword="true"/>.</para>
        /// <para>The <paramref name="dataContext"/> parameter behaves like the corresponding parameter on <see cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/>.
        /// Whatever value the last plugin left in <paramref name="dataContext"/> in <see cref="ValidateAndPopulateKnownMetadata(Mod, Stream, ref object?, out object?)"/> is the value
        /// the first plugin will see in this method.</para>
        /// </remarks>
        /// <param name="mod">The mod object to populate.</param>
        /// <param name="data">The uploaded file.</param>
        /// <param name="dataContext">The context persisted between plugins for sharing data instead of reparsing it.</param>
        void LatePopulateKnownMetadata(Mod mod, Stream data,
            ref object? dataContext)
            => LatePopulateKnownMetadata(mod, data);

        /// <summary>
        /// Validates and fixes the mod data after the user has supplied all metadata.
        /// </summary>
        /// <remarks>
        /// <para><see cref="Mod.DownloadLink"/> may not be set on <paramref name="mod"/> when this is called.</para>
        /// </remarks>
        /// <param name="mod">The mod object as populated by the user.</param>
        /// <param name="originalAdditionalData">The value of the <see cref="Mod.AdditionalData"/> property as populated in the initial upload phase.</param>
        /// <param name="validationFailureInfo">An object containing information about the rejection, if any.</param>
        /// <returns><see langword="true"/> if the upload is valid, <see langword="false"/> otherwise.</returns>
        [return: StopIfReturns(false)]
        bool ValidateAndFixUploadedData(Mod mod, JsonElement originalAdditionalData, [ReturnLast] out object? validationFailureInfo);

        /// <summary>
        /// A hook that is called when a mod has been fully uploaded and added to the database.
        /// </summary>
        /// <param name="modData">The mod that was just uploaded.</param>
        void UploadFinished(Mod modData) { }
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

    /// <summary>
    /// The controller for the <c>/upload/</c> portion of the API, handling the upload flow.
    /// </summary>
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

        /// <summary>
        /// Constructs the UploadController with the injected components it needs. For use with DI only.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="perms"></param>
        /// <param name="plugins"></param>
        /// <param name="auth"></param>
        /// <param name="cdn"></param>
        /// <param name="tokenAlgo"></param>
        /// <param name="db"></param>
        /// <param name="clock"></param>
        /// <param name="config"></param>
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

        /// <summary>
        /// The type of the resulting <see cref="UploadResult"/> structure.
        /// </summary>
        public enum ResultType
        {
            /// <summary>
            /// The structure represents a successful operation.
            /// </summary>
            Success,

            /// <summary>
            /// The structure represents an operation that needs to be confirmed.
            /// </summary>
            Confirm,

            /// <summary>
            /// The structure indicates that an error ocurred while processing the operation.
            /// </summary>
            Error
        }

        /// <summary>
        /// The structure which is returned from the enpoints in the upload flow.
        /// </summary>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible",
            Justification = "This structure has no meaning outside of this class. It needs to be public so that the signature of the methods" +
                            "implementing the API can have a generic return type.")]
        [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
            Justification = "Because this structure is used only as an API response, it never needs to be compared, and so does not need these members.")]
        public struct UploadResult
        {
            /// <summary>
            /// The type of result that this structure represents.
            /// </summary>
            [JsonPropertyName("type")]
            public ResultType Type { get; init; }

            /// <summary>
            /// An object representing the error in this result, if there is one. Null if there is no error.
            /// </summary>
            [JsonPropertyName("error")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public object? ErrorContext { get; init; }

            /// <summary>
            /// The mod data extracted during the first stage of the upload flow.
            /// </summary>
            [JsonPropertyName("data")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public SerializedMod? ExtractedData { get; init; }

            /// <summary>
            /// A cookie that is used to persist server-side information statelessly, so that a 2-stage upload is simple.
            /// Null if not the result of the first stage.
            /// </summary>
            [JsonPropertyName("actionCookie")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ActionCookie { get; init; }

            internal static UploadResult ErrNoFile()
                => new()
                {
                    Type = ResultType.Error,
                    ErrorContext = "No file was given"
                };

            internal static UploadResult ErrTooBig()
                => new()
                {
                    Type = ResultType.Error,
                    ErrorContext = "Uploaded file was too large"
                };

            internal static UploadResult ErrValidationFailed(object? context)
                => new()
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
                => new()
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

        private const string BaseUploadAction = "hive.mod.upload";

        [ThreadStatic]
        private static PermissionActionParseState UploadWithDataParseState;

        private const string UploadWithDataAction = "hive.mod.upload.with_data";

        /// <summary>
        /// The first stage of the upload flow. This stage is the file upload, which is then sent to whichever CDN is being used for this instance.
        /// </summary>
        /// <param name="file">The file that was uploaded.</param>
        /// <returns>An <see cref="UploadResult"/> which represents the result of the upload. May be either a failure status code or a success code.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(UploadResult))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UploadResult>> Upload(IFormFile file)
        {
            var user = await HttpContext.GetHiveUser(authService).ConfigureAwait(false);

            if (user is null)
                return new UnauthorizedResult();

            if (file is null)
                return BadRequest(UploadResult.ErrNoFile());

            if (file.Length > maxFileSize)
                return BadRequest(UploadResult.ErrTooBig());

            // check if the user is allowed to upload at all
            if (!permissions.CanDo(BaseUploadAction, new PermissionContext { User = user }, ref BaseUploadParseState))
                return new ForbidResult();

            logger.Information("Began mod upload by user {User}", user.Username);

            // we'll start by copying the file into an in-memory stream
            using var memStream = new MemoryStream((int)file.Length);

            await file.CopyToAsync(memStream).ConfigureAwait(false);

            // go back to the beginning
            _ = memStream.Seek(0, SeekOrigin.Begin);

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
                return new ForbidResult();

            // we've gotten the OK based on all of our other checks, lets upload the file to the actual CDN
            _ = memStream.Seek(0, SeekOrigin.Begin);
            var cdnObject = await cdn.UploadObject(file.FileName, memStream, nodaClock.GetCurrentInstant() + Duration.FromHours(1)).ConfigureAwait(false);

            var uploadResult = UploadResult.Confirm(tokenAlgorithm, modData, cdnObject);
            logger.Information("First stage of mod upload complete (cookie {ID})", uploadResult.ActionCookie!.Substring(0, 16));

            // this method encrypts the extracted data into a cookie in the resulting object that is sent along
            return uploadResult;
        }

        /// <summary>
        /// The final stage of the upload flow. Completes an upload by providing the final metadata of the mod.
        /// </summary>
        /// <param name="finalMetadata">The final mod metadata to upload.</param>
        /// <param name="cookie">The cookie returned in the response to the first stage.</param>
        /// <returns>An <see cref="UploadResult"/> repersenting the upload operation. If it completes normally with a
        /// <see cref="ResultType"/> of <see cref="ResultType.Success"/>, the upload was successful.</returns>
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

            var user = await HttpContext.GetHiveUser(authService).ConfigureAwait(false);

            if (user is null)
                return new UnauthorizedResult();

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

            #endregion Create modObject

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
                _ = await cdn.TryDeleteObject(cdnObject).ConfigureAwait(false); // if it failed, that should mean that it doesn't exist
                return new ForbidResult();
            }

            // ok, we're good to just go ahead and insert it into the database
            await using (var transaction = await database.Database.BeginTransactionAsync().ConfigureAwait(false))
            {
                // we don't care about the created entries
                _ = database.ModLocalizations.Add(localization);
                _ = database.Mods.Add(modObject);
                // we don't care how many things were saved
                _ = await database.SaveChangesAsync().ConfigureAwait(false);

                await transaction.CommitAsync().ConfigureAwait(false);
            }

            logger.Information("Upload {ID} complete: {Name} by {Author}", cookie.Substring(0, 16), localization.Name, modObject.Uploader.Username);
            plugins.UploadFinished(modObject);

            return UploadResult.Finish();
        }
    }
}
