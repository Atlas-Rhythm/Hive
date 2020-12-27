using Hive.Dependencies;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
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
        private readonly PermissionsManager<PermissionContext> permissions;
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IResolveDependenciesPlugin> plugin;
        private readonly IProxyAuthenticationService proxyAuth;
        [ThreadStatic] private static PermissionActionParseState permissionParseState;

        public ResolveDependenciesController([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IProxyAuthenticationService proxyAuth, IAggregate<IResolveDependenciesPlugin> plugin)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<ResolveDependenciesController>();
            permissions = perms;
            context = ctx;
            this.plugin = plugin;
            this.proxyAuth = proxyAuth;
        }

        private const string ActionName = "hive.resolve_dependencies";

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

            // All this stuff seems pretty standard by now. Perform a permissions check for the user, forbid if they don't have permission.
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref permissionParseState))
                return Forbid();

            var combined = plugin.Instance;

            if (!combined.GetAdditionalChecks(user))
                return Forbid();

            // So... we somehow successfully deserialized the list of mod identifiers, only to find that it is null.
            if (identifiers == null)
            {
                return BadRequest("Invalid identifiers.");
            }

            // I'm not gonna bother doing dependency resolution if there is nothing to resolve.
            if (identifiers.Length == 0)
            {
                return BadRequest("No mods were provided; no dependency resolution can occur.");
            }

            log.Debug("Finding mods from parsed identifiers...");

            // We iterate through each mod identifier, then attempt to grab mods that match them.
            var mods = new List<Mod>() { };
            var query = context.Mods.AsNoTracking();

            foreach (var identifier in identifiers)
            {
                var targetVersion = new Version(identifier.Version);

                var mod = await query
                    .FirstOrDefaultAsync(m => m.ReadableID == identifier.ID && m.Version == targetVersion)
                    .ConfigureAwait(false);

                if (mod is null)
                {
                    return NotFound($"Could not find a Mod in that matches identifier \"{identifier}\".");
                }

                mods.Add(mod);
            }

            var dependencyValueAccessor = new HiveValueAccessor(context);
            var resolvedMods = Enumerable.Empty<Mod>();
            var result = new DependencyResolutionResult();

            log.Debug("Resolving dependencies...");

            try
            {
                // Run our obtained mods through DaNike's F# dependency resolution library.
                resolvedMods = await Resolver.Resolve(dependencyValueAccessor, mods).ConfigureAwait(false);
                result.AdditionalMods.AddRange(resolvedMods);
            }
            catch (AggregateException exceptionAggregate)
            {
                foreach (var e in exceptionAggregate.InnerExceptions)
                {
                    var isDependencyException = await HandleDependencyException(e, result, query).ConfigureAwait(false);

                    if (!isDependencyException) throw;
                }
            }
            catch (Exception e)
            {
                var isDependencyException = await HandleDependencyException(e, result, query).ConfigureAwait(false);

                if (!isDependencyException) throw;
            }

            if (result.MissingMods.Any() || result.ConflictingMods.Any() || result.VersionMismatches.Any())
            {
                result.Message = "Dependency Resolution completed with some errors.";
                return StatusCode(StatusCodes.Status424FailedDependency, result);
            }

            result.Message = "Dependency Resolution completed.";
            return Ok(result);
        }

        // Helper function that handles certain dependency resolution exceptions.
        // Returns false if Hive should re-throw the exception
        private static async Task<bool> HandleDependencyException(Exception e, DependencyResolutionResult result, IQueryable<Mod> query)
        {
            if (e is DependencyRangeInvalidException depRangeInvalid)
            {
                result.ConflictingMods.Add(depRangeInvalid.ID);
                return true;
            }
            else if (e is VersionNotFoundException<ModReference> versionNotFound)
            {
                var reference = versionNotFound.ModReference;

                // I'm pretty sure we need to check if a mod even exists with this ID to tell if its a version mismatch, or a missing mod
                var mod = await query
                    .FirstOrDefaultAsync(m => m.ReadableID == reference.ModID)
                    .ConfigureAwait(false);

                if (mod is null)
                {
                    result.MissingMods.Add(reference);
                }
                else
                {
                    result.VersionMismatches.Add(versionNotFound.ModReference);
                }

                return true;
            }
            return false;
        }

        private class HiveValueAccessor : IValueAccessor<Mod, ModReference, Version, VersionRange>
        {
            private readonly HiveContext context;

            public HiveValueAccessor(HiveContext ctx) => context = ctx;

            // Mod Accessors
            public string ID(Mod mod_) => mod_.ReadableID;

            public Version Version(Mod mod_) => mod_.Version;

            public IEnumerable<ModReference> Dependencies(Mod mod_) => mod_.Dependencies;

            public IEnumerable<ModReference> Conflicts(Mod mod_) => mod_.Conflicts;

            // ModRef Accessors
            public string ID(ModReference @ref) => @ref.ModID;

            public VersionRange Range(ModReference @ref) => @ref.Versions;

            public ModReference CreateRef(string id, VersionRange range) => new(id, range);

            // Comparisons
            public bool Matches(VersionRange range, Version version) => range.Matches(version);

            public int Compare(Version a, Version b) => a.CompareTo(b);

            // Combiners
            public VersionRange Either(VersionRange a, VersionRange b) => a.Disjunction(b);

            public FSharpValueOption<VersionRange> And(VersionRange a, VersionRange b)
            {
                var res = a & b;
                return res == VersionRange.Nothing ? FSharpValueOption<VersionRange>.None : FSharpValueOption<VersionRange>.Some(res);
            }

            public VersionRange Not(VersionRange a) => a.Invert();

            // External
            public async Task<IEnumerable<Mod>> ModsMatching(ModReference @ref)
            {
                var mods = context.Mods.AsNoTracking();

                // I'm not sure how necessary this is, but it prevents Visual Studio yelling at me because:
                // 1) Without it, Visual Studio would yell at me because I had an "async" method with no awaited calls
                // 2) Removing "async" throws an error because now it doesn't return a Task<IEnumerable<Mod>>
                await mods.LoadAsync().ConfigureAwait(false);

                // Turn it into enumerable because the Where chain is too complex for EF to handle
                return mods.AsEnumerable().Where(m => m.ReadableID == @ref.ModID && @ref.Versions.Matches(m.Version));
            }
        }
    }
}
