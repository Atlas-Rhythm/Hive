using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;

namespace Hive.Versioning
{
    public class Version
    {
        private readonly int major;
        private readonly int minor;
        private readonly int patch;
        private readonly string[] prereleaseIds;
        private readonly string[] buildIds;

        public Version(ReadOnlySpan<char> text)
        {
            text = text.Trim();

            if (text.Length < 5)
                throw new ArgumentException("Input too short to be a SemVer version", nameof(text));

            if (!TryParse(ref text, out major, out minor, out patch, out var preIds, out var buildIds))
                throw new ArgumentException("Input was not a valid SemVer version", nameof(text));
            if (text.Length > 0)
                throw new ArgumentException("Input was not a valid SemVer version", nameof(text))

            prereleaseIds = preIds;
            this.buildIds = buildIds;
        }


        #region Parser
        internal static bool TryParse(
            ref ReadOnlySpan<char> text, 
            out int major,
            out int minor,
            out int patch,
            [MaybeNullWhen(false)] out string[] prereleaseIds,
            [MaybeNullWhen(false)] out string[] buildIds
        )
        {

        }

        private static bool TryParseCore(ref ReadOnlySpan<char> text, out int major, out int minor, out int patch)
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

            
        }

        private static bool TryReadPreReleaseId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id)
        {
            if (TryReadNumId(ref text, out id)) return true;
            if (TryReadAlphaNumId(ref text, out id)) return true;
            return false;
        }

        private static bool TryReadAlphaNumId(ref ReadOnlySpan<char> text, out ReadOnlySpan<char> id)
        {
            char c;
            int i = 0;
            do c = text[i++];
            while ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'));

            int len = i - 1;

            id = text.Slice(0, len);

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

        private static bool TryParseNumId(ref ReadOnlySpan<char> text, out int num)
        {
            var copy = text;
            if (TryReadNumId(ref text, out var id))
            {
                if (!int.TryParse(id, out num))
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
            //num = 0;
            if (TryTake(ref text, '0')) // we can take a single 0
            {
                id = text.Slice(0, 1);
                return true;
            }

            // or any nonzero character followed by any character
            var c = text[0];
            if (c > '0' && c <= '9')
            { // we start with a positive number
                int i = 1;
                do c = text[i++];
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

        private static bool TryTake(ref ReadOnlySpan<char> input, char next)
        {
            if (input[0] != next) return false;
            input = input.Slice(1);
            return true;
        }
        #endregion
    }
}
