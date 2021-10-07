using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hive.Utilities
{
    public class ReadOnlySubList<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> inner;
        private readonly int start;

        public int Count { get; }

        public ReadOnlySubList(IReadOnlyList<T> list, int start, int length)
            => (inner, this.start, Count) = (list, start, length);

        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return inner[start + index];
            }
        }

        public IEnumerator<T> GetEnumerator() => inner.Skip(start).Take(Count).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
