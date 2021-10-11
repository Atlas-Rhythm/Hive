using System.Collections.Generic;

namespace Hive.Utilities
{
    public interface IPartitioner<T>
    {
        IEnumerable<IEnumerable<T>> Partition(IEnumerable<T> source);
    }
}
