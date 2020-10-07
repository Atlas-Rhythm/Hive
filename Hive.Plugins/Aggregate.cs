using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    internal class Aggregate<T> : IAggregate<T>
        where T : class
    {
        private Aggregate(IEnumerable<T> aggregate)
        {
            Instance = AggregatedInstanceGenerator<T>.Create(aggregate);
        }

        public Aggregate(IServiceProvider services) : this(services.GetServices<T>())
        {
        }

        public T Instance { get; }
    }
}