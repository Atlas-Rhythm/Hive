using System.Collections.Generic;

namespace Hive.Plugins.Aggregates
{
    internal interface IAggregateList<out T>
    {
        IEnumerable<T> List { get; }
    }
}
