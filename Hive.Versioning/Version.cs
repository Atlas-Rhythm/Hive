using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using Hive.Utilities;

namespace Hive.Versioning
{
    public class Version
    {
        private readonly ulong major;
        private readonly ulong minor;
        private readonly ulong patch;
        private readonly string[] prereleaseIds;
        private readonly string[] buildIds;

        public Version(ReadOnlySpan<char> text)
        {
            text = text.Trim();

            if (text.Length < 5)
                throw new ArgumentException("Input too short to be a SemVer version", nameof(text));

            if (!TryParseInternal(ref text, out major, out minor, out patch, out var preIds, out var buildIds))
                throw new ArgumentException("Input was not a valid SemVer version", nameof(text));
            if (text.Length > 0)
                throw new ArgumentException("Input was not a valid SemVer version", nameof(text));

            prereleaseIds = preIds;
            this.buildIds = buildIds;
        }

        public Version(ulong major, ulong minor, ulong patch, IEnumerable<string> prereleaseIds, IEnumerable<string> buildIds)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.prereleaseIds = prereleaseIds.ToArray();
            this.buildIds = buildIds.ToArray();
        }

        public ulong Major => major;
        public ulong Minor => minor;
        public ulong Patch => patch;

        public IEnumerable<string> PreReleaseIds => prereleaseIds;
        public IEnumerable<string> BuildIds => buildIds;


        public static bool TryParse(string text, [MaybeNullWhen(false)] out Version version)
            => TryParse((ReadOnlySpan<char>)text, out version);
        public static bool TryParse(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Version version)
        {
            text = text.Trim();
            return TryParse(ref text, out version) && text.Length == 0;
        }

        public static bool TryParse(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Version version)
        {
            version = null;

            if (!TryParseInternal(ref text, out var maj, out var min, out var pat, out var pre, out var build))
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

            if (TryReadPreReleaseId(ref text, out var id))
            {
                var ab = new ArrayBuilder<string>(4);
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

                prereleaseIds = ab.ToArray();
                return true;
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

            if (TryReadBuildId(ref text, out var id))
            {
                var ab = new ArrayBuilder<string>(4);
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

                buildIds = ab.ToArray();
                return true;
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

            // TODO: this parsing is kinda whack and doesn't quite work correctly
            char c;
            int i = 0;
            do c = text[i++];
            while (((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-')
                   && text.Length > i);

            int len = text.Length == i ? i : i - 1;

            id = text.Slice(0, len);

            if (skipNonDigitCheck)
            {
                if (len > 0)
                {
                    text = text.Slice(len);
                    return true;
                }
                else
                {
                    return false;
                }
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
                // TODO: this parsing is kinda whack
                int i = 1;
                if (text.Length > i)
                {
                    do c = text[i++];
                    while (c >= '0' && c <= '9' && text.Length > i); // find as many digits as we can
                }

                int len = text.Length == i ? i : i - 1;

                id = text.Slice(0, len);
                text = text.Slice(len);
                return true;
            }

            // or this isn't a numeric id
            id = default;
            return false;
        }

        private static bool TryTake(ref ReadOnlySpan<char> input, char next)
        {
            if (input.Length == 0) return false;
            if (input[0] != next) return false;
            input = input.Slice(1);
            return true;
        }
        #endregion
    }
}
