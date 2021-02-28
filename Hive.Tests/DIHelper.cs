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
using Xunit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using GraphQL;
using System.Threading.Tasks;
using Hive.Graphing;

namespace Hive.Tests
{
    public static class DIHelper
    {
        internal static JsonElement EmptyAdditionalData => JsonDocument.Parse("{}").RootElement.Clone();

        /// <summary>
        /// Configures a <see cref="ServiceCollection"/> for usage in a test.
        /// Provides default services for many options.
        /// </summary>
        /// <param name="options">The <see cref="DbContextOptions{TContext}"/> to use for this test.</param>
        /// <param name="outputHelper">The output helper to log Hive.Permissions to.</param>
        /// <param name="ruleProvider">The rule provider for the <see cref="PermissionsManager{TContext}"/>.</param>
        /// <param name="builtIns">The built in functions to provide to the <see cref="PermissionsManager{TContext}"/>.</param>
        /// <param name="context">Additional data to explicitly add to the database.</param>
        /// <returns>The created <see cref="ServiceCollection"/></returns>
        internal static IServiceCollection ConfigureServices(
            DbContextOptions<HiveContext> options,
            ITestOutputHelper? outputHelper = null,
            IRuleProvider? ruleProvider = null,
            List<(string, Delegate)>? builtIns = null,
            PartialContext? context = null
            )
        {
            var services = new ServiceCollection();
            // Initial services
            services.AddSingleton<ILogger>(sp => new LoggerConfiguration().WriteTo.Debug().CreateLogger())
                .AddSingleton<IProxyAuthenticationService>(sp => new MockAuthenticationService())
                .AddSingleton<IClock>(SystemClock.Instance);
            if (outputHelper != null)
                services.AddSingleton<Permissions.Logging.ILogger>(sp => new TestOutputWrapper(outputHelper));

            // Rule provider
            if (ruleProvider is null)
            {
                var mockRuleProvider = CreateRuleProvider();
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

            // Context and DB
            // Using the created DB, create exactly one singleton object that lives on it.
            var dbContext = new HiveContext(options);
            // Used for ensuring objects exist in context
            if (context is not null)
            {
                DbHelper.CopyData(context, dbContext);
            }
            // This singleton SHOULD be properly initialized to operate with the test DB.
            // There should only ever be one per service collection, so this checks out.
            services.AddSingleton(sp => dbContext);
            return services;
        }

        /// <summary>
        /// Creates a default <see cref="IRuleProvider"/> which has no rules and uses the current time.
        /// </summary>
        /// <returns>The mocked rule provider for further edits.</returns>
        internal static Mock<IRuleProvider> CreateRuleProvider()
        {
            var mockRuleProvider = new Mock<IRuleProvider>();
            var start = SystemClock.Instance.GetCurrentInstant();
            mockRuleProvider.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
            mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
            mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
            mockRuleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
            mockRuleProvider.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
            return mockRuleProvider;
        }

        internal class TestAccessor : IHttpContextAccessor
        {
            public TestAccessor(HttpContext httpContext) => HttpContext = httpContext;

            public HttpContext? HttpContext { get; set; }
        }

        internal static async Task<ExecutionResult> ExecuteGraphAsync(this ServiceProvider provider, string query)
        {
            var executer = provider.GetRequiredService<IDocumentExecuter>();
            var schema = provider.GetRequiredService<HiveSchema>();
            return await executer.ExecuteAsync(_ =>
            {
                _.Query = query;
                _.Schema = schema;
                _.RequestServices = provider;
            });
        }
    }
}
