using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Utilities;
using static Hive.Versioning.ParseHelpers;
#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning
{
    internal static class VersionParser
    {
        #region Parser

        public static bool TryParseInternal(
            ref StringPart text,
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

        private static bool TryParseCore(ref StringPart text, out ulong major, out ulong minor, out ulong patch)
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

        private static bool TryParsePreRelease(ref StringPart text, [MaybeNullWhen(false)] out string[] prereleaseIds)
        {
            prereleaseIds = null;

            var copy = text;
            if (TryReadPreReleaseId(ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(id.ToString());
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

        private static bool TryReadPreReleaseId(ref StringPart text, out StringPart id)
        {
            if (TryReadAlphaNumId(ref text, out id)) return true;
            if (TryReadNumId(ref text, out id)) return true;
            return false;
        }

        private static bool TryParseBuild(ref StringPart text, [MaybeNullWhen(false)] out string[] buildIds)
        {
            buildIds = null;

            var copy = text;
            if (TryReadBuildId(ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(id.ToString());
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

        private static bool TryReadBuildId(ref StringPart text, out StringPart id)
            => TryReadAlphaNumId(ref text, out id, true);

        private static bool TryReadAlphaNumId(ref StringPart text, out StringPart id, bool skipNonDigitCheck = false)
        {
            if (text.Length == 0)
            {
                id = default;
                return false;
            }

            char c;
            var i = 0;
            do
            {
                if (text.Length <= i)
                {
                    i++;
                    break;
                }
                c = text[i++];
            }
            while (c is (>= '0' and <= '9') or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or '-');

            var len = i - 1;

            id = text.Slice(0, len);

            if (len <= 0) return false;

            if (skipNonDigitCheck)
            {
                text = text.Slice(len);
                return true;
            }

            var hasNonDigit = false;
            foreach (var chr in id)
            {
                if (chr is < '0' or > '9')
                {
                    hasNonDigit = true;
                    text = text.Slice(len);
                    break;
                }
            }

            return hasNonDigit;
        }

        internal static bool TryParseNumId(ref StringPart text, out ulong num)
        {
            var copy = text;
            if (TryReadNumId(ref text, out var id))
            {
                if (!ulong.TryParse(id.ToString(), out num))
                {
                    text = copy;
                    return false;
                }

                return true;
            }

            num = 0;
            return false;
        }

        private static bool TryReadNumId(ref StringPart text, out StringPart id)
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
            if (c is > '0' and <= '9')
            { // we start with a positive number
                var i = 1;
                do
                {
                    if (text.Length <= i)
                    {
                        i++;
                        break;
                    }
                    c = text[i++];
                }
                while (c is >= '0' and <= '9'); // find as many digits as we can

                var len = i - 1;

                id = text.Slice(0, len);
                text = text.Slice(len);
                return true;
            }

            // or this isn't a numeric id
            id = default;
            return false;
        }
        #endregion Parser
    }
}
