using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A mockup of how the authentication service will behave.
    /// </summary>
    public class MockAuthenticationService : IProxyAuthenticationService, IAuth0Service
    {
        private Dictionary<string, User?> Users { get; } = new Dictionary<string, User?>();

        /// <inheritdoc/>
        public Auth0ReturnData Data => null!;

        /// <summary>
        /// Creates and populates some dummy users
        /// </summary>
        public MockAuthenticationService()
        {
            // Create dummy data for now
            var user1 = new User { Username = "test" };
            var user2 = new User { Username = "test2" };
            Users.Add("Bearer: test", user1);
            Users.Add("Bearer: asdf", user2);
        }

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We return null from this on ANY exception type instead of forwarding it to our callers.")]
        public Task<User?> GetUser(HttpRequest request)
        {
            if (request is null)
                // If we have a null request, we return a null user. This is the same as an unauthenticated request.
                return Task.FromResult<User?>(null);
            try
            {
                if (request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
                {
                    if (Users.TryGetValue(authHeader, out var outp))
                        return Task.FromResult(outp);
                }
                return Task.FromResult<User?>(null);
            }
            catch
            {
                return Task.FromResult<User?>(null);
            }
        }

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We return null from this on ANY exception type instead of forwarding it to our callers.")]
        public Task<User?> GetUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));
            try
            {
                return Users.TryGetValue(userId, out var outp) ? Task.FromResult(outp) : Task.FromResult<User?>(null);
            }
            catch
            {
                return Task.FromResult<User?>(null);
            }
        }

        /// <inheritdoc/>
        public Task<Auth0TokenResponse?> RequestToken(Uri sourceUri, string code, string? state) => Task.FromResult<Auth0TokenResponse?>(null);
    }
}
