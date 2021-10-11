using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (values is null) throw new ArgumentNullException(nameof(values));

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
        /// Appends a <see cref="StringView"/> to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The string builder to append to.</param>
        /// <param name="text">The string view to append.</param>
        /// <returns>The given string builder.</returns>
        public static StringBuilder Append(this StringBuilder sb, StringView text)
        {
            if (sb is null) throw new ArgumentNullException(nameof(sb));
            return sb.Append(text.BaseString, text.Start, text.Length);
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> which is exactly <paramref name="seq"/>, except without the item at
        /// <paramref name="index"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences being operated on.</typeparam>
        /// <param name="seq">The input sequence.</param>
        /// <param name="index">The index to skip.</param>
        /// <returns>A sequence containing all element except the one at <paramref name="index"/>.</returns>
        public static IEnumerable<T> SkipIndex<T>(this IEnumerable<T> seq, int index)
            => seq.Zip(Helpers.Indexes(), (a, b) => (a, b))
                .Where(t => t.b != index)
                .Select(t => t.a);

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> which is exactly <paramref name="seq"/>, except without the items
        /// with indicies between <paramref name="startIndex"/> and <paramref name="lastIndex"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the sequences being operated on.</typeparam>
        /// <param name="seq">The input sequence.</param>
        /// <param name="startIndex">The first index to skip.</param>
        /// <param name="lastIndex">The last index to skip.</param>
        /// <returns>A sequence containing all element except the one between <paramref name="startIndex"/> and <paramref name="lastIndex"/>.</returns>
        public static IEnumerable<T> SkipIndicies<T>(this IEnumerable<T> seq, int startIndex, int lastIndex)
            => seq.Zip(Helpers.Indexes(), (a, b) => (a, b))
                .Where(t => t.b < startIndex || t.b > lastIndex)
                .Select(t => t.a);

        /// <summary>
        /// Converts the provided sequence to a <see cref="LazyList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the sequence.</typeparam>
        /// <param name="seq">The input sequence.</param>
        /// <returns>A <see cref="LazyList{T}"/> containing the elements in <paramref name="seq"/>.</returns>
        public static LazyList<T> ToLazyList<T>(this IEnumerable<T> seq)
            => new(seq);

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> seq, IPartitioner<T> partitioner)
            => (partitioner ?? throw new ArgumentNullException(nameof(partitioner))).Partition(seq);

        /// <summary>
        /// Creates an <see cref="IReadOnlyList{T}"/> which is a slice over <paramref name="list"/> starting at index <paramref name="start"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="list">The input list.</param>
        /// <param name="start">The index to start the slice at.</param>
        /// <returns>A new <see cref="IReadOnlyList{T}"/> containing a slice over the input.</returns>
        public static IReadOnlyList<T> Slice<T>(this IReadOnlyList<T> list, int start)
            => new ReadOnlySubList<T>(list ?? throw new ArgumentNullException(nameof(list)), start, list.Count - start);

        /// <summary>
        /// Creates an <see cref="IReadOnlyList{T}"/> which is a slice over <paramref name="list"/> starting at index <paramref name="start"/>
        /// and containing <paramref name="length"/> elements.
        /// </summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="list">The input list.</param>
        /// <param name="start">The index to start the slice at.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>A new <see cref="IReadOnlyList{T}"/> containing a slice over the input.</returns>
        public static IReadOnlyList<T> Slice<T>(this IReadOnlyList<T> list, int start, int length)
            => new ReadOnlySubList<T>(list, start, length);

        /// <summary>
        /// Filters the provided sequence to contain only the non-null values in a null-safe way.
        /// </summary>
        /// <typeparam name="T">The type of the sequence elements.</typeparam>
        /// <param name="sequence">The sequence to filter.</param>
        /// <returns>A sequence which conains only non-null values.</returns>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence) where T : class
            => sequence.Where(v => v is not null)!;

        /// <summary>
        /// Filters the provided sequence to contain only the non-null values in a null-safe way.
        /// </summary>
        /// <remarks>
        /// The only difference between this and <see cref="WhereNotNull{T}(IEnumerable{T?})"/> is that this operates on
        /// the value type <see cref="Nullable{T}"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the sequence elements.</typeparam>
        /// <param name="sequence">The sequence to filter.</param>
        /// <returns>A sequence which conains only non-null values.</returns>
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> sequence) where T : struct
            => sequence.Where(v => v.HasValue).Select(v => v!.Value);


        /// <summary>
        /// Returns an <see cref="IAsyncEnumerable{T}"/> consisting of every non-null element of <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the values in the enumerables.</typeparam>
        /// <param name="enumerable">The enumerable containing possibly null values.</param>
        /// <returns>An enumerable containing no null values.</returns>
        public static IAsyncEnumerable<T> WhereNotNull<T>(this IAsyncEnumerable<T?> enumerable) where T : class
            => enumerable.Where(v => v is not null)!;

        /// <summary>
        /// Returns an <see cref="IAsyncEnumerable{T}"/> consisting of every non-null element of <paramref name="enumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the values in the enumerables.</typeparam>
        /// <param name="enumerable">The enumerable containing possibly null values.</param>
        /// <returns>An enumerable containing no null values.</returns>
        public static IAsyncEnumerable<T> WhereNotNull<T>(this IAsyncEnumerable<T?> enumerable) where T : struct
            => enumerable.Where(v => v.HasValue).Select(v => v!.Value);
    }
}
