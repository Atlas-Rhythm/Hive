using Hive.Models;
using Hive.Plugins;
using Hive.Controllers;
using Hive.Permissions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using NodaTime;

namespace Hive.Services.Common
{
    /// <summary>
    /// Common functionality for game version related actions.
    /// </summary>
    public class GameVersionService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IGameVersionsPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly IClock clock;
        [ThreadStatic] private static PermissionActionParseState versionsParseState;

        private const string ListActionName = "hive.game.version.list";
        private const string FilterActionName = "hive.game.version.filter";
        private const string CreateActionName = "hive.game.version.create";

        private static readonly HiveObjectQuery<IEnumerable<GameVersion>> forbiddenEnumerableResponse = new(null, "Forbidden", StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<GameVersion> forbiddenSingularResponse = new(null, "Forbidden", StatusCodes.Status403Forbidden);

        /// <summary>
        /// Create a GameVersionService with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="perms"></param>
        /// <param name="ctx"></param>
        /// <param name="plugin"></param>
        /// <param name="clock"></param>
        public GameVersionService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IGameVersionsPlugin> plugin, IClock clock)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<GameVersionService>();
            context = ctx;
            permissions = perms;
            this.plugin = plugin;
            this.clock = clock;
        }

        /// <summary>
        /// Gets all available <see cref="GameVersion"/> objects.
        /// This performs a permission check at: <c>hive.game.version.list</c>.
        /// Furthermore, game versions are further filtered by a permission check at: <c>hive.game.version.filter</c>.
        /// </summary>
        /// <param name="user">The user to associate with this request.</param>
        /// <returns>A wrapped enumerable of <see cref="GameVersion"/> objects, if successful.</returns>
        public HiveObjectQuery<IEnumerable<GameVersion>> RetrieveAllVersions(User? user)
        {
            if (!permissions.CanDo(ListActionName, new PermissionContext { User = user }, ref versionsParseState))
                return forbiddenEnumerableResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Perform additional checks for GetGameVersions...");

            // If the plugins say the user cannot access the list of game versions, then we forbid.
            if (!combined.ListGameVersionsAdditionalChecks(user))
                return forbiddenEnumerableResponse;

            // Grab our list of game versions
            var versions = context.GameVersions.ToList();
            log.Debug("Filtering versions from all {0} versions...", versions.Count);

            // First, we perform a permission check on each game version, in case we need to filter any specific ones
            // (Use additionalData to flag beta game versions, perhaps? Could be a plugin.)
            var filteredVersions = versions.Where(v => permissions.CanDo(FilterActionName, new PermissionContext { GameVersion = v, User = user }, ref versionsParseState));

            log.Debug("Remaining versions after permissions check: {0}", filteredVersions.Count());
            // Then we filter this even further by passing it through all of our Hive plugins.
            filteredVersions = combined.GetGameVersionsFilter(user, filteredVersions);
            // This final filtered list of versions is what we'll return back to the user.
            log.Debug("Remaining versions after plugin filters: {0}", filteredVersions.Count());

            return new HiveObjectQuery<IEnumerable<GameVersion>>(filteredVersions, null, StatusCodes.Status200OK);
        }

        /// <summary>
        /// Creates a new <see cref="GameVersion"/>, and adds it to the database.
        /// This performs a permission check at: <c>hive.game.version.create</c>
        /// </summary>
        /// <param name="user">The user to associate with this request.</param>
        /// <param name="versionName">The name of the new version</param>
        /// <returns>A wrapped <see cref="GameVersion"/> object, if successful.</returns>
        public HiveObjectQuery<GameVersion> CreateNewGameVersion(User? user, string versionName)
        {
            // If permission system says the user cannot create a new game version, forbid.
            if (!permissions.CanDo(CreateActionName, new PermissionContext { User = user }, ref versionsParseState))
                return forbiddenSingularResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Perform additional checks for CreateNewGameVersion...");

            // If the plugins say the user cannot create a new game version, then we forbid.
            if (!combined.CreateGameVersionsAdditionalChecks(user))
                return forbiddenSingularResponse;

            log.Debug("Creating a new Game Version...");

            // REVIEW: Do I need to fill any other fields here?
            var version = new GameVersion()
            {
                Name = versionName,
                CreationTime = clock.GetCurrentInstant()
            };

            _ = context.GameVersions.Add(version);

            return new HiveObjectQuery<GameVersion>(version, null, StatusCodes.Status200OK);
        }
    }
}
