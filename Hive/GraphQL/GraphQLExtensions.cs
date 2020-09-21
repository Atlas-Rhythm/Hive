using System;
using GraphQL;
using GraphQL.Utilities;
using Hive.Models;
using Hive.Permissions;
using Microsoft.AspNetCore.Http;

namespace Hive.GraphQL
{
    public static class GraphQLExtensions
    {
        public static IServiceProvider Resolve(this IResolveFieldContext resolveFieldContext)
        {
            if (resolveFieldContext is null)
                throw new ArgumentNullException(nameof(resolveFieldContext));
            return resolveFieldContext.RequestServices;
        }

        /// <summary>
        /// Get the current <see cref="Microsoft.AspNetCore.Http.HttpContext"/> from a field context.
        /// </summary>
        /// <param name="resolveFieldContext"></param>
        /// <returns></returns>
        public static HttpContext HttpContext(this IResolveFieldContext resolveFieldContext)
        {
            HttpContext? context = resolveFieldContext.Resolve<IHttpContextAccessor>().HttpContext;
            if (context is null)
                throw new NullReferenceException(nameof(context) + "is null");
            return context;
        }

        /// <summary>
        /// Resolve an object from a service container in an <see cref="IResolveFieldContext"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object being resolved.</typeparam>
        /// <param name="resolveFieldContext">The field context.</param>
        /// <returns></returns>
        public static T Resolve<T>(this IResolveFieldContext resolveFieldContext) => (T)resolveFieldContext.Resolve().GetRequiredService(typeof(T));
    }
}