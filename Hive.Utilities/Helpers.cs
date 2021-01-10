using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Hive.Utilities
{
    /// <summary>
    /// A collection of helper functions for Hive.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Interleaves 2 enumerables with each other, starting with <paramref name="first"/> and alternating until
        /// both are out of items.
        /// </summary>
        /// <typeparam name="T">The element type of the enumerable.</typeparam>
        /// <param name="first">The first enumerable.</param>
        /// <param name="second">The second enumerable to be interleaved with <paramref name="first"/></param>
        /// <returns>An enumerable that is the parameters interleaved.</returns>
        public static IEnumerable<T> InterleaveWith<T>(this IEnumerable<T> first, IEnumerable<T> second)
        {
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null) throw new ArgumentNullException(nameof(second));

            var a = first.GetEnumerator();
            var b = second.GetEnumerator();

            while (true)
            {
                bool ba, bb;
                if (ba = a.MoveNext()) yield return a.Current;
                if (bb = b.MoveNext()) yield return b.Current;
                if (!ba && !bb) yield break;
            }
        }

        /// <summary>
        /// Repeats a value <paramref name="val"/> times.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="val">The value to repeat.</param>
        /// <param name="count">The number of times to repeat the value.</param>
        /// <returns>An enumerable that is simply <paramref name="val"/> repeated <paramref name="count"/> times.</returns>
        public static IEnumerable<T> Repeat<T>(T val, int count)
        {
            for (var i = 0; i < count; i++)
                yield return val;
        }

        /// <summary>
        /// Converts a tuple type to an array of the tuple elements.
        /// </summary>
        /// <typeparam name="T">The type of the tuple.</typeparam>
        /// <param name="tuple">The tuple to convert to an array.</param>
        /// <returns>The values in the tuple in an array.</returns>
        public static object?[] ToArray<T>(this ref T tuple) where T : struct, ITuple
        {
            var array = new object?[tuple.Length];
            for (var i = 0; i < tuple.Length; i++)
            {
                array[i] = tuple[i];
            }
            return array;
        }

        /// <summary>
        /// Flattens an <see cref="IEnumerable{T}"/> of <see cref="Task{T}"/> into an equivalent <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type being enumerated.</typeparam>
        /// <param name="enumerable">The enumerator of tasks to flatten.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> consisting of the result values of the tasks in <paramref name="enumerable"/>.</returns>
        public static async IAsyncEnumerable<T> FlattenToAsyncEnumerable<T>(this IEnumerable<Task<T>> enumerable)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var task in enumerable)
            {
                yield return await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Flattens an <see cref="IEnumerable{T}"/> of <see cref="ValueTask{T}"/> into an equivalent <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type being enumerated.</typeparam>
        /// <param name="enumerable">The enumerator of tasks to flatten.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> consisting of the result values of the tasks in <paramref name="enumerable"/>.</returns>
        public static async IAsyncEnumerable<T> FlattenToAsyncEnumerable<T>(this IEnumerable<ValueTask<T>> enumerable)
        {
            if (enumerable is null)
                throw new ArgumentNullException(nameof(enumerable));

            foreach (var task in enumerable)
            {
                yield return await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> consisting of every non-null element of <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the values in the enumerables.</typeparam>
        /// <param name="enumerable">The enumerable containing possibly null values.</param>
        /// <returns>An enumerable containing no null values.</returns>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable)
            => enumerable.Where(v => v is not null)!;

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerable{T}"/> consisting of every non-null element of <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the values in the enumerables.</typeparam>
        /// <param name="enumerable">The enumerable containing possibly null values.</param>
        /// <returns>An enumerable containing no null values.</returns>
        public static IAsyncEnumerable<T> WhereNotNull<T>(this IAsyncEnumerable<T?> enumerable)
            => enumerable.Where(v => v is not null)!;
    }
}
