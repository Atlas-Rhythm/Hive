﻿using Hive.Models;
using Hive.Plugins;
using Hive.Services;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Hive.Controllers
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="GameVersionsController"/>
    /// </summary>
    [Aggregable]
    public interface IGameVersionsPlugin
    {
        /// <summary>
        /// Returns true if the sepcified user has access to view the list of all game versions. False otherwise.
        /// A false return will cause the endpoint to return a Forbid before executing the rest of the endpoint.
        /// <para>It is recommended to use <see cref="GetGameVersionsFilter(User?, IEnumerable{GameVersion})"/> for filtering out game versions.</para>
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        [return: StopIfReturns(false)]
        bool GetGameVersionsAdditionalChecks(User? user) => true;

        /// <summary>
        /// Returns a filtered enumerable of <see cref="GameVersion"/>.
        /// <para>Hive default is to return input game versions.</para>
        /// </summary>
        /// <param name="user">User to filter on</param>
        /// <param name="versions">Input versions to filter</param>
        [return: StopIfReturnsEmpty]
        IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions) => versions;
    }

    internal class HiveGameVersionsControllerPlugin : IGameVersionsPlugin { }

    /// <summary>
    /// A REST controller for game version related actions.
    /// </summary>
    [Route("api/game/versions")]
    [ApiController]
    public class GameVersionsController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly GameVersionService gameVersionService;
        private readonly IProxyAuthenticationService proxyAuth;

        /// <summary>
        /// Create a GameVersionsController with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="gameVersionService"></param>
        /// <param name="proxyAuth"></param>
        public GameVersionsController([DisallowNull] Serilog.ILogger logger, GameVersionService gameVersionService, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<GameVersionsController>();
            this.gameVersionService = gameVersionService;
            this.proxyAuth = proxyAuth;
        }

        /// <summary>
        /// Gets all available <see cref="GameVersion"/> objects.
        /// This performs a permission check at: <c>hive.game.version</c>.
        /// </summary>
        /// <returns>A wrapped enumerable of <see cref="GameVersion"/> objects, if successful.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<GameVersion>>> GetGameVersions()
        {
            log.Debug("Getting game versions...");
            // Get the user, do not need to capture context.
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            var queryResult = gameVersionService.RetrieveAllVersions(user);

            return queryResult.Convert();
        }
    }
}
