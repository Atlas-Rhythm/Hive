using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using GraphQL.Server;

namespace Hive.Graphing
{
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
    }
}
