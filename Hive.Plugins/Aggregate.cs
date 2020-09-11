using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    internal class Aggregate<T> : IAggregate<T>
    {
        public Aggregate(IEnumerable<T> aggregate)
        {

        }

        public T Instance => throw new NotImplementedException();
    }
}
