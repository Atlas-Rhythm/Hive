using System.Linq;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Plugins.Aggregates;
using Hive.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hive.Controllers
{
    /// <summary>
    /// A plugin used for user related endpoints.
    /// </summary>
    [Aggregable]
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
        string ForceRename(string username) => username;
    }

    internal class HiveUserPlugin : IUserPlugin { }

    [Route("api/user/")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IProxyAuthenticationService authService;
        private readonly IAggregate<IUserPlugin> plugin;
        private readonly HiveContext context;

        public UserController(HiveContext context, IProxyAuthenticationService authService, IAggregate<IUserPlugin> plugin)
        {
            this.context = context;
            this.authService = authService;
            this.plugin = plugin;
        }

        [Route("rename")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<string>> Rename([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest();
            }
            var pluginInstance = plugin.Instance;
            try
            {
                var user = await authService.GetUser(Request, true).ConfigureAwait(false);
                // Get plugin username/allow/deny
                if (!pluginInstance.AllowUsername(username))
                {
                    return Unauthorized();
                }
                username = pluginInstance.ForceRename(username);
                if (await context.Users.AnyAsync(u => u.Username == username).ConfigureAwait(false))
                {
                    // Deny this username because it conflicts with A user who already exists (this means that a user renaming themselves to their own name is denied)
                    return Unauthorized();
                }
                user!.Username = username;
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
