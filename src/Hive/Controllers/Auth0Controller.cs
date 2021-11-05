using System;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Hive.Controllers
{
    /// <summary>
    /// A REST controller for Auth0 related actions.
    /// </summary>
    [Route("api/auth0/")]
    [ApiController]
    public class Auth0Controller : ControllerBase
    {
        private readonly IAuth0Service auth0Service;
        private readonly Uri baseUri;

        /// <summary>
        /// Create a Auth0Controller with DI.
        /// </summary>
        /// <param name="auth0Service"></param>
        /// <param name="config"></param>
        public Auth0Controller(IAuth0Service auth0Service, IConfiguration config)
        {
            this.auth0Service = auth0Service;
            // Look in Auth0 for the domain string, it MUST be a valid URI and it MUST exist.
            baseUri = config.GetValue<Uri>("Auth0:BaseDomain");
        }

        /// <summary>
        /// Returns the Auth0 data for this Hive instance.
        /// </summary>
        /// <returns>The <see cref="Auth0ReturnData"/> available.</returns>
        [HttpGet("get_data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Auth0ReturnData> GetData() => Ok(auth0Service.Data);

        /// <summary>
        /// Authenticates and returns a <see cref="Auth0TokenResponse"/> for the provided authentication code and state, or null on failure.
        /// </summary>
        /// <param name="code">The authentication code to provide (from a call to Auth0/authenticate).</param>
        /// <param name="state">The state to provide.</param>
        /// <returns>The resultant <see cref="Auth0TokenResponse"/> or null on failure.</returns>
        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Auth0TokenResponse?>> Callback([FromQuery] string code, [FromQuery] string? state)
        {
            return await auth0Service.RequestToken(new UriBuilder
            {
                Host = baseUri.Host,
                Scheme = baseUri.Scheme,
                Port = baseUri.Port,
                Path = baseUri.LocalPath + '/' + Request.Path.Value
            }.Uri, code, state).ConfigureAwait(false);
        }
    }
}
