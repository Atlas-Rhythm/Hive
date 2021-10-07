using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Hive.Utilities
{
    public class LazyList<T> : IReadOnlyList<T>, IDisposable
    {
        private readonly List<T> cacheList = new();
        private readonly IEnumerable<T> source;
        private IEnumerator<T>? srcEnum;
        private bool completed;
        private bool disposedValue;

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
                    Interlocked.Exchange<IEnumerator<T>?>(ref srcEnum, null)?.Dispose();
                    completed = true;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= 0)
                    ReadToIndex(index);
                return cacheList[index];
            }
        }

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

        public Enumerator GetEnumerator() => new(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public sealed class Enumerator : IEnumerator<T>
        {
            private readonly LazyList<T> list;
            private int index = -1;

            internal Enumerator(LazyList<T> list)
                => this.list = list;

            public T Current => list[index];
            object? IEnumerator.Current => Current;

            public bool MoveNext()
            {
                index++;
                list.ReadToIndex(index);
                var nec = list.NoEnumerateCount;
                return !(nec is { } val) || index < val;
            }

            public void Reset() => index = -1;
            public void Dispose() { }
        }

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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
