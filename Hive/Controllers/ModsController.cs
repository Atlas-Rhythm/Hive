using Hive.Extensions;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Services;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hive.Services.Common;

namespace Hive.Controllers
{
    /// <summary>
    /// A REST controller for performing mod related actions.
    /// </summary>
    [Route("api/")]
    [ApiController]
    public class ModsController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly ModService modService;
        private readonly IProxyAuthenticationService proxyAuth;

        /// <summary>
        /// Create a ModsController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="modService"></param>
        /// <param name="proxyAuth"></param>
        public ModsController([DisallowNull] Serilog.ILogger logger, ModService modService, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ModsController>();
            this.modService = modService;
            this.proxyAuth = proxyAuth;
        }

        /// <summary>
        /// Performs a search for all mods within the provided channel IDs (if provided, otherwise defaults to the instance default channel(s)), an optional <see cref="GameVersion"/>, and a filter type.
        /// <para><paramref name="channelIds"/> Will default to empty/the instance default if not provided. Otherwise, only obtains mods from the specified channel IDs.</para>
        /// <para><paramref name="gameVersion"/> Will default to search all game versions if not provided. Otherwise, filters on only this game version.</para>
        /// <para><paramref name="filterType"/> Will default to <c>latest</c> if not provided or not one of: <c>all</c>, <c>latest</c>, or <c>recent</c>.</para>
        /// This performs a permission check at: <c>hive.mod</c>.
        /// </summary>
        /// <param name="channelIds">The channel IDs to filter the mods.</param>
        /// <param name="gameVersion">The game version to search within.</param>
        /// <param name="filterType">How to filter the results.</param>
        /// <returns>A wrapped collection of <see cref="SerializedMod"/>, if successful.</returns>
        [HttpGet("mods")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SerializedMod>>> GetAllMods([FromQuery] string[]? channelIds = null, [FromQuery] string? gameVersion = null, [FromQuery] string? filterType = null)
        {
            log.Debug("Getting all mods...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            var queryResult = modService.GetAllMods(user, channelIds, gameVersion, filterType);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Gets a <see cref="SerializedMod"/> of the specific <see cref="VersionRange"/> of this particular mod's <seealso cref="Mod.ReadableID"/>.
        /// This performs a permission check at: <c>hive.mod</c>.
        /// </summary>
        /// <param name="id">The <seealso cref="Mod.ReadableID"/> to find.</param>
        /// <param name="range">The <see cref="VersionRange"/> to match.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> of the found mod, if successful.</returns>
        [HttpGet("mod/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> GetSpecificMod([FromRoute] string id, [FromQuery] string? range = null)
        {
            log.Debug("Getting a specific mod...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            var filteredRange = range != null ? new VersionRange(range) : null;
            var queryResult = modService.GetMod(user, id, filteredRange);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Gets a <see cref="SerializedMod"/> of the latest version of this particular mod's <seealso cref="Mod.ReadableID"/>.
        /// This performs a permission check at: <c>hive.mod</c>.
        /// </summary>
        /// <param name="id">The <seealso cref="Mod.ReadableID"/> to find the latest version of.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> that is the latest version available, if successful.</returns>
        [HttpGet("mod/{id}/latest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // GET /mod/{id} includes a few extra query params to further filter the returned mod, this is just a simplified version
        public async Task<ActionResult<SerializedMod>> GetSpecificModLatestVersion([FromRoute] string id)
        {
            log.Debug("Getting the latest version of a specific mod...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            var queryResult = modService.GetMod(user, id);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Moves the specified <see cref="ModIdentifier"/> from whatever channel it was in to the specified channel.
        /// This performs a permission check at: <c>hive.mod.move</c>.
        /// </summary>
        /// <param name="channelId">The destination channel ID to move the mod to.</param>
        /// <param name="identifier">The <see cref="ModIdentifier"/> to move.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> of the moved mod, if successful.</returns>
        [HttpPost("mod/move/{channelId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> MoveModToChannel([FromRoute] string channelId, [FromBody] ModIdentifier identifier)
        {
            log.Debug("Attempting to move a mod to a new channel...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // This probably isn't something that the average Joe can do, so we return unauthorized if there is no user.
            if (user is null)
            {
                return Unauthorized();
            }

            var queryResult = await modService.MoveMod(user, channelId, identifier).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        // This code was generously provided by the following StackOverflow user, with some slight tweaks.
        // https://stackoverflow.com/questions/9414123/get-cultureinfo-from-current-visitor-and-setting-resources-based-on-that/51144362#51144362
        private IEnumerable<string> GetAcceptLanguageCultures()
        {
            // We start with an empty list
            var preferredCultures = Enumerable.Empty<string>();
            if (Request != null)
            {
                var requestedLanguages = Request.Headers["Accept-Language"];
                if (!StringValues.IsNullOrEmpty(requestedLanguages) && requestedLanguages.Count > 0)
                {
                    preferredCultures = requestedLanguages.ToString().Split(',')
                        // Parse the header values
                        .Select(s => new StringSegment(s))
                        .Select(StringWithQualityHeaderValue.Parse)
                        // Ignore the "any language" rule
                        .Where(sv => sv.Value != "*")
                        // Remove duplicate rules with a lower value
                        .GroupBy(sv => sv.Value).Select(svg => svg.OrderByDescending(sv => sv.Quality.GetValueOrDefault(1)).First())
                        // Sort by preference level
                        .OrderByDescending(sv => sv.Quality.GetValueOrDefault(1))
                        // Then re-select the text values as strings.
                        .Select(sv => sv.Value.Value);
                }
            }

            return preferredCultures.Append(CultureInfo.CurrentCulture.ToString()); // Add system culture to the end and return the result.
        }
    }
}
