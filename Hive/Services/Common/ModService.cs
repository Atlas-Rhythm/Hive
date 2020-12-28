using Hive.Models;
using Hive.Plugins;
using Hive.Versioning;
using Hive.Controllers;
using Hive.Permissions;
using Hive.Models.ReadOnly;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Version = Hive.Versioning.Version;

namespace Hive.Services.Common
{
    public class ModService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IModsPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;

        private const string GetModsActionName = "hive.mod";
        private const string MoveModActionName = "hive.mod.move";

        [ThreadStatic] private static PermissionActionParseState getModsParseState;
        [ThreadStatic] private static PermissionActionParseState moveModsParseState;

        private static readonly HiveObjectQuery<IEnumerable<Mod>> forbiddenEnumerableResponse = new HiveObjectQuery<IEnumerable<Mod>>(null, "Forbidden", StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<Mod> forbiddenModResponse = new HiveObjectQuery<Mod>(null, "Forbidden", StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<Mod> notFoundModResponse = new HiveObjectQuery<Mod>(null, "Not Found", StatusCodes.Status404NotFound);

        public ModService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IModsPlugin> plugin)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ModService>();
            this.plugin = plugin;
            permissions = perms;
            context = ctx;
        }

        public HiveObjectQuery<IEnumerable<Mod>> GetAllMods(User? user, string[]? channelIds = null, string? gameVersion = null, string? filterType = null)
        {
            // iff a given user (or none) is allowed to access any mods. This should almost always be true.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user }, ref getModsParseState))
                return forbiddenEnumerableResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Create some objects to filter by
            // TODO: default to the instance default Channel
            IEnumerable<Channel>? filteredChannels = null;
            GameVersion? filteredVersion = null;
            string filteredType = "LATEST";

            if (channelIds != null && channelIds.Length >= 0)
            {
                filteredChannels = context.Channels.AsNoTracking().Where(c => channelIds.Contains(c.Name));
            }
            if (gameVersion != null)
            {
                filteredVersion = context.GameVersions.AsNoTracking().Where(g => g.Name == gameVersion).FirstOrDefault();
            }
            if (filterType != null)
            {
                filteredType = filterType.ToUpperInvariant(); // To remove case-sensitivity
            }

            // Construct our list of serialized mods here.
            log.Debug("Filtering and serializing mods by existing plugins...");

            // Grab our initial set of mods, filtered by a channel and game version if provided.
            var mods = CreateModQuery()
                .AsNoTracking();

            if (filteredChannels != null)
            {
                mods = mods.Where(m => filteredChannels.Contains(m.Channel));
            }
            if (filteredVersion != null)
            {
                mods = mods.Where(m => m.SupportedVersions.Contains(filteredVersion));
            }

            // Because EF (or PostgreSQL or both) does not like advanced LINQ expressions (like GroupBy), 
            // we convert to an enumerable and do the calculations on the client.
            IEnumerable<Mod> filteredMods = mods.AsEnumerable();

            // Filter these mods based on the query param we've retrieved (or default behavior)
            switch (filteredType)
            {
                case "ALL":
                    break; // We already have all of the mods that should be returned by "ALL", no need to do work.
                case "RECENT":
                    filteredMods = filteredMods // With "RECENT", we group each mod by their ID, and grab the most recently uploaded version.
                        .GroupBy(m => m.ReadableID)
                        .Select(g => g.OrderByDescending(m => m.UploadedAt).First());
                    break;
                default: // This is "LATEST", but should probably be default behavior, just to be safe.
                    filteredMods = filteredMods // With "LATEST", we group each mod by their ID, and grab the most up-to-date version.
                        .GroupBy(m => m.ReadableID)
                        .Select(g => g.OrderByDescending(m => m.Version).First());
                    break;
            }

            filteredMods = filteredMods.Where(m =>
                permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = m }, ref getModsParseState)
                        && combined.GetSpecificModAdditionalChecks(user, m));

            return new HiveObjectQuery<IEnumerable<Mod>>(filteredMods, null, StatusCodes.Status200OK);
        }

        public HiveObjectQuery<Mod> GetMod(User? user, string id, VersionRange? range = null)
        {
            // iff a given user (or none) is allowed to access any mods. This should almost always be true.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user }, ref getModsParseState))
                return forbiddenModResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // Grab the list of mods that match our ID.
            var mods = CreateModQuery()
                .AsNoTracking()
                .Where(m => m.ReadableID == id);

            // If the user wants a specific version range, filter by that.
            if (range != null)
            {
                mods = mods.Where(m => range.Matches(m.Version));
            }

            // Grab the latest version in our list
            var mod = mods
                .OrderByDescending(m => m.Version)
                .FirstOrDefault();

            if (mod == null)
            {
                return notFoundModResponse;
            }

            // Forbid if a permissions check or plugins check prevents the user from accessing this mod.
            if (!permissions.CanDo(GetModsActionName, new PermissionContext { User = user, Mod = mod }, ref getModsParseState)
                || !combined.GetSpecificModAdditionalChecks(user, mod))
                return forbiddenModResponse;

            return new HiveObjectQuery<Mod>(mod, null, StatusCodes.Status200OK);
        }

        public async Task<HiveObjectQuery<Mod>> MoveMod(User user, string channelDestination, ModIdentifier identifier)
        {
            log.Debug("Getting database objects...");

            if (identifier is null)
                return new HiveObjectQuery<Mod>(null, "Mod Identifier is invalid", StatusCodes.Status400BadRequest);

            var targetVersion = new Version(identifier.Version);

            // Get the database mod that represents the ModIdentifier.
            var databaseMod = CreateModQuery().Where(x => x.ReadableID == identifier.ID
                    && x.Version == targetVersion)
                .FirstOrDefault();

            if (databaseMod == null) // The POSTed mod was successfully deserialzed, but no Mod exists in the database.
                return new HiveObjectQuery<Mod>(null, "Mod does not exist", StatusCodes.Status404NotFound);

            // Grab our origin and destination channels.
            var origin = databaseMod.Channel;
            var destination = context.Channels.Where(x => x.Name == channelDestination).FirstOrDefault();

            if (destination is null) // The channelId from our Route does not point to an existing Channel. Okay, we just return 404.
                return new HiveObjectQuery<Mod>(null, $"No channel exists with the name \"{channelDestination}\".", StatusCodes.Status404NotFound);

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

            await context.SaveChangesAsync().ConfigureAwait(false);

            return new HiveObjectQuery<Mod>(databaseMod, null, StatusCodes.Status200OK);
        }

        // Abstracts the construction of a Mod access query with necessary Include calls to a helper function
        private IQueryable<Mod> CreateModQuery() => context
            .Mods
            .Include(m => m.Localizations)
            .Include(m => m.Channel)
            .Include(m => m.SupportedVersions);
    }
}