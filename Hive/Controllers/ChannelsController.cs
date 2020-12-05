using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        /// <para>It is recommended to use <see cref="GetChannelsFilter(IEnumerable{Channel})"/> for filtering user specific channels.</para>
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
    }

    internal class HiveChannelsControllerPlugin : IChannelsControllerPlugin { }

    [Route("api/channels")]
    [ApiController]
    public class ChannelsController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly ChannelService channelService;
        private readonly IProxyAuthenticationService authService;

        public ChannelsController([DisallowNull] Serilog.ILogger logger, ChannelService channelService, IProxyAuthenticationService authService)
        {
            if (logger is null)
                throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ChannelsController>();
            
            this.authService = authService;
            this.channelService = channelService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        // TODO: Perhaps return a subset of Channel, instead only containing information desired as opposed to the whole model?
        // This is probably applicable via a GraphQL endpoint, however.
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            log.Debug("Getting channels...");
            // Get the user, do not need to capture context.
            User? user = await authService.GetUser(Request).ConfigureAwait(false);

            // If user is null, we can simply forward it anyways
            var queryResult = channelService.RetrieveAllChannels(user);

            if (queryResult.Value == null)
            {
                return StatusCode(queryResult.StatusCode, queryResult.Message);
            }

            return Ok(queryResult.Value);
        }
    }
}