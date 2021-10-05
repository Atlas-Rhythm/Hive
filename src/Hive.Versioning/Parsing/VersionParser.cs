using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Utilities;
using static Hive.Versioning.Parsing.ParseHelpers;
#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning.Parsing
{
#pragma warning disable IDE0065 // Misplaced using directive
    // Having this inside the namespace makes it *far* shorter
    using ErrorState = ParserErrorState<VersionParseAction>;
#pragma warning restore IDE0065 // Misplaced using directive

    /// <summary>
    /// The parse actions for version parsing.
    /// </summary>
    public enum VersionParseAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// Did not find a core version number.
        /// </summary>
        ECoreVersionNumber,
        /// <summary>
        /// Did not find a core version dot.
        /// </summary>
        ECoreVersionDot,
        /// <summary>
        /// Found a core version.
        /// </summary>
        FCoreVersion,

        /// <summary>
        /// Did not find a prerelease clause.
        /// </summary>
        EPrerelease,
        /// <summary>
        /// Did not find a prerelease ID.
        /// </summary>
        EPrereleaseId,
        /// <summary>
        /// Found a prerelease clause.
        /// </summary>
        FPrerelease,

        /// <summary>
        /// Did not find a build clause.
        /// </summary>
        EBuild,
        /// <summary>
        /// Did not find a build ID.
        /// </summary>
        EBuildId,
        /// <summary>
        /// Did not find a build ID dot.
        /// </summary>
        EBuildIdDot,
        /// <summary>
        /// Found a build clause.
        /// </summary>
        FBuild,

        /// <summary>
        /// Did not find an alphanumeric ID.
        /// </summary>
        EAlphaNumericId,
        /// <summary>
        /// Found an alphanumeric ID.
        /// </summary>
        FAlphaNumericId,

        /// <summary>
        /// Did not find a numeric ID.
        /// </summary>
        ENumericId,
        /// <summary>
        /// Did not find a valid numeric ID.
        /// </summary>
        EValidNumericId,
        /// <summary>
        /// Found a numeric ID.
        /// </summary>
        FNumericId,
        /// <summary>
        /// Found a valid numeric ID.
        /// </summary>
        FValidNumericId,

        /// <summary>
        /// There was extra input after the version.
        /// </summary>
        ExtraInput,
    }

    internal static class VersionParser
    {

        public static bool TryParseInternal(
            ref ErrorState errors,
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

            if (!TryParseCore(ref errors, ref text, out major, out minor, out patch))
                return false;

            if (TryTake(ref text, '-'))
            {
                if (!TryParsePreRelease(ref errors, ref text, out prereleaseIds))
                {
                    errors.Report(VersionParseAction.EPrerelease, text, text);
                    return false;
                }
            }
            else
            {
                prereleaseIds = Array.Empty<string>();
            }

            if (TryTake(ref text, '+'))
            {
                if (!TryParseBuild(ref errors, ref text, out buildIds))
                {
                    errors.Report(VersionParseAction.EBuild, text, text);
                    return false;
                }
            }
            else
            {
                buildIds = Array.Empty<string>();
            }

            return true;
        }

        private static bool TryParseCore(ref ErrorState errors, ref StringPart text, out ulong major, out ulong minor, out ulong patch)
        {
            minor = 0;
            patch = 0;

            var copy = text;
            if (!TryParseNumId(ref errors, ref text, out major))
            {
                errors.Report(VersionParseAction.ECoreVersionNumber, text, text);
                text = copy;
                return false;
            }
            if (!TryTake(ref text, '.'))
            {
                errors.Report(VersionParseAction.ECoreVersionDot, text, text);
                text = copy;
                return false;
            }
            if (!TryParseNumId(ref errors, ref text, out minor))
            {
                errors.Report(VersionParseAction.ECoreVersionNumber, text, text);
                text = copy;
                return false;
            }
            if (!TryTake(ref text, '.'))
            {
                errors.Report(VersionParseAction.ECoreVersionDot, text, text);
                text = copy;
                return false;
            }
            if (!TryParseNumId(ref errors, ref text, out patch))
            {
                errors.Report(VersionParseAction.ECoreVersionNumber, text, text);
                text = copy;
                return false;
            }

            errors.Report(VersionParseAction.FCoreVersion, copy, text);
            return true;
        }

        private static bool TryParsePreRelease(ref ErrorState errors, ref StringPart text, [MaybeNullWhen(false)] out string[] prereleaseIds)
        {
            prereleaseIds = null;

            var copy = text;
            if (TryReadPreReleaseId(ref errors, ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(id.ToString());
                    if (!TryTake(ref text, '.'))
                    { // exit condition
                        prereleaseIds = ab.ToArray();
                        errors.Report(VersionParseAction.FPrerelease, copy, text);
                        return true;
                    }
                }
                while (TryReadPreReleaseId(ref errors, ref text, out id));

                ab.Clear();
                errors.Report(VersionParseAction.EPrereleaseId, text, text);
                text = copy;
                return false;
            }

            errors.Report(VersionParseAction.EPrereleaseId, text, text);
            return false;
        }

        private static bool TryReadPreReleaseId(ref ErrorState errors, ref StringPart text, out StringPart id)
        {
            var copy = text;
            if (TryReadAlphaNumId(ref errors, ref text, out id)) return true;
            if (TryReadNumId(ref errors, ref text, out id))
            {
                errors.Report(VersionParseAction.FNumericId, copy, text);
                return true;
            }
            return false;
        }

        private static bool TryParseBuild(ref ErrorState errors, ref StringPart text, [MaybeNullWhen(false)] out string[] buildIds)
        {
            buildIds = null;

            var copy = text;
            if (TryReadBuildId(ref errors, ref text, out var id))
            {
                using var ab = new ArrayBuilder<string>(4);
                do
                {
                    ab.Add(id.ToString());
                    if (!TryTake(ref text, '.'))
                    { // exit condition
                        buildIds = ab.ToArray();
                        errors.Report(VersionParseAction.FBuild, copy, text);
                        return true;
                    }
                }
                while (TryReadBuildId(ref errors, ref text, out id));

                ab.Clear();
                errors.Report(VersionParseAction.EBuildIdDot, text, text);
                text = copy;
                return false;
            }

            errors.Report(VersionParseAction.EBuildId, text, text);
            return false;
        }

        private static bool TryReadBuildId(ref ErrorState errors, ref StringPart text, out StringPart id)
            => TryReadAlphaNumId(ref errors, ref text, out id, true);

        private static bool TryReadAlphaNumId(ref ErrorState errors, ref StringPart text, out StringPart id, bool skipNonDigitCheck = false)
        {
            var copy = text;
            if (text.Length == 0)
            {
                id = default;
                errors.Report(VersionParseAction.EAlphaNumericId, copy, text);
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

            if (len <= 0)
            {
                errors.Report(VersionParseAction.EAlphaNumericId, copy, text);
                return false;
            }

            if (skipNonDigitCheck)
            {
                errors.Report(VersionParseAction.FAlphaNumericId, copy, text);
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

            errors.Report(hasNonDigit ? VersionParseAction.FAlphaNumericId : VersionParseAction.EAlphaNumericId, copy, text);
            return hasNonDigit;
        }

        internal static bool TryParseNumId(ref ErrorState errors, ref StringPart text, out ulong num)
        {
            var copy = text;
            if (TryReadNumId(ref errors, ref text, out var id))
            {
                if (!ulong.TryParse(id.ToString(), out num))
                {
                    errors.Report(VersionParseAction.EValidNumericId, copy, text);
                    text = copy;
                    return false;
                }

                errors.Report(VersionParseAction.FValidNumericId, copy, text);
                return true;
            }

            num = 0;
            return false;
        }

        private static bool TryReadNumId(ref ErrorState errors, ref StringPart text, out StringPart id)
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
                errors.Report(VersionParseAction.ENumericId, text, text);
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
            errors.Report(VersionParseAction.ENumericId, copy, text);
            return false;
        }
    }
}
