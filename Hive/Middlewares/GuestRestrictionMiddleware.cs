using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hive.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Hive
{
    /// <summary>
    /// Middleware for restricting access to guest (non-authenticated) users.
    /// </summary>
    public class GuestRestrictionMiddleware
    {
        private static readonly JsonSerializerOptions serializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private readonly RequestDelegate next;
        private readonly Serilog.ILogger logger;
        private readonly IProxyAuthenticationService auth;
        private readonly IList<string> restrictedRoutes;

        /// <summary>
        /// Creates a Middleware instance using DI.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="logger"></param>
        /// <param name="auth"></param>
        /// <param name="configuration"></param>
        public GuestRestrictionMiddleware([DisallowNull] RequestDelegate next, [DisallowNull] Serilog.ILogger logger,
            [DisallowNull] IProxyAuthenticationService auth, [DisallowNull] IConfiguration configuration)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.next = next;
            this.logger = logger.ForContext<GuestRestrictionMiddleware>();
            this.auth = auth;

            // This configuration option is simply a list of routes ("/api/mod", "/api/upload", etc.)
            // REVIEW: Should I handle cases like "/api/mod/{id}/latest"? How would I go about doing that?
            restrictedRoutes = configuration.GetValue<List<string>>("RestrictedRoutes");
        }

        /// <summary>
        /// Invokes the delegate, only if
        ///     (1) the route is not marked as restricted in the config, or
        ///     (2) if there is an authenticated user behind the request.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Grab the route the user is wanting to access
            var route = httpContext.Request.Path.Value!;

            // We do not bother with extra computations if the request is already processed, or our route is not restricted.
            if (!httpContext.Response.HasStarted && restrictedRoutes.Contains(route, StringComparer.InvariantCultureIgnoreCase))
            {
                // See if we can obtain user information from the request
                // REVIEW: We already have to grab our user here. Can/should I find a way to pass this User object down to the controller/services?
                var user = await auth.GetUser(httpContext.Request).ConfigureAwait(false);

                // If the user is not authenticated, and trying to access a restricted endpoint, return 401 Unauthorized.
                if (user == null)
                {
                    // REVIEW: Should this string be integrated into Resources?
                    logger.Error("Non-authenticated user prevented access to {0}", route);

                    httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    httpContext.Response.ContentType = "application/json";

                    var jsonException = new JsonApiException
                    {
                        StatusCode = httpContext.Response.StatusCode,
                        // REVIEW: Should this string be integrated into Resources?
                        Message = "You must be logged in to gain access."
                    };

                    var json = JsonSerializer.Serialize(jsonException, serializerOptions);
                    await httpContext.Response.WriteAsync(json).ConfigureAwait(false);

                    return;
                }
            }

            await next.Invoke(httpContext).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>    
    public static class GuestRestrictingMiddlewareExtensions
    {
        /// <summary>
        /// Extension method used to add <see cref="GuestRestrictionMiddleware"/> to the HTTP request pipeline.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseGuestRestrictionMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<GuestRestrictionMiddleware>();
    }
}
