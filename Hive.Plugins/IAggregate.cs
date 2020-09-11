using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Plugins
{
    public interface IAggregate<out T>
        where T : class
    {
        T Instance { get; }
    }
}
