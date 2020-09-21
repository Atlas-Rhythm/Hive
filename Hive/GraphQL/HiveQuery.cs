using System;
using GraphQL;
using System.Linq;
using Hive.Models;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace Hive.GraphQL
{
    public class HiveQuery : ObjectGraphType
    {
        private readonly int itemsPerPage = 10;

        public HiveQuery()
        {
            Field<ListGraphType<ChannelType>>(
                "channels",
                arguments: new QueryArguments(
                    HiveArguments.Page(Resources.GraphQL.channels_query_page)
                ),
                resolve: context =>
                {
                    HiveContext hiveContext = context.Hive();
                    int page = context.GetArgument<int>("page");

                    return hiveContext.Channels.Skip(Math.Abs(page)).Take(itemsPerPage);
                }
            );
            Field<ChannelType>(
                "channel",
                arguments: new QueryArguments(
                    HiveArguments.ID(Resources.GraphQL.channel_name_query)
                ),
                resolve: context =>
                {
                    HiveContext hiveContext = context.Hive();
                    string id = context.GetArgument<string>("id");

                    return hiveContext.Channels.FirstOrDefaultAsync(c => c.Name == id);
                }
            );
        }
    }
}