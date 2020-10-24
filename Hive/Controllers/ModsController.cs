﻿using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
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
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Hive.Versioning;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ModsController"/>
    /// </summary>
    [Aggregable]
    public interface IModsPlugin
    {
        /// <summary>
        /// Returns true if the specified user has access to view a particular mod, false otherwise.
        /// This method is called for each mod the user wants to access.
        /// <para>Hive default is to return true for each mod.</para>
        /// </summary>
        /// <remarks>
        /// This method is called in a LINQ expression that is not tracked by EntityFramework,
        /// so modifications done to the <see cref="Mod"/> object will not be reflected in the database.
        /// </remarks>
        /// <param name="user">User in context</param>
        /// <param name="contextMod">Mod in context</param>
        [return: StopIfReturns(false)]
        bool GetSpecificModAdditionalChecks(User? user, Mod contextMod) => true;

        /// <summary>
        /// Returns true if the specified user has access to move a particular mod from <paramref name="origin"/> to <paramref name="destination"/>. False otherwise.
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        /// <param name="contextMod">Mod that is attempting to be moved</param>
        /// <param name="origin">Channel that the Mod was located in before the move.</param>
        /// <param name="destination">New channel that the Mod will reside in.</param>
        /// <returns></returns>
        [return: StopIfReturns(false)]
        // REVIEW: Consider turning these Channel objects into channel IDs, or a wrapper type, for fool/user-proofing
        bool GetMoveModAdditionalChecks(User user, Mod contextMod, Channel origin, Channel destination) => true;

        /// <summary>
        /// Allows modification of a <see cref="Mod"/> object after a move operation has been performed.
        /// </summary>
        /// <param name="input">The mod in which the move operation was performed on.</param>
        void ModifyAfterModMove(in Mod input) { }
    }

    internal class HiveModsControllerPlugin : IModsPlugin { }

    [Route("api/mods")]
    [ApiController]
    public class ModsController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly HiveContext context;
        private readonly IProxyAuthenticationService proxyAuth;
        private readonly IAggregate<IModsPlugin> plugin;

        [ThreadStatic] private static PermissionActionParseState getModsParseState;
        [ThreadStatic] private static PermissionActionParseState moveModsParseState;

        public ModsController([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IModsPlugin> plugin, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ModsController>();
            permissions = perms;
            context = ctx;
            this.proxyAuth = proxyAuth;
            this.plugin = plugin;
        }

        private const string GetModsActionName = "hive.mod";
        private const string MoveModActionName = "hive.mod.move";

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<SerializedMod>>> GetAllMods() 
        {
            log.Debug("Getting all mods...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // iff a given user (or none) is allowed to access any mods. This should almost always be true.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user }, ref getModsParseState))
                return Forbid();

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Create some objects to filter by
            // TODO: default to the instance default Channel
            IEnumerable<Channel>? filteredChannels = null;
            GameVersion? filteredVersion = null;
            string filteredType = "LATEST";

            if (Request != null && Request.Query != null)
            {
                if (Request.Query.TryGetValue("channelId", out var filteredChannelValues))
                {
                    var filteredChannelID = filteredChannelValues.First();
                    // While, yes, this CAN return multiple objects if they have the same channel name... but why would they have the same channel name
                    filteredChannels = context.Channels.Where(c => c.Name == filteredChannelID);
                }
                if (Request.Query.TryGetValue("channelIds", out var filteredChannelsValues))
                {
                    filteredChannels = context.Channels.Where(c => filteredChannelsValues.Contains(c.Name));
                }
                if (Request.Query.TryGetValue("gameVersion", out var filteredVersionValues))
                {
                    filteredVersion = context.GameVersions.Where(g => g.Name == filteredVersionValues.First()).FirstOrDefault();
                }
                if (Request.Query.TryGetValue("filterType", out var filterTypeValues))
                {
                    filteredType = filterTypeValues.First().ToUpperInvariant(); // To remove case-sensitivity
                }
            }

            // Construct our list of serialized mods here.
            log.Debug("Filtering and serializing mods by existing plugins...");

            // Grab our initial set of mods, filtered by a channel and game version if provided.
            var mods = context.Mods
                .AsNoTracking()
                .Where(m => (filteredChannels == null || filteredChannels.Contains(m.Channel)) &&
                    (filteredVersion == null || m.SupportedVersions.Contains(filteredVersion)));

            // Filter these mods based on the query param we've retrieved (or default behavior)
            switch (filteredType)
            {
                case "ALL":
                    break; // We already have all of the mods that should be returned by "ALL", no need to do work.
                case "RECENT":
                    mods = mods // With "RECENT", we group each mod by their ID, and grab the most recently uploaded version.
                       .GroupBy(m => m.ReadableID)
                       .Select((g) => g.OrderByDescending(m => m.UploadedAt).First());
                    break;
                default: // This is "LATEST", but should probably be default behavior, just to be safe.
                    mods = mods // With "LATEST", we group each mod by their ID, and grab the most up-to-date version.
                       .GroupBy(m => m.ReadableID)
                       .Select((g) => g.OrderByDescending(m => m.Version).First());
                    break;
            }

            // Finally, perform a final permissions and plugins filter on each mod, then serialize these to return to the user.
            var serialized = mods
                .Where(m =>
                    permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = m }, ref getModsParseState)
                    && combined.GetSpecificModAdditionalChecks(user, m))
                .Select(m => SerializedMod.Serialize(m, GetLocalizedModInfoFromMod(m)));

            return Ok(serialized);
        }

        [HttpGet("api/mod/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SerializedMod>> GetSpecificMod([FromRoute] string id)
        {
            log.Debug("Getting a specific mod...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Get the latest version of the mod we are looking for
            // TODO: Add version range query param, or more routes
            var mod = context.Mods
                .AsNoTracking()
                .GroupBy(m => m.ReadableID)
                .Where(g => g.Key == id)
                .Select(g => g.OrderBy(m => m.Version).First())
                .FirstOrDefault();

            if (mod == null)
            {
                return NotFound();
            }

            // Forbid if a permissions check or plugins check prevents the user from accessing this mod.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = mod }, ref getModsParseState)
                || !combined.GetSpecificModAdditionalChecks(user, mod))
                return Forbid();

            var localizedModInfo = GetLocalizedModInfoFromMod(mod);

            var serializedMod = SerializedMod.Serialize(mod, localizedModInfo);
            return Ok(serializedMod);
        }

        [HttpPost("api/mod/move/{channelId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // TODO: I am once again asking for proper testing.
        public async Task<ActionResult<SerializedMod>> MoveModToChannel([FromRoute] string channelId)
        {
            log.Debug("Attempting to move a mod to a new channel...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // This probably isn't something that the average Joe can do, so we return unauthorized if there is no user.
            if (user is null)
            {
                return Unauthorized();
            }

            log.Debug("Serializing Mod from JSON...");
            // Parse our body as JSON.
            SerializedMod? postedMod = null;
            try
            {
                postedMod = await JsonSerializer.DeserializeAsync<SerializedMod>(Request.Body).ConfigureAwait(false);
            }
            catch(Exception e) when (e is JsonException) // Catch errors that can be attributed to malformed JSON from the user
            {
                return BadRequest(e);
            }
            catch // This was not an error due to the user. Ruh roh.
            {
                throw;
            }

            if (postedMod == null) // So... we somehow successfully deserialized the mod, only to find that it is null. What?
            {
                return BadRequest("POSTed Mod information was successfully deserialized, but the resulting object was null.");
            }

            log.Debug("Getting database objects...");

            // Get the database mod that represents the SerializedMod.
            var databaseMod = context.Mods.Where(x => x.ReadableID == postedMod.ID).FirstOrDefault();

            if (databaseMod == null) // The POSTed mod was successfully deserialzed, but no Mod exists in the database.
            {
                return NotFound("POSTed Mod does not exist.");
            }

            // Grab our origin and destination channels.
            var origin = databaseMod.Channel;
            var destination = context.Channels.Where(x => x.Name == channelId).FirstOrDefault();

            if (destination is null) // The channelId from our Route does not point to an existing Channel. Okay, we just return 404.
            {
                return NotFound($"No channel exists with the name \"{channelId}\".");
            }

            // Forbid iff a given user (or none) is allowed to move the mod.
            if (!permissions.CanDo(MoveModActionName, new PermissionContext { User = user, Mod = databaseMod, SourceChannel = origin, DestinationChannel = destination }, ref moveModsParseState))
                return Forbid();

            // Combine plugins and check if the user can still move the mod.
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Forbid iff a given user (or none) is allowed to move the mod.
            if (!combined.GetMoveModAdditionalChecks(user, databaseMod, origin, destination))
                return Forbid();

            // All of our needed information is non-null, and we have permission to perform the move.
            databaseMod.Channel = destination;

            combined.ModifyAfterModMove(in databaseMod); // If any plugins want to modify the object further after the move operation, they can do so here.

            await context.SaveChangesAsync().ConfigureAwait(false);

            
            return Ok(SerializedMod.Serialize(databaseMod, GetLocalizedModInfoFromMod(databaseMod)));
        }

        private LocalizedModInfo GetLocalizedModInfoFromMod(Mod mod)
        {
            // Get requested languages
            var searchingCultureInfos = GetAcceptLanguageCultures();

            // If the plugins allow us to access this mod, we then perform a search on localized data to grab what we need.
            LocalizedModInfo? localizedModInfo = null;

            // Just cache all localizations for the mod we're looking for.
            var localizations = mod.Localizations;

            // We loop through each preferred language first, as they are what the user asked for.
            // This list is already sorted by quality values, so none should be needed.
            // We do not need to explicitly search for the System culture since it was already added to the end of this list.
            foreach (CultureInfo preferredLanguage in searchingCultureInfos)
            {
                var localizedInfos = localizations.Where(x => x.Language == preferredLanguage);
                if (localizedInfos.Any())
                {
                    localizedModInfo = localizedInfos.First();
                    break;
                }
            }

            // If no preferred languages were found, we then grab the first found LocalizedModData is found.
            if (localizedModInfo is null)
            {
                if (localizations.Any()) localizedModInfo = localizations.First();
            }

            // If we still have no language, then... fuck.
            if (localizedModInfo is null)
            {
                throw new ArgumentException($"Mod {mod.ReadableID} does not have any LocalizedModInfos attached to it.");
            }

            return localizedModInfo;
        }

        // This code was generously provided by the following StackOverflow user, with some slight tweaks.
        // https://stackoverflow.com/questions/9414123/get-cultureinfo-from-current-visitor-and-setting-resources-based-on-that/51144362#51144362
        private IEnumerable<CultureInfo> GetAcceptLanguageCultures()
        {
            // We start with an empty list
            var preferredCultures = Enumerable.Empty<CultureInfo>();
            if (Request != null)
            {
                var requestedLanguages = Request.Headers["Accept-Language"];
                if (!StringValues.IsNullOrEmpty(requestedLanguages) && requestedLanguages.Count > 0)
                {
                    // TODO: Ignore cases where CultureInfo constructor throws
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
                        .Select(sv => new CultureInfo(sv.Value.ToString()));
                }
            }

            return preferredCultures.Append(CultureInfo.CurrentCulture); // Add system culture to the end and return the result.
        }
    }
}