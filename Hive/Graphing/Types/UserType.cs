using Hive.Models;
using GraphQL.Types;
using System.Collections.Generic;
using GraphQL;
using Hive.Services.Common;
using System;
using Microsoft.AspNetCore.Http;
using Hive.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Graphing.Types
{
    /// <summary>
    /// The GQL representation of a <see cref="User"/>.
    /// </summary>
    public class UserType : ObjectGraphType<User>
    {
        /// <summary>
        /// Setup a UserType for GQL.
        /// </summary>
        public UserType()
        {
            Name = nameof(User);
            Description = Resources.GraphQL.User;

            _ = Field(u => u.Username)
                .Description(Resources.GraphQL.User_Username);

            _ = Field<ListGraphType<ModType>, IEnumerable<Mod>>()
                .Name("uploaded").AddModFilters()
                .Description(Resources.GraphQL.User_Uploaded)
                .ResolveAsync((ctx) => GetAllModsFromUser(ctx, mod => mod.Uploader.Username == ctx.Source.Username));

            _ = Field<ListGraphType<ModType>, IEnumerable<Mod>>()
                .Name("contributedOn").AddModFilters()
                .Description(Resources.GraphQL.User_ContributedOn)
                .ResolveAsync((ctx) => GetAllModsFromUser(ctx, mod => mod.Contributors.Any(contributor => contributor.Username == ctx.Source.Username)));

            _ = Field<ListGraphType<ModType>, IEnumerable<Mod>>()
                .Name("authoredFor").AddModFilters()
                .Description(Resources.GraphQL.User_AuthoredFor)
                .ResolveAsync((ctx) => GetAllModsFromUser(ctx, mod => mod.Authors.Any(author => author.Username == ctx.Source.Username)));
        }

        private static async Task<IEnumerable<Mod>> GetAllModsFromUser(IResolveFieldContext<User> ctx, Func<Mod, bool> searchFunc)
        {
            (var modService, var http, var authService)
                = ctx.RequestServices.GetRequiredServices<ModService, IHttpContextAccessor, IProxyAuthenticationService>();

            var filterType = ctx.GetArgument("filter", ModType.Filter.Latest).ToString();
            var channels = ctx.GetArgument<IEnumerable<string>?>("channelIds");
            var gameVersion = ctx.GetArgument<string?>("gameVersion");

            var user = await authService.GetUser(http.HttpContext!.Request).ConfigureAwait(false);
            var queryResult = await modService.GetAllMods(user, channels?.ToArray(), gameVersion, filterType).ConfigureAwait(false);

            ctx.Analyze(queryResult);
            return queryResult.Value?.Where(searchFunc) ?? Array.Empty<Mod>();
        }
    }
}
