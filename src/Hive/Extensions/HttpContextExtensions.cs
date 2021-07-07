using System;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;

namespace Hive.Extensions
{
    /// <summary>
    /// Extensions for an <see cref="HttpContext"/>.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// The key in <see cref="HttpContext.Items"/> where Hive stores a cached User attached to a given <see cref="HttpContext"/>.
        /// </summary>
        public const string HiveCachedUserKey = "HiveUser";

        /// <summary>
        /// Attempts to retrieve a cached <see cref="User"/> attached to the given <see cref="HttpContext"/>.
        /// If no users are cached, Hive will forward the request to the given <see cref="IProxyAuthenticationService"/>, and cache that result.
        /// </summary>
        /// <param name="context">Context </param>
        /// <param name="authenticationService"></param>
        /// <returns>The <see cref="User"/> attached to this context, if any.</returns>
        // REVIEW: Would it be better to extend HttpContext or HttpRequest? If the latter then I might have to use a private dictionary to cache users.
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

            if (context.Items.TryGetValue(HiveCachedUserKey, out var cachedObject))
            {
                // REVIEW: Should I do a "cachedObject is User" check?
                return cachedObject as User;
            }

            // If our context does not have a cached user, we forward to the authentication service.
            var user = await authenticationService.GetUser(context.Request).ConfigureAwait(false);

            context.Items[HiveCachedUserKey] = user;

            return user;
        }
    }
}
