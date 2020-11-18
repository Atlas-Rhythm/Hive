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
            Field(u => u.Name)
                .Description(Resources.GraphQL.User_Name);
            Field(u => u.DumbId)
                .Name("id")
                .Description(Resources.GraphQL.User_ID);
        }
    }
}