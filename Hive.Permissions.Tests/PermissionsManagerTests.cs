using Hive.Permissions;
using Hive.Permissions.Logging;
using Hive.Utilities;
using MathExpr.Compiler.Compilation;
using MathExpr.Syntax;
using MathExpr.Utilities;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
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

            public void Info(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
                => Write("Info", message, messageInfo, api, action, currentRule);

            public void Warn(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
                => Write("Warn", message, messageInfo, api, action, currentRule);

            private void Write(string ident, string message, object[] messageInfo, string api, StringView action, Rule? currentRule)
            {
                output.WriteLine($"[{api}][action: {action}]{(currentRule != null ? $"[rule: {currentRule.Name}]" : "")} {ident}: {message} {{ {string.Join(", ", messageInfo)} }}");
            }
        }

        public class Context
        {
            public class Inner
            {
                public bool DoesThing { get; set; } = false;

                public static implicit operator bool(Inner _) => true;
            }

            public bool Hive { get; set; } = false;
            public bool HiveMod { get; set; } = false;
            public bool HiveModUpload { get; set; } = false;
            public bool HiveModDelete { get; set; } = false;
            public bool IsHiveUser { get; set; } = false;
            public string ArbitraryString { get; set; } = string.Empty;
            public Inner? NonTrivialObject { get; set; } = null;

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

            PermissionActionParseState hiveModUploadParseState = default;
            Assert.False(permManager.CanDo("hive.mod.upload", new Context(), ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { Hive = true }, ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { HiveMod = true }, ref hiveModUploadParseState));
            Assert.True(permManager.CanDo("hive.mod.upload", new Context { HiveModUpload = true }, ref hiveModUploadParseState));
            Assert.False(permManager.CanDo("hive.mod.upload", new Context { HiveModDelete = true }, ref hiveModUploadParseState));

            PermissionActionParseState hiveModDeleteParseState = default;
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

            PermissionActionParseState state = default;
            PermissionActionParseState modState = default;
            // We should be able to successfully compile this to a true
            Assert.True(manager.CanDo("hive", new Context { Hive = true }, ref state));
            // We should not be able to successfully compile this at all, so it should default to return false
            Assert.False(manager.CanDo("hive.mod", new Context { HiveMod = true }, ref modState));

            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), It.Is<object[]>(arr => arr.Length == 1 && arr[0] is CompilationException), "CanDo", hiveModRuleInvalid.Name, hiveModRuleInvalid, manager), Times.Once);
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

            PermissionActionParseState state = default;
            PermissionActionParseState modState = default;
            // We should be able to successfully compile this to a true
            Assert.True(manager.CanDo("hive", new Context { Hive = true }, ref state));
            // We should not be able to successfully compile this at all, so it should default to return false
            Assert.False(manager.CanDo("hive.mod", new Context { HiveMod = true }, ref modState));

            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), It.Is<object[]>(arr => arr.Length == 1 && arr[0] is SyntaxException), "CanDo", hiveModRuleInvalid.Name, hiveModRuleInvalid, manager), Times.Once);
        }

        [Fact]
        public void TestSimpleSeparator()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive/mod", "ctx.HiveMod | next(false)");
            var hiveModUploadRule = new Rule("hive/mod/upload", "ctx.HiveModUpload | next(false)");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModUploadRule.Name, out hiveModUploadRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, "/");

            PermissionActionParseState state = default;
            Assert.False(permManager.CanDo("hive/mod/upload", new Context(), ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { HiveMod = true }, ref state));
            Assert.True(permManager.CanDo("hive/mod/upload", new Context { HiveModUpload = true }, ref state));
        }

        [Fact]
        public void TestComplexSeparator()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hiveakljsdfgvhbakjfghmod", "ctx.HiveMod | next(false)");
            var hiveModUploadRule = new Rule("hiveakljsdfgvhbakjfghmodakljsdfgvhbakjfghupload", "ctx.HiveModUpload | next(false)");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModUploadRule.Name, out hiveModUploadRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, "akljsdfgvhbakjfgh");

            PermissionActionParseState state = default;
            Assert.False(permManager.CanDo("hiveakljsdfgvhbakjfghmodakljsdfgvhbakjfghupload", new Context(), ref state));
            Assert.True(permManager.CanDo("hiveakljsdfgvhbakjfghmodakljsdfgvhbakjfghupload", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hiveakljsdfgvhbakjfghmodakljsdfgvhbakjfghupload", new Context { HiveMod = true }, ref state));
            Assert.True(permManager.CanDo("hiveakljsdfgvhbakjfghmodakljsdfgvhbakjfghupload", new Context { HiveModUpload = true }, ref state));
        }

        [Fact]
        public void TestString()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | ctx.ArbitraryString = \"test\" | next(false)");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, ".");

            PermissionActionParseState state = default;
            Assert.False(permManager.CanDo("hive", new Context(), ref state));
            Assert.True(permManager.CanDo("hive", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hive", new Context { ArbitraryString = "test" }, ref state));
            Assert.False(permManager.CanDo("hive", new Context { ArbitraryString = "asdf" }, ref state));
        }

        [Fact]
        public void TestChangeRule()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive.mod", "ctx.HiveMod");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, ".");

            PermissionActionParseState state = default;
            Assert.False(permManager.CanDo("hive.mod", new Context(), ref state));
            Assert.True(permManager.CanDo("hive.mod", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hive.mod", new Context { HiveMod = true }, ref state));

            var newHiveRule = new Rule("hive.mod", "false");
            mock.Setup(rules => rules.TryGetRule(newHiveRule.Name, out newHiveRule)).Returns(true);

            // always returning true is the correct behaviour, because hiveModRule is the old rule and is known to have changed
            mock.Setup(rules => rules.HasRuleChangedSince(hiveModRule, It.IsAny<Instant>())).Returns(true);

            // Shouldn't need to create a new permission manager

            Assert.False(permManager.CanDo("hive.mod", new Context(), ref state));
            Assert.True(permManager.CanDo("hive.mod", new Context { Hive = true }, ref state));
            Assert.False(permManager.CanDo("hive.mod", new Context { HiveMod = true }, ref state));
        }

        [Fact]
        public void TestStringResult()
        {
            // This test has a rule that returns a string, which should not be further evaluated and should cause a CompilationException.
            var mock = MockRuleProvider();
            var mockLogger = MockLogger<ILogger>();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive.mod", "ctx.NonTrivialFunction");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, mockLogger.Object, ".");

            // Should not allow for permission injection

            PermissionActionParseState state = default;
            Assert.False(permManager.CanDo("hive.mod", new Context { ArbitraryString = "ctx.Hive" }, ref state));
            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), It.Is<object[]>(arr => arr.Length == 1 && arr[0] is CompilationException), "CanDo", hiveModRule.Name, hiveModRule, permManager), Times.Once);
            Assert.True(permManager.CanDo("hive.mod", new Context { Hive = true, ArbitraryString = "ctx.Hive" }, ref state));
            Assert.False(permManager.CanDo("hive.mod", new Context { HiveMod = true, ArbitraryString = "ctx.HiveMod" }, ref state));
            mockLogger.Verify(l => l.Warn(It.IsAny<string>(), It.Is<object[]>(arr => arr.Length == 1 && arr[0] is CompilationException), "CanDo", hiveModRule.Name, hiveModRule, permManager), Times.Exactly(2));
        }

        [Fact]
        public void TestNonTrivialObject()
        {
            var mock = MockRuleProvider();

            var hiveRule = new Rule("hive", "ctx.Hive | next(false)");
            var hiveModRule = new Rule("hive.mod", "ctx.NonTrivialObject.DoesThing");
            mock.Setup(rules => rules.TryGetRule(hiveRule.Name, out hiveRule)).Returns(true);
            mock.Setup(rules => rules.TryGetRule(hiveModRule.Name, out hiveModRule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger, ".");

            PermissionActionParseState state = default;
            // This will throw a PermissionException containing an NRE, because Context.NonTrivialObject is null
            Assert.Throws<PermissionException>(() => permManager.CanDo("hive.mod", new Context(), ref state));
            Assert.True(permManager.CanDo("hive.mod", new Context { Hive = true }, ref state));
            Assert.True(permManager.CanDo("hive.mod", new Context { NonTrivialObject = new Context.Inner { DoesThing = true } }, ref state));
            Assert.False(permManager.CanDo("hive.mod", new Context { NonTrivialObject = new Context.Inner { DoesThing = false } }, ref state));
        }

        [Fact]
        [SuppressMessage("Hive.Permissions", "Hive0012:Use the CanDo(StringView, TContext, ref PermissionActionParseState) overload when possible", 
            Justification = "The action string will only ever be invoked once.")]
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
        [SuppressMessage("Hive.Permissions", "Hive0012:Use the CanDo(StringView, TContext, ref PermissionActionParseState) overload when possible",
            Justification = "The action string will only ever be invoked once.")]
        public void TestPreCompileHook()
        {
            var mock = MockRuleProvider<IPreCompileRuleProvider>();

            mock.Setup(rules => rules.PreCompileTransform(It.IsAny<MathExpression>())).Returns(MathExpression.Parse("next(true)"));

            var rule = new Rule("rule", "Invalid()");
            mock.Setup(rules => rules.TryGetRule(rule.Name, out rule)).Returns(true);

            var permManager = new PermissionsManager<Context>(mock.Object, logger);

            Assert.True(permManager.CanDo("rule", new Context()));
        }

        private static Mock<IRuleProvider> MockRuleProvider() => MockRuleProvider<IRuleProvider>();

        private static Mock<T> MockRuleProvider<T>() where T : class, IRuleProvider
        {
            var mock = new Mock<T>();

            var start = SystemClock.Instance.GetCurrentInstant();
            mock.Setup(rules => rules.CurrentTime).Returns(() => SystemClock.Instance.GetCurrentInstant());
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<StringView>(), It.Is<Instant>(i => i < start))).Returns(true);
            mock.Setup(rules => rules.HasRuleChangedSince(It.IsAny<Rule>(), It.IsAny<Instant>())).Returns(false);
            mock.Setup(rules => rules.TryGetRule(It.IsAny<StringView>(), out It.Ref<Rule>.IsAny!)).Returns(false);
            return mock;
        }

        private static Mock<T> MockLogger<T>() where T : class, ILogger
        {
            var mock = new Mock<T>();
            // Add setup here if ever needed
            return mock;
        }
    }
}