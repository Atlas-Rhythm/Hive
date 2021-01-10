using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="InstanceDataController"/>
    /// </summary>
    [Aggregable]
    public interface IInstanceDataPlugin
    {
        /// <summary>
        /// Returns true if the sepcified user has access to view instance data. False otherwise.
        /// A false return will cause the endpoint to return a Forbid before executing the rest of the endpoint.
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        [return: StopIfReturns(false)]
        bool GetAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a further modified <see cref="InstanceData"/> object.
        /// <para>Hive default is to return the input instance.</para>
        /// </summary>
        /// <param name="user">User to determine further additions</param>
        /// <param name="data">Instance data to modify before being returned</param>
        [return: StopIfReturnsNull]
        InstanceData? GetAdditionalData(User? user, [TakesReturnValue] InstanceData? data) => data;
    }

    internal class HiveInstanceDataPlugin : IInstanceDataPlugin { }

    /// <summary>
    /// A REST controller for obtaining instance data.
    /// </summary>
    [Route("api/instance")]
    [ApiController]
    public class InstanceDataController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly IProxyAuthenticationService proxyAuth;

        private readonly HiveContext context;
        private readonly IAggregate<IInstanceDataPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;
        [ThreadStatic] private static PermissionActionParseState instanceParseState;

        private const string ActionName = "hive.game.version";

        /// <summary>
        /// Create an InstanceDataController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="proxyAuth"></param>
        public InstanceDataController([DisallowNull] Serilog.ILogger logger, IProxyAuthenticationService proxyAuth, HiveContext ctx, IAggregate<IInstanceDataPlugin> plugin, PermissionsManager<PermissionContext> perms)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<InstanceDataController>();
            this.proxyAuth = proxyAuth;
            context = ctx;
            this.plugin = plugin;
            permissions = perms;
        }

        /// <summary>
        /// Gets data about the instance.
        /// This performs a permission check at: <c>hive.instance</c>.
        /// </summary>
        /// <returns>A wrapped <see cref="InstanceData"/> instance, if successful.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<InstanceData>> GetInstanceData()
        {
            log.Debug("Getting instance data...");
            // Get the user, do not need to capture context.
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref instanceParseState))
                return Forbid();
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Performing additional checks for GetChannels...");
            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            if (!combined.GetAdditionalChecks(user))
                return Forbid();

            var result = new InstanceData { };
            // Interesting data includes:
            // Number and list of plugins
            // Various configuration members
            // Authentication server
            // CDN server
            // Other data
            return combined.GetAdditionalData(user, result);
        }
    }
}
