using Hive.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hive.Tests
{
    public class SingleAggregate<T> : IAggregate<T>
        where T : class
    {
        public T Instance { get; }

        public SingleAggregate(T inst)
            => Instance = inst;
    }
}
