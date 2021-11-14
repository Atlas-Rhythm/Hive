using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Hive.Utilities
{
    /// <summary>
    /// A read only list which is populated lazily from an <see cref="IEnumerable{T}"/>, as elements
    /// are needed.
    /// </summary>
    /// <typeparam name="T">The element type of the list.</typeparam>
    public class LazyList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> cacheList = new();
        private readonly IEnumerable<T> source;
        private IEnumerator<T>? srcEnum;
        private bool completed;
        private bool disposedValue;

        /// <summary>
        /// Constructs a new <see cref="LazyList{T}"/> with the specified sequence.
        /// </summary>
        /// <param name="src">The sequence to build this list from.</param>
        public LazyList(IEnumerable<T> src)
            => source = src;

        private void ReadToIndex(int index)
        {
            if (cacheList.Count > index && index != -1)
                return;

            lock (cacheList)
            {
                if (completed)
                    return;

                srcEnum ??= source.GetEnumerator();

                var result = true;
                while ((cacheList.Count <= index || index == -1) && (result = srcEnum.MoveNext()))
                {
                    cacheList.Add(srcEnum.Current);
                }

                if (!result)
                {
                    // we finished evaluating the enumerator, so lets dispose it eagerly
                    Interlocked.Exchange(ref srcEnum, null)?.Dispose();
                    completed = true;
                }
            }
        }

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                if (index >= 0)
                    ReadToIndex(index);
                return cacheList[index];
            }
        }

        /// <summary>
        /// Attempts to get the count of items in the collection without
        /// fully enumerating the input sequence.
        /// </summary>
        /// <value><see langword="null"/> if the length of the collection cannot be
        /// determined without enumeration, otherwise the length of the collection.</value>
        public int? NoEnumerateCount
        {
            get
            {
                if (source is IReadOnlyCollection<T> collection)
                    return collection.Count;

                if (completed)
                    return cacheList.Count;

                return null;
            }
        }

        /// <inheritdoc/>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count
        {
            get
            {
                if (source is IReadOnlyCollection<T> collection)
                    return collection.Count;

                ReadToIndex(-1); // we have to evaluate the whole thing to get the count
                return cacheList.Count;
            }
        }

        /// <summary>
        /// Gets an enumerator for this list.
        /// </summary>
        /// <returns>An enumerator for this list.</returns>
        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// The enumerator type for <see cref="LazyList{T}"/>.
        /// </summary>
        public sealed class Enumerator : IEnumerator<T>
        {
            private readonly LazyList<T> list;
            private int index = -1;

            internal Enumerator(LazyList<T> list)
                => this.list = list;

            /// <inheritdoc/>
            public T Current => list[index];
            object? IEnumerator.Current => Current;

            /// <inheritdoc/>
            public bool MoveNext()
            {
                index++;
                list.ReadToIndex(index);
                var nec = list.NoEnumerateCount;
                return !(nec is { } val) || index < val;
            }

            /// <inheritdoc/>
            public void Reset() => index = -1;
            /// <inheritdoc/>
            public void Dispose() { }
        }

        /// <summary>
        /// Disposes the resources held by this object.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this should dispose managed state, <see langword="false"/> otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    srcEnum?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
