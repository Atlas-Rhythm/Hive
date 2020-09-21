using System;
using GraphQL;
using System.Linq;
using Hive.Models;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Hive.GraphQL
{
    public class HiveQuery : ObjectGraphType
    {
        private readonly Serilog.ILogger log;
        private readonly int itemsPerPage = 10;

        public HiveQuery([DisallowNull] Serilog.ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<HiveQuery>();

            // Channel Stuff
            Field<ListGraphType<ChannelType>>(
                "channels",
                arguments: new QueryArguments(
                    HiveArguments.Page(Resources.GraphQL.Channels_QueryPage)
                ),
                resolve: context =>
                {
                    // Resolve services
                    HiveContext hiveContext = context.Resolve<HiveContext>();
                    
                    // Should the channels endpoint need paging?
                    int page = context.GetArgument<int>("page");

                    return hiveContext.Channels.Skip(Math.Abs(page)).Take(itemsPerPage);
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