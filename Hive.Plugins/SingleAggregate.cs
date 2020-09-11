using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    internal class SingleAggregate<T> : IAggregate<T>
    {
        public SingleAggregate(T instance)
            => Instance = instance;

        public T Instance { get; }
    }
}
