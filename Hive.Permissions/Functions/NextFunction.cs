using MathExpr.Compiler.Compilation;
using MathExpr.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Hive.Permissions.Functions
{
    internal class NextFunction : IBuiltinFunction<object?>
    {
        public string DelegateArgumentName { get; }
        public NextFunction(string delArgName) => DelegateArgumentName = delArgName;

        public string Name => "next";

        public bool TryCompile(IReadOnlyList<MathExpression> arguments, ICompilationContext<object?> context, ITypeHintHandler typeHintHandler, [MaybeNullWhen(false)] out Expression expr)
        {
            expr = null;
            if (arguments.Count != 1) return false;

            var arg = arguments.First();

            Expression? defaultValue = null;

            if (arg is VariableExpression var)
            {
                if (var.Name == "true")
                    defaultValue = Expression.Constant(true);
                else if (var.Name == "false")
                    defaultValue = Expression.Constant(false);
            }
            defaultValue ??= CompilerHelpers.ConvertToType(typeHintHandler.TransformWithHint(arg, typeof(bool), context), typeof(bool));

            var nextDel = context.Transform(new VariableExpression(DelegateArgumentName));

            expr = Expression.Invoke(nextDel, defaultValue);
            return true;
        }
    }
}
