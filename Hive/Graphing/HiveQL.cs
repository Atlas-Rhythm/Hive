using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Server;
using GraphQL.Types;
using Hive.Models;
using Microsoft.Extensions.DependencyInjection;
using static Hive.Graphing.Types.ModType;

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
        public static void Analyze<T>(this IResolveFieldContext ctx, HiveObjectQuery<T> queryResult)
        {
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            if (queryResult is null)
                throw new ArgumentNullException(nameof(queryResult));

            if (!queryResult.Successful)
                ctx.Errors.Add(new ExecutionError(queryResult.Message!));
        }


        /// <summary>
        /// Generates basic mod filters.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static FieldBuilder<T, IEnumerable<Mod>> AddModFilters<T>(this FieldBuilder<T, IEnumerable<Mod>> builder)
        {
            return builder is null
                ? throw new ArgumentNullException(nameof(builder))
                : builder
                .Argument<EnumerationGraphType<Filter>, Filter>("filter", description: "The filter", defaultValue: Filter.Latest)
                .Argument<ListGraphType<IdGraphType>, IEnumerable<string>?>("channelIds", description: "The ids for channels to look through", defaultValue: null)
                .Argument<StringGraphType, string?>("gameVersion", description: "The game version of mods to look for.", defaultValue: null);
        }
    }
}
