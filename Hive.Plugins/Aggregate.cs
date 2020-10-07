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

        /// <summary>
        /// Create an aggregate instance from an <see cref="IServiceProvider"/>.
        /// This call forwards to <see cref="Aggregate{T}.Aggregate(IEnumerable{T})"/>.
        /// </summary>
        /// <param name="services">The service provider to use.</param>
        public Aggregate(IServiceProvider services) : this(services.GetServices<T>())
        {
        }

        /// <inheritdoc/>
        public T Instance { get; }
    }
}