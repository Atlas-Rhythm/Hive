using Hive.Models;
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

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ModsController"/>
    /// </summary>
    [Aggregable]
    public interface IModsPlugin
    {
        /// <summary>
        /// Returns true if the specified user has access to view a particular mod. False otherwise.
        /// When retrieving all mods, the list of mods is filtered using this method before serializing and returning to the user.
        /// <para>Hive default is to return true for each mod.</para>
        /// </summary>
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
        bool GetMoveModAdditionalChecks(User user, Mod contextMod, Channel origin, Channel destination) => true;

        /// <summary>
        /// Allows modification of a <see cref="Mod"/> object after a move operation has been performed.
        /// </summary>
        /// <param name="input">The mod in which the move operation was performed on.</param>
        void ModifyAfterModMove([TakesReturnValue] in Mod input) { }
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

            Channel? filteredChannel = null;
            if (Request != null && Request.Query != null && Request.Query.TryGetValue("channel", out var filteredChannelValues))
            {
                var filteredChannelID = filteredChannelValues.First(); // REVIEW: Would it make sense to allow filtering through multiple channels?
                filteredChannel = await context.Channels.Where(c => c.Name == filteredChannelID).FirstAsync().ConfigureAwait(false);
            }

            // Construct our list of serialized mods here.
            log.Debug("Filtering and serializing mods by existing plugins...");

            // Construct our list of serialized mods via some LINQ-y bois (thanks sc2ad)
            // We first perform a filtered channel check (if specified), then group each mod by IDs, then grab the latest versions of each.
            // We then perform a permissions and plugins check on each mod, then construct SerializedMods from them.
            var mods = context.Mods
                .Where(m => filteredChannel == null || m.Channel == filteredChannel)
                .GroupBy(m => m.ReadableID)
                .Select(g => g.OrderByDescending(m => m.Version).First())
                .Where(m => permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = m }, ref getModsParseState) && combined.GetSpecificModAdditionalChecks(user, m))
                .Select(m => SerializedMod.Serialize(m, GetLocalizedModInfoFromMod(m)!));

            return Ok(mods);
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

            // Get the ID of the mod we are looking for
            var mod = context.Mods.Where(x => x.ReadableID == id).FirstOrDefault();

            if (mod == null)
            {
                return NotFound();
            }

            // Forbid if a permissions check or plugins check prevents the user from accessing this mod.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = mod }, ref getModsParseState) || !combined.GetSpecificModAdditionalChecks(user, mod))
                return Forbid();

            var localizedModInfo = GetLocalizedModInfoFromMod(mod);

            var serializedMod = SerializedMod.Serialize(mod, localizedModInfo!); // REVIEW: Perhaps throw an exception instead of giving out no localizations/null?
            return Ok(serializedMod);
        }

        [HttpPost("api/mod/move/{channelId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // TODO: I am once again asking for proper testing.
        public async Task<ActionResult> MoveModToChannel([FromRoute] string channelId)
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
            var databaseMod = await context.Mods.Where(x => x.ReadableID == postedMod.ID).FirstOrDefaultAsync().ConfigureAwait(false);

            if (databaseMod == null) // The POSTed mod was successfully deserialzed, but no Mod exists in the database. Okay, we just return 404.
            {
                return NotFound("POSTed Mod does not exist.");
            }

            // Grab our origin and destination channels.
            var origin = databaseMod.Channel;
            var destination = await context.Channels.Where(x => x.Name == channelId).FirstOrDefaultAsync().ConfigureAwait(false);

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

            // REVIEW: Perhaps re-construct a SerializedMod from the mod we just moved, and return that back to the user?

            return Ok();
        }

        private LocalizedModInfo? GetLocalizedModInfoFromMod(Mod mod)
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
                log.Error("Mod {ReadableID} does not have any LocalizedModInfos attached to it.", mod.ReadableID);
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
