using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Services;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NodaTime;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Tests.Endpoints
{
    public class GameVersionsController
    {
        [Fact]
        public async Task PermissionForbid()
        {
            var plugin = CreateDefaultPlugin();
            var versions = CreateVersionList();

            var controller = CreateController("next(false)", versions, plugin);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the permission rule.
        }

        [Fact]
        public async Task PluginDeny()
        {
            var plugin = CreateMockPlugin(); // Let's create a new plugin.
            plugin.Setup(p => p.GetGameVersionsAdditionalChecks(It.IsAny<User>())).Returns(false); // This plugin will deny user access.
            plugin.Setup(p => p.GetGameVersionsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<GameVersion>>()))
                .Returns((User u, IEnumerable<GameVersion> v) => v); // This plugin will not filter out any channels.
            var versions = CreateVersionList();

            var controller = CreateController("next(true)", versions, plugin.Object);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the plugin.
        }

        [Fact]
        public async Task Standard()
        {
            var plugin = CreateDefaultPlugin();
            var versions = CreateVersionList();

            var controller = CreateController("next(true)", versions, plugin);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Check if the result given back contains all of the versions we put into it.
            foreach (var version in versions)
                Assert.Contains(version, value);
        }

        [Fact]
        public async Task StandardFilter()
        {
            var plugin = CreateDefaultPlugin();
            var versions = CreateVersionList();

            // The isNull check is for the permission check on the entire endpoint
            // The doesNotContain check is to filter out versions marked as "beta"
            var controller = CreateController("isNull(ctx.GameVersion) | isNotBeta(ctx.GameVersion.Name) | next(false)", versions, plugin);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, and the permission rule gives us all public versions.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(versions.Last(), value);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (int i = 0; i < versions.Count() - 1; i++)
            {
                Assert.Contains(versions.ElementAt(i), value);
            }
        }

        [Fact]
        public async Task PluginFilter()
        {
            var plugin = CreateMockPlugin(); // Let's create a new plugin.
            // This plugin does not modify who can access the endpoint.
            plugin.Setup(p => p.GetGameVersionsAdditionalChecks(It.IsAny<User>())).Returns(true);
            // However, this plugin does filter what game versions the user can access.
            // More specifically, it will filter out all versions marked as "beta".
            plugin.Setup(p => p.GetGameVersionsFilter(It.IsAny<User>(), It.IsAny<IEnumerable<GameVersion>>()))
                .Returns((User user, IEnumerable<GameVersion> g) => g
                    .Where(version => !version.Name.Contains("beta", StringComparison.InvariantCultureIgnoreCase)));
            var versions = CreateVersionList();

            // Create our controller like normal, and allow everything.
            var controller = CreateController("next(true)", versions, plugin.Object);
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<OkObjectResult>(res.Result); // The above endpoint must succeed, and the plugin will gives us all public versions.
            var result = res.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as IEnumerable<GameVersion>;
            Assert.NotNull(value); // We must be given a list of versions back.

            // Should only contain non-beta versions
            Assert.DoesNotContain(versions.Last(), value);
            // Should contain all public versions (in our case, all versions except the very last one)
            for (int i = 0; i < versions.Count() - 1; i++)
            {
                Assert.Contains(versions.ElementAt(i), value);
            }
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

        private static Controllers.GameVersionsController CreateController(string permissionRule, IQueryable<GameVersion> versions, IGameVersionsPlugin plugin)
        {
            var logger = new LoggerConfiguration().WriteTo.Debug().CreateLogger();
            var ruleProvider = CreateMockRuleProvider();
            var hiveRule = new Rule("hive", "next(false)");
            var r = new Rule("hive.game.version", permissionRule);
            ruleProvider.Setup(m => m.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            ruleProvider.Setup(m => m.TryGetRule(r.Name, out r)).Returns(true);
            var manager = new PermissionsManager<PermissionContext>(ruleProvider.Object, new List<(string, Delegate)>
            {
                ("isNull", new Func<object?, bool>(o => o is null)),
                ("isNotBeta", new Func<string, bool>(s => !s.Contains("beta", StringComparison.InvariantCultureIgnoreCase)))
            });

            var mockVersions = GetGameVersions(versions);
            var mockContext = new Mock<HiveContext>();
            mockContext.Setup(c => c.GameVersions).Returns(mockVersions.Object);
            var gameVersionPlugins = new SingleAggregate<IGameVersionsPlugin>(plugin);
            var authService = new MockAuthenticationService();

            return new Controllers.GameVersionsController(logger, manager, mockContext.Object, gameVersionPlugins, authService);
        }

        // Taken from sc2ad's test for ChannelsControllers
        private static Mock<IRuleProvider> CreateMockRuleProvider()
        {
            var mock = new Mock<IRuleProvider>();

            var start = SystemClock.Instance.GetCurrentInstant();
            mock.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
            return mock;
        }

        private static Mock<IGameVersionsPlugin> CreateMockPlugin() => new Mock<IGameVersionsPlugin>();
        
        private static IGameVersionsPlugin CreateDefaultPlugin() => new HiveGameVersionsControllerPlugin();

        private static IQueryable<GameVersion> CreateVersionList() => new List<GameVersion>()
        {
            new GameVersion() { Name = "1.10.0" },
            new GameVersion() { Name = "1.11.0" },
            new GameVersion() { Name = "1.12.0-beta" }
        }.AsQueryable();
    }
}
