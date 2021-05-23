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
    /// A class for plugins that allow modifications of <see cref="ChannelsController"/>
    /// </summary>
    [Aggregable]
    public interface IChannelsControllerPlugin
    {
        /// <summary>
        /// Returns true if the specified user has access to ANY of the channels. False otherwise.
        /// A false return will cause the endpoint in question to return a Forbid before executing the rest of the endpoint.
        /// <para>It is recommended to use <see cref="GetChannelsFilter(User?, IEnumerable{Channel})"/> for filtering user specific channels.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        public bool GetChannelsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="Channel"/>
        /// <para>Hive default is to return input channels.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="channels">Input channels to filter</param>
        /// <returns>Filtered channels</returns>
        public IEnumerable<Channel> GetChannelsFilter(User? user, [TakesReturnValue] IEnumerable<Channel> channels) => channels;

        /// <summary>
        /// Returns true if the specified user has access to view a particular channel, false otherwise.
        /// This method is called for each channel the user wants to access.
        /// <para>Hive default is to return true for each channel.</para>
        /// </summary>
        /// <remarks>
        /// This method is called in a LINQ expression that is not tracked by EntityFramework,
        /// so modifications done to the <see cref="Channel"/> object will not be reflected in the database.
        /// </remarks>
        /// <param name="user">User in context</param>
        /// <param name="contextChannel">Mod in context</param>
        [return: StopIfReturns(false)]
        public bool GetSpecificChannelAdditionalChecks(User? user, Channel? contextChannel) => true;
    }

    internal class HiveChannelsControllerPlugin : IChannelsControllerPlugin { }

    /// <summary>
    /// A REST controller for channel related actions.
    /// </summary>
    [Route("api/")]
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
        /// This performs a permission check at: <c>hive.channels.list</c>.
        /// Furthermore, channels are further filtered by a permission check at: <c>hive.channels.filter</c>.
        /// </summary>
        /// <returns>A wrapped collection of <see cref="Channel"/>, if successful.</returns>
        [HttpGet("channels")]
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
            var queryResult = await channelService.RetrieveAllChannels(user).ConfigureAwait(false);

            return queryResult.Convert();
        }

        /// <summary>
        /// Creates a new <see cref="Channel"/> object with the specified name.
        /// This performs a permission check at: <c>hive.channel.create</c>.
        /// </summary>
        /// <param name="newChannel">The the new channel to add</param>
        /// <returns>The wrapped <see cref="Channel"/> that was created, if successful.</returns>
        [HttpPost("channels/new")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Channel>> CreateNewChannel([FromBody] Channel newChannel)
        {
            log.Debug("Creating new channel...");

            // Get the user, do not need to capture context.
            var user = await authService.GetUser(Request).ConfigureAwait(false);

            // This probably isn't something that the average Joe can do, so we return unauthorized if there is no user.
            if (user is null) return new UnauthorizedResult();

            // If user is null, we can simply forward it anyways
            var queryResult = await channelService.CreateNewChannel(user, newChannel).ConfigureAwait(false);

            return queryResult.Convert();
        }
    }
}
