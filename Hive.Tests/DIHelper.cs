using Hive.Models;
using Hive.Permissions;
using Hive.Services;
using Hive.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Hive.Tests
{
    public static class DIHelper
    {
        /// <summary>
        /// Configures a <see cref="ServiceCollection"/> for usage in a test.
        /// Provides default services for many options.
        /// </summary>
        /// <param name="outputHelper">The output helper to log Hive.Permissions to.</param>
        /// <param name="ruleProvider">The rule provider for the <see cref="PermissionsManager{TContext}"/>.</param>
        /// <param name="builtIns">The built in functions to provide to the <see cref="PermissionsManager{TContext}"/>.</param>
        /// <param name="context">The context to place in the service collection.</param>
        /// <returns>The created <see cref="ServiceCollection"/></returns>
        public static IServiceCollection ConfigureServices(
            ITestOutputHelper? outputHelper = null,
            IRuleProvider? ruleProvider = null,
            List<(string, Delegate)>? builtIns = null,
            HiveContext? context = null
            )
        {
            var services = new ServiceCollection();
            // Initial services
            services.AddSingleton<ILogger>(sp => new LoggerConfiguration().WriteTo.Debug().CreateLogger())
                .AddSingleton<IProxyAuthenticationService>(sp => new MockAuthenticationService());
            if (outputHelper != null)
                services.AddSingleton<Permissions.Logging.ILogger>(sp => new TestOutputWrapper(outputHelper));

            // Rule provider
            if (ruleProvider is null)
            {
                var mockRuleProvider = new Mock<IRuleProvider>();
                var start = SystemClock.Instance.GetCurrentInstant();
                mockRuleProvider.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
                mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
                mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
                mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
                mockRuleProvider.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
                services.AddSingleton((sp) => mockRuleProvider.Object);
            }
            else
                services.AddSingleton((sp) => ruleProvider);

            // Permissions Manager
            if (builtIns is null)
                builtIns = new List<(string, Delegate)>();
            services.AddSingleton(sp =>
                new PermissionsManager<PermissionContext>(sp.GetRequiredService<IRuleProvider>(), sp.GetService<Permissions.Logging.ILogger>(), ".", builtIns)
            );

            // Context
            if (context is null)
                context = new HiveContext();
            services.AddSingleton(sp => context);
            return services;
        }
    }
}