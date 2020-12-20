using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Name = nameof(User);
            Description = Resources.GraphQL.User;

            _ = Field(u => u.Username)
                .Description(Resources.GraphQL.User_Username);
        }
    }
}
