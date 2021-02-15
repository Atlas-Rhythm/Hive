using System;

namespace Hive.Plugins
{
    /// <summary>
    /// Indicates that an interface can be used in an <see cref="IAggregate{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class AggregableAttribute : Attribute
    {
    }
}
