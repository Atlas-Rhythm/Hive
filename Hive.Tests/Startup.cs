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
using Xunit.Sdk;

namespace Hive.Tests
{
    public class Startup
    {
        private static readonly IReadOnlyList<(string, Delegate)> builtIns = new List<(string, Delegate)>
        {
            ("isNull", new Func<object?, bool>(o => o is null))
        };

        // One rule provider for everything
        private static Mock<IRuleProvider>? ruleProvider = null;

        internal static Mock<IRuleProvider> MockRuleProvider
        {
            get
            {
                if (ruleProvider is null)
                {
                    ruleProvider = new Mock<IRuleProvider>();
                    var start = SystemClock.Instance.GetCurrentInstant();
                    ruleProvider.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
                    ruleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
                    ruleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
                    ruleProvider.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
                    ruleProvider.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
                }
                return ruleProvider;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ILogger>((s) => new LoggerConfiguration().WriteTo.Debug().CreateLogger())
                .AddSingleton((sp) => MockRuleProvider.Object)
                // If we ignore the logger for PermissionsManager, everything can be done here.
                // However, if we need the logger for PermissionsManager, we can only get a valid ITestOutputHelper from WITHIN the type.
                // We could do this similarly to how we do our rule provider: have yet another singleton + reference and share it
                // But MAN does that feel ugly (not to mention that this ALREADY feels plenty ugly)
                .AddSingleton(sp =>
                    new PermissionsManager<PermissionContext>(sp.GetRequiredService<IRuleProvider>(), ".", builtIns))
                .AddSingleton<IProxyAuthenticationService>(sp => new MockAuthenticationService());
        }
    }
}