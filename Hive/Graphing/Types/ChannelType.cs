using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    public class ChannelType : ObjectGraphType<Channel>
    {
        public ChannelType()
        {
            Name = nameof(Channel);
            Description = Resources.GraphQL.Channel;

            Field(c => c.Name)
                .Description(Resources.GraphQL.Channel_Name);
        }
    }
}