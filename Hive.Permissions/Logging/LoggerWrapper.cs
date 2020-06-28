using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Permissions.Logging
{
    internal struct LoggerWrapper
    {
        private readonly ILogger? logger;
        private readonly object manager;

        [ThreadStatic]
        private static StringView CurrentAction = "";
        [ThreadStatic]
        private static Rule? CurrentRule = null;

        public LoggerWrapper(ILogger? logger, object manager)
        {
            this.logger = logger;
            this.manager = manager;
        }

        public void Info(string message, params object[] info) => logger?.Info(message, info, CurrentAction, CurrentRule, manager);
        public void Warn(string message, params object[] info) => logger?.Warn(message, info, CurrentAction, CurrentRule, manager);

        public ActionScope WithAction(StringView action) => new ActionScope(action);
        public RuleScope WithRule(Rule? rule) => new RuleScope(rule);

        public struct ActionScope : IDisposable
        {
            private readonly StringView prevAction;
            public ActionScope(StringView action)
            {
                prevAction = CurrentAction;
                CurrentAction = action;
            }
            public void Dispose()
            {
                CurrentAction = prevAction;
            }
        }

        public struct RuleScope : IDisposable
        {
            private readonly Rule? prevRule;
            public RuleScope(Rule? rule)
            {
                prevRule = CurrentRule;
                CurrentRule = rule;
            }
            public void Dispose()
            {
                CurrentRule = prevRule;
            }
        }
    }
}
