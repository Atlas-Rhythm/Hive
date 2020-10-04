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

namespace Hive.Tests.Endpoints
{
    public class GameVersionsController
    {
        private ServiceProvider serviceProvider { get; set; }

        // oh god help me idk how to improve this
        [ThreadStatic]
        private static string gameVersionPermissionRule = "next(false)";

        private static IEnumerable<GameVersion> defaultGameVersions = new List<GameVersion>()
        {
            new GameVersion() { Name = "1.10.0" },
            new GameVersion() { Name = "1.11.0" },
            new GameVersion() { Name = "1.12.0-beta" }
        };

        public GameVersionsController()
        {
            var services = new ServiceCollection();
            services
                .AddScoped<IRuleProvider>(sp => new GameVersionRuleProvider())
                .AddTransient<Permissions.Logging.ILogger, Logging.PermissionsProxy>()
                .AddTransient<ILogger>(sp => new LoggerConfiguration().WriteTo.Debug().CreateLogger())
                .AddScoped(sp =>
                    new PermissionsManager<PermissionContext>(
                        sp.GetRequiredService<IRuleProvider>(),
                        sp.GetService<Permissions.Logging.ILogger>(),
                        ".",
                        new List<(string, Delegate)>
                        {
                            ("isNull", new Func<object?, bool>(o => o is null)),
                            ("isNotBeta", new Func<string, bool>(s => !s.Contains("beta", StringComparison.InvariantCultureIgnoreCase)))
                        }
                    )
                )
                //.AddSingleton<IProxyAuthenticationService>(sp => new VaulthAuthenticationService(sp.GetService<Serilog.ILogger>(), sp.GetService<IConfiguration>()));
                .AddTransient<IProxyAuthenticationService>(sp => new MockAuthenticationService())
                .AddTransient<IGameVersionsPlugin>(sp => new HiveGameVersionsControllerPlugin())
                .AddTransient<IEnumerable<IGameVersionsPlugin>>(sp => new List<IGameVersionsPlugin>()
                {
                    new HiveGameVersionsControllerPlugin(), // Default implementation plugin
                    new BullyPlugin(), // Gives empty list of game versions
                    new DenyUserAccessPlugin(), // Denies user access to the endpoint
                    new FilterBetaVersionsPlugin() // Filters all beta game versions
                })
                .AddScoped(sp => new HiveContext() { GameVersions = GetGameVersions(defaultGameVersions.AsQueryable()).Object })
                .AddScoped<Controllers.GameVersionsController>();

            services.AddAggregates();

            services.AddAuthentication(a =>
            {
                a.AddScheme<MockAuthenticationHandler>("Bearer", "MockAuth");
                a.DefaultScheme = "Bearer";
            });

            serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task PermissionForbid()
        {
            // Reset controller state.
            gameVersionPermissionRule = "next(false)";
            BullyPlugin.IsActive = DenyUserAccessPlugin.IsActive = FilterBetaVersionsPlugin.IsActive = false;

            var controller = serviceProvider.GetRequiredService<Controllers.GameVersionsController>();
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the permission rule.
        }

        [Fact]
        public async Task PluginDeny()
        {
            gameVersionPermissionRule = "next(true)"; // This would usually allow user access, but should be blocked by plugin.
            BullyPlugin.IsActive = FilterBetaVersionsPlugin.IsActive = false;
            DenyUserAccessPlugin.IsActive = true; // This plugin should immediately block access to the endpoint.

            var controller = serviceProvider.GetRequiredService<Controllers.GameVersionsController>();
            var res = await controller.GetGameVersions();

            Assert.NotNull(res); // Result must not be null.
            Assert.NotNull(res.Result);
            Assert.IsType<ForbidResult>(res.Result); // The above endpoint must fail from the plugin.
        }

        [Fact]
        public async Task Standard()
        {
            // Reset controller state.
            gameVersionPermissionRule = "next(true)";
            BullyPlugin.IsActive = DenyUserAccessPlugin.IsActive = FilterBetaVersionsPlugin.IsActive = false;

            var controller = serviceProvider.GetRequiredService<Controllers.GameVersionsController>();
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
            gameVersionPermissionRule = "isNull(ctx.GameVersion) | isNotBeta(ctx.GameVersion.Name) | next(false)";
            BullyPlugin.IsActive = DenyUserAccessPlugin.IsActive = FilterBetaVersionsPlugin.IsActive = false;

            // The isNull check is for the permission check on the entire endpoint
            // The doesNotContain check is to filter out versions marked as "beta"
            var controller = serviceProvider.GetRequiredService<Controllers.GameVersionsController>();
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
            // Reset controller state.
            gameVersionPermissionRule = "next(true)";
            BullyPlugin.IsActive = DenyUserAccessPlugin.IsActive = false;
            FilterBetaVersionsPlugin.IsActive = true; // This plugin will do the same behavior as FilterViaPermissionRule.

            // Create our controller like normal, and allow everything.
            var controller = serviceProvider.GetRequiredService<Controllers.GameVersionsController>();
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

        private class BullyPlugin : IGameVersionsPlugin
        {
            [ThreadStatic]
            public static bool IsActive = false;

            public IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions)
            {
                if (!IsActive) return versions;
                return new List<GameVersion>() { };
            }
        }

        private class DenyUserAccessPlugin : IGameVersionsPlugin
        {
            [ThreadStatic]
            public static bool IsActive = false;

            public bool GetGameVersionsAdditionalChecks(User? _) => !IsActive; // If it is active, restrict access.
        }

        private class FilterBetaVersionsPlugin : IGameVersionsPlugin
        {
            [ThreadStatic]
            public static bool IsActive = false;

            public IEnumerable<GameVersion> GetGameVersionsFilter(User? user, [TakesReturnValue] IEnumerable<GameVersion> versions)
            {
                if (!IsActive) return versions;
                return versions.Where(x => !x.Name.Contains("beta", StringComparison.InvariantCultureIgnoreCase));
            }
        }

        private class GameVersionRuleProvider : IRuleProvider
        {
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
                        gotten = new Rule(nameString, gameVersionPermissionRule);
                        return true;
                    default:
                        gotten = null;
                        return false;
                }
            }
        }
    }
}
