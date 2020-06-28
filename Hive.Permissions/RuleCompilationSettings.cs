using Hive.Permissions.Logging;
using MathExpr.Compiler.Compilation;
using MathExpr.Compiler.Compilation.Builtins;
using MathExpr.Compiler.Compilation.Settings;
using MathExpr.Syntax;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Hive.Permissions
{
    internal class RuleCompilationSettings :
        ICompileToLinqExpressionSettings<RuleCompilationSettings>,
        IWritableCompileToLinqExpressionSettings,
        IBuiltinFunctionWritableCompilerSettings<RuleCompilationSettings>
    {
        public LoggerWrapper Logger { get; }

        public Type ExpectReturn { get; set; } = typeof(bool);

        public IDictionary<VariableExpression, ParameterExpression> ParameterMap { get; } = new Dictionary<VariableExpression, ParameterExpression>();

        public IDictionary<Type, TypedFactorialCompiler> TypedFactorialCompilers { get; } = new Dictionary<Type, TypedFactorialCompiler>();

        public IList<ISpecialBinaryOperationCompiler> PowerCompilers { get; } = new List<ISpecialBinaryOperationCompiler>();

        public bool IgnoreDomainRestrictions => true;

        public bool AllowDomainChangingOptimizations => false;

        private readonly List<IBuiltinFunction<RuleCompilationSettings>> builtins = new List<IBuiltinFunction<RuleCompilationSettings>>();

        public IReadOnlyCollection<IBuiltinFunction<RuleCompilationSettings>> BuiltinFunctions => builtins;

        public ICollection<IBuiltinFunction<RuleCompilationSettings>> WritableBuiltinFunctions => builtins;

        public RuleCompilationSettings(LoggerWrapper logger)
        {
            Logger = logger;
            this.AddBuiltin().OfType<BuiltinFunctionIf>();
        }
    }
}
