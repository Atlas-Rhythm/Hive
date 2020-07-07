using FastExpressionCompiler;
using Hive.Permissions.Functions;
using Hive.Permissions.Logging;
using Hive.Utilities;
using MathExpr.Compiler;
using MathExpr.Compiler.Compilation.Passes;
using MathExpr.Compiler.Compilation.Settings;
using MathExpr.Compiler.Optimization.Settings;
using MathExpr.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

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
        #endregion

        /// <summary>
        /// Checks if an action is permitted, given <paramref name="context"/>.
        /// </summary>
        /// <param name="action">The action being performed.</param>
        /// <param name="context">The context that it is acting in.</param>
        /// <returns><see langword="true"/> if the action is permitted, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="CanDo(StringView, TContext, ref PermissionActionParseState)"/>
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
        public bool CanDo(StringView action, TContext context, ref PermissionActionParseState actionParseState)
        {
            using var _ = logger.WithAction(action);

            var order = ParseAction(action, ref actionParseState);

            ContinueDelegate GetContinueStartingAt(int idx)
                => defaultValue =>
                {
                    for (int i = idx; i < order.Length; i++)
                    {
                        using (logger.WithRule(order[i].Rule))
                        {
                            if (TryPrepare(ref order[i], out var impl))
                            {
                                return impl(context, GetContinueStartingAt(i + 1));
                            }
                        }
                    }
                    return defaultValue;
                };

            return GetContinueStartingAt(0)(false);
        }

        private const string ErrInvalidParseContextType = nameof(PermissionActionParseState) + " used when parsing action was previously used with a different context type!";
        private const string ErrIncompatableCompiledRule = "Existing compiled rule incompatable with current permission manager";
        private const string ErrCompilationFailed = "Rule compilation failed";

        private PermissionActionParseState.SearchEntry[] ParseAction(StringView action, ref PermissionActionParseState actionParseState)
        {
            using var _ = logger.WithAction(action);

            if (actionParseState.ContextType != null && actionParseState.ContextType != typeof(TContext))
            {
                logger.Warn(ErrInvalidParseContextType, typeof(TContext), actionParseState.ContextType);
                // the existing compiled rules are invalid, so we will clear the parse state and retry it all
                actionParseState.Reset();
            }

            if (actionParseState.SearchOrder == null)
            { // build up our search order
                var parts = action.Split(splitToken, ignoreEmpty: false).ToArray();
                var combos = new PermissionActionParseState.SearchEntry[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    combos[i].Name = StringView.Concat(parts.Take(i + 1).InterleaveWith(Helpers.Repeat(splitToken, i)));
                    ruleProvider.TryGetRule(combos[i].Name, out combos[i].Rule);
                }
                actionParseState.SearchOrder = combos;
                actionParseState.ContextType = typeof(TContext);
            }

            return actionParseState.SearchOrder;
        }

        private bool TryPrepare(ref PermissionActionParseState.SearchEntry entry, [MaybeNullWhen(false)] out RuleImplDelegate del)
        {
            if (entry.Rule != null)
            {
                using (logger.WithRule(entry.Rule))
                {
                    if (entry.Rule.Compiled != null)
                    { // the rule has been compiled before
                        if (!ruleProvider.HasRuleChangedSince(entry.Rule, entry.Rule.CompiledAt))
                        {
                            if (entry.Rule.Compiled is RuleImplDelegate implDel)
                            {
                                del = implDel;
                                return true;
                            }
                            else
                            {
                                logger.Warn(ErrIncompatableCompiledRule, entry.Rule.Compiled, typeof(TContext));
                                entry.Rule.Compiled = null;
                                entry.Rule.CompiledAt = default;
                                return TryCompileRule(entry.Rule, out del, out entry.CheckedAt);
                            }
                        }
                        else
                        { // we should re-grab the rule object
                            if (ruleProvider.TryGetRule(entry.Name, out entry.Rule))
                            {
                                return TryCompileRule(entry.Rule, out del, out entry.CheckedAt);
                            }
                            else
                            { // the rule no longer exists, so we clear out 
                                entry.Rule = null;
                                del = null;
                                entry.CheckedAt = ruleProvider.CurrentTime;
                                return false;
                            }
                        }
                    }

                    return TryCompileRule(entry.Rule, out del, out entry.CheckedAt);
                }
            }
            else if (ruleProvider.HasRuleChangedSince(entry.Name, entry.CheckedAt))
            { // the rule was added
                if (ruleProvider.TryGetRule(entry.Name, out entry.Rule))
                {
                    return TryCompileRule(entry.Rule, out del, out entry.CheckedAt);
                }
            }

            del = null;
            entry.CheckedAt = ruleProvider.CurrentTime;
            return false;
        }

        internal delegate bool ContinueDelegate(bool defaultValue);
        internal delegate bool RuleImplDelegate(TContext context, ContinueDelegate next);

        private bool TryCompileRule(Rule rule, [MaybeNullWhen(false)] out RuleImplDelegate impl, out DateTime compiledAt, bool throwOnError = false)
        {
            using var _ = logger.WithRule(rule);

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
                    logger.Warn(ErrIncompatableCompiledRule, rule.Compiled, typeof(TContext));
                    rule.Compiled = null;
                    rule.CompiledAt = default;
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
                    throw;

                logger.Warn(ErrCompilationFailed, e);

                impl = null;
                compiledAt = default;
                return false;
            }
        }

        private RuleImplDelegate CompileRule(Rule rule, out DateTime time)
        {
            using var _ = logger.WithRule(rule);

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
