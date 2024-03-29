﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Hive.Extensions;
using Hive.Models;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// A QGL Query.
    /// </summary>
    public class HiveQuery : ObjectGraphType
    {
        /// <summary>
        /// Create a GQL query.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="customGraphs"></param>
        public HiveQuery([DisallowNull] ILogger logger, IEnumerable<ICustomHiveGraph<HiveQuery>> customGraphs)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            var l = logger.ForContext<HiveQuery>();

            l.Debug("Initializing");

            _ = Field<ChannelType, Channel?>()
                .Name("channel").Argument<IdGraphType>("id", Resources.GraphQL.Channel_Name)
                .Description(Resources.GraphQL.Channel)
                .ResolveAsync(GetChannel);

            _ = Field<ListGraphType<ChannelType>, IEnumerable<Channel>>()
                .Name("channels")
                .Description(Resources.GraphQL.Query_Channels)
                .ResolveAsync(GetAllChannels);

            _ = Field<ModType, Mod?>()
                .Name("mod").Argument<IdGraphType>("id", Resources.GraphQL.Mod_ID)
                .Description(Resources.GraphQL.Mod)
                .ResolveAsync(GetMod);

            _ = Field<ListGraphType<ModType>, IEnumerable<Mod>>()
                .Name("mods").AddModFilters()
                .Description(Resources.GraphQL.Query_Mods)
                .ResolveAsync(GetAllMods);

            _ = Field<GameVersionType, GameVersion?>()
                .Name("gameVersion").Argument<IdGraphType>("name", Resources.GraphQL.GameVersion_Name)
                .Description(Resources.GraphQL.GameVersion)
                .ResolveAsync(GetGameVersion);

            _ = Field<ListGraphType<GameVersionType>, IEnumerable<GameVersion?>>()
                .Name("gameVersions")
                .Description(Resources.GraphQL.Query_GameVersions)
                .ResolveAsync(GetAllGameVersions);

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }

        private async Task<Channel?> GetChannel(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await channelService.GetChannel(ctx.GetArgument<string>("id")!, user).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value;
        }

        private async Task<IEnumerable<Channel>?> GetAllChannels(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await channelService.RetrieveAllChannels(user).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value ?? Array.Empty<Channel>();
        }

        private async Task<Mod?> GetMod(IResolveFieldContext<object> ctx)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await modService.GetMod(user, ctx.GetArgument<string>("id")!).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value;
        }

        private async Task<IEnumerable<Mod>?> GetAllMods(IResolveFieldContext<object> ctx)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var filterType = ctx.GetArgument("filter", ModType.Filter.Latest).ToString();
            var channels = ctx.GetArgument<IEnumerable<string>?>("channelIds");
            var gameVersion = ctx.GetArgument<string?>("gameVersion");

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await modService.GetAllMods(user, channels?.ToArray(), gameVersion, filterType).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value ?? Array.Empty<Mod>();
        }

        private async Task<GameVersion?> GetGameVersion(IResolveFieldContext<object> ctx)
        {
            (var versionService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<GameVersionService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await versionService.RetrieveAllVersions(user).ConfigureAwait(false);
            var version = ctx.GetArgument<string>("name");

            ctx.Analyze(queryResult);
            return queryResult.Value?.FirstOrDefault(v => version == v.Name);
        }

        private async Task<IEnumerable<GameVersion?>?> GetAllGameVersions(IResolveFieldContext<object> ctx)
        {
            (var versionService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<GameVersionService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await versionService.RetrieveAllVersions(user).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value ?? Array.Empty<GameVersion>();
        }

    }
}
