using Hive.Permissions.Resources;
using MathExpr.Compiler.Compilation;
using MathExpr.Syntax;
using MathExpr.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Hive.Permissions.Functions
{
    internal class UserBuiltinFunction : IBuiltinFunction<RuleCompilationSettings>
    {
        private readonly Delegate @delegate;
        public string Name { get; }

        public UserBuiltinFunction(string name, Delegate @delegate)
        {
            Name = name;
            this.@delegate = @delegate;
        }

        public bool TryCompile(IReadOnlyList<MathExpression> arguments, ICompilationContext<RuleCompilationSettings> context, ITypeHintHandler typeHintHandler, [MaybeNullWhen(false)] out Expression expr)
        {
            try
            {
                var invoke = @delegate.GetType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)!;
                var @params = invoke.GetParameters();
                if (@params.Length != arguments.Count)
                {
                    expr = null;
                    return false;
                }

                var argExprs = arguments.Zip(@params, Helpers.Tuple)
                    .Select(t => typeHintHandler.TransformWithHint(t.First, t.Second.ParameterType, context));

                expr = Expression.Invoke(Expression.Constant(@delegate), argExprs);
                return true;
            }
            catch (Exception e)
            {
                context.Settings.Logger.Info(SR.UserBuiltin_CompilationError, Name, e);

                expr = null;
                return false;
            }
        }
    }
}
