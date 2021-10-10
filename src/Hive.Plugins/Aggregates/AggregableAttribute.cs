using System;

namespace Hive.Plugins.Aggregates
{
    /// <summary>
    /// Indicates that an interface can be used in an <see cref="IAggregate{T}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public sealed class AggregableAttribute : Attribute
    {
        /// <summary>
        /// Gets the type of the default implementation for this aggregate. This will be used if no services of the aggregated type are registered.
        /// </summary>
        public Type? Default { get; set; }
    }
}
