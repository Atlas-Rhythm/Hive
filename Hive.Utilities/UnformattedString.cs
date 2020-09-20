using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Hive.CodeGen;

namespace Hive.Utilities
{
    /// <summary>
    /// A wrapper struct representing an unformatted format string with associated culture.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument.</typeparam>
    /// <typeparam name="TRest">The type of the eighth argument, or a <see cref="ValueTuple"/> type holding the remaining elements.</typeparam>
    [ParameterizeGenericParameters(1, 7)]
    public partial struct UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> : IEquatable<UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest>>
        where TRest : struct
    {
        private readonly CultureInfo Culture;
        private readonly string FormatString;
        
        /// <summary>
        /// Constructs a new wrapper for the provided culture and format string.
        /// </summary>
        /// <param name="culture">The culture info to use to format the string.</param>
        /// <param name="formatString">The unformatted format string.</param>
        public UnformattedString(CultureInfo culture, string formatString)
            => (Culture, FormatString) = (culture, formatString);

        /// <summary>
        /// Formats the internal format string using <paramref name="args"/> as arguments.
        /// </summary>
        /// <param name="args">The tuple holding the values to format.</param>
        /// <returns>The formatted string.</returns>
        public string Format(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> args)
            => string.Format(Culture, FormatString, args.ToArray());

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> ufsr && Equals(ufsr);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Culture, FormatString);

        /// <inheritdoc/>
        public bool Equals(UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> other)
            => Culture == other.Culture && FormatString == other.FormatString;

        /// <summary>
        /// Compares two unformatted strings for equality.
        /// </summary>
        /// <param name="left">The first string to compare.</param>
        /// <param name="right">The second string to compare.</param>
        /// <returns><see langword="true"/> if the arguments are equal, <see langword="false"/> otherwise.</returns>
        public static bool operator ==(UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> left, UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> right)
            => left.Equals(right);

        /// <summary>
        /// Compares two unformatted strings for inequality.
        /// </summary>
        /// <param name="left">The first string to compare.</param>
        /// <param name="right">The second string to compare.</param>
        /// <returns><see langword="true"/> if the arguments are not equal, <see langword="false"/> otherwise.</returns>
        public static bool operator !=(UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> left, UnformattedString<T1, T2, T3, T4, T5, T6, T7, TRest> right)
            => !(left == right);
    }

    /// <summary>
    /// A wrapper struct representing an unformatted format string with associated culture.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="T2">The type of the second argument.</typeparam>
    /// <typeparam name="T3">The type of the third argument.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument.</typeparam>
    [ParameterizeGenericParameters(1, 6)]
    public partial struct UnformattedString<T1, T2, T3, T4, T5, T6, T7>
    {
        /// <summary>
        /// Formats the internal format string using the provided values.
        /// </summary>
        /// <param name="_1">The first value.</param>
        /// <param name="_2">The second value.</param>
        /// <param name="_3">The third value.</param>
        /// <param name="_4">The fourth value.</param>
        /// <param name="_5">The fifth value.</param>
        /// <param name="_6">The sixth value.</param>
        /// <param name="_7">The seventh value.</param>
        /// <returns>The formatted string.</returns>
        /// <seealso cref="Format(ValueTuple{T1, T2, T3, T4, T5, T6, T7})"/>
        public string Format(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7)
            => Format(new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_1, _2, _3, _4, _5, _6, _7));
    }
}
