using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A mockup of how the authentication service will behave.
    /// </summary>
    public class MockAuthenticationService : IProxyAuthenticationService
    {
        private Dictionary<string, User?> Users { get; } = new Dictionary<string, User?>();

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
        public Task<User?> GetUser(HttpRequest request, bool throwOnError = false)
        {
            if (request is null)
            {
                return throwOnError ? throw new ArgumentNullException(nameof(request)) : Task.FromResult<User?>(null);
            }

            if (request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
            {
                if (Users.TryGetValue(authHeader, out var outp))
                    return Task.FromResult(outp);
            }
            return Task.FromResult<User?>(null);
        }

        /// <inheritdoc/>
        public Task<User?> GetUser(string userId, bool throwOnError = false)
        {
            return string.IsNullOrEmpty(userId)
                ? throwOnError ? throw new ArgumentNullException(nameof(userId)) : Task.FromResult<User?>(null)
                : Users.TryGetValue(userId, out var outp) ? Task.FromResult(outp) : Task.FromResult<User?>(null);
        }

        /// <inheritdoc/>
        public Task<bool> IsValid(HttpRequest request) => throw new NotImplementedException();
    }
}
