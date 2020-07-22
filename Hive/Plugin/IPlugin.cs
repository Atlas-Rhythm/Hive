using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Plugin
{
    public interface IAggregate<out T> where T : IPlugin
    {
        T Combine();
    }

    public interface IPlugin
    {
    }
}