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
#pragma warning disable IDE0065 // Misplaced using directive
    // Having this inside the namespace makes it *far* shorter
    using ErrorState = ParserErrorState<AnyParseAction>;
    using VersionErrorState = ParserErrorState<VersionParseAction>;
#pragma warning restore IDE0065 // Misplaced using directive

    public enum RangeParseAction
    {
        StarRange = RangeParser.RangeFlag,

        HyphenVersion,
        Hyphen,

        Caret,
        CaretVersion,

        Subrange,
        OrderedSubrange,
        ClosedSubrange,

        CompareType,
        Comparer,
        ComparerVersion,

        Component,
    }

    public struct AnyParseAction
    {
        public RangeParseAction Value { get; }
        public VersionParseAction VersionAction => (VersionParseAction)Value;
        public bool IsVersionAction => (Value & RangeParser.RangeFlag) == RangeParser.RangeFlag;

        public AnyParseAction(RangeParseAction action) => Value = action;
        public AnyParseAction(VersionParseAction action) => Value = (RangeParseAction)action;

        internal static readonly Func<VersionParseAction, AnyParseAction> Convert
            = action => new(action);
    }

    internal static class RangeParser
    {

        public const RangeParseAction RangeFlag = (RangeParseAction)0x40000000;

        internal static readonly Subrange[] EverythingSubranges = new[] { Subrange.Everything };

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
        internal static bool TryParse(in ErrorState errors, ref StringPart text, [MaybeNullWhen(false)] out Subrange[] sranges, out VersionComparer? comparer)
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

            if (!TryReadComponent(errors, ref text, out var range, out var compare))
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
            while (TryReadComponent(errors, ref text, out range, out compare));

            text = restoreTo;
            sranges = ab.ToArray();
            return true;
        }

        private static bool TryReadComponent(in ErrorState errors, ref StringPart text, out Subrange? range, out VersionComparer? compare)
        {
            if (TryParseSubrange(errors, ref text, false, out var sr))
            {
                range = sr;
                compare = null;
                return true;
            }

            if (TryParseComparer(errors, ref text, out var comp))
            {
                range = null;
                compare = comp;
                return true;
            }

            errors.Report(new(RangeParseAction.Component), text);
            range = null;
            compare = null;
            return false;
        }

        public static bool TryParseComparer(in ErrorState errors, ref StringPart text, out VersionComparer comparer)
        {
            var copy = text;
            if (!TryReadCompareType(errors, ref text, out var compareType))
            {
                errors.Report(new(RangeParseAction.Comparer), text);
                comparer = default;
                return false;
            }

            using var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            text = text.TrimStart();
            if (!Version.TryParse(verErrors, ref text, out var version))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.ComparerVersion), text);
                text = copy;
                comparer = default;
                return false;
            }

            comparer = new VersionComparer(version, compareType);
            return true;
        }

        private static bool TryReadCompareType(in ErrorState errors, ref StringPart text, out ComparisonType type)
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
                errors.Report(new(RangeParseAction.CompareType), text);
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

        public static bool TryParseSubrange(in ErrorState errors, ref StringPart text, bool allowOutward, out Subrange subrange)
        {
            var copy = text;

            // first we check for a star range
            if (TryReadStarRange(errors, ref text, out subrange)) return true;
            // then we check for a hyphen range
            if (TryReadHyphenRange(errors, ref text, out subrange)) return true;

            //---EVERYTHING AFTER THIS POINT HAS A SPECIAL FIRST CHARACTER---\\

            // then we check for a ^ range
            if (TryReadCaretRange(errors, ref text, out subrange)) return true;

            // otherwise we just try read two VersionComparers in a row
            if (!TryParseComparer(errors, ref text, out var lower))
            {
                errors.Report(new(RangeParseAction.Subrange), text);
                text = copy;
                subrange = default;
                return false;
            }
            text = text.TrimStart();
            if (!TryParseComparer(errors, ref text, out var upper))
            {
                errors.Report(new(RangeParseAction.Subrange), text);
                text = copy;
                subrange = default;
                return false;
            }

            if (lower.CompareTo > upper.CompareTo)
            {
                errors.Report(new(RangeParseAction.OrderedSubrange), text);
                text = copy;
                subrange = default;
                return false;
            }

            if (lower.Type == ComparisonType.ExactEqual || upper.Type == ComparisonType.ExactEqual
             || (lower.Type & ~ComparisonType.ExactEqual) == (upper.Type & ~ComparisonType.ExactEqual))
            { // if the bounds point the same direction, the subrange is invalid
                errors.Report(new(RangeParseAction.ClosedSubrange), text);
                text = copy;
                subrange = default;
                return false;
            }

            subrange = new Subrange(lower, upper);

            if (!allowOutward && !subrange.IsInward)
            { // reject outward-facing subranges for consistency on the outside
                errors.Report(new(RangeParseAction.ClosedSubrange), text);
                text = copy;
                subrange = default;
                return false;
            }

            return true;
        }

        private static bool TryReadCaretRange(in ErrorState errors, ref StringPart text, out Subrange range)
        {
            var copy = text;
            if (!TryTake(ref text, '^'))
            {
                errors.Report(new(RangeParseAction.Caret), text);
                range = default;
                return false;
            }

            using var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            text = text.TrimStart();
            if (!Version.TryParse(verErrors, ref text, out var lower))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.CaretVersion), text);
                text = copy;
                range = default;
                return false;
            }

            // make sure we pull in versions
            errors.FromState(verErrors, AnyParseAction.Convert);

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

        private static bool TryReadHyphenRange(in ErrorState errors, ref StringPart text, out Subrange range)
        {
            using var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            var copy = text;
            if (!Version.TryParse(verErrors, ref text, out var lowVersion))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.HyphenVersion), text);
                range = default;
                text = copy;
                return false;
            }

            text = text.TrimStart();
            if (!TryTake(ref text, '-'))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.Hyphen), text);
                range = default;
                text = copy;
                return false;
            }
            text = text.TrimStart();

            if (!Version.TryParse(verErrors, ref text, out var highVersion))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.HyphenVersion), text);
                range = default;
                text = copy;
                return false;
            }

            // make sure we always pull in parse errors so we have a full listing of what happened
            errors.FromState(verErrors, AnyParseAction.Convert);
            range = new(new(lowVersion, ComparisonType.GreaterEqual),
                new(highVersion, ComparisonType.LessEqual));
            return true;
        }

        private static bool TryReadStarRange(in ErrorState errors, ref StringPart text, out Subrange range)
        {
            var copy = text;

            using var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            if (!VersionParser.TryParseNumId(verErrors, ref text, out var majorNum) || !TryTake(ref text, '.'))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.StarRange), text);
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

                // make sure we pull in parse errors
                errors.FromState(verErrors, AnyParseAction.Convert);
                return true;
            }

            // try to read the second number
            if (!VersionParser.TryParseNumId(verErrors, ref text, out var minorNum) || !TryTake(ref text, '.'))
            {
                // if we can't read the last bit then rewind and exit
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.StarRange), text);
                text = copy;
                range = default;
                return false;
            }

            // if our last thing isn't a star, then this isn't a star range
            if (!TryTakePlaceholder(ref text))
            {
                errors.FromState(verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.StarRange), text);
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

            // make sure we pull in parse errors
            errors.FromState(verErrors, AnyParseAction.Convert);
            return true;
        }
    }
}
