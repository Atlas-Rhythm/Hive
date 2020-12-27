using System;
using Serilog;
using Hive.Models;
using GraphQL.Types;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Graphing.Types
{
    public class HiveQuery : ObjectGraphType
    {
        public HiveQuery([DisallowNull] ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            var l = logger.ForContext<HiveQuery>();

            l.Debug("Initializing");

            _ = Field<ListGraphType<ChannelType>>(
                name: "channels",
                resolve: context => Array.Empty<Channel>());
        }
    }
}
