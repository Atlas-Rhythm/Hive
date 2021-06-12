using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Hive.Permissions.Resources;
using MathExpr.Compiler.Compilation;
using MathExpr.Syntax;

namespace Hive.Permissions.Functions
{
    internal class CastFunction : IBuiltinFunction<RuleCompilationSettings>
    {
        public const string FunctionName = "cast";
        public string Name => FunctionName;

        public bool TryCompile(IReadOnlyList<MathExpression> arguments, ICompilationContext<RuleCompilationSettings> context, ITypeHintHandler typeHintHandler, [MaybeNullWhen(false)] out Expression expr)
        {
            expr = null;
            if (arguments.Count != 2)
                return false;

            var typenameExpr = arguments[0];
            var valueExpr = arguments[1];

            Type? castTo = null;
            if (typenameExpr is VariableExpression varExpr)
            {
                // convert a nice short name to its equivalent type
                castTo = varExpr.Name switch
                {
                    "string" => typeof(string),
                    "byte" => typeof(byte),
                    "sbyte" => typeof(sbyte),
                    "short" => typeof(short),
                    "ushort" => typeof(ushort),
                    "int" => typeof(int),
                    "uint" => typeof(uint),
                    "long" => typeof(long),
                    "ulong" => typeof(ulong),
                    "object" => typeof(object),
                    "float" => typeof(float),
                    "double" => typeof(double),
                    "decimal" => typeof(decimal),
                    "bool" => typeof(bool),
                    _ => null
                };
            }
            else if (typenameExpr is MathExpr.Syntax.MemberExpression memberExpr)
            {
                // convert something like System.String into its string equivalent so that users don't always need to
                // use quotes
                static string? GetNameFor(MathExpression expr)
                    => expr switch
                    {
                        VariableExpression v => v.Name,
                        MathExpr.Syntax.MemberExpression m => GetNameFor(m.Target) + "." + m.MemberName,
                        _ => null
                    };
                var name = GetNameFor(memberExpr);
                if (name is not null)
                {
                    castTo = Type.GetType(name);
                }
            }
            else if (typenameExpr is StringExpression strExpr)
            {
                // just use the specified string in Type.GetType();
                castTo = Type.GetType(strExpr.Value);
            }

            if (castTo is null)
            {
                throw new InvalidOperationException(SR.Error_CastTypeInvalid.Format(typenameExpr.ToString()));
            }

            var value = typeHintHandler.TransformWithHint(valueExpr, castTo, context);

            if (CompilerHelpers.HasConversionPathTo(value.Type, castTo))
            {
                // we have a path which we can take to get there *without* a direct cast
                expr = CompilerHelpers.ConvertToType(value, castTo);
            }
            else
            {
                // we have to use an actual cast
                expr = Expression.Convert(value, castTo);
            }

            return true;
        }
    }
}
