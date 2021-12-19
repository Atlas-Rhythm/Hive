using System.Threading.Tasks;
using Hive.Models;

namespace Hive.Services
{
    /// <summary>
    /// Represents an Auth0 service, which should be capable of requesting tokens and providing necessary information as responses from the API.
    /// </summary>
    public interface IAuth0Service
    {
        /// <summary>
        /// Holds the data necessary for external Auth0 requests. This is used by the <see cref="Controllers.Auth0Controller"/> type to return valid data to the frontend.
        /// </summary>
        Auth0ReturnData Data { get; }

        /// <summary>
        /// Requests a token from the provided source Uri, authentication code, and state. This should be called as part of the callback from Auth0.
        /// </summary>
        /// <param name="code">The authentication code for this client.</param>
        /// <param name="redirectUri">The callback request uri.</param>
        /// <returns>The <see cref="Auth0TokenResponse"/> that results from the request. The access token property from this response should be all that is necessary.</returns>
        Task<Auth0TokenResponse?> RequestToken(string code, string redirectUri);
    }
}
