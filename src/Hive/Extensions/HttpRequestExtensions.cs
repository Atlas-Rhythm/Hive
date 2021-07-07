using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for an <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpRequestExtensions
    {
        private static readonly Dictionary<HttpRequest, User?> cachedUsers = new();

        /// <summary>
        /// Attempts to retrieve a cached <see cref="User"/> attached to the given <see cref="HttpRequest"/>.
        /// If no users are cached, Hive will forward the request to the given <see cref="IProxyAuthenticationService"/>, and cache that result.
        /// </summary>
        /// <param name="request">Request to retrieve/cache a <see cref="User"/> from.</param>
        /// <param name="authenticationService">Authentication service to forward uncached requests to.</param>
        /// <returns>The <see cref="User"/> attached to this context, if any.</returns>
        // REVIEW: Would it be better to extend HttpContext or HttpRequest?
        public static async Task<User?> GetHiveUser(this HttpRequest request, IProxyAuthenticationService authenticationService)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (authenticationService is null)
            {
                throw new ArgumentNullException(nameof(authenticationService));
            }

            if (cachedUsers.TryGetValue(request, out var user))
            {
                return user;
            }

            // If our context does not have a cached user, we forward to the authentication service.
            user = await authenticationService.GetUser(request).ConfigureAwait(false);

            cachedUsers.Add(request, user);

            return user;
        }
    }
}
