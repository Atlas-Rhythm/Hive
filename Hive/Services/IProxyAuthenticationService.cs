using Hive.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A service that performs authentication requests
    /// </summary>
    public interface IProxyAuthenticationService
    {
        public Task<bool> IsValid(HttpRequest request);

        public Task<User?> GetUser(HttpRequest request, bool throwOnError = false);
    }
}