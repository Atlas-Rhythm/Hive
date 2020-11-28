using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
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
    [Aggregable]
    public interface IChannelsControllerPlugin
    {
        /// <summary>
        /// Returns true if the specified user has access to ANY of the channels. False otherwise.
        /// A false return will cause the endpoint in question to return a Forbid before executing the rest of the endpoint.
        /// <para>It is recommended to use <see cref="GetChannelsFilter(IEnumerable{Channel})"/> for filtering user specific channels.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        public bool GetChannelsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="Channel"/>
        /// <para>Hive default is to return input channels.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="channels">Input channels to filter</param>
        /// <returns>Filtered channels</returns>
        public IEnumerable<Channel> GetChannelsFilter(User? user, [TakesReturnValue] IEnumerable<Channel> channels) => channels;
    }

    internal class HiveChannelsControllerPlugin : IChannelsControllerPlugin { }

    [Route("api/channels")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IChannelsControllerPlugin> plugin;
        private readonly IProxyAuthenticationService authService;

        public ChannelsController([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IChannelsControllerPlugin> plugin, IProxyAuthenticationService authService)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelsController>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
            this.authService = authService;
        }

        private const string ActionName = "hive.channel";

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SuppressMessage("Hive.Permissions", "Hive0012:Use the CanDo(StringView, TContext, ref PermissionActionParseState) overload when possible", Justification = "Caching causes problems with two varying contexts")]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            log.Debug("Getting channels...");
            // Get the user, do not need to capture context.
            User? user = await authService.GetUser(Request).ConfigureAwait(false);
            // If user is null, we can simply forward it anyways
            // TODO: Wrap with user != null, either anonymize "hive.channel" or remove entirely.
            // hive.channel with a null channel in the context should be permissible
            // iff a given user (or none) is allowed to view any channels. Thus, this should almost always be true
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }))
                return Forbid();
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Performing additional checks for GetChannels...");
            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            if (!combined.GetChannelsAdditionalChecks(user))
                return Forbid();
            // Filter channels based off of user-level permission
            // Permission for a given channel is entirely plugin-based, channels in Hive are defaultly entirely public.
            // For a mix of private/public channels, a plugin that maintains a user-level list of read/write channels is probably ideal.
            var channels = await context.Channels.AsNoTracking().ToListAsync().ConfigureAwait(false);
            log.Debug("Filtering channels from {0} channels...", channels.Count);
            // First, we filter over if the given channel is accessible to the given user.
            // This allows for much more specific permissions, although chances are that roles will be used (and thus a plugin) instead.
            var filteredChannels = channels.Where(c => permissions.CanDo(ActionName, new PermissionContext { Channel = c, User = user }));
            log.Debug("Remaining channels before plugin: {0}", filteredChannels.Count());
            filteredChannels = combined.GetChannelsFilter(user, filteredChannels);
            log.Debug("Remaining channels: {0}", filteredChannels.Count());
            return Ok(filteredChannels.ToList());
        }
    }
}