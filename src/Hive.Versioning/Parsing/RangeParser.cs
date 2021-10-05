using System;
using System.Diagnostics.CodeAnalysis;
using Hive.Utilities;
using static Hive.Versioning.VersionRange;
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
    using ErrorState = ParserErrorState<AnyParseAction>;
    using VersionErrorState = ParserErrorState<VersionParseAction>;
#pragma warning restore IDE0065 // Misplaced using directive

    /// <summary>
    /// The parse actions for version range parsing.
    /// </summary>
    public enum RangeParseAction
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// Did not find the first number in a star range.
        /// </summary>
        EStarRange1 = RangeParser.RangeFlag,
        /// <summary>
        /// Did not find the second number in a star range.
        /// </summary>
        EStarRange2,
        /// <summary>
        /// Did not find the third star in a star range.
        /// </summary>
        EStarRange3,
        /// <summary>
        /// Found a star range.
        /// </summary>
        FStarRange,

        /// <summary>
        /// Did not find the first version in a hyphen range.
        /// </summary>
        EHyphenVersion,
        /// <summary>
        /// Did not find the second version in a hyphen range.
        /// </summary>
        EHyphenVersion2,
        /// <summary>
        /// Did not find the hyphen in a hyphen range.
        /// </summary>
        EHyphen,
        /// <summary>
        /// Found a hyphen range.
        /// </summary>
        FHyphenRange,

        /// <summary>
        /// Did not find a caret for a caret range.
        /// </summary>
        ECaret,
        /// <summary>
        /// Did not find the version in a caret version.
        /// </summary>
        ECaretVersion,
        /// <summary>
        /// Found a caret range.
        /// </summary>
        FCaretRange,

        /// <summary>
        /// Did not find the first comparer in a subrange.
        /// </summary>
        ESubrange1,
        /// <summary>
        /// The found subrange was not ordered correctly.
        /// </summary>
        EOrderedSubrange,
        /// <summary>
        /// The found subrange was not closed.
        /// </summary>
        EClosedSubrange,
        /// <summary>
        /// Found a subrange.
        /// </summary>
        FSubrange,

        /// <summary>
        /// Did not find a comparer type.
        /// </summary>
        ECompareType,
        /// <summary>
        /// Did not find a comparer.
        /// </summary>
        EComparer,
        /// <summary>
        /// Did not find a comparer after a comparer type.
        /// </summary>
        EComparerVersion,
        /// <summary>
        /// Found a comparer.
        /// </summary>
        FComparer,

        /// <summary>
        /// Did not find a component.
        /// </summary>
        EComponent,
        /// <summary>
        /// Found a range component.
        /// </summary>
        FComponent,

        /// <summary>
        /// Found an everything range.
        /// </summary>
        FStar,
        /// <summary>
        /// Found a nothing range.
        /// </summary>
        FZero,

        /// <summary>
        /// There was extra input after the range.
        /// </summary>
        ExtraInput = VersionParseAction.ExtraInput,
    }

    /// <summary>
    /// A parse action that can be either a <see cref="RangeParseAction"/> or a <see cref="VersionParseAction"/>.
    /// </summary>
    public struct AnyParseAction : IEquatable<AnyParseAction>
    {
        /// <summary>
        /// The value of this action, as a <see cref="RangeParseAction"/>.
        /// </summary>
        public RangeParseAction Value { get; }
        /// <summary>
        /// The value of this action, as a <see cref="VersionParseAction"/>.
        /// </summary>
        public VersionParseAction VersionAction => (VersionParseAction)Value;
        /// <summary>
        /// Gets whether this is a <see cref="VersionParseAction"/> or not.
        /// </summary>
        public bool IsVersionAction => (Value & RangeParser.RangeFlag) != RangeParser.RangeFlag;

        /// <summary>
        /// Constructs an <see cref="AnyParseAction"/> from a <see cref="RangeParseAction"/>.
        /// </summary>
        /// <param name="action">The <see cref="RangeParseAction"/> to construct it with.</param>
        public AnyParseAction(RangeParseAction action) => Value = action;
        /// <summary>
        /// Constructs an <see cref="AnyParseAction"/> from a <see cref="VersionParseAction"/>.
        /// </summary>
        /// <param name="action">The <see cref="VersionParseAction"/> to construct it with.</param>
        public AnyParseAction(VersionParseAction action) => Value = (RangeParseAction)action;

        internal static readonly Func<VersionParseAction, AnyParseAction> Convert
            = action => new(action);

        /// <inheritdoc/>
        public override string ToString() => IsVersionAction ? VersionAction.ToString() : Value.ToString();

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is AnyParseAction action && Equals(action);
        /// <inheritdoc/>
        public bool Equals(AnyParseAction other)
            => Value == other.Value && VersionAction == other.VersionAction && IsVersionAction == other.IsVersionAction;
        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Value, VersionAction, IsVersionAction);

        /// <summary>
        /// Compares two <see cref="AnyParseAction"/>s for equality.
        /// </summary>
        /// <param name="left">The firrst parse action to compare.</param>
        /// <param name="right">The second parse action to compare.</param>
        /// <returns><see langword="true"/> if the actions are equivalent, <see langword="false"/> otherwise.</returns>
        public static bool operator ==(AnyParseAction left, AnyParseAction right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="AnyParseAction"/>s for inequality.
        /// </summary>
        /// <param name="left">The firrst parse action to compare.</param>
        /// <param name="right">The second parse action to compare.</param>
        /// <returns><see langword="true"/> if the actions are not equivalent, <see langword="false"/> otherwise.</returns>
        public static bool operator !=(AnyParseAction left, AnyParseAction right)
            => !(left == right);
    }

    internal static class RangeParser
    {

        public const RangeParseAction RangeFlag = (RangeParseAction)0x40000000;

        internal static readonly Subrange[] EverythingSubranges = new[] { Subrange.Everything };

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
        internal static bool TryParse(ref ErrorState errors, ref StringPart text, [MaybeNullWhen(false)] out Subrange[] sranges, out VersionComparer? comparer)
        {
            sranges = null;
            comparer = null;

            var copy = text;
            // check for the "everything" range first, which is just a star
            if (TryTake(ref text, '*'))
            {
                sranges = EverythingSubranges;
                errors.Report(new(RangeParseAction.FStar), copy);
                return true;
            }

            // then check for the "nothing" range, which is z or Z
            if (TryTake(ref text, 'z') || TryTake(ref text, 'Z'))
            {
                sranges = Array.Empty<Subrange>();
                errors.Report(new(RangeParseAction.FZero), copy);
                return true;
            }

            if (!TryReadComponent(ref errors, ref text, false, out var range, out var compare))
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
            while (TryReadComponent(ref errors, ref text, false, out range, out compare));

            text = restoreTo;
            sranges = ab.ToArray();
            return true;
        }

        public static bool TryParseComparer(ref ErrorState errors, ref StringPart text, out VersionComparer comparer)
        {
            var copy = text;
            if (!TryReadCompareType(ref errors, ref text, out var compareType))
            {
                errors.Report(new(RangeParseAction.EComparer), text);
                comparer = default;
                return false;
            }

            var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            text = text.TrimStart();
            if (!Version.TryParse(ref verErrors, ref text, out var version))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EComparerVersion), text);
                text = copy;
                comparer = default;
                return false;
            }
            verErrors.Dispose();

            comparer = new VersionComparer(version, compareType);
            errors.Report(new(RangeParseAction.FComparer), copy);
            return true;
        }

        private static bool TryReadCompareType(ref ErrorState errors, ref StringPart text, out ComparisonType type)
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
                errors.Report(new(RangeParseAction.ECompareType), text);
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

        public static bool TryReadComponent(ref ErrorState errors, ref StringPart text, bool allowOutward,
            out Subrange? nrange, out VersionComparer? ncompare)
        {
            var copy = text;

            nrange = null;
            ncompare = null;

            switch (text[0])
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case var c when char.IsDigit(c):
                    // first we check for a star range
                    if (TryReadStarRange(ref errors, ref text, out var subrange))
                    {
                        nrange = subrange;
                        errors.Report(new(RangeParseAction.FComponent), copy);
                        return true;
                    }
                    // then we check for a hyphen range
                    if (TryReadHyphenRange(ref errors, ref text, out subrange))
                    {
                        nrange = subrange;
                        errors.Report(new(RangeParseAction.FComponent), copy);
                        return true;
                    }

                    errors.Report(new(RangeParseAction.EComponent), text);
                    return false;

                //---EVERYTHING AFTER THIS POINT HAS A SPECIAL FIRST CHARACTER---\\
                case '^':
                    // then we check for a ^ range
                    if (TryReadCaretRange(ref errors, ref text, out subrange))
                    {
                        nrange = subrange;
                        errors.Report(new(RangeParseAction.FComponent), copy);
                        return true;
                    }

                    errors.Report(new(RangeParseAction.EComponent), text);
                    return false;

                default:
                    // otherwise we just try read two VersionComparers in a row
                    if (!TryParseComparer(ref errors, ref text, out var lower))
                    {
                        errors.Report(new(RangeParseAction.ESubrange1), text);
                        text = copy;
                        return false;
                    }
                    text = text.TrimStart();
                    var copy2 = text;
                    if (!TryParseComparer(ref errors, ref text, out var upper))
                    {
                        errors.Report(new(RangeParseAction.FComponent), copy);
                        text = copy2;
                        ncompare = lower;
                        return true;
                    }

                    if (lower.CompareTo > upper.CompareTo)
                    {
                        errors.Report(new(RangeParseAction.EOrderedSubrange), text);
                        text = copy;
                        return false;
                    }

                    if (lower.Type == ComparisonType.ExactEqual || upper.Type == ComparisonType.ExactEqual
                     || (lower.Type & ~ComparisonType.ExactEqual) == (upper.Type & ~ComparisonType.ExactEqual))
                    { // if the bounds point the same direction, the subrange is invalid
                        text = copy;
                        errors.Report(new(RangeParseAction.EClosedSubrange), text);
                        return false;
                    }

                    subrange = new Subrange(lower, upper);

                    if (!allowOutward && !subrange.IsInward)
                    { // reject outward-facing subranges for consistency on the outside
                        text = copy;
                        errors.Report(new(RangeParseAction.EClosedSubrange), text);
                        return false;
                    }

                    nrange = subrange;
                    errors.Report(new(RangeParseAction.FSubrange), copy);
                    errors.Report(new(RangeParseAction.FComponent), copy);
                    return true;
            }
        }

        private static bool TryReadCaretRange(ref ErrorState errors, ref StringPart text, out Subrange range)
        {
            var copy = text;
            if (!TryTake(ref text, '^'))
            {
                errors.Report(new(RangeParseAction.ECaret), text);
                range = default;
                return false;
            }

            text = text.TrimStart();
            var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            if (!Version.TryParse(ref verErrors, ref text, out var lower))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.ECaretVersion), text);
                text = copy;
                range = default;
                return false;
            }
            verErrors.Dispose();

            // if we don't fail to read a version, its info is just gonna make processing harder

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
            errors.Report(new(RangeParseAction.FCaretRange), copy);
            return true;
        }

        private static bool TryReadHyphenRange(ref ErrorState errors, ref StringPart text, out Subrange range)
        {
            var copy = text;
            var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            if (!Version.TryParse(ref verErrors, ref text, out var lowVersion))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EHyphenVersion), text);
                range = default;
                text = copy;
                return false;
            }

            text = text.TrimStart();
            if (!TryTake(ref text, '-'))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EHyphen), text);
                range = default;
                text = copy;
                return false;
            }
            text = text.TrimStart();

            if (!Version.TryParse(ref verErrors, ref text, out var highVersion))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EHyphenVersion2), text);
                range = default;
                text = copy;
                return false;
            }
            verErrors.Dispose();

            range = new(new(lowVersion, ComparisonType.GreaterEqual),
                new(highVersion, ComparisonType.LessEqual));
            errors.Report(new(RangeParseAction.FHyphenRange), copy);
            return true;
        }

        private static bool TryReadStarRange(ref ErrorState errors, ref StringPart text, out Subrange range)
        {
            var copy = text;

            var verErrors = new VersionErrorState()
            {
                ReportErrors = errors.ReportErrors,
                InputText = errors.InputText,
            };
            if (!VersionParser.TryParseNumId(ref verErrors, ref text, out var majorNum) || !TryTake(ref text, '.'))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EStarRange1), text);
                text = copy;
                range = default;
                return false;
            }

            static bool TryTakePlaceholder(ref StringPart text)
                => TryTake(ref text, '*') || TryTake(ref text, 'x') || TryTake(ref text, 'X');

            // at this point, we *know* that we have a star range
            if (TryTakePlaceholder(ref text))
            {
                var copy2 = text;
                // try to read another star
                if (!TryTake(ref text, '.')
                    || !TryTakePlaceholder(ref text))
                {
                    // if we can't, that's fine, just rewind to copy2
                    // this might be something else
                    text = copy2;
                }

                // we now have a star range
                var versionBase = new Version(majorNum, 0, 0);
                var versionUpper = new Version(majorNum + 1, 0, 0);
                // the range shouldn't include prereleases of the upper bound
                range = new Subrange(new VersionComparer(versionBase, ComparisonType.GreaterEqual),
                    new VersionComparer(versionUpper, ComparisonType.PreReleaseLess));

                // make sure we pull in parse errors
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.FStarRange), copy);
                return true;
            }

            // try to read the second number
            if (!VersionParser.TryParseNumId(ref verErrors, ref text, out var minorNum) || !TryTake(ref text, '.'))
            {
                // if we can't read the last bit then rewind and exit
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EStarRange2), text);
                text = copy;
                range = default;
                return false;
            }

            // if our last thing isn't a star, then this isn't a star range
            if (!TryTakePlaceholder(ref text))
            {
                errors.FromState(ref verErrors, AnyParseAction.Convert);
                errors.Report(new(RangeParseAction.EStarRange3), text);
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
            errors.FromState(ref verErrors, AnyParseAction.Convert);
            errors.Report(new(RangeParseAction.FStarRange), copy);
            return true;
        }
    }
}
