using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hive.Services
{
    public class MockAuthenticationHandler : IAuthenticationHandler
    {
        private HttpContext? context;
        private string? authType;
        private readonly IProxyAuthenticationService proxyAuth;

        public MockAuthenticationHandler(IProxyAuthenticationService proxyAuth)
        {
            this.proxyAuth = proxyAuth;
        }

        public async Task<AuthenticateResult> AuthenticateAsync()
        {
            if (context is null)
                return AuthenticateResult.NoResult();
            var user = await proxyAuth.GetUser(context.Request);
            if (user is null)
                return AuthenticateResult.Fail("Could not find user");
            user.AuthenticationType = authType;
            user.IsAuthenticated = true;
            ClaimsPrincipal p = new ClaimsPrincipal(user);
            return AuthenticateResult.Success(new AuthenticationTicket(p, authType));
        }

        public async Task ChallengeAsync(AuthenticationProperties properties)
        {
            if (context is null)
                return;
            await context.Response.WriteAsync("challenge");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        }

        public async Task ForbidAsync(AuthenticationProperties properties)
        {
            if (context is null)
                return;
            await context.Response.WriteAsync("forbidden");
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            authType = scheme.Name;
            this.context = context;
            return Task.CompletedTask;
        }
    }
}