using Hive.Models;
using GraphQL.Types;

namespace Hive.GraphQL
{
    public class UserType : ObjectGraphType<User>
    {
        public UserType()
        {
            Name = "User";
            Field(u => u.DumbId).Description(Resources.GraphQL.User_ID);
            Field(u => u.Username).Description(Resources.GraphQL.User_Username);

            // Probably authenticate?
            Field(u => u.AuthenticationType).Description(Resources.GraphQL.User_AuthenticationType);
            Field(u => u.IsAuthenticated).Description(Resources.GraphQL.User_IsAuthenticated);

            Field(u => u.Name).Description(Resources.GraphQL.User_Name);
        }
    }
}