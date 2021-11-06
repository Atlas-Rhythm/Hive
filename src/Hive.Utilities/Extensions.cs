using System;
using System.Collections.Generic;
using System.Linq;

#if NETSTANDARD2_0

using System.Text;

#endif

namespace Hive.Utilities
{
    /// <summary>
    /// Provides helpful extension methods for various classes.
    /// </summary>
    public static class Extensions
    {
#if NETSTANDARD2_0

        /// <summary>
        /// Concatenates the strings of the provided array, using the specified separator between each string,
        /// then appends the result to the current instance of the string builder.
        /// </summary>
        /// <param name="sb">The string builder to append to.</param>
        /// <param name="seperator">The string to use as a separator. <paramref name="seperator"/> is included in
        /// the joined strings only if <paramref name="values"/> has more than one element.</param>
        /// <param name="values">An array that contains the strings to concatenate and append to the current instance of the string builder.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public static StringBuilder AppendJoin(this StringBuilder sb, string seperator, params string[] values)
        {
            if (sb is null) throw new ArgumentNullException(nameof(sb));

            for (var i = 0; i < values.Length; i++)
            {
                if (i != 0)
                    _ = sb.Append(seperator);
                _ = sb.Append(values[i]);
            }
            return sb;
        }

#endif

        /// <summary>
        /// Filters the provided sequence to contain only the non-null values in a null-safe way.
        /// </summary>
        /// <typeparam name="T">The type of the sequence elements.</typeparam>
        /// <param name="sequence">The sequence to filter.</param>
        /// <returns>A sequence which conains only non-null values.</returns>
        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T?> sequence) where T : class
            => sequence.Where(v => v is not null)!;

        /// <summary>
        /// Filters the provided sequence to contain only the non-null values in a null-safe way.
        /// </summary>
        /// <remarks>
        /// The only difference between this and <see cref="WhereNonNull{T}(IEnumerable{T?})"/> is that this operates on
        /// the value type <see cref="Nullable{T}"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the sequence elements.</typeparam>
        /// <param name="sequence">The sequence to filter.</param>
        /// <returns>A sequence which conains only non-null values.</returns>
        public static IEnumerable<T> WhereNonNull<T>(this IEnumerable<T?> sequence) where T : struct
            => sequence.Where(v => v.HasValue).Select(v => v!.Value);
    }
}
