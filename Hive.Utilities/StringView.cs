using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hive.Utilities
{
    /// <summary>
    /// A relatively light weight view into a string.
    /// </summary>
    [DebuggerDisplay("{AsString}", Type = "StringView")]
    public struct StringView : IEnumerable<char>, IEquatable<StringView>, IEquatable<string>
    {
        /// <summary>
        /// Gets the string that this is a view into.
        /// </summary>
        public string BaseString { get; }
        /// <summary>
        /// Gets the starting index of the view in <see cref="BaseString"/>.
        /// </summary>
        public int Start { get; }
        /// <summary>
        /// Gets the length of this view.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Constructs a <see cref="StringView"/> that is a view of the entire <paramref name="source"/> string.
        /// </summary>
        /// <param name="source">The string to wrap.</param>
        public StringView(string source)
        {
            BaseString = source;
            Start = 0;
            Length = source.Length;
        }

        /// <summary>
        /// Constructs a <see cref="StringView"/> that is a specified region of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The string to wrap.</param>
        /// <param name="start">The starting index of the view.</param>
        /// <param name="len">The length of the view.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="start"/> or <paramref name="len"/> point out of bounds of <paramref name="source"/>.
        /// </exception>
        public StringView(string source, int start, int len)
        {
            if (start > source.Length)
                throw new ArgumentException("Start is past the end of the string", nameof(start));
            if (start + len > source.Length)
                throw new ArgumentException("Length is longer than the string", nameof(len));

            BaseString = source;
            Start = start;
            Length = len;
        }

        /// <summary>
        /// Constructs a <see cref="StringView"/> that is a specified region of <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="StringView"/> acting as the source string.</param>
        /// <param name="start">The starting index of the view.</param>
        /// <param name="len">The length of the view.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="start"/> or <paramref name="len"/> point out of bounds of <paramref name="source"/>.
        /// </exception>
        public StringView(StringView source, int start, int len)
        {
            if (start > source.Length)
                throw new ArgumentException("Start is past the end of the string", nameof(start));
            if (start + len > source.Length)
                throw new ArgumentException("Length is longer than the string", nameof(len));

            BaseString = source.BaseString;
            Start = source.Start + start;
            Length = len;
        }

        /// <summary>
        /// Gets the character at the specified index in the view.
        /// </summary>
        /// <param name="index">The index of the character to get.</param>
        /// <returns>The character at <paramref name="index"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown if <paramref name="index"/> is out of bounds of this view.
        /// </exception>
        public char this[int index]
        {
            get
            {
                if (index >= Length || index < 0)
                    throw new IndexOutOfRangeException();
                return BaseString[Start + index];
            }
        }

        /// <summary>
        /// Gets a view of the substring starting at the provided index, and ending at the end of the view.0
        /// </summary>
        /// <param name="start">The starting index of the resulting view.</param>
        /// <returns>A view over the substring.</returns>
        /// <seealso cref="Substring(int, int)"/>
        public StringView Substring(int start)
            => Substring(start, Length - start);
        /// <summary>
        /// Gets a view of a substring of this view. Equivalent to <see cref="StringView(StringView, int, int)"/>.
        /// </summary>
        /// <param name="start">The starting index of the substring.</param>
        /// <param name="length">The length of the substring.</param>
        /// <returns>A view over the substring.</returns>
        /// <seealso cref="StringView(StringView, int, int)"/>
        public StringView Substring(int start, int length)
            => new StringView(this, start, length);

        /// <summary>
        /// Lazily splits this view into sub-views, using <paramref name="sep"/> as the seperator.
        /// </summary>
        /// <param name="sep">The seperator to split this view by.</param>
        /// <param name="ignoreEmpty">Whether or not to ignore empty splits.</param>
        /// <returns>An enumerable which lazily returns split elements.</returns>
        public IEnumerable<StringView> Split(StringView sep, bool ignoreEmpty = true)
        {
            if (Length < sep.Length)
            {
                if (!(ignoreEmpty && Length == 0))
                    yield return this;
                yield break;
            }

            int i = 0;
            int regionBegin = 0;
            for (; i < Length; i++)
            {
                int loopStart = i;
                int j = 0;
                while (i < Length && j < sep.Length && this[i] == sep[j]) { i++; j++; }
                if (j == sep.Length)
                {
                    if (!ignoreEmpty || loopStart - regionBegin != 0)
                        yield return new StringView(this, regionBegin, loopStart - regionBegin);
                    regionBegin = i--;
                }
            }

            if (!ignoreEmpty || Length - regionBegin != 0)
                yield return new StringView(this, regionBegin, Length - regionBegin);
        }

        /// <summary>
        /// Implicitly converts a <see cref="string"/> to a <see cref="StringView"/>, using <see cref="StringView(string)"/>.
        /// </summary>
        /// <param name="value">The string to wrap.</param>
        /// <seealso cref="StringView(string)"/>
        public static implicit operator StringView(string value)
            => new StringView(value);

        /// <summary>
        /// Compares content of the two <see cref="StringView"/>s for equality.
        /// </summary>
        /// <param name="a">The first view to compare.</param>
        /// <param name="b">The second view to compare.</param>
        /// <returns><see langword="true"/> if the two are equal, <see langword="false"/> otherwise.</returns>
        /// <see cref="Equals(StringView)"/>
        public static bool operator ==(StringView a, StringView b)
            => a.Equals(b);

        /// <summary>
        /// Compares content of the two <see cref="StringView"/>s for inequality.
        /// </summary>
        /// <param name="a">The first view to compare.</param>
        /// <param name="b">The second view to compare.</param>
        /// <returns><see langword="true"/> if the two are not equal, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="operator=="/>
        public static bool operator !=(StringView a, StringView b)
            => !(a == b);

        /// <summary>
        /// Compares this view with another object.
        /// </summary>
        /// <remarks>
        /// This will compare against both <see cref="StringView"/>s and <see cref="string"/>s.
        /// </remarks>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><see langword="true"/> if the two are equal, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(StringView)"/>
        public override bool Equals(object obj)
            => (obj is StringView sv && Equals(sv))
            || (obj is string s && Equals(s));

        /// <summary>
        /// Compares the content of this view to the provided string.
        /// </summary>
        /// <param name="other">The string to compare to.</param>
        /// <returns><see langword="true"/> if the two are equal, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="Equals(StringView)"/>
        public bool Equals(string other) => Equals((StringView)other);
        /// <summary>
        /// Compares the content of this view to the provided view for equality.
        /// </summary>
        /// <param name="other">The view to compare to.</param>
        /// <returns><see langword="true"/> if the two are equal, <see langword="false"/> otherwise.</returns>
        public bool Equals(StringView other)
        {
            if (ReferenceEquals(BaseString, other.BaseString))
            {
                if (Start == other.Start && Length == other.Length)
                    return true; // they can still be equal if this isn't true
            }
            if (Length != other.Length) return false;
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != other[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Concatenates an array of <see cref="StringView"/>s into a single <see cref="StringView"/>.
        /// </summary>
        /// <param name="views">An array of views to concatenate.</param>
        /// <returns>The concatenated <see cref="StringView"/>.</returns>
        public static StringView Concat(params StringView[] views) => Concat(views.AsEnumerable());
        /// <summary>
        /// Concatenates a sequence of <see cref="StringView"/>s into a single <see cref="StringView"/>.
        /// </summary>
        /// <param name="views">A sequence of views to concatenate.</param>
        /// <returns>The concatenated <see cref="StringView"/>.</returns>
        public static StringView Concat(IEnumerable<StringView> views)
        {
            var list = views.ToList(); // evaluate the parameter exactly once
            if (list.Count == 0) return "";

            int i = 0;
            var root = list[i++];
            for (; i < list.Count; i++)
            {
                var view = list[i];
                if (ReferenceEquals(root.BaseString, view.BaseString))
                { // they reference the same underlying string
                    if (root.Start + root.Length == view.Start)
                    { // and view starts immediately after root
                        root = new StringView(root.BaseString, root.Start, root.Length + view.Length);
                        continue;
                    }
                }
                if (root.BaseString.Length >= root.Start + root.Length + view.Length
                    && view == new StringView(root.BaseString, root.Start + root.Length, view.Length))
                { // view has the same content as immediately after root
                    root = new StringView(root.BaseString, root.Start, root.Length + view.Length);
                    continue;
                }
                else if (view.BaseString.Length >= view.Length + root.Length && view.Start >= root.Length
                    && root == new StringView(view.BaseString, view.Start - root.Length, root.Length))
                { // root has the same content as immediately befire view
                    root = new StringView(view.BaseString, view.Start - root.Length, view.Length + root.Length);
                    continue;
                }
                else
                {
                    // we cannot re-use the existing strings, so we gotta build up a new allocation
                    goto BuildWithNewAllocation;
                }
            }

            return root;

        BuildWithNewAllocation:

            var sb = new StringBuilder(root.ToString(), list.Select(v => v.Length).Sum());
            for (; i < list.Count; i++)
            {
                var sv = list[i];
                if (sv.Length == 0) continue;
                sb.Append(sv.BaseString, sv.Start, sv.Length);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Concatenates two <see cref="StringView"/>s into a single <see cref="StringView"/>.
        /// </summary>
        /// <param name="a">The first view to concatenate.</param>
        /// <param name="b">The second view to concatenate.</param>
        /// <returns>The concatenated <see cref="StringView"/>.</returns>
        public static StringView operator +(StringView a, StringView b) => Concat(a, b);

        /// <summary>
        /// Gets the hash code of the content of this view.
        /// </summary>
        /// <returns>The hash code for the content of this view.</returns>
        public override int GetHashCode()
        {
            int code = Length ^ unchecked(0x0adeb890);
            code ^= code << 8;
            code ^= code << 16;
            for (int i = 0; i < Length; i++)
            {
                code ^= ((ushort)BaseString[Start + i]) << 8 * (i % 4);
            }
            return code;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string AsString => ToString();

        /// <summary>
        /// Gets the content of this view as a <see cref="string"/>.
        /// </summary>
        /// <returns>The string content of this view.</returns>
        public override string ToString()
            => BaseString?.Substring(Start, Length) ?? "";

        /// <summary>
        /// Gets an enumerator for the characters in this view.
        /// </summary>
        /// <returns>An enumerator capable of enumerating this view's characters.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator capable of enumerating the characters in a <see cref="StringView"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<char>
        {
            private readonly string source;
            private readonly int start;
            private readonly int end;
            private int current;

            internal Enumerator(StringView source)
            {
                this.source = source.BaseString;
                start = source.Start;
                end = start + source.Length;
                current = start-1;
            }

            /// <summary>
            /// Gets the current character.
            /// </summary>
            public char Current => source[current];

            object IEnumerator.Current => Current;

            /// <summary>
            /// Disposes of this enumerator.
            /// </summary>
            public void Dispose() { }
            /// <summary>
            /// Advances to the next character.
            /// </summary>
            /// <returns><see langword="true"/> if there is another character, <see langword="false"/> otherwise.</returns>
            public bool MoveNext() => ++current < end;
            /// <summary>
            /// Resets this enumerator to the beginning of the view.
            /// </summary>
            public void Reset() => current = start-1;
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
