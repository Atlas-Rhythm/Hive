using System.Collections.Generic;

namespace Hive.Utilities
{
    /// <summary>
    /// A partitioner capable of partitioning a sequence of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the input sequence.</typeparam>
    public interface IPartitioner<T>
    {
        /// <summary>
        /// Partitions the input sequence.
        /// </summary>
        /// <param name="source">The input sequence.</param>
        /// <returns>A sequence of partitioned sequences.</returns>
        IEnumerable<IEnumerable<T>> Partition(IEnumerable<T> source);
    }
}
