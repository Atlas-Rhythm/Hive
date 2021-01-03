using Hive.Models;
using GraphQL.Types;

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
        public ChannelType()
        {
            Name = nameof(Channel);
            Description = Resources.GraphQL.Channel;

            _ = Field(c => c.Name)
                .Description(Resources.GraphQL.Channel_Name);
        }
    }
}
