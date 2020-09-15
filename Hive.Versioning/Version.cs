using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using Hive.Utilities;
using System.Diagnostics;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    /// <summary>
    /// A version that meets the Semantic Versioning specification.
    /// </summary>
    public class Version : IComparable<Version>, IEquatable<Version>
    {
        private readonly ulong major;
        private readonly ulong minor;
        private readonly ulong patch;
        private readonly string[] prereleaseIds;
        private readonly string[] buildIds;

        /// <summary>
        /// Gets the zero version (0.0.0).
        /// </summary>
        public static Version Zero { get; } = new Version(0, 0, 0);

        /// <summary>
        /// Parses and creates a version object from a sequence of characters.
        /// </summary>
        /// <remarks>
        /// This is roughly equivalent to <see cref="Parse(ReadOnlySpan{char})"/>.
        /// </remarks>
        /// <param name="text">The sequence of characters to parse as a version.</param>
        /// <exception cref="ArgumentException">Thrown when the input is not a valid SemVer version.</exception>
        public Version(ReadOnlySpan<char> text)
        {
            text = text.Trim();

            if (text.Length < 5)
                throw new ArgumentException("Input too short to be a SemVer version", nameof(text));

            if (!TryParseInternal(ref text, out major, out minor, out patch, out var preIds, out var buildIds) || text.Length > 0)
                throw new ArgumentException("Input was not a valid SemVer version", nameof(text));

            prereleaseIds = preIds;
            this.buildIds = buildIds;
        }

        /// <summary>
        /// Creates a version object from the component parts of the version.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="patch">The patch number.</param>
        /// <param name="prereleaseIds">A sequence of IDs specifying the prerelease.</param>
        /// <param name="buildIds">A sequence of IDs representing the build.</param>
        public Version(ulong major, ulong minor, ulong patch, IEnumerable<string> prereleaseIds, IEnumerable<string> buildIds)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.prereleaseIds = prereleaseIds.ToArray();
            this.buildIds = buildIds.ToArray();
        }

        /// <summary>
        /// Creates a version object from the component parts of the version.
        /// </summary>
        /// <param name="major">The major version number.</param>
        /// <param name="minor">The minor version number.</param>
        /// <param name="patch">The patch number.</param>
        public Version(ulong major, ulong minor, ulong patch) : this(major, minor, patch, Enumerable.Empty<string>(), Enumerable.Empty<string>())
        { }

        /// <summary>
        /// Gets the major version number.
        /// </summary>
        public ulong Major => major;
        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public ulong Minor => minor;
        /// <summary>
        /// Gets the patch number.
        /// </summary>
        public ulong Patch => patch;

        /// <summary>
        /// Gets the sequence of prerelease IDs.
        /// </summary>
        public IEnumerable<string> PreReleaseIds => prereleaseIds;
        /// <summary>
        /// Gets the sequence of build IDs.
        /// </summary>
        public IEnumerable<string> BuildIds => buildIds;

        /// <summary>
        /// Appends this <see cref="Version"/> to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        /// <returns>The provided <see cref="StringBuilder"/></returns>
        public StringBuilder ToString(StringBuilder sb)
        {
            if (sb is null)
                throw new ArgumentNullException(nameof(sb));

            sb.Append(Major).Append('.')
              .Append(Minor).Append('.')
              .Append(Patch);
            if (prereleaseIds.Length > 0)
            {
                sb.Append('-')
                  .AppendJoin(".", prereleaseIds);
            }
            if (buildIds.Length > 0)
            {
                sb.Append('+')
                  .AppendJoin(".", buildIds);
            }
            return sb;
        }

        /// <inheritdoc/>
        public override string ToString()
            => ToString(new StringBuilder()).ToString();

        /// <summary>
        /// Compares two versions for equality.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if they are equal, <see langword="false"/> otherwise.</returns>
        public static bool operator ==(Version? a, Version? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }
        /// <summary>
        /// Compares two versions for inequality.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if they are not equal, <see langword="false"/> otherwise.</returns>
        public static bool operator !=(Version? a, Version? b)
            => !(a == b);

        /// <summary>
        /// Checks if <paramref name="a"/> is greater than <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than <paramref name="b"/>, <see langword="false"/></returns>
        public static bool operator >(Version a, Version b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            return a.CompareTo(b) > 0;
        }

        /// <summary>
        /// Checks if <paramref name="a"/> is less than <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than <paramref name="b"/>, <see langword="false"/></returns>
        public static bool operator <(Version a, Version b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            return a.CompareTo(b) < 0;
        }
        /// <summary>
        /// Checks if <paramref name="a"/> is greater than or equal to <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is greater than or equal to <paramref name="b"/>, <see langword="false"/></returns>
        public static bool operator >=(Version a, Version b)
            => !(a < b);
        /// <summary>
        /// Checks if <paramref name="a"/> is less than or equal to <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first version to compare.</param>
        /// <param name="b">The second version to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> is less than or equal to <paramref name="b"/>, <see langword="false"/></returns>
        public static bool operator <=(Version a, Version b)
            => !(a > b);

        /// <summary>
        /// Determines the maximum of two versions.
        /// </summary>
        /// <param name="a">The first version.</param>
        /// <param name="b">The second version.</param>
        /// <returns>The maximum of <paramref name="a"/> and <paramref name="b"/></returns>
        public static Version Max(Version a, Version b)
            => a > b ? a : b;
        /// <summary>
        /// Determines the minimum of two versions.
        /// </summary>
        /// <param name="a">The first version.</param>
        /// <param name="b">The second version.</param>
        /// <returns>The minimum of <paramref name="a"/> and <paramref name="b"/></returns>
        public static Version Min(Version a, Version b)
            => a < b ? a : b;

        /// <summary>
        /// Compares <see langword="this"/> version to <paramref name="o"/> for equality.
        /// </summary>
        /// <param name="o">The object to compare to.</param>
        /// <returns><see langword="true"/> if they are equal, <see langword="false"/> otherwise.</returns>
        public override bool Equals(object o) => o is Version v && Equals(v);

        /// <summary>
        /// Gets the hash code of this <see cref="Version"/>.
        /// </summary>
        /// <returns>The hash code for this <see cref="Version"/>.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Major);
            hash.Add(Minor);
            hash.Add(Patch);
            foreach (var item in PreReleaseIds)
                hash.Add(item, StringComparer.Ordinal);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Compares this version to another version according to the SemVer specification.
        /// </summary>
        /// <param name="other">The version to compare to.</param>
        /// <returns><see langword="true"/> if the versions are equal, <see langword="false"/> otherwise.</returns>
        public bool Equals(Version other)
            => !(other is null)
            && Major == other.Major && Minor == other.Minor && Patch == other.Patch
            && prereleaseIds.Length == other.prereleaseIds.Length
            && prereleaseIds.Zip(other.prereleaseIds, (a, b) => a == b).All(a => a);

        /// <summary>
        /// Compares this version to another version according to the SemVer specification.
        /// </summary>
        /// <param name="other">The version to compare to.</param>
        /// <returns>Less than zero if <see langword="this"/> is less than <paramref name="other"/>, zero if they are equal, and 
        /// more than zero if <see langword="this"/> is greater than <paramref name="other"/></returns>
        public int CompareTo(Version other)
        {
            if (other is null) throw new ArgumentNullException(nameof(other));

            var val = Major.CompareTo(other.Major);
            if (val != 0) return val;
            val = Minor.CompareTo(other.Minor);
            if (val != 0) return val;
            val = Patch.CompareTo(other.Patch);
            if (val != 0) return val;

            if (prereleaseIds.Length != 0 && other.prereleaseIds.Length == 0)
                return -1;
            if (prereleaseIds.Length == 0 && other.prereleaseIds.Length != 0)
                return 1;

            var len = Math.Min(prereleaseIds.Length, other.prereleaseIds.Length);
            for (int i = 0; i < len; i++)
            {
                var a = prereleaseIds[i];
                ulong? anum = null;
                var b = other.prereleaseIds[i];
                ulong? bnum = null;

                if (ulong.TryParse(a, out var an))
                    anum = an;
                if (ulong.TryParse(b, out var bn))
                    bnum = bn;

                if (anum != null && bnum == null)
                    return -1;
                if (anum == null && bnum != null)
                    return 1;

                if (anum != null && bnum != null)
                { // compare by numbers
                    val = anum.Value.CompareTo(bnum.Value);
                    if (val != 0) return val;
                }
                else
                { // compare by strings
                    val = string.CompareOrdinal(a, b);
                    if (val != 0) return val;
                }
            }

            if (prereleaseIds.Length > other.prereleaseIds.Length)
                return 1;
            if (prereleaseIds.Length < other.prereleaseIds.Length)
                return -1;
            return 0;
        }

        /// <summary>
        /// Parses a sequence of characters into a <see cref="Version"/> object.
        /// </summary>
        /// <param name="text">The sequence of characters to parse.</param>
        /// <returns>The parsed version object.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is not a valid SemVer version.</exception>
        public static Version Parse(ReadOnlySpan<char> text)
        {
            if (!TryParse(text, out var ver))
                throw new ArgumentException("Input not a valid SemVer version", nameof(text));
            return ver;
        }

        /// <summary>
        /// Attempts to parse a sequence of characters into a version object.
        /// </summary>
        /// <param name="text">The sequence of characters to parse.</param>
        /// <param name="version">The parsed version, if the input is valid.</param>
        /// <returns><see langword="true"/> if the text is valid and could be parsed, <see langword="false"/> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Version version)
        {
            text = text.Trim();
            return TryParse(ref text, true, out version) && text.Length == 0;
        }

        /// <summary>
        /// Attempts to parse a sequence of characters into a version object, as part of a larger parse.
        /// </summary>
        /// <remarks>
        /// When this method returns, <paramref name="text"/> will begin after the end of the parsed version, if it is present, or
        /// what it initially contained if no version is present and this returns <see langword="false"/>
        /// </remarks>
        /// <param name="text">The sequence of characters to parse.</param>
        /// <param name="version">The parsed version, if the input is valid.</param>
        /// <returns><see langword="true"/> if the text is valid and could be parsed, <see langword="false"/> otherwise.</returns>
        public static bool TryParse(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Version version)
            => TryParse(ref text, false, out version);

        private static bool TryParse(ref ReadOnlySpan<char> text, bool checkLength, [MaybeNullWhen(false)] out Version version)
        {
            version = null;

            if (!TryParseInternal(ref text, out var maj, out var min, out var pat, out var pre, out var build))
                return false;
            if (checkLength && text.Length > 0)
                return false;

            version = new Version(maj, min, pat, pre, build);
            return true;
        }

        #region Parser
        private static bool TryParseInternal(
            ref ReadOnlySpan<char> text, 
            out ulong major,
            out ulong minor,
            out ulong patch,
            [MaybeNullWhen(false)] out string[] prereleaseIds,
            [MaybeNullWhen(false)] out string[] buildIds
        )
        {
            prereleaseIds = null;
            buildIds = null;

            if (!TryParseCore(ref text, out major, out minor, out patch))
                return false;

            if (TryTake(ref text, '-'))
            {
                if (!TryParsePreRelease(ref text, out prereleaseIds))
                    return false;
            }
            else
            {
                prereleaseIds = Array.Empty<string>();
            }

            if (TryTake(ref text, '+'))
            {
                if (!TryParseBuild(ref text, out buildIds))
                    return false;
            }
            else
            {
                buildIds = Array.Empty<string>();
            }

            return true;
        }

        private static bool TryParseCore(ref ReadOnlySpan<char> text, out ulong major, out ulong minor, out ulong patch)
        {
            minor = 0;
            patch = 0;

            var copy = text;
            if (!TryParseNumId(ref text, out major))
            {
                text = copy;
                return false;
            }
            if (!TryTake(ref text, '.'))
            {
                text = copy;
                return false;
            }
            if (!TryParseNumId(ref text, out minor))
            {
                text = copy;
                return false;
            }
            if (!TryTake(ref text, '.'))
            {
                text = copy;
                return false;
            }
            if (!TryParseNumId(ref text, out patch))
            {
                text = copy;
                return false;
            }

            return true;
        }

        private static bool TryParsePreRelease(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out string[] prereleaseIds)
        {
            prereleaseIds = null;

            var copy = text;
            if (TryReadPreReleaseId(ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(new string(id));
                    if (!TryTake(ref text, '.'))
                    { // exit condition
                        prereleaseIds = ab.ToArray();
                        return true;
                    }
                }
                while (TryReadPreReleaseId(ref text, out id));

                ab.Clear();
                text = copy;
                return false;
            }

            return false;
        }

        private static bool TryReadPreReleaseId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id)
        {
            if (TryReadAlphaNumId(ref text, out id)) return true;
            if (TryReadNumId(ref text, out id)) return true;
            return false;
        }

        private static bool TryParseBuild(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out string[] buildIds)
        {
            buildIds = null;

            var copy = text;
            if (TryReadBuildId(ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(new string(id));
                    if (!TryTake(ref text, '.'))
                    { // exit condition
                        buildIds = ab.ToArray();
                        return true;
                    }
                }
                while (TryReadBuildId(ref text, out id));

                ab.Clear();
                text = copy;
                return false;
            }

            return false;
        }

        private static bool TryReadBuildId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id)
            => TryReadAlphaNumId(ref text, out id, true);

        private static bool TryReadAlphaNumId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id, bool skipNonDigitCheck = false)
        {
            if (text.Length == 0)
            {
                id = default;
                return false;
            }

            char c;
            int i = 0;
            do
            {
                if (text.Length <= i)
                {
                    i++;
                    break;
                }
                c = text[i++];
            }
            while ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-');

            int len = i - 1;

            id = text.Slice(0, len);

            if (len <= 0) return false;

            if (skipNonDigitCheck)
            {
                text = text.Slice(len);
                return true;
            }

            bool hasNonDigit = false;
            foreach (var chr in id)
            {
                if (chr < '0' || chr > '9')
                {
                    hasNonDigit = true;
                    text = text.Slice(len);
                    break;
                }
            }

            return hasNonDigit;
        }

        private static bool TryParseNumId(ref ReadOnlySpan<char> text, out ulong num)
        {
            var copy = text;
            if (TryReadNumId(ref text, out var id))
            {
                if (!ulong.TryParse(id, out num))
                {
                    text = copy;
                    return false;
                }

                return true;
            }

            num = 0;
            return false;
        }

        private static bool TryReadNumId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id)
        {
            var copy = text;
            if (TryTake(ref text, '0')) // we can take a single 0
            {
                id = copy.Slice(0, 1);
                return true;
            }

            if (text.Length == 0)
            {
                id = default;
                return false;
            }

            // or any nonzero character followed by any character
            var c = text[0];
            if (c > '0' && c <= '9')
            { // we start with a positive number
                int i = 1;
                do
                {
                    if (text.Length <= i)
                    {
                        i++;
                        break;
                    }
                    c = text[i++];
                }
                while (c >= '0' && c <= '9'); // find as many digits as we can

                int len = i - 1;

                id = text.Slice(0, len);
                text = text.Slice(len);
                return true;
            }

            // or this isn't a numeric id
            id = default;
            return false;
        }
        #endregion
    }
}
