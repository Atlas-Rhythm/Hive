using System;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for an <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// The key in <see cref="HttpContext.Items"/> where Hive stores a cached User attached to a given <see cref="HttpContext"/>.
        /// </summary>
        public const string HiveCachedUserKey = "HiveUser";

        /// <summary>
        /// Attempts to retrieve a cached <see cref="User"/> attached to the given <see cref="HttpRequest"/>.
        /// If no users are cached, Hive will forward the request to the given <see cref="IProxyAuthenticationService"/>, and cache that result.
        /// </summary>
        /// <param name="context">Request to retrieve/cache a <see cref="User"/> from.</param>
        /// <param name="authenticationService">Authentication service to forward uncached requests to.</param>
        /// <returns>The <see cref="User"/> attached to this context, if any.</returns>
        public static async Task<User?> GetHiveUser(this HttpContext context, IProxyAuthenticationService authenticationService)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (authenticationService is null)
            {
                throw new ArgumentNullException(nameof(authenticationService));
            }

            if (context.Items.TryGetValue(HiveCachedUserKey, out var cachedObject) && cachedObject is User cachedUser)
            {
                return cachedUser;
            }

            // If our context does not have a cached user, we forward to the authentication service.
            var user = await authenticationService.GetUser(context.Request).ConfigureAwait(false);

            context.Items[HiveCachedUserKey] = user;

            return user;
        }
    }
}
