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
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public Task<User?> GetUser(HttpRequest request, bool throwOnError = false);

        /// <summary>
        /// Returns a <see cref="User"/> from a user ID, throwing an exception if specified.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        public Task<User?> GetUser(string userId, bool throwOnError = false);
    }
}
