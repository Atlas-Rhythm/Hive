using Hive.Models;
using GraphQL.Types;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="User"/>.
    /// </summary>
    public class UserType : ObjectGraphType<User>
    {
        /// <summary>
        /// Setup a UserType for GQL.
        /// </summary>
        public UserType()
        {
            Name = nameof(User);
            Description = Resources.GraphQL.User;

            _ = Field(u => u.Username)
                .Description(Resources.GraphQL.User_Username);
        }
    }
}
