using System;
using GraphQL;
using System.Linq;
using Hive.Models;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Hive.Permissions;
using Hive.Controllers;
using Hive.Plugins;
using Hive.Services;
using Microsoft.AspNetCore.Http;

namespace Hive.GraphQL
{
    public class HiveQuery : ObjectGraphType
    {
        private readonly Serilog.ILogger log;
        private readonly int itemsPerPage = 10;

        private const string HiveChannelActionName = "hive.channel";

        public HiveQuery([DisallowNull] Serilog.ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<HiveQuery>();

            // Channel Stuff
            FieldAsync<ListGraphType<ChannelType>>(
                "channels",
                arguments: new QueryArguments(
                    HiveArguments.Page(Resources.GraphQL.Channels_QueryPage)
                ),
                resolve: async context =>
                {
                    // Resolve services
                    HttpContext httpContext = context.HttpContext();
                    HiveContext hiveContext = context.Resolve<HiveContext>();
                    IProxyAuthenticationService authService = context.Resolve<IProxyAuthenticationService>();
                    PermissionsManager<PermissionContext> perms = context.Resolve<PermissionsManager<PermissionContext>>();
                    IAggregate<IChannelsControllerPlugin> plugin = context.Resolve<IAggregate<IChannelsControllerPlugin>>();

                    // Validate and Filter
                    User? user = await authService.GetUser(httpContext.Request).ConfigureAwait(false);
                    PermissionActionParseState channelsParseState;
                    if (!perms.CanDo(HiveChannelActionName, new PermissionContext { User = user }, ref channelsParseState))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }
                    var combined = plugin.Instance;
                    if (!combined.GetChannelsAdditionalChecks(user))
                    {
                        context.Errors.Add(new ExecutionError("Unauthorized"));
                        return null;
                    }
                    var channels = hiveContext.Channels.ToList();
                    var filteredChannels = channels.Where(c => perms.CanDo(HiveChannelActionName, new PermissionContext { Channel = c, User = user }, ref channelsParseState));
                    filteredChannels = combined.GetChannelsFilter(user, filteredChannels);

                    // Should the channels endpoint need paging?
                    int page = context.GetArgument<int>("page");

                    return filteredChannels.Skip(Math.Abs(page)).Take(itemsPerPage);
                }
            );
            FieldAsync<ChannelType>(
                "channel",
                arguments: new QueryArguments(
                    HiveArguments.ID(Resources.GraphQL.Channel_NameQuery)
                ),
                resolve: async context =>
                {
                    HiveContext hiveContext = context.Resolve<HiveContext>();
                    string id = context.GetArgument<string>("id");

                    return await hiveContext.Channels.FirstOrDefaultAsync(c => c.Name == id).ConfigureAwait(false);
                }
            );

            // Mod Stuff
            Field<ListGraphType<ModType>>(
                "mods",
                arguments: new QueryArguments(
                    HiveArguments.Page(Resources.GraphQL.Mods_QueryPage)),
                resolve: context =>
                {
                    // Resolve services
                    HiveContext hiveContext = context.Resolve<HiveContext>();

                    int page = context.GetArgument<int>("page");

                    return hiveContext.Mods.Skip(Math.Abs(page)).Take(itemsPerPage);
                }

            );
        }
    }
}