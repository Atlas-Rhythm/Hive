using Hive.Models;
using GraphQL.Types;
using System.Collections.Generic;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="Channel"/>.
    /// </summary>
    public class ChannelType : ObjectGraphType<Channel>
    {
        /// <summary>
        /// Setup a ChannelType for GQL.
        /// </summary>
        public ChannelType(IEnumerable<ICustomHiveGraph<ChannelType>> customGraphs)
        {
            if (customGraphs is null)
                throw new System.ArgumentNullException(nameof(customGraphs));

            Name = nameof(Channel);
            Description = Resources.GraphQL.Channel;

            _ = Field(c => c.Name)
                .Description(Resources.GraphQL.Channel_Name);

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
