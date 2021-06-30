using Hive.Permissions;
using Hive.Permissions.Logging;
using Hive.Utilities;
using Xunit.Abstractions;

namespace Hive.Tests
{
    internal class TestOutputWrapper : ILogger
    {
        private readonly ITestOutputHelper output;

        public TestOutputWrapper(ITestOutputHelper outp) => output = outp;

        public void Info(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
            => Write("Info", message, messageInfo, api, action, currentRule);

        public void Warn(string message, object[] messageInfo, string api, StringView action, Rule? currentRule, object manager)
            => Write("Warn", message, messageInfo, api, action, currentRule);

        private void Write(string ident, string message, object[] messageInfo, string api, StringView action, Rule? currentRule)
        {
            output.WriteLine($"[{api}][action: {action}]{(currentRule != null ? $"[rule: {currentRule.Name}]" : "")} {ident}: {message} {{ {string.Join(", ", messageInfo)} }}");
        }
    }
}
