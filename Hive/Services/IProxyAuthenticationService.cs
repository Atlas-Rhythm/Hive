using Hive.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Hive.Services
{
    /// <summary>
    /// A service that performs authentication requests
    /// </summary>
    public interface IProxyAuthenticationService
    {
        /// <summary>
        /// Returns a <see cref="User"/> from a request, throwing an exception if specified.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task<User?> GetUser(HttpRequest request);

        /// <summary>
        /// Returns a <see cref="User"/> from a user ID, throwing an exception if specified.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task<User?> GetUser(string userId);
    }
}
