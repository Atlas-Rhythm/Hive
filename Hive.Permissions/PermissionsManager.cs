using Hive.Permissions.Functions;
using Hive.Permissions.Logging;
using Hive.Permissions.Resources;
using Hive.Utilities;
using MathExpr.Compiler;
using MathExpr.Compiler.Compilation.Settings;
using MathExpr.Compiler.Optimization.Settings;
using MathExpr.Syntax;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Hive.Permissions
{
    /// <summary>
    /// An object that handles evaluating permissions that act on a particular context object.
    /// </summary>
    /// <typeparam name="TContext">The type of the context object.</typeparam>
    /// <remarks>
    /// <para>The rules that this manager uses are gotten through the synchronous interface <see cref="IRuleProvider"/>.</para>
    /// </remarks>
    public class PermissionsManager<TContext>
    {
        private readonly IRuleProvider ruleProvider;
        private readonly StringView splitToken;
        private readonly IEnumerable<(string, Delegate)> builtinFunctions;
        private readonly LoggerWrapper logger;

        #region Constructors

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using <c>.</c> as the action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        public PermissionsManager(IRuleProvider provider)
            : this(provider, ".") { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using the specified action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="splitToken">The string to use as a seperator for action scopes.</param>
        public PermissionsManager(IRuleProvider provider, StringView splitToken)
            : this(provider, splitToken, Enumerable.Empty<(string, Delegate)>()) { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using <c>.</c> as the action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="builtinFunctions">A dictionary containing delegates that will be invoked when calling the builtin.</param>
        public PermissionsManager(IRuleProvider provider, IEnumerable<(string, Delegate)> builtinFunctions)
            : this(provider, ".", builtinFunctions) { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using the specified action scope token and set of builtins.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="splitToken">The string to use as a seperator for action scopes.</param>
        /// <param name="builtinFunctions">A dictionary containing delegates that will be invoked when calling the builtin.</param>
        public PermissionsManager(IRuleProvider provider, StringView splitToken, IEnumerable<(string, Delegate)> builtinFunctions)
            : this(provider, null, splitToken, builtinFunctions) { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using <c>.</c> as the action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="logger">The logger to use with this instance.</param>
        public PermissionsManager(IRuleProvider provider, ILogger? logger)
            : this(provider, logger, ".") { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using the specified action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="logger">The logger to use with this instance.</param>
        /// <param name="splitToken">The string to use as a seperator for action scopes.</param>
        public PermissionsManager(IRuleProvider provider, ILogger? logger, StringView splitToken)
            : this(provider, logger, splitToken, Enumerable.Empty<(string, Delegate)>()) { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using <c>.</c> as the action scope token.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="logger">The logger to use with this instance.</param>
        /// <param name="builtinFunctions">A dictionary containing delegates that will be invoked when calling the builtin.</param>
        public PermissionsManager(IRuleProvider provider, ILogger? logger, IEnumerable<(string, Delegate)> builtinFunctions)
            : this(provider, logger, ".", builtinFunctions) { }

        /// <summary>
        /// Constructs a <see cref="PermissionsManager{TContext}"/> that gets its rules from the specified <see cref="IRuleProvider"/>,
        /// using the specified action scope token and set of builtins.
        /// </summary>
        /// <param name="provider">The rule provider to get permission rules from.</param>
        /// <param name="logger">The logger to use with this instance.</param>
        /// <param name="splitToken">The string to use as a seperator for action scopes.</param>
        /// <param name="builtinFunctions">A dictionary containing delegates that will be invoked when calling the builtin.</param>
        public PermissionsManager(IRuleProvider provider, ILogger? logger, StringView splitToken, IEnumerable<(string, Delegate)> builtinFunctions)
        {
            ruleProvider = provider;
            this.splitToken = splitToken;
            this.builtinFunctions = builtinFunctions;
            this.logger = new LoggerWrapper(logger, this);
        }

        #endregion Constructors

        /// <summary>
        /// Checks if an action is permitted, given <paramref name="context"/>.
        /// </summary>
        /// <param name="action">The action being performed.</param>
        /// <param name="context">The context that it is acting in.</param>
        /// <returns><see langword="true"/> if the action is permitted, <see langword="false"/> otherwise.</returns>
        /// <exception cref="PermissionException">Thrown when there is an exception when executing a rule.</exception>
        /// <seealso cref="CanDo(StringView, TContext, ref PermissionActionParseState)"/>
        [SuppressMessage("Hive.Permissions", "Hive0013:Use the CanDo(StringView, TContext) overload for runtime-specified actions",
            Justification = "This is the implementation for that overload.")]
        public bool CanDo(StringView action, TContext context)
        {
            var state = new PermissionActionParseState();
            return CanDo(action, context, ref state);
        }

        /// <summary>
        /// Checks if an action is permitted, given <paramref name="context"/>, caching the parsed action if possible.
        /// </summary>
        /// <param name="action">The action being performed.</param>
        /// <param name="context">The context that it is acting in.</param>
        /// <param name="actionParseState">The cache for the action parse state.</param>
        /// <returns><see langword="true"/> if the action is permitted, <see langword="false"/> otherwise.</returns>
        /// <exception cref="PermissionException">Thrown when there is an exception when executing a rule.</exception>
        public bool CanDo(StringView action, TContext context, ref PermissionActionParseState actionParseState)
        {
            using (logger.InApi(nameof(CanDo)))
            using (logger.WithAction(action))
            {
                var order = ParseAction(action, ref actionParseState);

                ContinueDelegate GetContinueStartingAt(int idx)
                    => defaultValue =>
                    {
                        for (var i = idx; i < order.Length; i++)
                        {
                            using (logger.WithRule(order[i].Rule))
                            {
                                if (TryPrepare(ref order[i], out var impl))
                                { // TODO: measure perf impact of the additional delegate here
                                    return logger.Wrap(() => impl(context, GetContinueStartingAt(i + 1)));
                                }
                            }
                        }
                        return defaultValue;
                    };

                return GetContinueStartingAt(0)(false);
            }
        }

        /// <summary>
        /// Attempts to pre-compile the rules for a given action.
        /// </summary>
        /// <remarks>
        /// If this method returns normally, there are no issues with the rules.
        /// </remarks>
        /// <param name="action">The action to compile the rules for.</param>
        /// <exception cref="PermissionException">Thrown if there was an error while compiling one of the rules for <paramref name="action"/>.</exception>
        /// <exception cref="AggregateException">Thrown if there were errors while compiling multiple rules for <paramref name="action"/>.
        /// <see cref="AggregateException.InnerExceptions"/> will all be <see cref="PermissionException"/>s.</exception>
        public void PreCompile(StringView action)
        {
            using (logger.InApi(nameof(PreCompile)))
            using (logger.WithAction(action))
            {
                PermissionActionParseState state = default;
                var order = ParseAction(action, ref state);

                var exceptions = new List<PermissionException>(order.Length);
                for (var i = 0; i < order.Length; i++)
                {
                    try
                    {
                        // when it throws, its already the public exception api type
                        _ = logger.Wrap(() => TryPrepare(ref order[i], out _, throwOnError: true));
                    }
                    catch (PermissionException e)
                    {
                        exceptions.Add(e);
                    }
                }

                if (exceptions.Count > 0)
                    throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Attempts to pre-compile a rule.
        /// </summary>
        /// <remarks>
        /// If this method returns normally, there are no issues with the rule.
        /// </remarks>
        /// <param name="rule">The rule to pre-compile.</param>
        /// <exception cref="PermissionException">Thrown if there was an error while compiling <paramref name="rule"/>.</exception>
        public void PreCompile(Rule rule)
        {
            using (logger.InApi(nameof(PreCompile)))
            using (logger.WithRule(rule))
            {
                // when it throws, its already the public exception api type
                _ = logger.Wrap(() => TryCompileRule(rule, out _, out _, throwOnError: true));
            }
        }

        private PermissionActionParseState.SearchEntry[] ParseAction(StringView action, ref PermissionActionParseState actionParseState)
        {
            using (logger.WithAction(action))
            {
                if (actionParseState.ContextType != null && actionParseState.ContextType != typeof(TContext))
                {
                    logger.Warn(SR.Error_InvalidParseContextType, typeof(TContext), actionParseState.ContextType);
                    // the existing compiled rules are invalid, so we will clear the parse state and retry it all
                    actionParseState.Reset();
                }

                if (actionParseState.SearchOrder == null)
                { // build up our search order
                    var parts = action.Split(splitToken, ignoreEmpty: false).ToArray();
                    var combos = new PermissionActionParseState.SearchEntry[parts.Length];
                    for (var i = 0; i < parts.Length; i++)
                    {
                        combos[i] = new PermissionActionParseState.SearchEntry(
                            StringView.Concat(parts.Take(i + 1).InterleaveWith(Helpers.Repeat(splitToken, i))));
                    }
                    actionParseState.SearchOrder = combos;
                    actionParseState.ContextType = typeof(TContext);
                }

                return actionParseState.SearchOrder;
            }
        }

        private bool TryPrepare(ref PermissionActionParseState.SearchEntry entry, [MaybeNullWhen(false)] out RuleImplDelegate del, bool throwOnError = false)
        {
            using (logger.WithRule(entry.Rule))
            {
                if (entry.Rule?.Compiled != null)
                { // rule exists and has been compiled before
                    if (!ruleProvider.HasRuleChangedSince(entry.Rule, entry.Rule.CompiledAt))
                    {
                        if (entry.Rule.Compiled is RuleImplDelegate implDel)
                        {
                            del = implDel;
                            return true;
                        }
                        else
                        {
                            logger.Warn(SR.Error_IncompatibleCompiledRule, entry.Rule.Compiled, typeof(TContext));
                            entry.Rule.Compiled = null;
                            entry.Rule.CompiledAt = Instant.MinValue;
                            return TryCompileRule(entry.Rule, out del, out entry.CheckedAt, throwOnError);
                        }
                    }
                    else
                    { // we should re-grab the rule object
                        if (ruleProvider.TryGetRule(entry.Name, out entry.Rule))
                        {
                            logger.ReplaceRule(entry.Rule);
                            return TryCompileRule(entry.Rule, out del, out entry.CheckedAt, throwOnError);
                        }
                        else
                        { // the rule no longer exists, so we clear out
                            logger.ReplaceRule(null);
                            entry.Rule = null;
                            del = null;
                            entry.CheckedAt = ruleProvider.CurrentTime;
                            return false;
                        }
                    }
                }
                else if (ruleProvider.HasRuleChangedSince(entry.Name, entry.CheckedAt))
                {
                    if (ruleProvider.TryGetRule(entry.Name, out entry.Rule))
                    {
                        logger.ReplaceRule(entry.Rule);
                        return TryCompileRule(entry.Rule, out del, out entry.CheckedAt, throwOnError);
                    }
                    else
                    { // the rule changed, and so its removed (this is only triggered when a given rule wasn't compiled)
                        logger.ReplaceRule(null);
                        entry.Rule = null;
                        del = null;
                        entry.CheckedAt = ruleProvider.CurrentTime;
                        return false;
                    }
                }
                else if (entry.Rule != null && entry.Rule.Compiled == null)
                { // never compiled, unchanged, but exists
                    // I don't think this path will ever be taken in normal execution
                    return TryCompileRule(entry.Rule, out del, out entry.CheckedAt, throwOnError);
                }

                del = null;
                entry.CheckedAt = ruleProvider.CurrentTime;
                return false;
            }
        }

        internal delegate bool ContinueDelegate(bool defaultValue);

        internal delegate bool RuleImplDelegate(TContext context, ContinueDelegate next);

        private bool TryCompileRule(Rule rule, [MaybeNullWhen(false)] out RuleImplDelegate impl, out Instant compiledAt, bool throwOnError)
        {
            using (logger.WithRule(rule))
            {
                if (rule.Compiled != null)
                {
                    if (rule.Compiled is RuleImplDelegate implDel)
                    {
                        impl = implDel;
                        compiledAt = rule.CompiledAt;
                        return true;
                    }
                    else
                    {
                        logger.Warn(SR.Error_IncompatibleCompiledRule, rule.Compiled, typeof(TContext));
                        rule.Compiled = null;
                        rule.CompiledAt = Instant.MinValue;
                    }
                }

                try
                {
                    rule.Compiled = impl = CompileRule(rule, out compiledAt);
                    return true;
                }
                catch (Exception e)
                {
                    if (throwOnError)
                        throw logger.Exception(e);

                    logger.Warn(SR.Error_RuleCompilationFailed, e);

                    impl = null;
                    compiledAt = ruleProvider.CurrentTime; // TODO: should this be current time, or pull from what the rule says?
                    return false;
                }
            }
        }

        private RuleImplDelegate CompileRule(Rule rule, out Instant time)
        {
            using (logger.WithRule(rule))
            {
                time = ruleProvider.CurrentTime;
                rule.CompiledAt = time;

                var compilerSettings = new RuleCompilationSettings(logger);
                var nextFunc = new NextFunction("<>next");
                compilerSettings.AddBuiltin(nextFunc);

                foreach (var (name, del) in builtinFunctions)
                    compilerSettings.AddBuiltin(new UserBuiltinFunction(name, del));

                var compiler = LinqExpressionCompiler.Create(
                    new DefaultOptimizationSettings(),
                    compilerSettings
                );

                var expr = MathExpression.Parse(rule.Definition);
                if (ruleProvider is IPreCompileRuleProvider precomp)
                    expr = precomp.PreCompileTransform(expr);

                return compiler.Compile<RuleImplDelegate>(expr, optimize: true, "ctx", nextFunc.DelegateArgumentName);
            }
        }
    }
}
