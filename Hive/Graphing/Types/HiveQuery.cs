using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

            _ = Field<ChannelType, Channel?>().Name("channel").Argument<StringGraphType>("id", Resources.GraphQL.Channel_Name).ResolveAsync(GetChannel);
            _ = Field<ListGraphType<ChannelType>, IEnumerable<Channel>>().Name("channels").ResolveAsync(GetAllChannels);
        }

        private async Task<Channel?> GetChannel(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = await channelService.GetChannel(ctx.GetArgument<string>("id"), user).ConfigureAwait(false);

            if (!queryResult.Successful)
                ctx.Errors.Add(new ExecutionError(queryResult.Message!));
            return queryResult.Value;
        }

        private async Task<IEnumerable<Channel>> GetAllChannels(IResolveFieldContext<object> ctx)
        {
            (var channelService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ChannelService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = channelService.RetrieveAllChannels(user);

            if (!queryResult.Successful)
                ctx.Errors.Add(new ExecutionError(queryResult.Message!));
            return queryResult.Value ?? Array.Empty<Channel>();
        }
    }
}
