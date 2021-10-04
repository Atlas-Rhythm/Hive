using System;
using System.Collections.Generic;

namespace Hive.Plugins.Aggregates
{
    /// <summary>
    /// An aggregation of a collection of <typeparamref name="T"/> implementations.
    /// </summary>
    /// <typeparam name="T">The type of the implementation to use.</typeparam>
    public class Aggregate<T> : IAggregate<T>
        where T : class
    {
        /// <summary>
        /// Creates an aggregate instance from the provided enumerable of <typeparamref name="T"/>.
        /// </summary>
        /// <param name="aggregate">The collection to create the aggregation with.</param>
        /// <param name="services">The service provider to use to construct the default implementation, if any.</param>
        public Aggregate(IEnumerable<T> aggregate, IServiceProvider services)
        {
            Instance = AggregatedInstanceGenerator<T>.Create(aggregate, services);
        }

        /// <inheritdoc/>
        public T Instance { get; }
    }
}
