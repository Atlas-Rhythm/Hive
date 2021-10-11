using System;
using System.Collections.Generic;

namespace Hive.Utilities
{
    public static class FuncPartitioner
    {
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
