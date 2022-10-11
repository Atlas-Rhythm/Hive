using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Hive.Controllers;
using Hive.Models;
using Hive.Models.ReadOnly;
using Hive.Permissions;
using Hive.Plugins.Aggregates;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Version = Hive.Versioning.Version;
using Hive.Extensions;
using Hive.Versioning.Parsing;

namespace Hive.Services.Common
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ModsController"/>
    /// </summary>
    [Aggregable(Default = typeof(HiveModsControllerPlugin))]
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
        bool GetMoveModAdditionalChecks(User? user, Mod contextMod, ReadOnlyChannel origin, ReadOnlyChannel destination) => true;

        /// <summary>
        /// Allows modification of a <see cref="Mod"/> object after a move operation has been performed.
        /// </summary>
        /// <param name="input">The mod in which the move operation was performed on.</param>
        void ModifyAfterModMove(in Mod input) { }
    }

    internal class HiveModsControllerPlugin : IModsPlugin { }

    /// <summary>
    /// Common functionality for mod related actions.
    /// </summary>
    public class ModService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IModsPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;

        // Actions done on a singular mod
        private const string FilterModActionName = "hive.mod.filter";

        private const string MoveModActionName = "hive.mod.move";

        [ThreadStatic] private static PermissionActionParseState getModsParseState;
        [ThreadStatic] private static PermissionActionParseState moveModsParseState;

        private static readonly HiveObjectQuery<Mod> forbiddenModResponse = new(StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<Mod> notFoundModResponse = new(StatusCodes.Status404NotFound, "Not Found");

        /// <summary>
        /// Create a ModService with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="perms"></param>
        /// <param name="ctx"></param>
        /// <param name="plugin"></param>
        public ModService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IModsPlugin> plugin)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ModService>();
            this.plugin = plugin;
            permissions = perms;
            context = ctx;
        }

        /// <summary>
        /// Performs a search for all mods within the provided channel IDs (if provided, otherwise defaults to the instance default channel(s)), an optional <see cref="GameVersion"/>, and a filter type.
        /// <para><paramref name="channelIds"/> Will default to empty/the instance default if not provided. Otherwise, only obtains mods from the specified channel IDs.</para>
        /// <para><paramref name="gameVersion"/> Will default to search all game versions if not provided. Otherwise, filters on only this game version.</para>
        /// <para><paramref name="filterType"/> Will default to <c>latest</c> if not provided or not one of: <c>all</c>, <c>latest</c>, or <c>recent</c>.</para>
        /// This performs a permission check at: <c>hive.mods.list</c>.
        /// Furthermore, mods are further filtered by a permission check at: <c>hive.mods.filter</c>.
        /// </summary>
        /// <param name="user">The user associated with this request.</param>
        /// <param name="channelIds">The channel IDs to filter the mods.</param>
        /// <param name="gameVersion">The game version to search within.</param>
        /// <param name="filterType">How to filter the results.</param>
        /// <returns>A wrapped collection of <see cref="Mod"/> objects, if successful.</returns>
        public async Task<HiveObjectQuery<IEnumerable<Mod>>> GetAllMods(User? user, string[]? channelIds = null, string? gameVersion = null, string? filterType = null)
        {
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Grab our filtered mod list with our various filtering parameters
            var filteredMods = await GetFilteredModList(channelIds, gameVersion, filterType, null).ConfigureAwait(false);

            // Further filter these mods by both a permissions check and a plugin check
            filteredMods = filteredMods.Where(m =>
                permissions.CanDo(FilterModActionName, new PermissionContext { User = user, Mod = m }, ref getModsParseState)
                        && combined.GetSpecificModAdditionalChecks(user, m));

            return new HiveObjectQuery<IEnumerable<Mod>>(StatusCodes.Status200OK, filteredMods);
        }

        /// <summary>
        /// Gets a <see cref="Mod"/> that matches the given ID, with some optional filtering.
        /// <para><paramref name="range"/> Will default to all mod versions if not provided. Otherwise, only obtain mods that match the specified version range.</para>
        /// <para><paramref name="channelIds"/> Will default to empty/the instance default if not provided. Otherwise, only obtains mods from the specified channel IDs.</para>
        /// <para><paramref name="gameVersion"/> Will default to search all game versions if not provided. Otherwise, filters on only this game version.</para>
        /// <para><paramref name="filterType"/> Will default to <c>latest</c> if not provided or not one of: <c>all</c>, <c>latest</c>, or <c>recent</c>.</para>
        /// </summary>
        /// <param name="user">The user associated with this request.</param>
        /// <param name="id">The <seealso cref="Mod.ReadableID"/> to find.</param>
        /// <param name="range">The <see cref="VersionRange"/> to match. If null, will return the latest version of a matching mod.</param>
        /// <param name="channelIds">The channel IDs to filter the mods.</param>
        /// <param name="gameVersion">The game version to search within.</param>
        /// <param name="filterType">How to filter the results.</param>
        /// <returns>A wrapped <see cref="Mod"/> of the found mod, if successful.</returns>
        public async Task<HiveObjectQuery<Mod>> GetMod(User? user, string id, VersionRange? range = null, string? channelIds = null, string? gameVersion = null, string? filterType = null)
        {
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // To keep things simple on the helper function, we'll make a one-item array to pass in (or null if we aren't filtering at all)
            var filteredChannels = channelIds == null
                ? null
                : new[] { channelIds };

            // Grab our filtered mod list with our various filtering parameters
            var filteredMods = await GetFilteredModList(filteredChannels, gameVersion, filterType, range).ConfigureAwait(false);

            // Grab the first mod version in our list (doesn't matter if it's the most recently uploaded, or most up-to-date)
            var mod = filteredMods
                .Where(m => m.ReadableID == id)
                .FirstOrDefault();

            if (mod == null)
            {
                return notFoundModResponse;
            }

            // Forbid if a permissions check or plugins check prevents the user from accessing this mod.
            if (!permissions.CanDo(FilterModActionName, new PermissionContext { User = user, Mod = mod }, ref getModsParseState)
                || !combined.GetSpecificModAdditionalChecks(user, mod))
                return forbiddenModResponse;

            return new HiveObjectQuery<Mod>(StatusCodes.Status200OK, mod);
        }

        /// <summary>
        /// Gets a <see cref="Mod"/> that matches the provided <paramref name="identifier"/>.
        /// </summary>
        /// <param name="user">The user associated with this request.</param>
        /// <param name="identifier">Identifier which represents the mod.</param>
        /// <returns>A warapped <see cref="Mod"/> of the found mod, if successful.</returns>
        public async Task<HiveObjectQuery<Mod>> GetMod(User? user, ModIdentifier identifier)
        {
            if (identifier is null)
                return new(StatusCodes.Status400BadRequest, "Mod Identifier is invalid");

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            var modVersion = AttemptParseVersionWithError(identifier.Version, out var error);
            if (modVersion == null)
            {
                return new(StatusCodes.Status400BadRequest, error);
            }

            var modId = identifier.ID;

            var mod = await CreateModQuery()
                .Where(m => m.ReadableID == modId && m.Version == modVersion)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (mod == null)
            {
                return notFoundModResponse;
            }

            // Forbid if a permissions check or plugins check prevents the user from accessing this mod.
            if (!permissions.CanDo(FilterModActionName, new PermissionContext { User = user, Mod = mod }, ref getModsParseState)
                || !combined.GetSpecificModAdditionalChecks(user, mod))
                return forbiddenModResponse;

            return new HiveObjectQuery<Mod>(StatusCodes.Status200OK, mod);
        }

        /// <summary>
        /// Moves the specified <see cref="ModIdentifier"/> to the specified channel.
        /// This performs a permission check at: <c>hive.mod.move</c>.
        /// </summary>
        /// <param name="user">The user associated with this request.</param>
        /// <param name="channelDestination">The destination channel ID to move the mod to.</param>
        /// <param name="identifier">The <see cref="ModIdentifier"/> to move.</param>
        /// <returns>A wrapped <see cref="Mod"/> of the moved mod, if successful.</returns>
        public async Task<HiveObjectQuery<Mod>> MoveMod(User? user, string channelDestination, ModIdentifier identifier)
        {
            log.Debug("Getting database objects...");

            if (identifier is null)
                return new HiveObjectQuery<Mod>(StatusCodes.Status400BadRequest, "Mod Identifier is invalid");

            var targetVersion = new Version(identifier.Version);

            // Get the database mod that represents the ModIdentifier.
            var databaseMod = await CreateModQuery().Where(x => x.ReadableID == identifier.ID && x.Version == targetVersion)
                .FirstOrDefaultAsync().ConfigureAwait(false);

            // The POSTed mod was successfully deserialzed, but no Mod exists in the database.
            if (databaseMod == null)
                return new HiveObjectQuery<Mod>(StatusCodes.Status404NotFound, "Mod does not exist");

            // Grab our origin and destination channels.
            var origin = databaseMod.Channel;
            // Specifically need identity here to ensure shared instance of channel across the move.
            // Also, we must be fully tracked (and not non-tracked) since instances are swapped.
            // This may actually be an EF bug, since AsNoTrackingWithIdentityResolution SHOULD work here, but does not.
            var destination = await context.Channels.AsTracking().FirstOrDefaultAsync(x => x.Name == channelDestination).ConfigureAwait(false);

            // The channelId from our Route does not point to an existing Channel. Okay, we just return 404.
            if (destination is null)
                return new HiveObjectQuery<Mod>(StatusCodes.Status404NotFound, $"No channel exists with the name \"{channelDestination}\".");

            // Forbid iff a given user (or none) is allowed to move the mod.
            if (!permissions.CanDo(MoveModActionName, new PermissionContext { User = user, Mod = databaseMod, SourceChannel = origin, DestinationChannel = destination }, ref moveModsParseState))
                return forbiddenModResponse;

            // Combine plugins and check if the user can still move the mod.
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Forbid iff a given user (or none) is allowed to move the mod.
            if (!combined.GetMoveModAdditionalChecks(user, databaseMod, new ReadOnlyChannel(origin), new ReadOnlyChannel(destination)))
                return forbiddenModResponse;

            // All of our needed information is non-null, and we have permission to perform the move.
            databaseMod.Channel = destination;

            // If any plugins want to modify the object further after the move operation, they can do so here.
            combined.ModifyAfterModMove(in databaseMod);

            _ = await context.SaveChangesAsync().ConfigureAwait(false);

            return new HiveObjectQuery<Mod>(StatusCodes.Status200OK, databaseMod);
        }

        // Helper function that abstracts common filtering functionality from GET /mods and GET /mod/{id}
        // TODO: default to the instance default Channel if channelIds is null
        private async Task<IEnumerable<Mod>> GetFilteredModList(string[]? channelIds, string? gameVersion, string? filterType, VersionRange? filteredVersionRange)
        {
            log.Debug("Filtering and serializing mods by existing plugins...");

            // Grab our initial set of mods
            var mods = CreateModQuery();

            // Perform various filtering on our mods
            if (channelIds != null && channelIds.Length >= 0)
            {
                var filteredChannels = context.Channels.Where(c => channelIds.Contains(c.Name));

                mods = mods.Where(m => filteredChannels.Contains(m.Channel));
            }

            if (gameVersion != null)
            {
                var filteredGameVersion = await context.GameVersions.Where(g => g.Name == gameVersion).FirstOrDefaultAsync().ConfigureAwait(false);

                if (filteredGameVersion != null)
                {
                    mods = mods.Where(m => m.SupportedVersions.Contains(filteredGameVersion));
                }
            }

            if (filteredVersionRange != null)
            {
                mods = mods.Where(m => filteredVersionRange.Matches(m.Version));
            }

            // Because EF (or PostgreSQL or both) does not like advanced LINQ expressions (like GroupBy),
            // we convert to an enumerable and do additional filtering on the client.
            var filteredMods = await mods.ToListAsync().ConfigureAwait(false);

            var filteredType = filterType?.ToUpperInvariant() ?? "LATEST";

            // Filter these mods based on the query param we've retrieved (or default behavior)
            switch (filteredType)
            {
                // We already have all of the mods that should be returned by "ALL", no need to do work.
                case "ALL":
                    break;

                // With "RECENT", we group each mod by their ID, and grab the most recently uploaded version.
                case "RECENT":
                    // TODO: this can *absolutely* be done in a DB query
                    filteredMods = filteredMods
                        .GroupBy(m => m.ReadableID)
                        .Select(g => g.OrderByDescending(m => m.UploadedAt).First())
                        .ToList();
                    break;

                // This is "LATEST", but should probably be default behavior, just to be safe.
                // We group each mod by their ID, and grab the most up-to-date version.
                default:
                    filteredMods = filteredMods
                        .GroupBy(m => m.ReadableID)
                        .Select(g => g.OrderByDescending(m => m.Version).First())
                        .ToList();
                    break;
            }

            return filteredMods;
        }

        private static Version? AttemptParseVersionWithError(string version, out string error)
        {
            var versionSpan = version.AsSpan();
            var errorState = new ParserErrorState<VersionParseAction>(in versionSpan);

            if (!Version.TryParse(ref errorState, versionSpan, out var parsedVersion))
            {
                error = ErrorMessages.GetVersionErrorMessage(ref errorState);
                return null;
            }

            error = string.Empty;
            return parsedVersion;
        }

        // Abstracts the construction of a Mod access query with necessary Include calls to a helper function
        // Due to an EF issue, non-tracking here does not work. Instead we must use tracking EVEN IF we don't want to perform writes.
        // Notably, non-tracking even WITH identity does not work.
        private IQueryable<Mod> CreateModQuery()
            => context.Mods.AsTracking()
            // TODO: only Include the aspects which we need at the time
            .Include(m => m.Localizations)
            .Include(m => m.Channel)
            .Include(m => m.SupportedVersions)
            .Include(m => m.Uploader)
            .Include(m => m.Authors)
            .Include(m => m.Contributors)
            .AsSingleQuery();
    }
}
