using Hive.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A class for Hive authentication.
    /// </summary>
    public class AuthenticationHandler : IAuthenticationHandler
    {
        private HttpContext? context;
        private static string authType = "Bearer";
        private readonly IProxyAuthenticationService proxyAuth;

        /// <summary>
        /// Construct a new mock authentication handler via DI.
        /// </summary>
        /// <param name="proxyAuth"></param>
        public AuthenticationHandler(IProxyAuthenticationService proxyAuth)
        {
            this.proxyAuth = proxyAuth;
        }

        /// <inheritdoc/>
        public async Task<AuthenticateResult> AuthenticateAsync()
        {
            if (context is null)
                return AuthenticateResult.NoResult();
            var user = await proxyAuth.GetUser(context.Request).ConfigureAwait(false);
            if (user is null)
                return AuthenticateResult.Fail("Could not find user");
            user.AuthenticationType = authType;
            user.IsAuthenticated = true;
            var p = new ClaimsPrincipal(user);
            return AuthenticateResult.Success(new AuthenticationTicket(p, authType));
        }

        /// <inheritdoc/>
        public async Task ChallengeAsync(AuthenticationProperties? properties)
        {
            if (context is null)
                return;
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync(Resource.challenge_respose).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ForbidAsync(AuthenticationProperties? properties)
        {
            if (context is null)
                return;
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync(Resource.forbidden_respose).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task InitializeAsync([DisallowNull] AuthenticationScheme scheme, HttpContext context)
        {
            if (scheme is null)
                throw new ArgumentNullException(nameof(scheme));
            authType = scheme.Name;
            this.context = context;
            return Task.CompletedTask;
        }
    }
}
