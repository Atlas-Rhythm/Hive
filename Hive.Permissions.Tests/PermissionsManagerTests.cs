using Hive.Permissions;
using Hive.Permissions.Logging;
using Hive.Utilities;
using MathExpr.Compiler.Compilation;
using MathExpr.Syntax;
using MathExpr.Utilities;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Helpers = MathExpr.Utilities.Helpers;

namespace Hive.Permissions.Tests
{
    public class PermissionsManagerTests
    {
        private readonly ILogger logger;

        public PermissionsManagerTests(ITestOutputHelper output)
        {
            logger = new OutputWrapper(output);
        }

        private class OutputWrapper : ILogger
        {
            private readonly ITestOutputHelper output;

            public OutputWrapper(ITestOutputHelper outp) => output = outp;

            public void Info(string message, object[] messageInfo, StringView action, Rule? currentRule, object manager)
                => Write("Info", message, messageInfo, action, currentRule);

            public void Warn(string message, object[] messageInfo, StringView action, Rule? currentRule, object manager)
                => Write("Warn", message, messageInfo, action, currentRule);

            private void Write(string ident, string message, object[] messageInfo, StringView action, Rule? currentRule)
            {
                output.WriteLine($"[action: {action}]{(currentRule != null ? $"[rule: {currentRule.Name}]" : "")} {ident}: {message} {{ {string.Join(", ", messageInfo)} }}");
            }
        }

        public class Context
        {
            public bool Hive { get; set; } = false;
            public bool HiveMod { get; set; } = false;
            public bool HiveModUpload { get; set; } = false;
            public bool HiveModDelete { get; set; } = false;

            public static implicit operator bool(Context _) => true;
        }

        [Fact]
        public void TestCanDo()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive.mod", "ctx.HiveMod | next(false)");
            var hiveModUploadRule = new Rule("hive.mod.upload", "ctx.HiveModUpload | next(false)");
            var hiveModDeleteRule = new Rule("hive.mod.delete", "ctx.HiveModDelete | next(false)");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModUploadRule.Name, out hiveModUploadRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModDeleteRule.Name, out hiveModDeleteRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, ".");

            PermissionActionParseState hiveModUploadParseState;
            Assert.False(permManager.CanDo("hive.mod.upload", new Context(), ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { Hive = true }, ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { HiveMod = true }, ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { HiveModUpload = true }, ref hiveModUploadParseState));
            Assert.False(permManager.CanDo("hive.mod.upload", new Context { HiveModDelete = true }, ref hiveModUploadParseState));

            PermissionActionParseState hiveModDeleteParseState;
            Assert.False(permManager.CanDo("hive.mod.delete", new Context(), ref hiveModDeleteParseState));
            Assert.True(permManager.CanDo("hive.mod.delete", new Context { Hive = true }, ref hiveModDeleteParseState));
            Assert.True(permManager.CanDo("hive.mod.delete", new Context { HiveMod = true }, ref hiveModDeleteParseState));
            Assert.True(permManager.CanDo("hive.mod.delete", new Context { HiveModDelete = true }, ref hiveModDeleteParseState));
            Assert.False(permManager.CanDo("hive.mod.delete", new Context { HiveModUpload = true }, ref hiveModDeleteParseState));
        }

        [Fact]
        public void TestCompilationException()
        {
            var mock = MockRuleProvider();
            var mockLogger = MockLogger<ILogger>();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRuleInvalid = new Rule("hive.mod", "ctx.HiveMod | ctx.Invalid");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRuleInvalid.Name, out hiveModRuleInvalid)).Returns(true);

            var manager = new PermissionsManager<Context>(mock.Object, mockLogger.Object, ".");

            PermissionActionParseState state;
            // We should be able to successfully compile this to a true
            Assert.True(manager.CanDo("hive", new Context { Hive = true }, ref state));
            // We should not be able to successfully compile this at all, so it should default to return false
            Assert.False(manager.CanDo("hive.mod", new Context { HiveMod = true }, ref state));

            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), new object[] { It.IsAny<CompilationException>() }, "hive.mod", hiveModRuleInvalid, manager));
        }

        [Fact]
        public void TestSyntaxException()
        {
            var mock = MockRuleProvider();
            var mockLogger = MockLogger<ILogger>();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRuleInvalid = new Rule("hive.mod", "ctx.HiveMod |");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRuleInvalid.Name, out hiveModRuleInvalid)).Returns(true);

            var manager = new PermissionsManager<Context>(mock.Object, mockLogger.Object, ".");

            PermissionActionParseState state;
            // We should be able to successfully compile this to a true
            Assert.True(manager.CanDo("hive", new Context { Hive = true }, ref state));
            // We should not be able to successfully compile this at all, so it should default to return false
            Assert.False(manager.CanDo("hive.mod", new Context { HiveMod = true }, ref state));

            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), new object[] { It.IsAny<SyntaxException>() }, "hive.mod", hiveModRuleInvalid, manager));
        }

        [Fact]
        public void TestSeparator()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive/mod", "ctx.HiveMod | next(false)");
            var hiveModUploadRule = new Rule("hive/mod/upload", "ctx.HiveModUpload | next(false)");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModUploadRule.Name, out hiveModUploadRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, "/");

            PermissionActionParseState state;
            Assert.False(permManager.CanDo("hive/mod/upload", new Context(), ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { HiveMod = true }, ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { HiveModUpload = true }, ref state));
        }

        [Fact]
        public void TestUserBuiltin()
        {
            var mock = MockRuleProvider();

            var rule = new Rule("rule", "TestThing(ctx)");
            mock.Setup(rules => rules.TryGetRule(rule.Name, out rule)).Returns(true);

            var invoked = false;
            var permManager = new PermissionsManager<Context>(mock.Object, logger, Helpers.Single(("TestThing", (Delegate)new Func<Context, bool>(c =>
            {
                invoked = true;
                return true;
            }))));

            Assert.True(permManager.CanDo("rule", new Context()));
            Assert.True(invoked);
        }

        [Fact]
        public void TestPreCompileHook()
        {
            var mock = MockRuleProvider<IPreCompileRuleProvider>();

            mock.Setup(rules => rules.PreCompileTransform(It.IsAny<MathExpression>())).Returns(MathExpression.Parse("next(true)"));

            var rule = new Rule("rule", "Invalid()");
            mock.Setup(rules => rules.TryGetRule(rule.Name, out rule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger);

            Assert.True(permManager.CanDo("rule", new Context()));
        }

        private Mock<IRuleProvider> MockRuleProvider() => MockRuleProvider<IRuleProvider>();

        private Mock<T> MockRuleProvider<T>() where T : class, IRuleProvider
        {
            var mock = new Mock<T>();
            mock.Setup(rules => rules.CurrentTime).Returns(() => DateTime.Now);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<DateTime>())).Returns(false);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<DateTime>())).Returns(false);
            mock.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
            return mock;
        }

        private Mock<T> MockLogger<T>() where T : class, ILogger
        {
            var mock = new Mock<T>();
            // Add setup here if ever needed
            return mock;
        }
    }
}