using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Utilities;
using static Hive.Versioning.VersionRange;
using static Hive.Versioning.ParseHelpers;

#if !NETSTANDARD2_0
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif

namespace Hive.Versioning
{
    internal static class RangeParser
    {
        internal static readonly Subrange[] EverythingSubranges = new[] { Subrange.Everything };

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
        internal static bool TryParse(ref StringPart text, [MaybeNullWhen(false)] out Subrange[] sranges, out VersionComparer? comparer)
        {
            sranges = null;
            comparer = null;

            // check for the "everything" range first, which is just a star
            if (TryTake(ref text, '*'))
            {
                sranges = EverythingSubranges;
                return true;
            }

            // then check for the "nothing" range, which is z or Z
            if (TryTake(ref text, 'z') || TryTake(ref text, 'Z'))
            {
                sranges = Array.Empty<Subrange>();
                return true;
            }

            if (!TryReadComponent(ref text, out var range, out var compare))
                return false;

            using var ab = new ArrayBuilder<Subrange>();

            StringPart restoreTo;
            do
            {
                restoreTo = text;

                if (range != null)
                    ab.Add(range.Value);
                if (compare != null)
                {
                    if (comparer != null)
                    {
                        var res = comparer.Value.TryDisjunction(compare.Value, out var newComparer, out var sr);
                        switch (res)
                        {
                            case CombineResult.OneComparer:
                                comparer = newComparer;
                                break;

                            case CombineResult.Everything:
                            case CombineResult.OneSubrange:
                                ab.Add(sr);
                                comparer = null;
                                break;

                            case CombineResult.Unrepresentable:
                                // one of them is an ExactEqual comparer
                                if (comparer.Value.Type == ComparisonType.ExactEqual)
                                {
                                    ab.Add(comparer.Value.ToExactEqualSubrange());
                                    comparer = compare;
                                }
                                else if (compare.Value.Type == ComparisonType.ExactEqual)
                                    ab.Add(compare.Value.ToExactEqualSubrange());
                                else if (comparer.Value.Type == compare.Value.Type)
                                    comparer = null;
                                break;

                            default: throw new InvalidOperationException();
                        }
                    }
                    else if (compare.Value.Type == ComparisonType.ExactEqual)
                    {
                        ab.Add(compare.Value.ToExactEqualSubrange());
                    }
                    else
                        comparer = compare;
                }

                text = text.TrimStart();
                if (!TryTake(ref text, '|') || !TryTake(ref text, '|'))
                {
                    text = restoreTo;
                    sranges = ab.ToArray();
                    return true;
                }
                text = text.TrimStart();
            }
            while (TryReadComponent(ref text, out range, out compare));

            text = restoreTo;
            sranges = ab.ToArray();
            return true;
        }

        private static bool TryReadComponent(ref StringPart text, out Subrange? range, out VersionComparer? compare)
        {
            if (TryParseSubrange(ref text, false, out var sr))
            {
                range = sr;
                compare = null;
                return true;
            }

            if (TryParseComparer(ref text, out var comp))
            {
                range = null;
                compare = comp;
                return true;
            }

            range = null;
            compare = null;
            return false;
        }

        public static bool TryParseComparer(ref StringPart text, out VersionComparer comparer)
        {
            var copy = text;
            if (!TryReadCompareType(ref text, out var compareType))
            {
                comparer = default;
                return false;
            }

            text = text.TrimStart();
            if (!Version.TryParse(ref text, out var version))
            {
                text = copy;
                comparer = default;
                return false;
            }

            comparer = new VersionComparer(version, compareType);
            return true;
        }

        private static bool TryReadCompareType(ref StringPart text, out ComparisonType type)
        {
            type = ComparisonType.None;

            var copy = text;
            if (TryTake(ref text, '~'))
                type |= ComparisonType.PreRelease;
            if (TryTake(ref text, '>'))
                type |= ComparisonType.Greater;
            else if (TryTake(ref text, '<'))
                type |= ComparisonType.Less;
            if (TryTake(ref text, '='))
                type |= ComparisonType.ExactEqual;

            if (!CheckCompareType(type))
            {
                text = copy;
                return false;
            }

            return true;
        }

        private static bool CheckCompareType(ComparisonType type)
            => type is ComparisonType.ExactEqual
            or ComparisonType.Greater
            or ComparisonType.GreaterEqual
            or ComparisonType.Less
            or ComparisonType.LessEqual
            or ComparisonType.PreReleaseGreaterEqual
            or ComparisonType.PreReleaseLess;

