using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Hive.Configuration;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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

        /// <summary>
        /// Create a Auth0Controller with DI.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="auth0Service"></param>
        public Auth0Controller([DisallowNull] ILogger log, IAuth0Service auth0Service)
        {
            if (log is null)
                throw new ArgumentNullException(nameof(log));
            this.auth0Service = auth0Service;
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
        /// <param name="redirect_uri">The redirect uri that MUST match the authorization endpoint's redirect uri.</param>
        /// <returns>The resultant <see cref="Auth0TokenResponse"/> or null on failure.</returns>
        [HttpGet("token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Auth0TokenResponse?>> Callback([FromQuery] string code, [FromQuery] string redirect_uri)
        {
            var val = await auth0Service.RequestToken(code, redirect_uri).ConfigureAwait(false);
            return val is null ? Unauthorized() : (ActionResult<Auth0TokenResponse?>)Ok(val);
        }
    }
}
