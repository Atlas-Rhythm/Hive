using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Plugin;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ChannelsController"/>
    /// </summary>
    public class ChannelsControllerPlugin : IPlugin
    {
        /// <summary>
        /// Throws an <see cref="ApiException"/> or does nothing. Useful for plugins which wish to exit early.
        /// <para>Hive default is to do nothing.</para>
        /// </summary>
        /// <exception cref="ApiException">Throw this (with a custom response code and message) when you wish to cause a failure</exception>
        /// <param name="user">User in context</param>
        public void GetChannelsAdditionalChecks(User? user)
        {
        }

        /// <summary>
        /// Returns a filtered enumerable of <see cref="Channel"/>
        /// <para>Hive default is to return input channels.</para>
        /// </summary>
        /// <param name="channels">Input channels to filter</param>
        /// <returns>Filtered channels</returns>
        public IEnumerable<Channel> GetChannelsFilter(IEnumerable<Channel> channels)
        {
            return channels;
        }

        /// <summary>
        /// Returns a selection function for selecting channels to return.
        /// objects will be serialized to JSON.
        /// <para>Hive default is to return a function that is only channel names.</para>
        /// </summary>
        /// <returns>Selection function</returns>
        public Func<Channel, object> GetChannelsSelect()
        {
            return c => new { c.Name };
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

        public ChannelsController(Serilog.ILogger log, PermissionsService perms, HiveContext ctx, IAggregate<ChannelsControllerPlugin> plugin)
        {
            this.log = log.ForContext<WeatherForecastController>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
        }

        [HttpGet]
        public ActionResult<IEnumerable<object>> GetChannels()
        {
            log.Debug("Getting channels...");
            // The existence of this method is determined through a configuration file, which is handled in Startup.cs
            // This method simply needs to exist.
            // TODO: Get user
            User? user = null;
            // TODO: Wrap with user != null, either anonymize "hive.channel" or remove entirely.
            if (!permissions.CanDo("hive.channel", new Permissions.PermissionContext { User = user }))
                return Forbid();
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Combine();
            log.Debug("Performing additional checks for GetChannels...");
            // May throw an ApiException, which should be handled by our MiddleWare
            combined.GetChannelsAdditionalChecks(user);
            // Filter channels based off of user-level permission
            // Permission for a given channel is entirely plugin-based, channels in Hive are defaulty entirely public.
            // For a mix of private/public channels, a plugin that maintains a user-level list of read/write channels is probably ideal.
            var channels = context.Channels.ToList();
            log.Debug("Filtering channels from {0} channels...", channels.Count);
            var filteredChannels = combined.GetChannelsFilter(channels);
            log.Debug("Remaining channels: {0}", filteredChannels.Count());
            var selection = filteredChannels.Select(combined.GetChannelsSelect());
            log.Debug("Selected channels: {0}", selection.Count());
            return selection.ToList();
        }
    }
}