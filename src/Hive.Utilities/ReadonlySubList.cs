using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hive.Utilities
{
    /// <summary>
    /// A read-only list that acts as a slice over another list.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    public class ReadOnlySubList<T> : IReadOnlyList<T>
    {
        private readonly IReadOnlyList<T> inner;
        private readonly int start;

        /// <inheritdoc/>
        public int Count { get; }

        /// <summary>
        /// Constructs a new <see cref="ReadOnlySubList{T}"/> as a view over <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The input list to act as a view over.</param>
        /// <param name="start">The start of the view.</param>
        /// <param name="length">The length of the view.</param>
        public ReadOnlySubList(IReadOnlyList<T> list, int start, int length)
            => (inner, this.start, Count) = (list, start, length);

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                if (index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return inner[start + index];
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => inner.Skip(start).Take(Count).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
