using System;
using System.Collections.Generic;
using System.Linq;
#if NETSTANDARD2_0
using System.Linq.Expressions;
#else
using System.Runtime.CompilerServices;
#endif
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
        /// Converts a value tuple type to an array of the tuple elements.
        /// </summary>
        /// <typeparam name="T">The type of the tuple.</typeparam>
        /// <param name="tuple">The tuple to convert to an array.</param>
        /// <returns>The values in the tuple in an array.</returns>
#if !NETSTANDARD2_0
        public static object?[] ToArray<T>(this ref T tuple) where T : struct, ITuple
        {
            var array = new object?[tuple.Length];
            for (var i = 0; i < tuple.Length; i++)
            {
                array[i] = tuple[i];
            }
            return array;
        }
#else
        public static object?[] ToArray<T>(this ref T tuple) where T : struct // muust be a variant of ValueTuple
        {
            if (typeof(T) == typeof(ValueTuple))
                return Array.Empty<object?>();

            var typeData = ValueTupleTypeData<T>.Instance;

            if (!typeData.IsValueTuple)
                throw new ArgumentException("This ToArray implementation only operates on ValueTuples");

            using var result = new ArrayBuilder<object?>(8);

            object tupleObj = tuple;

            var lastValueSet = false;
            object? lastValue = null;
            do
            {
                foreach (var getter in typeData.ValueGetters)
                {
                    if (lastValueSet)
                        result.Add(lastValue);

                    lastValue = getter(tupleObj);
                    lastValueSet = true;
                }

                if (typeData.HasLastArgTuple)
                {
                    tupleObj = lastValue!;
                    typeData = typeData.LastArgTypeData;
                    continue;
                }
                break;
            }
            while (true);

            if (lastValueSet)
                result.Add(lastValue);

            return result.ToArray();
        }

        private interface IValueTupleTypeData
        {
            Type Type { get; }
            bool IsValueTuple { get; }
            IReadOnlyList<Type> TypeArguments { get; }
            IReadOnlyList<Func<object, object?>> ValueGetters { get; }
            bool HasLastArgTuple { get; }
            IValueTupleTypeData LastArgTypeData { get; }
        }

        private class ValueTupleTypeData<T> : IValueTupleTypeData where T : struct
        {
            public static readonly IValueTupleTypeData Instance = new ValueTupleTypeData<T>();

            public Type Type { get; } = typeof(T);
            public bool IsValueTuple { get; }

            // we don't need to support just base ValueTuple
            private Type[]? typeArgs;
            public IReadOnlyList<Type> TypeArguments
                => typeArgs ??= Type.GetGenericArguments();

            private Func<object, object?>[]? valueGetters;
            public IReadOnlyList<Func<object, object?>> ValueGetters
                => valueGetters ??= MakeValueGetters(Type);

            private bool? hasLastArgTuple;
            public bool HasLastArgTuple
                => hasLastArgTuple ??= TypeArguments.Count > 7 && TypeIsValueTuple(TypeArguments[7]);

            private IValueTupleTypeData? lastArgData;
            public IValueTupleTypeData LastArgTypeData
                => lastArgData ??= (IValueTupleTypeData)typeof(ValueTupleTypeData<>).MakeGenericType(TypeArguments[7]).GetField(nameof(Instance)).GetValue(null);

            private ValueTupleTypeData()
                => IsValueTuple = TypeIsValueTuple(Type);

            private static bool TypeIsValueTuple(Type type)
                => type.FullName.StartsWith("System.ValueTuple`");

            private static Func<object, object?>[] MakeValueGetters(Type type)
                => type.GetFields()
                    .Select(f => (f, p: Expression.Parameter(typeof(object))))
                    .Select(t => (t.p, e: Expression.Convert(Expression.Field(Expression.Convert(t.p, type), t.f), typeof(object))))
                    .Select(t => Expression.Lambda<Func<object, object?>>(t.e, t.p).Compile())
                    .ToArray();
        }
#endif
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
