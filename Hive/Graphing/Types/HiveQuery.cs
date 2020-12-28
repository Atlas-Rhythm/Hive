using System;
using Serilog;
using Hive.Models;
using GraphQL.Types;
using System.Diagnostics.CodeAnalysis;

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
