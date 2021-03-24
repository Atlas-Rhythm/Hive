using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
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
        public HiveQuery([DisallowNull] ILogger logger)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));

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
        }

        private async Task<Channel?> GetChannel(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = await channelService.GetChannel(ctx.GetArgument<string>("id"), user).ConfigureAwait(false);

            ctx.Anaylze(queryResult);
            return queryResult.Value;
        }

        private async Task<IEnumerable<Channel>> GetAllChannels(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = channelService.RetrieveAllChannels(user);

            ctx.Anaylze(queryResult);
            return queryResult.Value ?? Array.Empty<Channel>();
        }

        private async Task<Mod?> GetMod(IResolveFieldContext<object> ctx)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = modService.GetMod(user, ctx.GetArgument<string>("id"));

            ctx.Anaylze(queryResult);
            return queryResult.Value;
        }

        private async Task<IEnumerable<Mod>> GetAllMods(IResolveFieldContext<object> ctx)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var filterType = ctx.GetArgument("filter", ModType.Filter.Latest).ToString();
            var channels = ctx.GetArgument<IEnumerable<string>?>("channelIds");
            var gameVersion = ctx.GetArgument<string?>("gameVersion");

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = modService.GetAllMods(user, channels?.ToArray(), gameVersion, filterType);

            ctx.Anaylze(queryResult);
            return queryResult.Value ?? Array.Empty<Mod>();
        }
    }
}
