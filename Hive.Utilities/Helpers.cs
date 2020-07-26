using System;
using System.Collections.Generic;
using System.Text;

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
            for (int i = 0; i < count; i++)
                yield return val;
        }
    }
}
