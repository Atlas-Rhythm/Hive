using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Controllers
{
    /// <summary>
    /// A REST controller for channel related actions.
    /// </summary>
    [Route("api/channels")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly ChannelService channelService;
        private readonly IProxyAuthenticationService authService;

        /// <summary>
        /// Create a ChannelsController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="channelService"></param>
        /// <param name="authService"></param>
        public ChannelsController([DisallowNull] Serilog.ILogger logger, ChannelService channelService, IProxyAuthenticationService authService)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelsController>();
            this.authService = authService;
            this.channelService = channelService;
        }

        /// <summary>
        /// Gets all <see cref="Channel"/> objects available.
        /// This performs a permission check at: <c>hive.channel</c>.
        /// </summary>
        /// <returns>A wrapped collection of <see cref="Channel"/>, if successful.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // TODO: Perhaps return a subset of Channel, instead only containing information desired as opposed to the whole model?
        // This is probably applicable via a GraphQL endpoint, however.
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            log.Debug("Getting channels...");
            // Get the user, do not need to capture context.
            var user = await authService.GetUser(Request).ConfigureAwait(false);
            // If user is null, we can simply forward it anyways
            var queryResult = channelService.RetrieveAllChannels(user);

            return queryResult.Convert();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<ActionResult<Channel>> CreateNewChannel([FromBody] string channelName)
        {
            log.Debug("Creating new channel...");

            // Get the user, do not need to capture context.
            var user = await authService.GetUser(Request).ConfigureAwait(false);

            // If user is null, we can simply forward it anyways
            var queryResult = channelService.CreateNewChannel(user, channelName);

            return queryResult.Convert();
        }
    }
}
