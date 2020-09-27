using Hive.Plugins.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Indicates that an aggregated method should stop executing implementations if it returns the provided 
    /// <see cref="bool"/> value, either with a normal return or out parameter, depending on where this attribute is placed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class StopIfReturnsAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType, IStopIfReturns
    {
        /// <summary>
        /// Gets the return value that signals the aggregator to exit.
        /// </summary>
        public bool ReturnValue { get; }
        /// <summary>
        /// Constructs a <see cref="StopIfReturnsAttribute"/> with the specified return value.
        /// </summary>
        /// <param name="returnValue">The return value that will signal the aggregator to exit.</param>
        public StopIfReturnsAttribute(bool returnValue)
            => ReturnValue = returnValue;

        bool IRequiresType.CheckType(Type type)
            => type == typeof(bool);

        Expression IStopIfReturns.Test(Expression value)
            => ReturnValue
                ? Expression.IsTrue(value)
                : Expression.IsFalse(value);
    }

    /// <summary>
    /// Indicates that an aggregated method should stop executing implementations if it returns <see langword="null"/>,
    /// either with a normal return or out parameter, depending on where this attribute is placed.
    /// </summary>
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

    /// <summary>
    /// Indicates that an aggregated method should stop executing implementations if the attached <see cref="IEnumerable{T}"/> becomes empty,
    /// either with a normal return or out parameter, depending on where this attribute is placed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    // TODO: Needs testing (Probably requires DI for endpoint testing?)
    public sealed class StopIfReturnsEmptyAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType, IStopIfReturns
    {
        bool IRequiresType.CheckType(Type type)
            => type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        Expression IStopIfReturns.Test(Expression value)
        {
            return Expression.IsFalse(
                Expression.Call(
                    typeof(Enumerable).GetMethod("Any", BindingFlags.Public | BindingFlags.Static),
                    value));
        }
    }

    /// <summary>
    /// Indicates that the result value for this attribute's target should be the value that the last executed implementation
    /// returned.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnLastAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IExpressionAggregator
    {
        Expression IExpressionAggregator.Aggregate(Expression prev, Expression next)
            => next;
    }

    /// <summary>
    /// Indicates that the result value for this attribute's target should be aggregated using the specified method.
    /// </summary>
    /// <remarks>
    /// <para>It first looks for a static method that takes two <see cref="Expression"/>s and returns an <see cref="Expression"/>. If it
    /// finds it, then it uses that to generate an expression tree during aggregate method generation to aggregate the values.</para>
    /// <para>Otherwise, it looks for a static method that takes two parameters that are assignable from the value type and returns a value
    /// that is assignable to the value type. It then calls that method at runtime to aggregate the values.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class AggregateWithAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IExpressionAggregator
    {
        /// <summary>
        /// Gets the type that has the aggregator method to use.
        /// </summary>
        public Type TypeWithAggregator { get; }
        /// <summary>
        /// Gets the name of the aggregator method.
        /// </summary>
        public string AggregatorName { get; }
        /// <summary>
        /// Gets the <see cref="Expression"/>-based aggregator method, if it is what is targeted.
        /// </summary>
        public MethodInfo? ExpressionAggregator { get; }
        /// <summary>
        /// Constructs an <see cref="AggregateWithAttribute"/> with the specified target type and method name.
        /// </summary>
        /// <param name="targetType">The type that contains the method to use to aggregate the values.</param>
        /// <param name="targetName">The name of the method to use to aggregate the values.</param>
        public AggregateWithAttribute(Type targetType, string targetName)
        {
            if (targetType is null)
                throw new ArgumentNullException(nameof(targetType));
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));

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
                    throw new ArgumentException(SR.AggregateWith_AlmostExpressionAggregator);

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
                throw new InvalidOperationException(SR.AggregateWith_NoSuchAggregator.Format(TypeWithAggregator, AggregatorName));

            return Expression.Call(valueAggregator, prev, next);
        }
    }

    /// <summary>
    /// Indicates that a particular parameter will take the return value of the previous invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesReturnValueAttribute : Attribute, ITargetsInParam, ISpecifiesInput
    {
    }

    /// <summary>
    /// Indicates that a parameter will take the value of the specified <see langword="out"/> parameter
    /// of the previous invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesOutValueAttribute : Attribute, ITargetsInParam, ISpecifiesInput
    {
        /// <summary>
        /// Gets the index of the <see langword="out"/> parameter referenced.
        /// </summary>
        public int ParameterIndex { get; }
        /// <summary>
        /// Constructs a <see cref="TakesOutValueAttribute"/> with the index of the <see langword="out"/>  parameter to reference.
        /// </summary>
        /// <remarks>
        /// <paramref name="index"/> <b>must</b> be a 0-indexed reference to an <see langword="out"/> parameter.
        /// </remarks>
        /// <param name="index">The index of the <see langword="out"/> parameter to take the value of.</param>
        public TakesOutValueAttribute(int index)
            => ParameterIndex = index;
    }

}
