using Hive.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Hive.Services
{
    public class MockAuthenticationService : IProxyAuthenticationService
    {
        public Dictionary<string, User?> Users { get; } = new Dictionary<string, User?>();

        public MockAuthenticationService()
        {
            // Create dummy data for now
            var user1 = new User { Username = "test" };
            var user2 = new User { Username = "test2" };
            Users.Add("Bearer: test", user1);
            Users.Add("Bearer: asdf", user2);
        }

        public Task<User?> GetUser(HttpRequest request, bool throwOnError = false)
        {
            if (request is null)
                if (throwOnError)
                    throw new ArgumentNullException(nameof(request));
                else
                    return Task.FromResult<User?>(null);
            if (request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeader))
            {
                if (Users.TryGetValue(authHeader, out User? outp))
                    return Task.FromResult(outp);
            }
            return Task.FromResult<User?>(null);
        }

        public Task<bool> IsValid(HttpRequest request) => throw new NotImplementedException();
    }
}