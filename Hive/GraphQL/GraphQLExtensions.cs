using System;
using GraphQL;
using GraphQL.Utilities;
using Hive.Models;

namespace Hive.GraphQL
{
    public static class GraphQLExtensions
    {
        /// <summary>
        /// Grab the <see cref="HiveContext"/> from an <see cref="IResolveFieldContext"./>
        /// </summary>
        /// <remarks>
        /// GraphQL .NET recommends all of its objects remain singletons and for any scoped
        /// services to be resolved through it's <seealso cref="IResolveFieldContext"/> object
        /// received in every query.
        /// </remarks>
        /// <param name="resolveFieldContext"></param>
        /// <returns></returns>
        public static HiveContext Hive(this IResolveFieldContext resolveFieldContext)
        {
            if (resolveFieldContext is null)
                throw new ArgumentNullException(nameof(resolveFieldContext));
            return resolveFieldContext.RequestServices.GetRequiredService<HiveContext>();
        }
    }
}