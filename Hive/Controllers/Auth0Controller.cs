using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Serilog.Core;

namespace Hive.Controllers
{
    /// <summary>
    /// A REST controller for channel related actions.
    /// </summary>
    [Route("api/auth0/")]
    [ApiController]
    public class Auth0Controller : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly IAuth0Service auth0Service;

        /// <summary>
        /// Create a ChannelsController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="auth0Service"></param>
        public Auth0Controller([DisallowNull] Serilog.ILogger logger, IAuth0Service auth0Service)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<Auth0Controller>();
            this.auth0Service = auth0Service;
        }

        [HttpGet("get_data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<Auth0ReturnData> GetData() => auth0Service.Enabled && auth0Service.Data != null ? auth0Service.Data : NotFound();

        [HttpGet("callback")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Auth0TokenResponse?>> Callback([FromQuery] string code, [FromQuery] string? state)
        {
            if (auth0Service.Enabled)
            {
                // This could throw an exception. If it does, just failsafe to returning a null.
                try
                {
                    // TODO: Also return state?
                    return await auth0Service.RequestToken(new Uri(HttpContext.Request.GetDisplayUrl()), code, state).ConfigureAwait(false);
                }
                catch
                {
                    return new ActionResult<Auth0TokenResponse?>((Auth0TokenResponse?)null);
                }
            }
            return NotFound();
        }
    }
}
