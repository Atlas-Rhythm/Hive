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
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using GraphQL;
using System.Threading.Tasks;
using Hive.Graphing;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Hive.Plugins.Aggregates;

namespace Hive.Tests
{
    public static class DIHelper
    {
        internal static Dictionary<string, JsonElement> EmptyAdditionalSet => new();
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
        internal static IContainer ConfigureServices(
            DbContextOptions<HiveContext> options,
            Action<IServiceCollection> configureServices,
            ITestOutputHelper? outputHelper = null,
            IRuleProvider? ruleProvider = null,
            List<(string, Delegate)>? builtIns = null,
            PartialContext? context = null
            )
        {
            var container = new Container(Rules.MicrosoftDependencyInjectionRules);
            container.Use<IServiceScopeFactory>(r => new DryIocServiceScopeFactory(r));

            var services = new ServiceCollection();
            configureServices(services);
            container.Populate(services);

            // Initial services
            container.RegisterInstance<ILogger>(new LoggerConfiguration().WriteTo.Debug().CreateLogger());
            container.RegisterInstance<IClock>(SystemClock.Instance);
            container.RegisterInstance(new JsonSerializerOptions()
            {
                // tuples please
                IncludeFields = true,
            });
            container.Register<IProxyAuthenticationService, MockAuthenticationService>(Reuse.Singleton);
            container.Register(typeof(IAggregate<>), typeof(Aggregate<>));
            if (outputHelper != null)
                container.Register<Permissions.Logging.ILogger, TestOutputWrapper>(Reuse.Singleton, Parameters.Of.Type(_ => outputHelper));

            // Rule provider
            if (ruleProvider is null)
            {
                var mockRuleProvider = CreateRuleProvider();
                container.RegisterInstance(mockRuleProvider.Object);
            }
            else
                container.RegisterInstance(ruleProvider);

            // Permissions Manager
            builtIns ??= new List<(string, Delegate)>();
            container.Register<PermissionsManager<PermissionContext>>(Reuse.Singleton, Made.Of(()
                => new PermissionsManager<PermissionContext>(Arg.Of<IRuleProvider>(), Arg.Of<Permissions.Logging.ILogger>(), ".", builtIns)));

            // Context and DB
            // Using the created DB, create exactly one singleton object that lives on it.
            var dbContext = new HiveContext(options);
            // Used for ensuring objects exist in context
            if (context is not null)
            {
                TestHelpers.CopyData(context, dbContext);
            }
            // This singleton SHOULD be properly initialized to operate with the test DB.
            // There should only ever be one per service collection, so this checks out.
            container.RegisterInstance(dbContext);

            return container;
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

        internal static async Task<ExecutionResult> ExecuteGraphAsync(this IServiceProvider services, string query)
        {
            var executer = services.GetRequiredService<IDocumentExecuter>();
            var schema = services.GetRequiredService<HiveSchema>();
            return await executer.ExecuteAsync(options =>
            {
                options.Query = query;
                options.Schema = schema;
                options.RequestServices = services;
            });
        }
    }
}
