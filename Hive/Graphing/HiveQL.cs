using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using GraphQL.Types;
using GraphQL.Server;

namespace Hive.Graphing
{
    public static class HiveQL
    {
        private const string gqlTypesNamespace = nameof(Types);

        public static void AddHiveQLTypes(this IServiceCollection services)
        {
            // Map Every Field in the Types folder
            // Mainly for development purposes so we dont have to re-add a new type every time one is created.
            IEnumerable<Type> types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.Namespace == gqlTypesNamespace && type.IsSubclassOf(typeof(GraphType));

            foreach (var graphType in types)
            {
                services.AddSingleton(graphType);
            }
        }

        public static void AddHiveGraphQL(this IServiceCollection services)
        {
            services.AddSingleton<HiveSchema>();
            services.AddGraphQL((options, provider) =>
            {
                Serilog.ILogger logger = provider.GetRequiredService<Serilog.ILogger>();
                options.UnhandledExceptionDelegate = ctx => logger.Error("An error has occured initializing GraphQL: {Message}", ctx.OriginalException.Message);
            }).AddSystemTextJson().AddGraphTypes(typeof(HiveSchema));
        }
    }
}