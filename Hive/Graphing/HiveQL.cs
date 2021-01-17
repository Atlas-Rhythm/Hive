using System;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.Server;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Graphing
{
    /// <summary>
    /// Extensions for GQL within Hive.
    /// </summary>
    public static class HiveQL
    {
        private const string gqlTypesNamespace = nameof(Types);

        /// <summary>
        /// Add Hive specific types
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHiveQLTypes(this IServiceCollection services)
        {
            // Map Every Field in the Types folder
            // Mainly for development purposes so we dont have to re-add a new type every time one is created.
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.Namespace == gqlTypesNamespace && type.IsSubclassOf(typeof(GraphType)));

            foreach (var graphType in types)
            {
                _ = services.AddSingleton(graphType);
            }
            return services;
        }

        /// <summary>
        /// Add GraphQL
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IGraphQLBuilder AddHiveGraphQL(this IServiceCollection services)
        {
            return services.AddSingleton<HiveSchema>()
                .AddGraphQL((options, provider) =>
                {
                    var logger = provider.GetRequiredService<Serilog.ILogger>();
                    options.UnhandledExceptionDelegate = ctx => logger.Error("An error has occured initializing GraphQL: {Message}", ctx.OriginalException.Message);
                })
                .AddSystemTextJson()
                .AddGraphTypes(typeof(HiveSchema));
        }

        /// <summary>
        /// Analyzes and fills out any errors in an object query.
        /// </summary>
        /// <typeparam name="T">The type of the object query.</typeparam>
        /// <param name="ctx">The resolver context.</param>
        /// <param name="queryResult">The query.</param>
        public static void Anaylze<T>(this IResolveFieldContext ctx, HiveObjectQuery<T> queryResult)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (queryResult is null)
                throw new ArgumentNullException(nameof(queryResult));

            if (!queryResult.Successful)
                ctx.Errors.Add(new ExecutionError(queryResult.Message!));
        }
    }
}
