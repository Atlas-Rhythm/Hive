using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using static Hive.Tests.TestHelpers;

namespace Hive.Tests.Middleware
{
    public class GuestRestrictionMiddleware : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;
        private readonly RequestDelegate requestDelegate;

        public GuestRestrictionMiddleware(ITestOutputHelper helper)
        {
            this.helper = helper;
            requestDelegate = new RequestDelegate(OnSuccessfulRequest);
        }

        private static async Task OnSuccessfulRequest(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;

            await context.Response.WriteAsync("OK").ConfigureAwait(false);
        }

        [Theory]
        // Having only wildcard
        [InlineData("/api", "*")]
        // Accessing a restricted endpoint
        [InlineData("/restricted", "/restricted")]
        // Accessing a restricted endpoint that cascades
        [InlineData("/api/", "/api/")]
        // Accessing an endpoint that isn't defined, but has a restricted cascading parent
        [InlineData("/api/mod", "/api/")]
        // Accessing a restricted wildcard endpoint
        [InlineData("/api/mod", "/api/*")]
        // Accessing an endpoint with a restricted, cascading wildcard route
        [InlineData("/api/mod/BSIPA/latest", "/api/*/")]
        // Recursive wildcards
        [InlineData("/api/mod/BSIPA", "/*/*/")]
        [InlineData("/api/mod/BSIPA", "/*/*/*")]
        public async Task RestrictedAuthenticated(string requestedEndpoint, params string[] restrictedEndpoints)
        {
            var middleware = SetupMiddleware(restrictedEndpoints);

            // Create a mock request, set our path
            var context = CreateMockRequest(null!);
            context.Request.Path = requestedEndpoint;

            await middleware.Invoke(context).ConfigureAwait(false);

            // Every single invocation of this test method should return 200 OK
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Theory]
        // Having only wildcard
        [InlineData("/api", "*")]
        // Accessing a restricted endpoint
        [InlineData("/restricted", "/restricted")]
        // Accessing a restricted endpoint that cascades
        [InlineData("/api/", "/api/")]
        // Accessing an endpoint that isn't defined, but has a restricted cascading parent
        [InlineData("/api/mod", "/api/")]
        // Accessing a restricted wildcard endpoint
        [InlineData("/api/mod", "/api/*")]
        // Accessing an endpoint with a restricted, cascading wildcard route
        [InlineData("/api/mod/BSIPA/latest", "/api/*/")]
        // Recursive wildcards
        [InlineData("/api/mod/BSIPA", "/*/*/")]
        [InlineData("/api/mod/BSIPA", "/*/*/*")]
        // Accessing endpoint with a cascading grandparent
        [InlineData("/api/mod/BSIPA", "/api/", "!/api/mod")]
        public async Task RestrictedNotAuthenticated(string requestedEndpoint, params string[] restrictedEndpoints)
        {
            var middleware = SetupMiddleware(restrictedEndpoints);

            // Create a mock request, set our path, clear the provided mock authentication headers
            var context = CreateMockRequest(null!);
            context.Request.Path = requestedEndpoint;
            context.Request.Headers.Clear();

            await middleware.Invoke(context).ConfigureAwait(false);

            // Every single invocation of this test method should return 401 Unauthorized
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        }

        [Theory]
        // Having no restricted endpoints should pass
        [InlineData("/api", "")]
        // "!/" (making the root a cascading exception) should pass
        [InlineData("/api", "!/")]
        // Accessing an explicitly unrestricted endpoint
        [InlineData("/api/mods", "!/api/mods")]
        // Accessing an implicitly unrestricted endpoint via cascading parent
        [InlineData("/api/mods/latest", "!/api/")]
        // Accessing an implicitly unrestricted endpoint via cascading wildcard
        [InlineData("/api/mods/latest", "!/api/*/")]
        // Accessing an explicitly unrestricted endpoint with restricted (maybe cascading) parent
        [InlineData("/api/mods/latest", "/api/mods", "!/api/mods/latest")]
        [InlineData("/api/mods/latest", "/api/mods/", "!/api/mods/latest")]
        // Accessing an unrestricted endpoint via wildcard with restricted, cascading parent
        [InlineData("/api/mod/BSIPA/latest", "/api/mod/", "!/api/mod/*/latest")]
        [InlineData("/api/mod/move", "/*/", "!/api/mod/*")]
        public async Task UnrestrictedEndpoints(string requestedEndpoint, params string[] restrictedEndpoints)
        {
            var middleware = SetupMiddleware(restrictedEndpoints);

            // Create a mock request, set our path, clear the provided mock authentication headers
            var context = CreateMockRequest(null!);
            context.Request.Path = requestedEndpoint;
            context.Request.Headers.Clear();

            await middleware.Invoke(context).ConfigureAwait(false);

            // While we're not authenticated, every single invocation should return 200 OK
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Theory]
        // These restrictions directly contradict each other. In these cases, we should throw.
        [InlineData("/api", "!/api")]
        [InlineData("/api/", "!/api/")]
        [InlineData("/*/", "!/*/")]
        [InlineData("/*", "!/*")]
        [InlineData("/api/mod", "!/api/mod")]
        public void AmbiguityExceptions(params string[] restrictedEndpoints)
            => Assert.Throws<InvalidOperationException>(() => SetupMiddleware(restrictedEndpoints));

        private Hive.GuestRestrictionMiddleware SetupMiddleware(params string[] restrictedEndpoints)
        {
            // Create our Configuration using a dictionary
            var configurationKVPs = new Dictionary<string, string>();

            // Iterate using a for loop, as we need to add each item as a new KVP, and also utilize their index.
            for (var i = 0; i < restrictedEndpoints.Length; i++)
            {
                configurationKVPs.Add($"RestrictedRoutes:{i}", restrictedEndpoints[i]);
            }

            var services = DIHelper.ConfigureServices(Options, helper);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationKVPs);

            services.AddSingleton<IConfiguration>(configuration.Build())
                .AddSingleton(requestDelegate)
                .AddScoped<Hive.GuestRestrictionMiddleware>();

            return services.BuildServiceProvider().GetRequiredService<Hive.GuestRestrictionMiddleware>();
        }
    }
}
