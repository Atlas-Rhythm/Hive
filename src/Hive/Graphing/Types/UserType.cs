using System;
using System.Collections.Generic;
using GraphQL.Types;
using Hive.Models;

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
        public UserType(IEnumerable<ICustomHiveGraph<UserType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = nameof(User);
            Description = Resources.GraphQL.User;

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }
    }
}
