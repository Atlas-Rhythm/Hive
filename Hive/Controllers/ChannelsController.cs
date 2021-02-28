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

        /// <summary>
        /// Create a ChannelsController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="channelService"></param>
        public ChannelsController([DisallowNull] Serilog.ILogger logger, ChannelService channelService)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelsController>();
            this.channelService = channelService;
        }

        /// <summary>
        /// Gets all <see cref="Channel"/> objects available.
        /// This performs a permission check at: <c>hive.channels.list</c>.
        /// Furthermore, channels are further filtered by a permission check at: <c>hive.channels.filter</c>.
        /// </summary>
        /// <returns>A wrapped collection of <see cref="Channel"/>, if successful.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            log.Debug("Getting channels...");
            // If user is null, we can simply forward it anyways
            var queryResult = await channelService.RetrieveAllChannels(User.Identity as User).ConfigureAwait(false);

            return queryResult.Convert();
        }

        /// <summary>
        /// Creates a new <see cref="Channel"/> object with the specified name.
        /// This performs a permission check at: <c>hive.channel.create</c>.
        /// </summary>
        /// <param name="channelName">The name of the new channel</param>
        /// <returns>The wrapped <see cref="Channel"/> that was created, if successful.</returns>
        [HttpPost("/new")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Channel>> CreateNewChannel([FromBody] string channelName)
        {
            log.Debug("Creating new channel...");

            // This probably isn't something that the average Joe can do, so we return unauthorized if there is no user.
            if (User.Identity is not User user) return Unauthorized();

            // If user is null, we can simply forward it anyways
            var queryResult = await channelService.CreateNewChannel(user, channelName).ConfigureAwait(false);

            return queryResult.Convert();
        }
    }
}
