using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    internal static class AggregatedMethodGenerator
    {
        private delegate ref int RefReturnDel();

        public static Delegate Generate(Type iface, MethodInfo toAggregate, Type delegateType)
        {
            var stopIfReturnsAttr = toAggregate.GetCustomAttribute<StopIfReturnsAttribute>();
            var stopIfReturnsNullAttr = toAggregate.GetCustomAttribute<StopIfReturnsNullAttribute>();
            var returnLastAttribute = toAggregate.GetCustomAttribute<ReturnLastAttribute>();

            if (stopIfReturnsAttr != null && stopIfReturnsNullAttr != null)
                throw new InvalidOperationException($"Method {toAggregate} cannot have both {nameof(StopIfReturnsAttribute)} and {nameof(StopIfReturnsNullAttribute)}");

            if (stopIfReturnsAttr != null && !CheckAttribute(toAggregate.ReturnParameter, stopIfReturnsAttr))
                throw new InvalidOperationException($"Method {toAggregate} must return {typeof(bool)} to use {nameof(StopIfReturnsAttribute)}");

            if (stopIfReturnsNullAttr != null && !CheckAttribute(toAggregate.ReturnParameter, stopIfReturnsNullAttr))
                throw new InvalidOperationException($"Method {toAggregate} must return a nullable type to use {nameof(StopIfReturnsNullAttribute)}");

            var targetParameters = toAggregate.GetParameters();

            var listParam = Expression.Parameter(typeof(IAggregateList<>).MakeGenericType(iface), "list");
            var lambdaParams = targetParameters.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();

            var expr = Expression.Lambda(
                delegateType,
                Expression.Block(
                    toAggregate.ReturnType,
                    Expression.Call(
                        typeof(AggregatedMethodGenerator).GetMethod(nameof(CheckList), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .MakeGenericMethod(iface),
                        listParam
                    ),
                    DefaultForType(toAggregate.ReturnType)
                ),
                lambdaParams.Prepend(listParam)
            );

            return expr.Compile();
        }

        // Note: The below method and type exist because expression trees *cannot* take a ref of a variable, but they *can* pass around references
        //       So, I have a small type and wrapper function to do it for me.
        private static Expression DefaultForType(Type type)
            => type.IsByRef
            ? Expression.Call(Expression.New(typeof(DefaultByRef<>).MakeGenericType(type.GetElementType())), nameof(DefaultByRef<object>.ByRefDefault), null)
            : (Expression)Expression.Default(type);

        private class DefaultByRef<T>
        {
            public T Default = default!;
            public ref T ByRefDefault() => ref Default;
        }

        public static void CheckList<T>(IAggregateList<T> list)
        {

        }

        private static bool CheckAttribute(ParameterInfo param, Attribute attr)
        {
            if (!CheckAttributeTarget(param, attr)) return false;
            if (attr is IRequiresType reqTy) return reqTy.CheckType(param.ParameterType);
            return true;
        }

        private static bool CheckAttributeTarget(ParameterInfo param, Attribute attr)
        {
            if (param.IsRetval) return attr is ITargetsReturn;
            if (param.IsOut) return attr is ITargetsOutParam;
            return attr is ITargetsInParam;
        }
    }
}
