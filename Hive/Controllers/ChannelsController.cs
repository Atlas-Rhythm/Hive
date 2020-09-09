using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Plugin;
using Hive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ChannelsController"/>
    /// </summary>
    public class ChannelsControllerPlugin : IPlugin
    {
        /// <summary>
        /// Returns true if the specified user has access to ANY of the channels. False otherwise.
        /// A false return will cause the endpoint in question to return a Forbid before executing the rest of the endpoint.
        /// <para>It is recommended to use <see cref="GetChannelsFilter(IEnumerable{Channel})"/> for filtering user specific channels.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        public virtual bool GetChannelsAdditionalChecks(User? user)
        {
            return true;
        }

        /// <summary>
        /// Returns a filtered enumerable of <see cref="Channel"/>
        /// <para>Hive default is to return input channels.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="channels">Input channels to filter</param>
        /// <returns>Filtered channels</returns>
        public virtual IEnumerable<Channel> GetChannelsFilter(User? user, IEnumerable<Channel> channels)
        {
            return channels;
        }
    }

    [Route("api/channels")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        private readonly PermissionsService permissions;
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<ChannelsControllerPlugin> plugin;
        private readonly IProxyAuthenticationService authService;

        public ChannelsController(Serilog.ILogger logger, PermissionsService perms, HiveContext ctx, IAggregate<ChannelsControllerPlugin> plugin, IProxyAuthenticationService authService)
        {
            log = logger.ForContext<ChannelsController>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
            this.authService = authService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // TODO: Perhaps return a subset of Channel, instead only containing information desired as opposed to the whole model?
        // This is probably applicable via a GraphQL endpoint, however.
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            log.Debug("Getting channels...");
            // The existence of this method is determined through a configuration file, which is handled in Startup.cs
            // This method simply needs to exist.
            // TODO: Get user
            User? user = await authService.GetUser(Request);
            // If user is null, we can simply forward it anyways
            // TODO: Wrap with user != null, either anonymize "hive.channel" or remove entirely.
            if (!permissions.CanDo("hive.channel", new Permissions.PermissionContext { User = user }))
                return Forbid();
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Combine();
            log.Debug("Performing additional checks for GetChannels...");
            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            if (!combined.GetChannelsAdditionalChecks(user))
                return Forbid();
            // Filter channels based off of user-level permission
            // Permission for a given channel is entirely plugin-based, channels in Hive are defaultly entirely public.
            // For a mix of private/public channels, a plugin that maintains a user-level list of read/write channels is probably ideal.
            var channels = context.Channels.ToList();
            log.Debug("Filtering channels from {0} channels...", channels.Count);
            var filteredChannels = combined.GetChannelsFilter(user, channels);
            log.Debug("Remaining channels: {0}", filteredChannels.Count());
            return Ok(filteredChannels.ToList());
        }
    }
}