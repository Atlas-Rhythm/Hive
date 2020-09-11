using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    internal static class AggregatedMethodGenerator
    {
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

            var expr = Expression.Lambda(delegateType,
                Expression.Constant(null));

            return expr.Compile();
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
