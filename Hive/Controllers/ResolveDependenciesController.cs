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
using System.Text;
using System.Text.Json;
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
        public async Task<ActionResult> ResolveDependencies()
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

            // I'm not gonna bother doing dependency resolution if there is nothing to resolve.
            if (Request is null || Request.Body == null)
            {
                return BadRequest("No mods were provided; no dependency resolution can occur.");
            }

            log.Debug("Parsing identifiers from request...");

            // Extract our array of mod identifiers from the Request
            // These are mod ID/version pairs
            ModIdentifier[]? identifiers = null;
            try
            {
                identifiers = await JsonSerializer.DeserializeAsync<ModIdentifier[]>(Request.Body).ConfigureAwait(false);
            }
            catch (JsonException e) // Catch errors that can be attributed to malformed JSON from the user
            {
                return BadRequest(e);
            }

            // So... we somehow successfully deserialized the list of mod identifiers, only to find that it is null.
            if (identifiers == null)
            {
                return BadRequest("Mod identifiers were successfully deserialized, but the resulting object was null.");
            }

            // I'm not gonna bother doing dependency resolution if there is nothing to resolve.
            if (identifiers.Length == 0)
            {
                return BadRequest("No mods were provided; no dependency resolution can occur.");
            }

            log.Debug("Finding mods from parsed identifiers...");

            // We iterate through each mod identifier, then attempt to grab mods that match them.
            // REVIEW: Ways to optimize?
            var mods = new List<Mod>() { };
            foreach (var identifier in identifiers)
            {
                var targetVersion = new Version(identifier.Version);

                var mod = await context.Mods
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ReadableID == identifier.ID && m.Version == targetVersion)
                    .ConfigureAwait(false);

                if (mod is null)
                {
                    return BadRequest($"Could not find a Mod in the database that matches identifier \"{identifier}\".");
                }

                mods.Add(mod);
            }

            var dependencyValueAccessor = new HiveValueAccessor(context);

            var resolvedMods = Enumerable.Empty<Mod>();

            log.Debug("Resolving dependencies...");

            try
            {
                // Run our obtained mods through DaNike's F# dependency resolution library. 
                resolvedMods = await Resolver.Resolve(dependencyValueAccessor, mods).ConfigureAwait(false);
            }
            catch (AggregateException exceptionAggregate)
            {
                var failure = new DependencyResolutionResult
                {
                    Message = "A multitude of errors occured: " + string.Join(", ", exceptionAggregate.InnerExceptions.Select(e => e.Message))
                };

                // REVIEW: For dependency resolution failures, is it wise to return 400? Is there a better error code to represent this?
                return BadRequest(failure);
            }
            catch (Exception e) when (e is DependencyRangeInvalidException || e is VersionNotFoundException<ModReference>)
            {
                var failure = new DependencyResolutionResult
                {
                    Message = e.Message,
                };

                return BadRequest(failure);
            }

            var success = new DependencyResolutionResult()
            {
                Message = "Dependency resolution succeeded. The input mods, as well as any retrieved dependencies, are included below.",
                AdditionalMods = resolvedMods
            };

            return Ok(success);
        }

        // REVIEW: Perhaps move this to Hive.Models, however I'm not sure whoever else will need this.
        // REVIEW: This was taken/modified from the Hive REST Schema for this endpoint. Is this even necessary?
        internal class DependencyResolutionResult
        {
            public string Message { get; init; } = null!;
            public IEnumerable<Mod> AdditionalMods { get; init; } = Enumerable.Empty<Mod>();
        }

        // REVIEW: Perhaps move this to Hive.Models, however I'm not sure whoever else will need this.
        private class HiveValueAccessor : IValueAccessor<Mod, ModReference, Version, VersionRange>
        {
            private HiveContext context;

            public HiveValueAccessor(HiveContext ctx)
            {
                context = ctx;
            }

            // Mod Accessors
            public string ID(Mod mod_) => mod_.ReadableID;

            public Version Version(Mod mod_) => mod_.Version;

            public IEnumerable<ModReference> Dependencies(Mod mod_) => mod_.Dependencies;

            public IEnumerable<ModReference> Conflicts(Mod mod_) => mod_.Conflicts;

            // ModRef Accessors
            public string ID(ModReference @ref) => @ref.ModID;

            public VersionRange Range(ModReference @ref) => @ref.Versions;

            public ModReference CreateRef(string id, VersionRange range) => new ModReference(id, range);

            // Comparisons
            public bool Matches(VersionRange range, Version version) => range.Matches(version);

            public int Compare(Version a, Version b) => a.CompareTo(b);

            // Combiners
            public VersionRange Either(VersionRange a, VersionRange b) => a.Disjunction(b);

            public FSharpValueOption<VersionRange> And(VersionRange a, VersionRange b) => a.Conjunction(b);

            public VersionRange Not(VersionRange a) => a.Invert();

            // External
            public async Task<IEnumerable<Mod>> ModsMatching(ModReference @ref)
            {
                if (context is null)
                {
                    throw new ArgumentException($"Context is null.");
                }

                var mods = context.Mods
                    .AsNoTracking()
                    .Where(m => m.ReadableID == @ref.ModID && @ref.Versions.Matches(m.Version));

                // I'm not sure how necessary this is, but it prevents Visual Studio yelling at me because:
                // 1) Without it, Visual Studio would yell at me because I had an "async" method with no awaited calls
                // 2) Removing "async" throws an error because now it doesn't return a Task<IEnumerable<Mod>>
                await mods.LoadAsync().ConfigureAwait(false);

                return mods;
            }
        }
    }
}
