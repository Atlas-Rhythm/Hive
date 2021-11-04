using Hive.Models;
using Hive.Plugins.Aggregates;
using Hive.Versioning;
using Hive.Controllers;
using Hive.Permissions;
using Hive.Dependencies;
using Microsoft.FSharp.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Version = Hive.Versioning.Version;
using System.Linq;

namespace Hive.Services.Common
{
    /// <summary>
    /// A class for plugins that allow modifications of <see cref="ResolveDependenciesController"/>
    /// </summary>
    [Aggregable(Default = typeof(HiveResolveDependenciesControllerPlugin))]
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

    internal class HiveResolveDependenciesControllerPlugin : IResolveDependenciesPlugin { }

    /// <summary>
    /// Common functionality for dependency resolution actions.
    /// </summary>
    public class DependencyResolverService
    {
        private readonly Serilog.ILogger log;
        private readonly HiveContext context;
        private readonly IAggregate<IResolveDependenciesPlugin> plugin;
        private readonly PermissionsManager<PermissionContext> permissions;
        [ThreadStatic] private static PermissionActionParseState permissionParseState;

        private const string ActionName = "hive.resolve_dependencies";

        private static readonly HiveObjectQuery<DependencyResolutionResult> forbiddenResponse = new(StatusCodes.Status403Forbidden);

        /// <summary>
        /// Create a DependencyResolverService with DI.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="perms"></param>
        /// <param name="ctx"></param>
        /// <param name="plugin"></param>
        public DependencyResolverService([DisallowNull] Serilog.ILogger logger, PermissionsManager<PermissionContext> perms, HiveContext ctx, IAggregate<IResolveDependenciesPlugin> plugin)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));
            log = logger.ForContext<DependencyResolverService>();
            this.plugin = plugin;
            permissions = perms;
            context = ctx;
        }

        /// <summary>
        /// Resolves the dependencies for a list of <see cref="ModIdentifier"/> objects.
        /// This performs a permission check at: <c>hive.resolve_dependencies</c>.
        /// </summary>
        /// <param name="user">The user to associate with this request.</param>
        /// <param name="identifiers">The identifiers to resolve dependencies for.</param>
        /// <returns>A wrapped <see cref="DependencyResolutionResult"/>, if successful.</returns>
        public async Task<HiveObjectQuery<DependencyResolutionResult>> ResolveAsync(User? user, IEnumerable<ModIdentifier> identifiers)
        {
            // All this stuff seems pretty standard by now. Perform a permissions check for the user, forbid if they don't have permission.
            if (!permissions.CanDo(ActionName, new PermissionContext { User = user }, ref permissionParseState))
                return forbiddenResponse;

            var combined = plugin.Instance;

            if (!combined.GetAdditionalChecks(user))
                return forbiddenResponse;

            // So... we somehow successfully deserialized the list of mod identifiers, only to find that it is null.
            if (identifiers == null)
            {
                return new HiveObjectQuery<DependencyResolutionResult>(StatusCodes.Status400BadRequest, "Invalid identifiers.");
            }

            // I'm not gonna bother doing dependency resolution if there is nothing to resolve.
            if (!identifiers.Any())
            {
                return new HiveObjectQuery<DependencyResolutionResult>(StatusCodes.Status400BadRequest, "No mods were provided; no dependency resolution can occur.");
            }

            log.Debug("Finding mods from parsed identifiers...");

            // We iterate through each mod identifier, then attempt to grab mods that match them.
            var mods = new List<Mod>() { };
            var query = context.Mods;

            foreach (var identifier in identifiers)
            {
                var targetVersion = new Version(identifier.Version);

                // We forcibly cast here because FirstOrDefaultAsync is ambigious with System.Linq otherwise.
                var mod = await (query as IQueryable<Mod>)
                    .FirstOrDefaultAsync(m => m.ReadableID == identifier.ID && m.Version == targetVersion)
                    .ConfigureAwait(false);

                if (mod is null)
                {
                    return new HiveObjectQuery<DependencyResolutionResult>(StatusCodes.Status404NotFound, $"Could not find a Mod in that matches identifier \"{identifier}\".");
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
                return new HiveObjectQuery<DependencyResolutionResult>(StatusCodes.Status424FailedDependency, result);
            }

            result.Message = "Dependency Resolution completed.";

            return new HiveObjectQuery<DependencyResolutionResult>(StatusCodes.Status200OK, result);
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

            public ModReference CreateRef(string id, VersionRange range) => new(id, range);

            // Comparisons
            public bool Matches(VersionRange range, Version version) => range.Matches(version);

            public int Compare(Version a, Version b) => a.CompareTo(b);

            // Combiners
            public VersionRange Either(VersionRange a, VersionRange b) => a.Disjunction(b);

            public FSharpValueOption<VersionRange> And(VersionRange a, VersionRange b)
            {
                var res = a & b;
                if (res == VersionRange.Nothing)
                    return FSharpValueOption<VersionRange>.None;
                return FSharpValueOption<VersionRange>.Some(res);
            }

            public VersionRange Not(VersionRange a) => a.Invert();

            // External
            public async Task<IEnumerable<Mod>> ModsMatching(ModReference @ref)
            {
                var mods = context.Mods;

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