        public static bool TryParseSubrange(ref StringPart text, bool allowOutward, out Subrange subrange)
        {
            var copy = text;

            // first we check for a star range
            if (TryReadStarRange(ref text, out subrange)) return true;
            // then we check for a hyphen range
            if (TryReadHyphenRange(ref text, out subrange)) return true;

            //---EVERYTHING AFTER THIS POINT HAS A SPECIAL FIRST CHARACTER---\\

            // then we check for a ^ range
            if (TryReadCaretRange(ref text, out subrange)) return true;

            // otherwise we just try read two VersionComparers in a row
            if (!TryParseComparer(ref text, out var lower))
            {
                text = copy;
                subrange = default;
                return false;
            }
            text = text.TrimStart();
            if (!TryParseComparer(ref text, out var upper))
            {
                text = copy;
                subrange = default;
                return false;
            }

            if (lower.CompareTo > upper.CompareTo)
            {
                text = copy;
                subrange = default;
                return false;
            }

            if (lower.Type == ComparisonType.ExactEqual || upper.Type == ComparisonType.ExactEqual
             || (lower.Type & ~ComparisonType.ExactEqual) == (upper.Type & ~ComparisonType.ExactEqual))
            { // if the bounds point the same direction, the subrange is invalid
                text = copy;
                subrange = default;
                return false;
            }

            subrange = new Subrange(lower, upper);

            if (!allowOutward && !subrange.IsInward)
            { // reject inward-facing subranges for consistency on the outside
                text = copy;
                subrange = default;
                return false;
            }

            return true;
        }

        private static bool TryReadCaretRange(ref StringPart text, out Subrange range)
        {
            var copy = text;
            if (!TryTake(ref text, '^'))
            {
                range = default;
                return false;
            }

            text = text.TrimStart();
            if (!Version.TryParse(ref text, out var lower))
            {
                text = copy;
                range = default;
                return false;
            }

            Version upper;
            if (lower.Major != 0)
                upper = new Version(lower.Major + 1, 0, 0);
            else if (lower.Minor != 0)
                upper = new Version(0, lower.Minor + 1, 0);
            else
                upper = new Version(0, 0, lower.Patch + 1);

            // caret ranges want the upper bound to exclude prereleases
            range = new Subrange(new VersionComparer(lower, ComparisonType.GreaterEqual),
                new VersionComparer(upper, ComparisonType.PreReleaseLess));
            return true;
        }

        private static bool TryReadHyphenRange(ref StringPart text, out Subrange range)
        {
            var copy = text;
            if (!Version.TryParse(ref text, out var lowVersion))
            {
                range = default;
                text = copy;
                return false;
            }

            text = text.TrimStart();
            if (!TryTake(ref text, '-'))
            {
                range = default;
                text = copy;
                return false;
            }
            text = text.TrimStart();

            if (!Version.TryParse(ref text, out var highVersion))
            {
                range = default;
                text = copy;
                return false;
            }

            range = new(new(lowVersion, ComparisonType.GreaterEqual),
                new(highVersion, ComparisonType.LessEqual));
            return true;
        }

        private static bool TryReadStarRange(ref StringPart text, out Subrange range)
        {
            var copy = text;
            if (!VersionParser.TryParseNumId(ref text, out var majorNum) || !TryTake(ref text, '.'))
            {
                text = copy;
                range = default;
                return false;
            }

            static bool TryTakePlaceholder(ref StringPart text)
                => TryTake(ref text, '*') || TryTake(ref text, 'x') || TryTake(ref text, 'X');

            // at this point, we *know* that we have a star range
            if (TryTakePlaceholder(ref text))
            {
                copy = text;
                // try to read another star
                if (!TryTake(ref text, '.')
                    || !TryTakePlaceholder(ref text))
                {
                    // if we can't, that's fine, just rewind to copy
                    // this might be something else
                    text = copy;
                }

                // we now have a star range
                var versionBase = new Version(majorNum, 0, 0);
                var versionUpper = new Version(majorNum + 1, 0, 0);
                // the range shouldn't include prereleases of the upper bound
                range = new Subrange(new VersionComparer(versionBase, ComparisonType.GreaterEqual),
                    new VersionComparer(versionUpper, ComparisonType.PreReleaseLess));
                return true;
            }

            // try to read the second number
            if (!VersionParser.TryParseNumId(ref text, out var minorNum) || !TryTake(ref text, '.'))
            {
                // if we can't read the last bit then rewind and exit
                text = copy;
                range = default;
                return false;
            }

            // if our last thing isn't a star, then this isn't a star range
            if (!TryTakePlaceholder(ref text))
            {
                text = copy;
                range = default;
                return false;
            }

            // we now have a star range
            var versionBase2 = new Version(majorNum, minorNum, 0);
            var versionUpper2 = new Version(majorNum, minorNum + 1, 0);
            // the range shouldn't include prereleases of the upper bound
            range = new Subrange(new VersionComparer(versionBase2, ComparisonType.GreaterEqual),
                new VersionComparer(versionUpper2, ComparisonType.PreReleaseLess));
            return true;
        }
    }
}
