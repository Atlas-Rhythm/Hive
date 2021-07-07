using System;
using Serilog;
using Hive.Models;
using GraphQL.Types;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// A QGL Query.
    /// </summary>
    public class HiveQuery : ObjectGraphType
    {
        /// <summary>
        /// Create a GQL query.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="customGraphs"></param>
        public HiveQuery([DisallowNull] ILogger logger, IEnumerable<ICustomHiveGraph<HiveQuery>> customGraphs)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            var l = logger.ForContext<HiveQuery>();

            l.Debug("Initializing");

            _ = Field<ListGraphType<ChannelType>>(
                name: "channels",
                resolve: context => Array.Empty<Channel>());

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
