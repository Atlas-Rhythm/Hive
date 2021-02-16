using Hive.Controllers;
using Hive.Permissions;
using Hive.Plugins.Aggregates;
using Hive.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Hive.Services.Common
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
        /// <para>It is recommended to use <see cref="GetChannelsFilter(User?, IEnumerable{Channel})"/> for filtering user specific channels.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        bool GetChannelsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns true if the specified user has access to creating new channels. False otherwise.
        /// A false return will cause the endpoint in question to return a Forbid before executing the rest of the endpoint.
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        bool CreateChannelAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="Channel"/>
        /// <para>Hive default is to return input channels.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="channels">Input channels to filter</param>
        /// <returns>Filtered channels</returns>
        IEnumerable<Channel> GetChannelsFilter(User? user, [TakesReturnValue] IEnumerable<Channel> channels) => channels;
    }

    internal class HiveChannelsControllerPlugin : IChannelsControllerPlugin { }

    /// <summary>
    /// Common functionality for channel related actions.
    /// </summary>
    public class ChannelService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IChannelsControllerPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;
        private PermissionActionParseState channelsParseState;

        private static readonly HiveObjectQuery<IEnumerable<Channel>> forbiddenEnumerableResponse = new(null, "Forbidden", StatusCodes.Status403Forbidden);
        private static readonly HiveObjectQuery<Channel> forbiddenSingularResponse = new(null, "Forbidden", StatusCodes.Status403Forbidden);

        private const string ListActionName = "hive.channels.list";
        private const string FilterActionName = "hive.channels.filter";

        private const string CreateActionName = "hive.channel.create";

        /// <summary>
        /// Create a ChannelService with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="perms"></param>
        /// <param name="ctx"></param>
        /// <param name="plugin"></param>
        public ChannelService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IChannelsControllerPlugin> plugin)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelService>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
        }

        /// <summary>
        /// Gets all <see cref="Channel"/> objects available.
        /// This performs a permission check at: <c>hive.channels.list</c>.
        /// Furthermore, channels are further filtered by a permission check at: <c>hive.channels.filter</c>.
        /// </summary>
        /// <param name="user">The users to associate with this request.</param>
        /// <returns>A wrapped collection of <see cref="Channel"/>, if successful.</returns>
        public HiveObjectQuery<IEnumerable<Channel>> RetrieveAllChannels(User? user)
        {
            // hive.channel with a null channel in the context should be permissible
            // iff a given user (or none) is allowed to view any channels. Thus, this should almost always be true
            if (!permissions.CanDo(ListActionName, new PermissionContext { User = user }, ref channelsParseState))
                return forbiddenEnumerableResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            log.Debug("Performing additional checks for GetChannels...");
            if (!combined.GetChannelsAdditionalChecks(user))
                return forbiddenEnumerableResponse;

            // Filter channels based off of user-level permission
            // Permission for a given channel is entirely plugin-based, channels in Hive are defaultly entirely public.
            // For a mix of private/public channels, a plugin that maintains a user-level list of read/write channels is probably ideal.
            var channels = context.Channels.ToList();
            log.Debug("Filtering channels from {0} channels...", channels.Count);

            // First, we filter over if the given channel is accessible to the given user.
            // This allows for much more specific permissions, although chances are that roles will be used (and thus a plugin) instead.
            var filteredChannels = channels.Where(c => permissions.CanDo(FilterActionName, new PermissionContext { Channel = c, User = user }, ref channelsParseState));

            log.Debug("Remaining channels before plugin: {0}", filteredChannels.Count());
            filteredChannels = combined.GetChannelsFilter(user, filteredChannels);
            log.Debug("Remaining channels: {0}", filteredChannels.Count());

            return new HiveObjectQuery<IEnumerable<Channel>>(filteredChannels, null, StatusCodes.Status200OK);
        }

        /// <summary>
        /// Creates a new <see cref="Channel"/> object with the specified name.
        /// This performs a permission check at: <c>hive.channel.create</c>.
        /// </summary>
        /// <param name="user">The user to associate with the request.</param>
        /// <param name="channelName">The name of the new channel</param>
        /// <returns>The wrapped <see cref="Channel"/> that was created, if successful.</returns>
        public HiveObjectQuery<Channel> CreateNewChannel(User? user, string channelName)
        {
            // hive.channel with a null channel in the context should be permissible
            // iff a given user (or none) is allowed to view any channels. Thus, this should almost always be true
            if (!permissions.CanDo(CreateActionName, new PermissionContext { User = user }, ref channelsParseState))
                return forbiddenSingularResponse;

            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;

            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            log.Debug("Performing additional checks for CreateNewChannel...");
            if (!combined.CreateChannelAdditionalChecks(user))
                return forbiddenSingularResponse;

            log.Debug("Creating a new channel...");

            var newChannel = new Channel
            {
                Name = channelName
            };

            _ = context.Channels.Add(newChannel);

            return new HiveObjectQuery<Channel>(newChannel, null, StatusCodes.Status200OK);
        }
    }
}
