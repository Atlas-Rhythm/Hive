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
        // TODO: Add methods for stuff that should be pluginable, or remove if there isn't any
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
        private PermissionActionParseState versionsParseState;

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
            User? user = await proxyAuth.GetUser(Request).ConfigureAwait(false);
            // iff a given user (or none) is allowed to access any game versions. This should almost always be true.
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref versionsParseState))
                return Forbid();

            // Grab our list of game versions
            var versions = context.GameVersions.ToList();
            log.Debug("Filtering versions from all {0} versions...", versions.Count);
            // First, we perform a permission check on each game version, in case we need to filter any specific ones (Beta game versions, perhaps?)
            var filteredVersions = versions.Where(v => permissions.CanDo(ActionName, new PermissionContext { GameVersion = v, User = user }, ref versionsParseState));
            log.Debug("Remaining versions after permissions check: {0}", filteredVersions.Count());
            // TODO: Add plugin method for further filtering specific game versions
            log.Debug("Remaining versions to return: {0}", filteredVersions.Count());
            return Ok(filteredVersions.ToList());
        }
    }
}
