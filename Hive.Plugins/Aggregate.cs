using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
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
        public Aggregate(IEnumerable<T> aggregate)
        {
            Instance = AggregatedInstanceGenerator<T>.Create(aggregate);
        }

        /// <inheritdoc/>
        public T Instance { get; }
    }
}