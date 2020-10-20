using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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

        [ThreadStatic] private static PermissionActionParseState modsParseState;

        public ModsController([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IModsPlugin> plugin, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ModsController>();
            permissions = perms;
            context = ctx;
            this.proxyAuth = proxyAuth;
            this.plugin = plugin;
        }

        private const string ActionName = "hive.mod";

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // TODO: I am once again asking for proper testing
        public async Task<ActionResult<IEnumerable<SerializedMod>>> GetAllMods() 
        {
            log.Debug("Getting all mods...");
            // Get the user, do not need to capture context
            User? user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // iff a given user (or none) is allowed to access any mods. This should almost always be true.
            // REVIEW: Is this first check necessary?
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref modsParseState))
                return Forbid();

            // Combine plugins
            log.Debug("Combining plugins...");
            IModsPlugin combined = plugin.Instance;

            // Construct a list of preferred languages
            var searchingCultureInfos = GetAcceptLanguageCultures();

            // Construct our list of serialized mods here.
            log.Debug("Filtering and serializing mods by existing plugins...");
            var mods = new List<SerializedMod>();

            // We loop through all mods in the DB.
            // REVIEW: Not sure if there's much I can do, but I'm not really liking this code. Any way to improve it?
            foreach (Mod mod in context.Mods)
            {
                // Perform a permissions check on this particular mod. If it fails, we just skip.
                if (!permissions.CanDo(ActionName, new PermissionContext { User = user, Mod = mod }, ref modsParseState))
                {
                    continue;
                }

                // We perform a plugin check on each mod.
                if (combined.GetSpecificModAdditionalChecks(user, mod))
                {
                    // If the plugins allow us to access this mod, we then perform a search on localized data to grab what we need.
                    LocalizedModInfo? localizedModInfo = null;

                    // Just cache all localizations for the mod we're looking for.
                    var localizations = context.ModLocalizations.Where(x => x.OwningMod == mod);

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
                    if (localizedModInfo == null)
                    {
                        if (localizations.Any()) localizedModInfo = localizations.First();
                    }

                    // If we still have no language, then... fuck.
                    if (localizedModInfo == null)
                    {
                        log.Error("Mod {ReadableID} does not have any LocalizedModInfos attached to it.", mod.ReadableID);
                    }

                    mods.Add(SerializeMod(mod, localizedModInfo!));
                }
            }

            // After all of this is done, we should have all of the mods that we want to return back to the user.
            log.Debug("Total amount of serialized mods: {0}", mods.Count);

            return Ok(mods);
        }

        [HttpGet("api/mod/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        // TODO: I am once again asking for proper testing
        public async Task<ActionResult<SerializedMod>> GetSpecificMod([FromRoute] string id)
        {
            log.Debug("Getting a specific mod...");
            // Get the user, do not need to capture context
            User? user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // Combine plugins
            log.Debug("Combining plugins...");
            IModsPlugin combined = plugin.Instance;

            // Get the ID of the mod we are looking for
            Mod? mod = context.Mods.Where(x => x.ReadableID == id).FirstOrDefault();

            if (mod == null)
            {
                return NotFound();
            }
            else
            {
                // Forbid if a given user (or none) is not allowed to access this mod.
                if (!permissions.CanDo(ActionName, new PermissionContext { User = user, Mod = mod }, ref modsParseState))
                    return Forbid();

                // Forbid if a plugin denies permission to access this mod.
                if (!combined.GetSpecificModAdditionalChecks(user, mod))
                    return Forbid();

                var searchingCultureInfos = GetAcceptLanguageCultures();

                // If the plugins allow us to access this mod, we then perform a search on localized data to grab what we need.
                LocalizedModInfo? localizedModInfo = null;

                // Just cache all localizations for the mod we're looking for.
                var localizations = context.ModLocalizations.Where(x => x.OwningMod == mod);

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
                if (localizedModInfo == null)
                {
                    if (localizations.Any()) localizedModInfo = localizations.First();
                }

                // If we still have no language, then... fuck.
                if (localizedModInfo == null)
                {
                    log.Error("Mod {ReadableID} does not have any LocalizedModInfos attached to it.", mod.ReadableID);
                }

                SerializedMod serialized = SerializeMod(mod, localizedModInfo!);
                return Ok(serialized);
            }
        }

        private static SerializedMod SerializeMod(Mod toSerialize, LocalizedModInfo localizedModInfo)
        {
            SerializedMod serialized = new SerializedMod()
            {
                Name = toSerialize.ReadableID,
                Version = toSerialize.Version,
                UpdatedAt = toSerialize.UploadedAt.ToString(),
                EditedAt = toSerialize.EditedAt?.ToString()!,
                UploaderUsername = toSerialize.Uploader.Name!,
                ChannelName = toSerialize.Channel.Name,
                DownloadLink = toSerialize.DownloadLink.AbsoluteUri,
                // REVIEW: Perhaps replace the unserialized LocalizedModInfo parameter with a serialized version, and have callers serialize it themselves?
                LocalizedModInfo = SerializeLocalizedModInfo(localizedModInfo),
                // REVIEW: Is cloning necessary?
                AdditionalData = toSerialize.AdditionalData.Clone()
            };
            serialized.Authors.AddRange(toSerialize.Authors.Select(x => x.Name!));
            serialized.Contributors.AddRange(toSerialize.Contributors.Select(x => x.Name!));
            serialized.SupportedGameVersions.AddRange(toSerialize.SupportedVersions.Select(x => x.Name!));
            // REVIEW: Do I need AbsoluteUri?
            serialized.Links.AddRange(toSerialize.Links.Select(x => (x.Name, x.Url.AbsoluteUri))!);
            serialized.Dependencies.AddRange(toSerialize.Dependencies);
            serialized.ConflictsWith.AddRange(toSerialize.Conflicts);
            return serialized;
        }

        private static SerializedLocalizedModInfo SerializeLocalizedModInfo(LocalizedModInfo toSerialize)
        {
            if (toSerialize is null) return null!;
            return new SerializedLocalizedModInfo()
            {
                Language = toSerialize.Language,
                Name = toSerialize.Name,
                Changelog = toSerialize.Changelog!,
                Credits = toSerialize.Credits!,
                Description = toSerialize.Description,
            };
        }

        // This code was generously provided by the following StackOverflow user, with some slight tweaks.
        // https://stackoverflow.com/questions/9414123/get-cultureinfo-from-current-visitor-and-setting-resources-based-on-that/51144362#51144362
        private IList<CultureInfo> GetAcceptLanguageCultures()
        {
            var requestedLanguages = Request.Headers["Accept-Language"];
            if (StringValues.IsNullOrEmpty(requestedLanguages) || requestedLanguages.Count == 0)
            {
                return Array.Empty<CultureInfo>().ToList();
            }

            // TODO: Ignore cases where CultureInfo constructor throws
            var preferredCultures = requestedLanguages.ToString().Split(',')
                // Parse the header values
                .Select(s => new StringSegment(s))
                .Select(StringWithQualityHeaderValue.Parse)
                // Ignore the "any language" rule
                .Where(sv => sv.Value != "*")
                // Remove duplicate rules with a lower value
                .GroupBy(sv => sv.Value).Select(svg => svg.OrderByDescending(sv => sv.Quality.GetValueOrDefault(1)).First())
                // Sort by preference level
                .OrderByDescending(sv => sv.Quality.GetValueOrDefault(1))
                .Select(sv => new CultureInfo(sv.Value.ToString()))
                .ToList();

            preferredCultures.Add(CultureInfo.CurrentCulture); // Add system culture to the end.

            return preferredCultures;
        }
    }
}
