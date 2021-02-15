using System.Collections.Generic;

namespace Hive.Plugins
{
    internal interface IAggregateList<out T>
    {
        IEnumerable<T> List { get; }
    }
}