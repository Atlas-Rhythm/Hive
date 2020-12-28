using Hive.Dependencies;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Services.Common;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Version = Hive.Versioning.Version;

namespace Hive.Controllers
{
    [Aggregable]
    public interface IResolveDependenciesPlugin
    {
        /// <summary>
        /// Returns true if the specified/anonymous user can resolve dependencies, false otherwise.
        /// <para>Hive default is to return true.</para>
        /// </summary>
        /// <param name="user">User in context</param>
        [return: StopIfReturns(false)]
        bool GetAdditionalChecks(User? user) => true;
    }

    public class HiveResolveDependenciesControllerPlugin : IResolveDependenciesPlugin { }

    [Route("api/resolve_dependencies")]
    public class ResolveDependenciesController : ControllerBase
    {
        private readonly Serilog.ILogger log;
        private readonly IProxyAuthenticationService proxyAuth;
        private readonly DependencyResolverService dependencyResolverService;
        public ResolveDependenciesController([DisallowNull] Serilog.ILogger logger, DependencyResolverService dependencyResolverService, IProxyAuthenticationService proxyAuth)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ResolveDependenciesController>();
            this.dependencyResolverService = dependencyResolverService;
            this.proxyAuth = proxyAuth;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status424FailedDependency)]
        public async Task<ActionResult<DependencyResolutionResult>> ResolveDependencies([FromBody] ModIdentifier[] identifiers)
        {
            log.Debug("Performing dependency resolution...");
            // Get the user, do not need to capture context
            var user = await proxyAuth.GetUser(Request).ConfigureAwait(false);

            var queryResult = await dependencyResolverService.ResolveAsync(user, identifiers).ConfigureAwait(false);

            return queryResult.Convert();
        }
    }
}
