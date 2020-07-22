using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Plugin
{
    // TODO
    public class Aggregation<T> : IAggregate<T> where T : IPlugin
    {
        private readonly T t;

        public Aggregation(T t)
        {
            this.t = t;
        }

        public T Combine()
        {
            return t;
        }
    }
}