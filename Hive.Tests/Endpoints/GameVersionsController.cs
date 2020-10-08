using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Plugins;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class GameVersionsController
    {
        private readonly ITestOutputHelper helper;

        private static IEnumerable<GameVersion> defaultGameVersions = new List<GameVersion>()
        {
            new GameVersion() { Name = "1.10.0" },
            new GameVersion() { Name = "1.11.0" },
            new GameVersion() { Name = "1.12.0-beta" }
        };

        private static IEnumerable<IGameVersionsPlugin> defaultPlugins = new List<IGameVersionsPlugin>()
        {
            new HiveGameVersionsControllerPlugin()
        };

        public GameVersionsController(ITestOutputHelper helper)
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
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the permission rule.
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
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the plugin.
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
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Check if the result given back contains all of the versions we put into it.
            foreach (var version in defaultGameVersions)
                Assert.Contains(version, value);
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
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(defaultGameVersions.Last(), value);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (int i = 0; i < defaultGameVersions.Count() - 1; i++)
            {
                Assert.Contains(defaultGameVersions.ElementAt(i), value);
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
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(defaultGameVersions.Last(), value);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (int i = 0; i < defaultGameVersions.Count() - 1; i++)
            {
                Assert.Contains(defaultGameVersions.ElementAt(i), value);
            }
        }

        private Controllers.GameVersionsController CreateController(string permissionRule, IEnumerable<IGameVersionsPlugin> plugins)
        {
            var services = DIHelper.ConfigureServices(
                helper,
                new GameVersionRuleProvider(permissionRule),
                new List<(string, Delegate)>
                {
                    ("isNull", new Func<object?, bool>(o => o is null)),
                    ("isNotBeta", new Func<string, bool>(s => !s.Contains("beta", StringComparison.InvariantCultureIgnoreCase)))
                },
                new HiveContext()
                {
                    GameVersions = GetGameVersions(defaultGameVersions.AsQueryable()).Object
                });

            services
                .AddTransient(sp => plugins)
                .AddScoped<Controllers.GameVersionsController>()
                .AddAggregates();

            return services.BuildServiceProvider().GetRequiredService<Controllers.GameVersionsController>();
        }

        // Taken from sc2ad's test for ChannelsControllers
        private static Mock<DbSet<GameVersion>> GetGameVersions(IQueryable<GameVersion> versions)
        {
            var channelSet = new Mock<DbSet<GameVersion>>();
            channelSet.As<IEnumerable<GameVersion>>()
                .Setup(m => m.GetEnumerator())
                .Returns(versions.GetEnumerator());
            channelSet.As<IQueryable<GameVersion>>()
                .Setup(m => m.Provider)
                .Returns(versions.Provider);
            channelSet.As<IQueryable<GameVersion>>().Setup(m => m.Expression).Returns(versions.Expression);
            channelSet.As<IQueryable<GameVersion>>().Setup(m => m.ElementType).Returns(versions.ElementType);
            channelSet.As<IQueryable<GameVersion>>().Setup(m => m.GetEnumerator()).Returns(versions.GetEnumerator());

            return channelSet;
        }

        private class DenyUserAccessPlugin : IGameVersionsPlugin
        {
            public bool GetGameVersionsAdditionalChecks(User? _) => false; // If it is active, restrict access.
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
                string nameString = name.ToString();
                switch (nameString)
                {
                    case "hive":
                        gotten = new Rule(nameString, "next(false)");
                        return true;
                    case "hive.game.version":
                        gotten = new Rule(nameString, permissionRule);
                        return true;
                    default:
                        gotten = null;
                        return false;
                }
            }
        }
    }
}
