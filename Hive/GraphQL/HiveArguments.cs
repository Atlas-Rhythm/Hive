using GraphQL.Types;

namespace Hive.GraphQL
{
    /// <summary>
    /// Useful shortcuts for creating GraphQL <see cref="QueryArgument"/>s.
    /// </summary>
    public static class HiveArguments
    {
        /// <summary>
        /// Shorthand for creating a <see cref="QueryArgument"/> with a <see cref="IntGraphType"/> type with the name "page" and a default value of zero. 
        /// </summary>
        /// <param name="description">The description of the argument.</param>
        /// <returns>A configured <see cref="QueryArgument"/> to use for a page argument.</returns>
        public static QueryArgument<IntGraphType> Page(string description = "The page number.")
        {
            return new QueryArgument<IntGraphType>
            {
                Name = "page",
                DefaultValue = 0,
                Description = description
            };
        }

        /// <summary>
        /// Shorthand for creating a <see cref="QueryArgument"/> with a <see cref="StringGraphType"/> type with the name "id".
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public static QueryArgument<StringGraphType> ID(string description = "The ID")
        {
            return new QueryArgument<StringGraphType>
            {
                Name = "id",
                Description = description
            };
        }
    }
}