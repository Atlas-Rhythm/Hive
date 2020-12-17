using System;
using Serilog;
using Hive.Models;
using GraphQL.Types;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Graphing.Types
{
    public class HiveQuery : ObjectGraphType
    {
        private readonly ILogger logger;

        public HiveQuery([DisallowNull] ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            this.logger = logger.ForContext<HiveQuery>();

            logger.Debug("Initializing");

            Field<ListGraphType<ChannelType>>(
                name: "channels",
                resolve: context => Array.Empty<Channel>());
        }
    }
}