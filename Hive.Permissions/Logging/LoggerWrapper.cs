using Hive.Utilities;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Permissions.Logging
{
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "The interface I want for this type has all of its members being instance members.")]
    internal struct LoggerWrapper
    {
        private readonly ILogger? logger;
        private readonly object manager;

        [ThreadStatic]
        private static string PublicApi = "";

        [ThreadStatic]
        private static StringView CurrentAction = "";

        [ThreadStatic]
        private static Rule? CurrentRule = null;

        public LoggerWrapper(ILogger? logger, object manager)
        {
            this.logger = logger;
            this.manager = manager;
        }

        public void Wrap(Action action)
        {
            try
            {
                action();
            }
            catch (PermissionException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw Exception(e);
            }
        }

        public T Wrap<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (PermissionException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw Exception(e);
            }
        }

        public PermissionException Exception(Exception e) => e is PermissionException pe ? pe : new PermissionException($"Error in {PublicApi}", e, CurrentAction, CurrentRule);

        public void Info(string message, params object[] info) => logger?.Info(message, info, PublicApi, CurrentAction, CurrentRule, manager);

        public void Warn(string message, params object[] info) => logger?.Warn(message, info, PublicApi, CurrentAction, CurrentRule, manager);

        public ApiScope InApi(string api) => new(api);

        public ActionScope WithAction(StringView action) => new(action);

        public RuleScope WithRule(Rule? rule) => new(rule);

        public void ReplaceRule(Rule? rule) => CurrentRule = rule;

        public struct ApiScope : IDisposable
        {
            private readonly string prevApi;

            public ApiScope(string api)
            {
                prevApi = PublicApi;
                PublicApi = api;
            }

            public void Dispose()
            {
                PublicApi = prevApi;
            }
        }

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
            private readonly bool revert;

            public RuleScope(Rule? rule)
            {
                revert = CurrentRule != rule;
                prevRule = CurrentRule;
                if (revert)
                    CurrentRule = rule;
            }

            public void Dispose()
            {
                if (revert)
                    CurrentRule = prevRule;
            }
        }
    }
}
