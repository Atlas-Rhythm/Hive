using System;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Builders;
using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.Types;
using Hive.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Hive.Graphing
{
    /// <summary>
    /// Extensions for GQL within Hive.
    /// </summary>
    public static class HiveQL
    {
        /// <summary>
        /// Add GraphQL
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddHiveGraphQL(this IServiceCollection services)
        {
            _ = services.AddSingleton(services => new HiveSchema(new SelfActivatingServiceProvider(services)));
            _ = services.AddGraphQL((options, provider) =>
            {
                var logger = provider.GetRequiredService<Serilog.ILogger>();
                options.UnhandledExceptionDelegate = ctx =>
                {
                    logger.Error("An error has occured initializing GraphQL: {Message}", ctx.OriginalException.Message);
                };
            }).AddSystemTextJson();
            return services;
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
                //.Argument<FilterEnumType>("filter", description: "The filter", defaultValue: FilterEnumType)
                .Argument<ListGraphType<IdGraphType>, IEnumerable<string>?>("channelIds", description: "The ids for channels to look through", defaultValue: null)
                .Argument<StringGraphType, string?>("gameVersion", description: "The game version of mods to look for.", defaultValue: null);
        }
    }
}
