using Hive.Controllers;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Hive.Services.Common
{
    public class ChannelService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IChannelsControllerPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;
        private PermissionActionParseState channelsParseState;

        private static readonly Query<IEnumerable<Channel>> forbiddenResponse = new Query<IEnumerable<Channel>>(null, "Forbidden", StatusCodes.Status403Forbidden);
        private const string ActionName = "hive.channel";

        public ChannelService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IChannelsControllerPlugin> plugin)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelService>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
        }

        public Query<IEnumerable<Channel>> RetrieveAllChannels(User? user)
        {
            // TODO: Wrap with user != null, either anonymize "hive.channel" or remove entirely.
            // hive.channel with a null channel in the context should be permissible
            // iff a given user (or none) is allowed to view any channels. Thus, this should almost always be true
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref channelsParseState))
                return forbiddenResponse;
            // Combine plugins
            log.Debug("Combining plugins...");
            var combined = plugin.Instance;
            log.Debug("Performing additional checks for GetChannels...");
            // May return false, which causes a Forbid.
            // If it throws an exception, it will be handled by our MiddleWare
            if (!combined.GetChannelsAdditionalChecks(user))
                return forbiddenResponse;
            // Filter channels based off of user-level permission
            // Permission for a given channel is entirely plugin-based, channels in Hive are defaultly entirely public.
            // For a mix of private/public channels, a plugin that maintains a user-level list of read/write channels is probably ideal.
            var channels = context.Channels.ToList();
            log.Debug("Filtering channels from {0} channels...", channels.Count);
            // First, we filter over if the given channel is accessible to the given user.
            // This allows for much more specific permissions, although chances are that roles will be used (and thus a plugin) instead.
            var filteredChannels = channels.Where(c => permissions.CanDo(ActionName, new PermissionContext { Channel = c, User = user }, ref channelsParseState));

            return new Query<IEnumerable<Channel>>(filteredChannels, null, StatusCodes.Status404NotFound);
        }
    }
}
