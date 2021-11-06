using Hive.Models;
using Hive.Plugins.Aggregates;
using Hive.Controllers;
using Hive.Permissions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using NodaTime;
using System.Threading.Tasks;
using Hive.Models.Serialized;

namespace Hive.Services.Common
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="GameVersionsController"/>
    /// </summary>
    [Aggregable(Default = typeof(HiveGameVersionsControllerPlugin))]
    public interface IGameVersionsPlugin
    {
        /// <summary>
        /// Returns true if the sepcified user has access to view the list of all game versions. False otherwise.
        /// A false return will cause the endpoint to return a Forbid before executing the rest of the endpoint.
        /// <para>It is recommended to use <see cref="GetGameVersionsFilter(User?, IEnumerable{GameVersion})"/> for filtering out game versions.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        [return: StopIfReturns(false)]
        bool ListGameVersionsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns true if the sepcified user has access to create a new game version. False otherwise.
        /// A false return will cause the endpoint to return a Forbid before executing the rest of the endpoint.
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        [return: StopIfReturns(false)]
        bool CreateGameVersionsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="GameVersion"/>.
        /// <para>Hive default is to return input game versions.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="versions">Input versions to filter</param>
        [return: StopIfReturnsEmpty]
        IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions) => versions;

        /// <summary>
        /// A hook that is called when a new <see cref="GameVersion"/> is successfully created and about to be added to the database.
        /// Perform additional data edits, or any edits that you would like to persist to the database, here.
        /// Hive default is to do nothing.
        /// </summary>
        /// <param name="gameVersion">The version that was just created.</param>
        void NewGameVersionCreated(GameVersion gameVersion) { }
    }

    internal class HiveGameVersionsControllerPlugin : IGameVersionsPlugin { }

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

        private const string ListActionName = "hive.game.versions.list";
        private const string FilterActionName = "hive.game.versions.filter";
        private const string CreateActionName = "hive.game.versions.create";

        private static readonly HiveObjectQuery<IEnumerable<GameVersion>> forbiddenEnumerableResponse = new(StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<GameVersion> forbiddenSingularResponse = new(StatusCodes.Status403Forbidden);

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
        public async Task<HiveObjectQuery<IEnumerable<GameVersion>>> RetrieveAllVersions(User? user)
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

            // First, we perform a permission check on each game version, in case we need to filter any specific ones
            // (Use additionalData to flag beta game versions, perhaps? Could be a plugin.)
            // Too complicated for EF, so go to IAsyncEnumerable.
            var filteredVersions = await (context.GameVersions as IAsyncEnumerable<GameVersion>)
                .Where(v => permissions.CanDo(FilterActionName, new PermissionContext { GameVersion = v, User = user }, ref versionsParseState))
                .ToListAsync().ConfigureAwait(false);

            // Then we filter this even further by passing it through all of our Hive plugins.
            filteredVersions = combined.GetGameVersionsFilter(user, filteredVersions).ToList();
            // This final filtered list of versions is what we'll return back to the user.
            log.Debug("Remaining versions after plugin filters: {0}", filteredVersions.Count);

            return new HiveObjectQuery<IEnumerable<GameVersion>>(StatusCodes.Status200OK, filteredVersions);
        }

        /// <summary>
        /// Creates a new <see cref="GameVersion"/>, and adds it to the database.
        /// This performs a permission check at: <c>hive.game.version.create</c>
        /// </summary>
        /// <param name="user">The user to associate with this request.</param>
        /// <param name="gameVersion">The new game version to create.</param>
        /// <returns>A wrapped <see cref="GameVersion"/> object, if successful.</returns>
        public async Task<HiveObjectQuery<GameVersion>> CreateNewGameVersion(User? user, InputGameVersion gameVersion)
        {
            if (gameVersion is null)
                throw new ArgumentNullException(nameof(gameVersion));

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

            // TODO: Pass the InputGameVersion into plugins before creating the GameVersion instance for further modification of additional data?
            var version = new GameVersion(gameVersion.AdditionalData)
            {
                Name = gameVersion.Name,
                CreationTime = clock.GetCurrentInstant()
            };

            // Exit if there's already an existing version with the same name
            if (await context.GameVersions.AnyAsync(x => x.Name == version.Name).ConfigureAwait(false))
                return new HiveObjectQuery<GameVersion>(StatusCodes.Status409Conflict, "A channel with this name already exists.");

            // Call our hooks
            combined.NewGameVersionCreated(version);

            _ = await context.GameVersions.AddAsync(version).ConfigureAwait(false);
            _ = await context.SaveChangesAsync().ConfigureAwait(false);

            return new HiveObjectQuery<GameVersion>(StatusCodes.Status200OK, version);
        }
    }
}
