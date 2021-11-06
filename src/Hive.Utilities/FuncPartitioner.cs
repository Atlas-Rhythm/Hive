using System;
using System.Collections.Generic;

namespace Hive.Utilities
{
    /// <summary>
    /// A class containing helpers to convert partiioner delegates into <see cref="IPartitioner{T}"/>s.
    /// </summary>
    public static class FuncPartitioner
    {
        /// <summary>
        /// Converts a delegate into a partitioner.
        /// </summary>
        /// <typeparam name="T">The sequence element type.</typeparam>
        /// <param name="func">The delegate to create a partitioner from.</param>
        /// <returns>A partitioner whose partition method invokes <paramref name="func"/>.</returns>
        public static IPartitioner<T> From<T>(Func<IEnumerable<T>, IEnumerable<IEnumerable<T>>> func)
            => new FuncPartitioner<T>(func);
    }

    internal sealed class FuncPartitioner<T> : IPartitioner<T>
    {
        private readonly Func<IEnumerable<T>, IEnumerable<IEnumerable<T>>> func;

        public FuncPartitioner(Func<IEnumerable<T>, IEnumerable<IEnumerable<T>>> func)
            => this.func = func;

        public IEnumerable<IEnumerable<T>> Partition(IEnumerable<T> source) => func(source);
    }
}
