using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Utilities;
using Hive.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class ResolveDependenciesController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly Channel DefaultChannel = new Channel
        {
            Name = "Public",
            AdditionalData = DIHelper.EmptyAdditionalData
        };

        // Build a simple-ish dependency tree.
        // BeatSaverDownloader explicitly/implicitly depends on all other mods in this list. We'll use this later.
        private static readonly IEnumerable<Mod> defaultMods = new List<Mod>()
        {
            GetPlaceholderMod("BSIPA", new List<ModReference>() { }),
            GetPlaceholderMod("ScoreSaberSharp", new List<ModReference>() { }),
            GetPlaceholderMod("BeatSaverSharp", new List<ModReference>() { }),
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
            }),
            GetPlaceholderMod("BeatSaverDownloader", new List<ModReference>()
            {
                new ModReference("BSIPA", new VersionRange("^1.0.0")),
                new ModReference("SongCore", new VersionRange("^1.0.0")),
                new ModReference("BSML", new VersionRange("^1.0.0")),
                new ModReference("ScoreSaberSharp", new VersionRange("^1.0.0")),
                new ModReference("BeatSaverSharp", new VersionRange("^1.0.0")),
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
            var resolutionResult = result!.Value as Controllers.ResolveDependenciesController.DependencyResolutionResult; // Get our dependency resolution result
            Assert.All(defaultMods, (m) => resolutionResult?.AdditionalMods.Contains(m)); // Ensure that every input mod is included in our result, since BeatSaverDownloader (explicitly or implicitly) depends on all test input mods
        }

        [Fact]
        public async Task ResolveDependenciesPermissionFailure()
        {
            var controller = CreateController("next(false)");

            // Input doesn't matter as much, since our test is defined to fail at the permission check anyways.
            var input = Array.Empty<ModIdentifier>();

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<ForbidResult>(res.Result); // This endpoint must fail at the permissions check.
        }

        [Fact]
        public async Task ResolveDependenciesPluginFailure()
        {
            var controller = CreateController("next(true)", new[] { new BullyPlugin() });

            // Input doesn't matter as much, since our test is defined to fail at the plugin check anyways.
            var input = Array.Empty<ModIdentifier>();

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<ForbidResult>(res.Result); // This endpoint must fail at the plugin check.
        }

        [Fact]
        public async Task ResolveDependenciesBlankListFailure()
        {
            var controller = CreateController("next(true)");

            // We're inputting an empty list, which would result in a BadRequest since there is nothing to resolve.
            var input = Array.Empty<ModIdentifier>();

            var res = await controller.ResolveDependencies(input);

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<BadRequestObjectResult>(res.Result); // This endpoint must fail.
        }

        [Fact]
        public async Task ResolveDependenciesBlankRequestFailure()
        {
            var controller = CreateController("next(true)");

            // Whoopsies, we forgot to set our body.
            var res = await controller.ResolveDependencies(Array.Empty<ModIdentifier>());

            Assert.NotNull(res.Result); // Make sure we got a request back
            Assert.IsType<BadRequestObjectResult>(res.Result); // This endpoint must fail.
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
            Assert.IsType<BadRequestObjectResult>(res.Result); // This endpoint must fail
        }

        private Controllers.ResolveDependenciesController CreateController(string permissionRule, IEnumerable<IResolveDependenciesPlugin>? plugins = null)
        {
            var services = DIHelper.ConfigureServices(Options, helper, new ResolveDependenciesRuleProvider(permissionRule));

            if (plugins is null)
            {
                plugins = new[] { new HiveResolveDependenciesControllerPlugin() };
            }
            else
            {
                plugins = plugins.Prepend(new HiveResolveDependenciesControllerPlugin());
            }

            services
                .AddTransient(sp => plugins)
                .AddScoped<Controllers.ResolveDependenciesController>()
                .AddAggregates();

            return services.BuildServiceProvider().GetRequiredService<Controllers.ResolveDependenciesController>();
        }

        // I need to set up a "proper" Mod object so that the controller won't throw a fit
        // from (understandably) having missing data.
        private static Mod GetPlaceholderMod(string name, IList<ModReference> dependencies)
        {
            var mod = new Mod()
            {
                ReadableID = name,
                Version = new Versioning.Version(1, 0, 0),
                UploadedAt = new Instant(),
                EditedAt = null,
                Uploader = new User() { DumbId = new Random().Next(0, 69).ToString(), Username = "Billy bob joe" },
                Channel = DefaultChannel,
                DownloadLink = new Uri("https://www.github.com/Atlas-Rhythm/Hive"),
                AdditionalData = DIHelper.EmptyAdditionalData,
                Dependencies = dependencies
            };

            LocalizedModInfo info = new LocalizedModInfo()
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
                string nameString = name.ToString();
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
