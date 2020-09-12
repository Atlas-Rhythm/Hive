using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hive.Plugins
{
    internal interface IAggregatorAttribute { }
    internal interface ITargetsInParam : IAggregatorAttribute { }
    internal interface ITargetsOutParam : IAggregatorAttribute { }
    internal interface ITargetsReturn : IAggregatorAttribute { }
    internal interface ISpecifiesInput : IAggregatorAttribute { }

    internal interface IRequiresType : IAggregatorAttribute
    {
        bool CheckType(Type type);
    }

    internal interface IExpressionAggregator
    {
        Expression Aggregate(Expression prev, Expression next);
    }

    internal interface IStopIfReturns
    {
        Expression Test(Expression value);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class StopIfReturnsAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType, IStopIfReturns
    {
        public bool ReturnValue { get; }
        public StopIfReturnsAttribute(bool returnValue)
            => ReturnValue = returnValue;

        bool IRequiresType.CheckType(Type type)
            => type == typeof(bool);

        Expression IStopIfReturns.Test(Expression value)
            => ReturnValue
                ? Expression.IsTrue(value)
                : Expression.IsFalse(value);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class StopIfReturnsNullAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType, IStopIfReturns
    {
        bool IRequiresType.CheckType(Type type)
            => !type.IsValueType || (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

        Expression IStopIfReturns.Test(Expression value)
        {
            var valType = value.Type;
            if (valType.IsConstructedGenericType && valType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Expression.IsFalse(Expression.Property(value, nameof(Nullable<int>.HasValue)));
            }
            else
            {
                return Expression.ReferenceEqual(value, Expression.Constant(null));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnLastAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IExpressionAggregator
    {
        Expression IExpressionAggregator.Aggregate(Expression prev, Expression next)
            => next;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class AggregateWithAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IExpressionAggregator
    {
        public Type TypeWithAggregator { get; }
        public string AggregatorName { get; }
        public MethodInfo? ExpressionAggregator { get; }
        public AggregateWithAttribute(Type targetType, string targetName)
        {
            TypeWithAggregator = targetType;
            AggregatorName = targetName;

            var exprAgg = targetType.GetMethod(targetName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(Expression), typeof(Expression) },
                Array.Empty<ParameterModifier>());
            if (exprAgg != null)
            {
                if (!typeof(Expression).IsAssignableFrom(exprAgg.ReturnType))
                    throw new ArgumentException("Target method takes Expressions but does not return an Expression type");

                ExpressionAggregator = exprAgg;
            }
        }

        Expression IExpressionAggregator.Aggregate(Expression prev, Expression next)
        {
            if (ExpressionAggregator != null)
                return (Expression)ExpressionAggregator.Invoke(null, new[] { prev, next });

            var valueAggregator = TypeWithAggregator.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == AggregatorName)
                .Where(m => m.GetParameters().Length == 2)
                .Where(m => m.GetParameters().All(p => p.ParameterType.IsAssignableFrom(prev.Type)))
                .Where(m => prev.Type.IsAssignableFrom(m.ReturnType))
                .FirstOrDefault();

            if (valueAggregator == null)
                throw new InvalidOperationException($"Could not find agggregator on {TypeWithAggregator} with name {AggregatorName}");

            return Expression.Call(valueAggregator, prev, next);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesReturnValueAttribute : Attribute, ITargetsInParam, ISpecifiesInput
    {
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesOutValueAttribute : Attribute, ITargetsInParam, ISpecifiesInput
    {
        public int ParameterIndex { get; }
        public TakesOutValueAttribute(int index)
            => ParameterIndex = index;
    }

}
