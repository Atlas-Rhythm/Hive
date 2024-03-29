﻿using System;
using System.Collections.Generic;
using System.Globalization;
using DryIoc;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Server;
using GraphQL.Types;
using Hive.Graphing.Types;
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
            _ = services.AddGraphQL((options, provider) =>
            {
                var logger = provider.GetRequiredService<Serilog.ILogger>();
                options.UnhandledExceptionDelegate = ctx => logger.Error(ctx.OriginalException, "An error has occured initializing GraphQL");
            }).AddSystemTextJson();
            return services;
        }

        /// <summary>
        /// Register Hive's GQL.
        /// </summary>
        /// <param name="container"></param>
        public static void RegisterHiveGraphQL(this IRegistrator container)
            => container.Register<HiveSchema>(Reuse.Scoped);

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
            {
                var err = new ExecutionError(queryResult.Message ?? "")
                {
                    Code = queryResult.StatusCode.ToString(CultureInfo.InvariantCulture)
                };
                ctx.Errors.Add(err);
            }
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
                .Argument<FilterEnumType, ModType.Filter>("filter", description: "The filter", defaultValue: ModType.Filter.Latest)
                .Argument<ListGraphType<IdGraphType>, IEnumerable<string>?>("channelIds", description: "The ids for channels to look through", defaultValue: null)
                .Argument<StringGraphType, string?>("gameVersion", description: "The game version of mods to look for.", defaultValue: null);
        }
    }
}
