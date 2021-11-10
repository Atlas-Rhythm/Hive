using DryIoc;
using Hive.Models;
using Hive.Permissions;
using Hive.Services.Common;
using Hive.Utilities;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Hive.Tests.TestHelpers;

namespace Hive.Tests.Endpoints
{
    public class ResolveDependenciesController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly Channel DefaultChannel = new()
        {
            Name = "Public"
        };

        // Build a simple-ish dependency tree.
        // BeatSaverDownloader explicitly/implicitly depends on all other mods in this list. We'll use this later.
        private static readonly IEnumerable<Mod> defaultMods = new List<Mod>()
        {
            GetPlaceholderMod("BSIPA"),
            GetPlaceholderMod("ScoreSaberSharp"),
            GetPlaceholderMod("BeatSaverSharp"),
            GetPlaceholderMod("BS_Utils", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0"))
            }),
            GetPlaceholderMod("BSML", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0")),
                new ModReference("BS_Utils", new VersionRange("^1.0.0"))
            }),
            GetPlaceholderMod("SongCore", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0")),
                new ModReference("BS_Utils", new VersionRange("^1.0.0")),
                new ModReference("BSML", new VersionRange("^1.0.0"))
            },
            // To test conflicts, we will have this mod conflict with BeatSaberPlus
            new List<ModReference>()
            {
                new ModReference("BeatSaberPlus", new VersionRange("^1.0.0"))
            }),
            GetPlaceholderMod("BeatSaverDownloader", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0")),
                new ModReference("SongCore", new VersionRange("^1.0.0")),
                new ModReference("BSML", new VersionRange("^1.0.0")),
                new ModReference("ScoreSaberSharp", new VersionRange("^1.0.0")),
                new ModReference("BeatSaverSharp", new VersionRange("^1.0.0")),
            }),
            // This plugin is used to check for invalid version ranges.
            GetPlaceholderMod("ChromaToggle", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^2.0.0")),
            }),
            // This plugin is used to check for conflicts
            GetPlaceholderMod("BeatSaberPlus", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0")),
            },
            // To test conflicts, we will have this mod conflict with BS Utils
            new List<ModReference>()
            {
                new ModReference("BS_Utils", new VersionRange("^1.0.0"))
            }),
            // This plugin is used to check for missing mods
            GetPlaceholderMod("Chroma", new List<ModReference>()
            {
                new ModReference("DNEE", new VersionRange("^1.0.0")),
            }),
        };

        public ResolveDependenciesController(ITestOutputHelper helper) : base(new PartialContext
        {
            Channels = new[] { DefaultChannel },
            Mods = defaultMods,
            ModLocalizations = defaultMods.SelectMany(m => m.Localizations)
        })
        {
            this.helper = helper;
        }

        [Fact]
        public async Task ResolveDependenciesStandard()
        {
            var controller = CreateController("next(true)");

            // Our test user wants BeatSaverDownloader, and needs to obtain all of the dependencies for it.
            var input = new ModIdentifier[]
            {
                new ModIdentifier
                {
                    ID = "BeatSaverDownloader",
                    Version = "1.0.0"
                }
            };

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<OkObjectResult>(res.Result); // This endpoint must succeed
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var resolutionResult = result!.Value as DependencyResolutionResult; // Get our dependency resolution result
            Assert.All(defaultMods, (m) => resolutionResult?.AdditionalMods.Contains(m)); // Ensure that every input mod is included in our result, since BeatSaverDownloader (explicitly or implicitly) depends on all test input mods
        }

        [Fact]
        public async Task ResolveDependenciesPermissionFailure()
        {
            var controller = CreateController("next(false)");

            // Input doesn't matter as much, since our test is defined to fail at the permission check anyways.
            var input = Array.Empty<ModIdentifier>();

            var res = await controller.ResolveDependencies(input);

            AssertNotNull(res.Result); // Make sure we got a request back
            AssertForbid(res.Result); // This endpoint must fail at the permissions check.
        }

        [Fact]
        public async Task ResolveDependenciesPluginFailure()
        {
            var controller = CreateController("next(true)", new[] { new BullyPlugin() });

            // Input doesn't matter as much, since our test is defined to fail at the plugin check anyways.
            var input = Array.Empty<ModIdentifier>();

            var res = await controller.ResolveDependencies(input);

            AssertNotNull(res.Result); // Make sure we got a request back
            AssertForbid(res.Result); // This endpoint must fail at the plugin check.
        }

        [Fact]
        public async Task ResolveDependenciesBlankRequestFailure()
        {
            var controller = CreateController("next(true)");

            // Whoopsies, we forgot to set our body.
            var res = await controller.ResolveDependencies(Array.Empty<ModIdentifier>());

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<BadRequestObjectResult>(res.Result); // This endpoint must fail because we have no identifiers
        }

        [Fact]
        public async Task ResolveDependenciesNonexistentMod()
        {
            var controller = CreateController("next(true)");

            var input = new ModIdentifier[]
            {
                // Wait a minute. This mod doesn't exist!
                new ModIdentifier
                {
                    ID = "sc2ad check your github notifications",
                    Version = "0.69.420"
                }
            };

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<NotFoundObjectResult>(res.Result); // This endpoint must fail because the mod could not be found
        }

        [Fact]
        public async Task ResolveDependenciesVersionMismatchError()
        {
            var controller = CreateController("next(true)");

            var input = new ModIdentifier[]
            {
                // The ChromaToggle mod does exist, however its dependencies aren't set up properly.
                new ModIdentifier
                {
                    ID = "ChromaToggle",
                    Version = "1.0.0"
                }
            };

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<ObjectResult>(res.Result); // This endpoint must fail at the dependency resolution phase
            var result = res.Result as ObjectResult;
            Assert.Equal(result!.StatusCode, StatusCodes.Status424FailedDependency); // We should be given 424 error
            var dependencyResult = result.Value as DependencyResolutionResult;
            Assert.NotEmpty(dependencyResult!.VersionMismatches); // Make sure we have a version mismatch.
            var mod = dependencyResult!.VersionMismatches.First();
            Assert.Equal("BSIPA", mod.ModID); // Ensure that the mismatched mod is BSIPA; we ask for 2.0.0 when only 1.0.0 exists.
        }

        [Fact]
        public async Task ResolveDependenciesWithModConflict()
        {
            var controller = CreateController("next(true)");

            // Our inputs will be two mods, where one mod conflicts with the other.
            var input = new ModIdentifier[]
            {
                new ModIdentifier
                {
                    ID = "BeatSaberPlus",
                    Version = "1.0.0"
                },
                new ModIdentifier
                {
                    ID = "SongCore",
                    Version = "1.0.0"
                },
            };

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<ObjectResult>(res.Result); // This endpoint must fail at the dependency resolution phase
            var result = res.Result as ObjectResult;
            Assert.Equal(result!.StatusCode, StatusCodes.Status424FailedDependency); // We should be given 424 error
            var dependencyResult = result.Value as DependencyResolutionResult;
            Assert.NotEmpty(dependencyResult!.ConflictingMods); // Make sure we have a conflicting mod.
            var mod = dependencyResult!.ConflictingMods.First();
            Assert.Equal("BS_Utils", mod); // Ensure that the conflicting mod is BS_Utils; BS+ conflicts with it while SongCore depends on it
        }

        [Fact]
        public async Task ResolveDependenciesWithMissingMod()
        {
            var controller = CreateController("next(true)");

            // Our input will be a mod that depends on a non-existent mod
            var input = new ModIdentifier[]
            {
                new ModIdentifier
                {
                    ID = "Chroma",
                    Version = "1.0.0"
                },
            };

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<ObjectResult>(res.Result); // This endpoint must fail at the dependency resolution phase
            var result = res.Result as ObjectResult;
            Assert.Equal(result!.StatusCode, StatusCodes.Status424FailedDependency); // We should be given 424 error
            var dependencyResult = result.Value as DependencyResolutionResult;
            Assert.NotEmpty(dependencyResult!.MissingMods); // Make sure that DNEE is not found.
            var mod = dependencyResult!.MissingMods.First();
            Assert.Equal("DNEE", mod.ModID); // Ensure that DNEE is not found, it does not exist in our list of default mods.
        }

        private Controllers.ResolveDependenciesController CreateController(string permissionRule, IEnumerable<IResolveDependenciesPlugin>? plugins = null)
        {
            var container = DIHelper.ConfigureServices(Options, _ => { }, helper, new ResolveDependenciesRuleProvider(permissionRule));

            plugins ??= Enumerable.Empty<IResolveDependenciesPlugin>();

            container.RegisterInstance(plugins);
            container.Register<DependencyResolverService>(Reuse.Scoped);
            container.Register<Controllers.ResolveDependenciesController>(Reuse.Scoped);

            var scope = container.CreateScope();

            var controller = scope.ServiceProvider.GetRequiredService<Controllers.ResolveDependenciesController>();

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // I need to set up a "proper" Mod object so that the controller won't throw a fit
        // from (understandably) having missing data.
        private static User DummyUser = null!;
        private static Mod GetPlaceholderMod(string name, IList<ModReference> dependencies = null!, IList<ModReference> conflicts = null!)
        {
            DummyUser ??= new User() { Username = "Billy bob joe", AlternativeId = "bbj altid" };
            var mod = new Mod()
            {
                ReadableID = name,
                Version = new Versioning.Version(1, 0, 0),
                UploadedAt = new Instant(),
                EditedAt = null,
                Uploader = DummyUser,
                Channel = DefaultChannel,
                DownloadLink = new Uri("https://www.github.com/Atlas-Rhythm/Hive"),
                Dependencies = dependencies ?? new List<ModReference>(),
                Conflicts = conflicts ?? new List<ModReference>()
            };

            var info = new LocalizedModInfo()
            {
                OwningMod = mod,
                Language = CultureInfo.CurrentCulture.ToString(),
                Name = name,
                Description = "if you read this, william gay"
            };

            mod.Localizations.Add(info);

            return mod;
        }

        private class BullyPlugin : IResolveDependenciesPlugin
        {
            public bool GetAdditionalChecks(User? _) => false;
        }

        private class ResolveDependenciesRuleProvider : IRuleProvider
        {
            private readonly string permissionRule;

            public ResolveDependenciesRuleProvider(string permissionRule)
            {
                this.permissionRule = permissionRule;
            }

            public bool HasRuleChangedSince(StringView name, Instant time) => true;

            public bool HasRuleChangedSince(Rule rule, Instant time) => true;

            public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
            {
                var nameString = name.ToString();
                if (name == "hive.resolve_dependencies")
                {
                    gotten = new Rule(nameString, permissionRule);
                    return true;
                }
                else
                {
                    gotten = new Rule(nameString, "next(false)");
                    return true;
                }
            }
        }
    }
}
