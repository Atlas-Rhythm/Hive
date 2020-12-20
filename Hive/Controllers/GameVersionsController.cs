using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="GameVersionsController"/>
    /// </summary>
    [Aggregable]
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
        bool GetGameVersionsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="GameVersion"/>.
        /// <para>Hive default is to return input game versions.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="versions">Input versions to filter</param>
        [return: StopIfReturnsEmpty]
        IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions) => versions;
    }

    internal class HiveGameVersionsControllerPlugin : IGameVersionsPlugin { }

    [Route("api/game/versions")]
    [ApiController]
    public class GameVersionsController : ControllerBase
    {
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IGameVersionsPlugin> plugin;
        private readonly IProxyAuthenticationService proxyAuth;
        [ThreadStatic] private static PermissionActionParseState versionsParseState;

        public GameVersionsController([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IGameVersionsPlugin> plugin, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<GameVersionsController>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
            this.proxyAuth = proxyAuth;
        }

        private const string ActionName = "hive.game.version";

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<GameVersion>>> GetGameVersions()
        {
            log.Debug("Getting game versions...");
            // Get the user, do not need to capture context.
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            // iff a given user (or none) is allowed to access any game versions. This should almost always be true.
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref versionsParseState))
                return Forbid();

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Perform additional checks for GetGameVersions...");
            // If the plugins say the user cannot access the list of game versions, then we forbid.
            if (!combined.GetGameVersionsAdditionalChecks(user))
                return Forbid();

            // Grab our list of game versions
            var versions = context.GameVersions.ToList();
            log.Debug("Filtering versions from all {0} versions...", versions.Count);
            // First, we perform a permission check on each game version, in case we need to filter any specific ones
            // (Use additionalData to flag beta game versions, perhaps? Could be a plugin.)
            var filteredVersions = versions.Where(v => permissions.CanDo(ActionName, new PermissionContext { GameVersion = v, User = user }, ref versionsParseState));
            log.Debug("Remaining versions after permissions check: {0}", filteredVersions.Count());
            // Then we filter this even further by passing it through all of our Hive plugins.
            filteredVersions = combined.GetGameVersionsFilter(user, filteredVersions);
            // This final filtered list of versions is what we'll return back to the user.
            log.Debug("Remaining versions after plugin filters: {0}", filteredVersions.Count());
            return Ok(filteredVersions.ToList());
        }
    }
}
