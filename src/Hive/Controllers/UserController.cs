﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hive.Extensions;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins.Aggregates;
using Hive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hive.Controllers
{
    /// <summary>
    /// A plugin used for user related endpoints.
    /// </summary>
    [Aggregable(Default = typeof(HiveUserPlugin))]
    public interface IUserPlugin
    {
        /// <summary>
        /// This function is used to explicitly allow or deny specific usernames during renames.
        /// Hive default is to allow all usernames.
        /// </summary>
        /// <param name="username">The username to allow or deny.</param>
        /// <returns>True to allow the username, false otherwise.</returns>
        [return: StopIfReturns(false)]
        bool AllowUsername(string username) => true;

        /// <summary>
        /// This function is used to explicitly force a rename of a user. This only happens after <see cref="AllowUsername(string)"/> returns true.
        /// Hive default is to attempt to use the provided username.
        /// </summary>
        /// <param name="username">The input username to rename.</param>
        /// <returns>The new username to apply.</returns>
        string ForceRename([TakesReturnValue] string username) => username;

        /// <summary>
        /// This function is used to expose additional user metadata to the <see cref="UserController.GetUserInfo"/> call.
        /// Hive default is to add nothing.
        /// </summary>
        /// <param name="data">The MUTABLE dictionary to perform modification to.</param>
        /// <param name="userData">The extra user data.</param>
        void ExposeUserInfo(Dictionary<string, object> data, ArbitraryAdditionalData userData)
        {
        }
    }

    internal class HiveUserPlugin : IUserPlugin { }

    /// <summary>
    /// User controller for API
    /// </summary>
    [Route("api/user/")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IProxyAuthenticationService authService;
        private readonly IAggregate<IUserPlugin> plugin;
        private readonly HiveContext context;
        private readonly PermissionsManager<PermissionContext> permissions;

        private const string RenameActionName = "hive.user.rename";
        private const string GetUserInfoActionName = "hive.user.info";
        [ThreadStatic]
        private static PermissionActionParseState renameParseState;
        [ThreadStatic]
        private static PermissionActionParseState getUserParseState;

        /// <summary>
        /// Create with DI
        /// </summary>
        /// <param name="context"></param>
        /// <param name="authService"></param>
        /// <param name="plugin"></param>
        /// <param name="perms"></param>
        public UserController(HiveContext context, IProxyAuthenticationService authService, IAggregate<IUserPlugin> plugin, PermissionsManager<PermissionContext> perms)
        {
            this.context = context;
            this.authService = authService;
            this.plugin = plugin;
            permissions = perms;
        }

        /// <summary>
        /// Returns the user information for a requested user.
        /// This is a JSON serialized collection of arbitrary objects, where the only two Hive defaults are the <see cref="User.Username"/> and <see cref="User.AlternativeId"/>.
        /// Everything else can be added (or removed) as per plugins.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <returns>The data to serialize.</returns>
        [Route("info")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Dictionary<string, object>>> GetUserInfo([FromQuery] string? username)
        {
            User? user;
            if (string.IsNullOrWhiteSpace(username))
            {
                // User should be self
                user = await HttpContext.GetHiveUser(authService).ConfigureAwait(false);
            }
            else
            {
                username = Uri.UnescapeDataString(username);
                user = await authService.GetUser(username).ConfigureAwait(false);
            }
            // User must exist
            if (user is null)
            {
                return NotFound();
            }
            // Permissions check
            if (!permissions.CanDo(GetUserInfoActionName,
                new PermissionContext
                {
                    User = await HttpContext.GetHiveUser(authService).ConfigureAwait(false),
                    Username = username
                }, ref getUserParseState))
            {
                return Unauthorized();
            }
            // Data to be serialized, pass through plugin first
            // Start by adding username and sub in case (for some reason) the plugin wants to remove those
            Dictionary<string, object> data = new() { { "username", username ?? user.Username }, { "sub", user.AlternativeId } };
            plugin.Instance.ExposeUserInfo(data, user.AdditionalData);
            return Ok(data);
        }

        /// <summary>
        /// Rename a username from a query string.
        /// </summary>
        /// <param name="username">Username to change the logged in user to.</param>
        /// <returns>The changed username (post-modification).</returns>
        [Route("rename")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> Rename([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest();
            }
            username = Uri.UnescapeDataString(username);
            var pluginInstance = plugin.Instance;
            try
            {
                var user = await HttpContext.GetHiveUser(authService).ConfigureAwait(false);
                if (user is null)
                {
                    // Unauthorized if user not logged in
                    return Unauthorized();
                }
                // Get plugin username/allow/deny
                if (!pluginInstance.AllowUsername(username))
                {
                    return Unauthorized();
                }
                // Get permissions username rename allow/deny
                if (!permissions.CanDo(RenameActionName, new PermissionContext { User = user, Username = username }, ref renameParseState))
                {
                    return Unauthorized();
                }
                username = pluginInstance.ForceRename(username);
                if (await context.Users.AsTracking().AnyAsync(u => u.Username == username).ConfigureAwait(false))
                {
                    // Deny this username because it conflicts with A user who already exists (this means that a user renaming themselves to their own name is denied)
                    return Unauthorized();
                }
                user.Username = username;
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }
            catch
            {
                // For any exception, we just return an unauthorized.
                return Unauthorized();
                throw;
            }
            return username;
        }
    }
}
