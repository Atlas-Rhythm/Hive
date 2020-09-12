using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    internal interface IAggregatorAttribute { }
    internal interface ITargetsInParam : IAggregatorAttribute { }
    internal interface ITargetsOutParam : IAggregatorAttribute { }
    internal interface ITargetsReturn : IAggregatorAttribute { }

    internal interface IRequiresType : IAggregatorAttribute
    {
        bool CheckType(Type type);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class StopIfReturnsAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType
    {
        public bool ReturnValue { get; }
        public StopIfReturnsAttribute(bool returnValue)
            => ReturnValue = returnValue;

        bool IRequiresType.CheckType(Type type)
            => type == typeof(bool);
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class StopIfReturnsNullAttribute : Attribute, ITargetsOutParam, ITargetsReturn, IRequiresType
    {
        bool IRequiresType.CheckType(Type type)
            => !type.IsValueType || (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ReturnLastAttribute : Attribute, ITargetsOutParam, ITargetsReturn
    {
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public sealed class AggregateWithAttribute : Attribute, ITargetsOutParam, ITargetsReturn
    {
        public Type TypeWithAggregator { get; }
        public string AggregatorName { get; }
        public AggregateWithAttribute(Type targetType, string targetName)
            => (TypeWithAggregator, AggregatorName) = (targetType, targetName);
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesReturnValueAttribute : Attribute, ITargetsInParam
    {
    }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class TakesOutParamValueAttribute : Attribute, ITargetsOutParam
    {
        public int ParameterIndex { get; }
        public TakesOutParamValueAttribute(int paramIndex)
            => ParameterIndex = paramIndex;
    }

}
