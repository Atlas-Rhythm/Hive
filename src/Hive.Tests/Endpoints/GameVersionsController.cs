using DryIoc;
using Hive.Models;
using Hive.Models.Serialized;
using Hive.Permissions;
using Hive.Services.Common;
using Hive.Plugins.Aggregates;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Hive.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;

namespace Hive.Tests.Endpoints
{
    public class GameVersionsController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        private static readonly IEnumerable<GameVersion> defaultGameVersions = new List<GameVersion>()
        {
            new GameVersion() { Name = "1.10.0" },
            new GameVersion() { Name = "1.11.0" },
            new GameVersion() { Name = "1.12.0-beta" }
        };

        private static readonly IEnumerable<IGameVersionsPlugin> defaultPlugins = new List<IGameVersionsPlugin>()
        {
            new HiveGameVersionsControllerPlugin()
        };

        public GameVersionsController(ITestOutputHelper helper) : base(new PartialContext
        {
            GameVersions = defaultGameVersions
        })
        {
            this.helper = helper;
        }

        [Fact]
        public async Task PermissionForbid()
        {
            var controller = CreateController("next(false)", defaultPlugins);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            AssertForbid(res.Result); // The above endpoint must fail from the permission rule.
        }

        [Fact]
        public async Task PluginDeny()
        {
            var controller = CreateController("next(true)", // This would usually allow user access, but should be blocked by plugin.
                new List<IGameVersionsPlugin>()
                    {
                        new HiveGameVersionsControllerPlugin(),
                        new DenyUserAccessPlugin(), // This plugin should deny user access.
                    });
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            AssertForbid(res.Result); // The above endpoint must fail from the plugin.
        }

        [Fact]
        public async Task Standard()
        {
            var controller = CreateController("next(true)", defaultPlugins);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<SerializedGameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Check if the result given back contains all of the versions we put into it.
            foreach (var version in defaultGameVersions)
                Assert.Contains(value, item => item.Name == version.Name);
        }

        [Fact]
        public async Task FilterViaPermissionRule()
        {
            // This rule will filter out all beta versions from our list.
            // See the constructor for impls of "isNull" and "isNotBeta"
            var controller = CreateController("isNull(ctx.GameVersion) | isNotBeta(ctx.GameVersion.Name) | next(false)", defaultPlugins);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, and the permission rule gives us all public versions.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<SerializedGameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(value, item => item.Name == defaultGameVersions.Last().Name);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (var i = 0; i < defaultGameVersions.Count() - 1; i++)
            {
                Assert.Contains(value, item => item.Name == defaultGameVersions.ElementAt(i).Name);
            }
        }

        [Fact]
        public async Task FilterViaPlugin()
        {
            var controller = CreateController("next(true)",
                new List<IGameVersionsPlugin>()
                    {
                        new HiveGameVersionsControllerPlugin(),
                        new FilterBetaVersionsPlugin(), // This plugin should filter beta game versions
                    });
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, and the plugin will gives us all public versions.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<SerializedGameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(value, item => item.Name == defaultGameVersions.Last().Name);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (var i = 0; i < defaultGameVersions.Count() - 1; i++)
            {
                Assert.Contains(value, item => item.Name == defaultGameVersions.ElementAt(i).Name);
            }
        }

        [Fact]
        public async Task CreateNewVersion()
        {
            // Yeah we're just going to simply make a new game version and call it a day.
            var controller = CreateController("next(true)", defaultPlugins);
            controller.ControllerContext.HttpContext = CreateMockRequest(GenerateStreamFromString("1.13.2"));

            var res = await controller.CreateGameVersion(new InputGameVersion("1.13.2", new ArbitraryAdditionalData()));

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as SerializedGameVersion;
            Assert.NotNull(value); // We must be given one GameVersion back whose name matches our input
            Assert.True(value!.Name == "1.13.2");
        }

        [Fact]
        public async Task CreateNewVersionUnauthorized()
        {
            var controller = CreateController("next(true)", defaultPlugins);

            // Whoops, we "forgot" to assign a user.
            var res = await controller.CreateGameVersion(new InputGameVersion("1.13.3", new ArbitraryAdditionalData()));

            Assert.NotNull(res);
            // Should fail.
            Assert.IsType<UnauthorizedResult>(res.Result);
        }

        private Controllers.GameVersionsController CreateController(string permissionRule, IEnumerable<IGameVersionsPlugin> plugins)
        {
            var container = DIHelper.ConfigureServices(
                Options,
                _ => { },
                helper,
                new GameVersionRuleProvider(permissionRule),
                new List<(string, Delegate)>
                {
                    ("isNull", new Func<object?, bool>(o => o is null)),
                    ("isNotBeta", new Func<string, bool>(s => !s.Contains("beta", StringComparison.InvariantCultureIgnoreCase)))
                });

            container.RegisterInstance(plugins);
            container.Register<GameVersionService>(Reuse.Scoped);
            container.Register<Controllers.GameVersionsController>(Reuse.Scoped);

            var scope = container.CreateScope();

            var controller = scope.ServiceProvider.GetRequiredService<Controllers.GameVersionsController>();

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        private class DenyUserAccessPlugin : IGameVersionsPlugin
        {
            public bool ListGameVersionsAdditionalChecks(User? _) => false; // If it is active, restrict access.
        }

        private class FilterBetaVersionsPlugin : IGameVersionsPlugin
        {
            public IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions)
            {
                return versions.Where(x => !x.Name.Contains("beta", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        private class GameVersionRuleProvider : IRuleProvider
        {
            private readonly string permissionRule;

            public GameVersionRuleProvider(string permissionRule)
            {
                this.permissionRule = permissionRule;
            }

            public bool HasRuleChangedSince(StringView name, Instant time) => true;

            public bool HasRuleChangedSince(Rule rule, Instant time) => true;

            public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
            {
                var nameString = name.ToString();
                switch (nameString)
                {
                    case "hive":
                        gotten = new Rule(nameString, "next(false)");
                        return true;

                    default:
                        gotten = new Rule(nameString, permissionRule);
                        return true;
                }
            }
        }
    }
}
