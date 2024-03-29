﻿using Hive.Extensions;
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
        /// This performs a permission check at: <c>hive.mods.list</c>.
        /// Furthermore, mods are further filtered by a permission check at: <c>hive.mods.filter</c>.
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
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            var queryResult = await modService.GetAllMods(user, channelIds, gameVersion, filterType).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Gets a <see cref="SerializedMod"/> that matches the given ID, with some optional filtering.
        /// <para><paramref name="range"/> Will default to all mod versions if not provided. Otherwise, only obtain mods that match the specified version range.</para>
        /// <para><paramref name="channelId"/> Will default to empty/the instance default if not provided. Otherwise, only obtains mods from the specified channel IDs.</para>
        /// <para><paramref name="gameVersion"/> Will default to search all game versions if not provided. Otherwise, filters on only this game version.</para>
        /// <para><paramref name="filterType"/> Will default to <c>latest</c> if not provided or not one of: <c>all</c>, <c>latest</c>, or <c>recent</c>.</para>
        /// This performs a permission check at <c>hive.mod.get</c>, and at <c>hive.mod.filter</c> once the <see cref="Mod"/> object was retrieved.
        /// </summary>
        /// <param name="id">The <seealso cref="Mod.ReadableID"/> to find.</param>
        /// <param name="range">The <see cref="VersionRange"/> to match.</param>
        /// <param name="channelId">The channel IDs to filter the mods.</param>
        /// <param name="gameVersion">The game version to search within.</param>
        /// <param name="filterType">How to filter the results.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> of the found mod, if successful.</returns>
        [HttpGet("mod/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> GetSpecificMod([FromRoute] string id, [FromQuery] string? range = null, [FromQuery] string? channelId = null, [FromQuery] string? gameVersion = null, [FromQuery] string? filterType = null)
        {
            log.Debug("Getting a specific mod...");
            // Get the user, do not need to capture context
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            var filteredRange = range != null ? new VersionRange(range) : null;

            var queryResult = await modService.GetMod(user, id, filteredRange, channelId, gameVersion, filterType).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Gets a specific version of the <see cref="Mod"/> that matches the given ID.
        /// This performs a permission check at <c>hive.mod.filter</c> once the <see cref="Mod"/> object was retrieved.
        /// </summary>
        /// <param name="id">The identifier of the mod.</param>
        /// <param name="version">The specific version of the mod.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> that is the latest version available, if successful.</returns>
        [HttpGet("mod/{id}/{version}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> GetSpecificModSpecificVersion([FromRoute] string id, [FromRoute] string version)
        {
            log.Debug("Getting a specific version of a specific mod...");
            // Get the user, do not need to capture context
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            // Version parsing is done in ModService.
            var identifier = new ModIdentifier
            {
                ID = id,
                Version = version
            };

            var queryResult = await modService.GetMod(user, identifier).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Gets the latest version of the <see cref="Mod"/> that matches the given ID.
        /// This performs a permission check at <c>hive.mod.get</c>, and at <c>hive.mod.filter</c> once the <see cref="Mod"/> object was retrieved.
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
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            var queryResult = await modService.GetMod(user, id).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Moves the specified <see cref="ModIdentifier"/> to the specified channel.
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
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            // This probably isn't something that the average Joe can do, so we return unauthorized if there is no user.
            // While this can be argued redudant by the GuestRestrictionMiddleware, it's probably worth keeping as a sanity check.
            if (user is null) return new UnauthorizedResult();

            var queryResult = await modService.MoveMod(user, channelId, identifier).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        /// <summary>
        /// Edits a specific mod with new information.
        /// This performs a permission check at: <c>hive.mod.edit</c>.
        /// </summary>
        /// <param name="serializedModUpdate">The <see cref="SerializedModUpdate"/> to update and the updated information.</param>
        /// <returns>A wrapped <see cref="SerializedMod"/> of the edited mod, if successful.</returns>
        [HttpPut("mod")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> UpdateSpecificMod([FromBody] SerializedModUpdate serializedModUpdate)
        {
            log.Debug("Attempting to update a mod");

            // Get the user, do not need to capture context
            var user = await HttpContext.GetHiveUser(proxyAuth).ConfigureAwait(false);

            if (user is null) return new UnauthorizedResult();

            var queryResult = await modService.UpdateMod(user, serializedModUpdate).ConfigureAwait(false);

            return queryResult.Serialize(GetAcceptLanguageCultures());
        }

        // This code was generously provided by the following StackOverflow user, with some slight tweaks.
        // https://stackoverflow.com/questions/9414123/get-cultureinfo-from-current-visitor-and-setting-resources-based-on-that/51144362#51144362
        private IEnumerable<string> GetAcceptLanguageCultures()
        {
            // We start with an empty list
            var preferredCultures = Enumerable.Empty<string>();
            if (Request != null && Request.Headers != null && Request.Headers.TryGetValue("Accept-Languages", out var requestedLanguages))
            {
                if (!StringValues.IsNullOrEmpty(requestedLanguages) && requestedLanguages.Count > 0)
                {
                    preferredCultures = requestedLanguages.ToString().Split(',')
                        .Select(s => new StringSegment(s)) // Parse the header values
                        .Select(StringWithQualityHeaderValue.Parse)
                        .Where(sv => sv.Value != "*") // Ignore the "any language" rule
                        .GroupBy(sv => sv.Value).Select(svg => svg.OrderByDescending(sv => sv.Quality.GetValueOrDefault(1)).First()) // Remove duplicate rules with a lower value
                        .OrderByDescending(sv => sv.Quality.GetValueOrDefault(1)) // Sort by preference level
                        .Select(sv => sv.Value.Value); // Then re-select the text values as strings.
                }
            }

            return preferredCultures.Append(CultureInfo.CurrentCulture.ToString()); // Add system culture to the end and return the result.
        }
    }
}
