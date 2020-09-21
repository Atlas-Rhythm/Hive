using Hive.Models;
using GraphQL.Types;

namespace Hive.GraphQL
{
    public class ChannelType : ObjectGraphType<Channel>
    {
        public ChannelType()
        {
            Field(c => c.Name).Description(Resources.GraphQL.channel_name);
        }
    }
}