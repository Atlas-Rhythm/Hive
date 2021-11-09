using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Hive.Extensions;
using Hive.Models;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="Channel"/>.
    /// </summary>
    public class ChannelType : ObjectGraphType<Channel>
    {
        /// <summary>
        /// Setup a ChannelType for GQL.
        /// </summary>
        public ChannelType(IEnumerable<ICustomHiveGraph<ChannelType>> customGraphs)
        {
            if (customGraphs is null)
                throw new ArgumentNullException(nameof(customGraphs));

            Name = nameof(Channel);
            Description = Resources.GraphQL.Channel;

            _ = Field(c => c.Name)
                .Description(Resources.GraphQL.Channel_Name);

            _ = Field<ListGraphType<ModType>, IEnumerable<Mod>>().Name("mods").ResolveAsync(GetChannelMods);

            foreach (var graph in customGraphs)
                graph.Configure(this);
        }

        private async Task<IEnumerable<Mod>?> GetChannelMods(IResolveFieldContext<Channel> ctx)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var user = await http.HttpContext!.GetHiveUser(authService).ConfigureAwait(false);
            var queryResult = await modService.GetAllMods(user, new[] { ctx.Source!.Name }).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value ?? Array.Empty<Mod>();
        }
    }
}
