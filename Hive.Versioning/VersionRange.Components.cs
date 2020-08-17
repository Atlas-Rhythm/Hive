using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    // This file has only the internal components of a VersionRange.
    // For everything else, see VersionRange.cs
    public partial class VersionRange
    {
        private static void Assert(bool value)
        {
            if (!value)
                throw new InvalidOperationException("Assertion failed");
        }

        [Flags]
        internal enum ComparisonType
        {
            None = 0,
            ExactEqual = 1,
            Greater = 2,
            GreaterEqual = Greater | ExactEqual,
            Less = 4,
            LessEqual = Less | ExactEqual,

            _All = ExactEqual | Greater | Less,
        }

        internal enum CombineResult
        {
            OneComparer,
            OneSubrange,
            TwoSubranges,
            Unrepresentable,
            Nothing,
            Everything
        }

        internal partial struct VersionComparer
        {
            public readonly Version CompareTo;
            public readonly ComparisonType Type;

            public VersionComparer(Version compTo, ComparisonType type)
            {
                if (type == ComparisonType.None)
                    throw new ArgumentException("SegmentType cannot be None", nameof(type));
                CompareTo = compTo;
                Type = type;
            }

            public bool Matches(Version ver)
                => Type switch
                {
                    ComparisonType.ExactEqual => ver == CompareTo,
                    ComparisonType.Greater => ver > CompareTo,
                    ComparisonType.Less => ver < CompareTo,
                    ComparisonType.GreaterEqual => ver >= CompareTo,
                    ComparisonType.LessEqual => ver <= CompareTo,
                    _ => throw new InvalidOperationException(),
                };

            public bool Matches(in VersionComparer other)
                => Matches(other.CompareTo)
                || (Type == other.Type && CompareTo == other.CompareTo);

            public Subrange ToExactEqualSubrange()
            {
                Assert(Type == ComparisonType.ExactEqual);
                return new Subrange(new VersionComparer(CompareTo, ComparisonType.GreaterEqual), new VersionComparer(CompareTo, ComparisonType.LessEqual));
            }

            public CombineResult Invert(out VersionComparer comparer, out Subrange range)
            {
                switch (Type)
                {
                    case ComparisonType.ExactEqual:
                        comparer = default;
                        range = new Subrange(new VersionComparer(CompareTo, ComparisonType.Less), new VersionComparer(CompareTo, ComparisonType.Greater));
                        return CombineResult.OneSubrange;
                    default:
                        range = default;
                        comparer = new VersionComparer(CompareTo,
                            Type switch
                            {
                                ComparisonType.Greater => ComparisonType.LessEqual,
                                ComparisonType.GreaterEqual => ComparisonType.Less,
                                ComparisonType.Less => ComparisonType.GreaterEqual,
                                ComparisonType.LessEqual => ComparisonType.Greater,
                                _ => throw new InvalidOperationException()
                            });
                        return CombineResult.OneComparer;
                }
            }

            public CombineResult TryConjunction(in VersionComparer other, out VersionComparer comparer, out Subrange range)
            {
                comparer = default;
                range = default;

                if ((Type & other.Type & ~ComparisonType.ExactEqual) != ComparisonType.None) // they're pointing the same direction
                { 
                    // so we want to pick the one that is encapsulated completely (the conjunction)
                    comparer = Matches(other) ? other : this;
                    return CombineResult.OneComparer;
                }

                if (Type == ComparisonType.ExactEqual && other.Type == ComparisonType.ExactEqual)
                {
                    if (CompareTo == other.CompareTo)
                    {
                        comparer = this;
                        return CombineResult.OneComparer;
                    }
                    else
                    {
                        // a conjunction of 2 different exact values is nothing
                        return CombineResult.Nothing;
                    }
                }

                if (TryConjunctionEqualPart(this, other, out comparer, out var res)) return res;
                if (TryConjunctionEqualPart(other, this, out comparer, out res)) return res;

                // at this point, we know that the versions form a range, be it inward or outward
                // but because this is a conjunction, the range *must* be inward, otherwise we return Nothing
                var thisIsMin = CompareTo < other.CompareTo;
                range = thisIsMin ? new Subrange(this, other) : new Subrange(other, this);
                if (range.IsInward)
                    return CombineResult.OneSubrange;
                else
                    return CombineResult.Nothing;
            }

            private static bool TryConjunctionEqualPart(in VersionComparer a, in VersionComparer b, out VersionComparer comp, out CombineResult res)
            {
                // if one is exact equal and the other matches it, then return the one that is an equal
                if (a.Type == ComparisonType.ExactEqual)
                {
                    if (b.Matches(a))
                    {
                        comp = a;
                        res = CombineResult.OneComparer;
                        return true;
                    }
                    else
                    {
                        // if we *don't* match, then this is similar to when they are both equal and not the same
                        comp = default;
                        res = CombineResult.Nothing;
                        return true;
                    }
                }

                comp = default;
                res = default;
                return false;
            }

            public CombineResult TryDisjunction(in VersionComparer other, out VersionComparer comparer, out Subrange range)
            {
                comparer = default;
                range = default;

                if ((Type & other.Type & ~ComparisonType.ExactEqual) != ComparisonType.None) // they're pointing the same direction
                {
                    // so we want to pick the one that is completely encapsulates the other
                    comparer = Matches(other) ? this : other;
                    return CombineResult.OneComparer;
                }

                if (Type == ComparisonType.ExactEqual && other.Type == ComparisonType.ExactEqual)
                {
                    if (CompareTo == other.CompareTo)
                    {
                        comparer = this;
                        return CombineResult.OneComparer;
                    }
                    else
                    {
                        // a disjunction of 2 different exact equal values cannot be represented by our outputs
                        return CombineResult.Unrepresentable;
                    }
                }

                if (TryDisjunctionEqualPart(this, other, out comparer, out var res)) return res;
                if (TryDisjunctionEqualPart(other, this, out comparer, out res)) return res;

                // at this point, we know that the versions form a range, be it inward or outward
                // but because this is a disjunction, the range *must* be outward, otherwise we return Everything
                var thisIsMin = CompareTo < other.CompareTo;
                range = thisIsMin ? new Subrange(this, other) : new Subrange(other, this);
                if (range.IsInward)
                {
                    range = Subrange.Everything;
                    return CombineResult.Everything;
                }
                else
                    return CombineResult.OneSubrange;
            }

            private static bool TryDisjunctionEqualPart(in VersionComparer a, in VersionComparer b, out VersionComparer comp, out CombineResult res)
            {
                // if one is exact equal and the other matches it, then return the one that matches it
                if (a.Type == ComparisonType.ExactEqual)
                {
                    if (b.Matches(a))
                    {
                        comp = b;
                        res = CombineResult.OneComparer;
                        return true;
                    }
                    else
                    {
                        // if we *don't* match, then this is similar to when they are both equal and not the same
                        comp = default;
                        res = CombineResult.Unrepresentable;
                        return true;
                    }
                }

                comp = default;
                res = default;
                return false;
            }
        }

        internal partial struct Subrange
        {
            public readonly VersionComparer LowerBound;
            public readonly VersionComparer UpperBound;
            public readonly bool IsInward;

            public Subrange(in VersionComparer lower, in VersionComparer upper)
            {
                if (lower.Type == ComparisonType.ExactEqual || upper.Type == ComparisonType.ExactEqual)
                    throw new ArgumentException("Subrange cannot take ExactEqual as one of its bounds");
                if (lower.CompareTo > upper.CompareTo)
                    throw new ArgumentException("Lower bound must be below upper bound");

                if (lower.CompareTo == upper.CompareTo && (lower.Type & upper.Type & ComparisonType.ExactEqual) != 0)
                { // this is only the case if this is an ExactEqual subrange, so we can set it more consistently
                    LowerBound = new VersionComparer(lower.CompareTo, ComparisonType.GreaterEqual);
                    UpperBound = new VersionComparer(upper.CompareTo, ComparisonType.LessEqual);
                    IsInward = true; // ExactEqual subranges are always considered inward
                }
                else
                {
                    LowerBound = lower;
                    UpperBound = upper;
                    IsInward = lower.Matches(upper) && upper.Matches(lower);
                }
            }

            public bool Matches(Version ver)
            {
                if (IsInward)
                { // this means they "face" each other, and so both must match
                    return LowerBound.Matches(ver) && UpperBound.Matches(ver);
                }
                else
                { // they "face" away from each other, and so either matches
                    return LowerBound.Matches(ver) || UpperBound.Matches(ver);
                }
            }
            public bool Matches(in VersionComparer ver)
            {
                if (IsInward)
                { // this means they "face" each other, and so both must match
                    return LowerBound.Matches(ver) && UpperBound.Matches(ver);
                }
                else
                { // they "face" away from each other, and so either matches
                    return LowerBound.Matches(ver) || UpperBound.Matches(ver);
                }
            }

            public Subrange Invert()
            {
                Assert(LowerBound.Invert(out var lower, out _) == CombineResult.OneComparer);
                Assert(UpperBound.Invert(out var upper, out _) == CombineResult.OneComparer);
                return new Subrange(lower, upper);
            }

            public CombineResult TryConjunction(in Subrange other, out Subrange result, out Subrange result2)
            {
                result2 = default;

                if (IsInward && other.IsInward)
                {
                    // we're combining inward ranges, so our job is fairly simple
                    // either, there is overlap
                    if (Matches(other.LowerBound) || other.Matches(LowerBound))
                    {
                        Assert(LowerBound.TryConjunction(other.LowerBound, out var lower, out _) == CombineResult.OneComparer);
                        Assert(UpperBound.TryConjunction(other.UpperBound, out var upper, out _) == CombineResult.OneComparer);
                        result = new Subrange(lower, upper);
                        return CombineResult.OneSubrange;
                    }

                    // otherwise, we can't combine them
                    result = default;
                    return CombineResult.Nothing;
                }

                // handle the case where there is exactly one inward range
                if (TryConjunctionOneInwardPart(this, other, out result, out result2, out var res)) return res;
                if (TryConjunctionOneInwardPart(other, this, out result, out result2, out res)) return res;

                // now we know that both are outward ranges
                {
                    var thisIsLower = LowerBound.CompareTo < other.LowerBound.CompareTo;
                    var lowerRange = thisIsLower ? this : other;
                    var upperRange = thisIsLower ? other : this;

                    var lowResult = lowerRange.LowerBound.TryConjunction(upperRange.LowerBound, out var lowCompare, out _);
                    var midResult = lowerRange.UpperBound.TryConjunction(upperRange.LowerBound, out _, out var midRange);
                    var highResult = lowerRange.UpperBound.TryConjunction(upperRange.UpperBound, out var highCompare, out _);

                    Assert(lowResult == CombineResult.OneComparer);
                    Assert(midResult == CombineResult.Nothing || midResult == CombineResult.OneSubrange);
                    Assert(highResult == CombineResult.OneComparer);

                    result = new Subrange(lowCompare, highCompare);
                    result2 = midRange;
                    return midResult == CombineResult.Nothing ? CombineResult.OneSubrange : CombineResult.TwoSubranges;
                }
            }

            private static bool TryConjunctionOneInwardPart(in Subrange a, in Subrange b, out Subrange result, out Subrange result2, out CombineResult retVal)
            {
                result2 = default;
                if (a.IsInward && !b.IsInward)
                {
                    // a is completely contained by b
                    if ((b.LowerBound.Matches(a.LowerBound) && b.LowerBound.Matches(a.UpperBound))
                     || (b.UpperBound.Matches(a.LowerBound) && b.UpperBound.Matches(a.UpperBound)))
                    {
                        result = a;
                        retVal = CombineResult.OneSubrange;
                        return true;
                    }

                    // lower bound is within the lower segment, upper is not
                    if (b.LowerBound.Matches(a.LowerBound) && !b.UpperBound.Matches(a.UpperBound))
                    {
                        retVal = b.LowerBound.TryConjunction(a.LowerBound, out _, out result);
                        Assert(retVal == CombineResult.OneSubrange);
                        return true;
                    }
                    // upper bound is within the upper segment, lower is not
                    if (b.UpperBound.Matches(a.UpperBound) && !b.LowerBound.Matches(a.LowerBound))
                    {
                        retVal = b.UpperBound.TryConjunction(a.UpperBound, out _, out result);
                        Assert(retVal == CombineResult.OneSubrange);
                        return true;
                    }

                    // the edges of A are in each side of B, so unrepresentable with only one Subrange, but representable with 2
                    if (b.LowerBound.Matches(a.LowerBound) && b.UpperBound.Matches(a.UpperBound))
                    {
                        Assert(b.LowerBound.TryConjunction(a.LowerBound, out _, out result) == CombineResult.OneSubrange);
                        Assert(b.UpperBound.TryConjunction(a.UpperBound, out _, out result2) == CombineResult.OneSubrange);
                        retVal = CombineResult.TwoSubranges;
                        return true;
                    }

                    // otherwise we can't combine them
                    result = default;
                    retVal = CombineResult.Nothing;
                    return true;
                }

                result = default;
                retVal = default;
                return false;
            }


            public CombineResult TryDisjunction(in Subrange other, out Subrange result, out Subrange result2)
            {
                result2 = default;

                if (IsInward && other.IsInward)
                {
                    // we're combining inward ranges, so our job is fairly simple
                    // either, there is overlap
                    if (Matches(other.LowerBound) || other.Matches(LowerBound))
                    {
                        Assert(LowerBound.TryDisjunction(other.LowerBound, out var lower, out _) == CombineResult.OneComparer);
                        Assert(UpperBound.TryDisjunction(other.UpperBound, out var upper, out _) == CombineResult.OneComparer);
                        result = new Subrange(lower, upper);
                        return CombineResult.OneSubrange;
                    }

                    // or the edges meet and leave no gap
                    if (TestExactMeeting(UpperBound, other.LowerBound))
                    {
                        result = new Subrange(LowerBound, other.UpperBound);
                        return CombineResult.OneSubrange;
                    }
                    if (TestExactMeeting(other.UpperBound, LowerBound))
                    {
                        result = new Subrange(other.LowerBound, UpperBound);
                        return CombineResult.OneSubrange;
                    }

                    // otherwise, we can't combine them, so are outputs are just our inputs
                    result = this;
                    result2 = other;
                    return CombineResult.TwoSubranges;
                }

                // handle the case where there is exactly one inward range
                if (TryDisjunctionOneInwardPart(this, other, out result, out result2, out var res)) return res;
                if (TryDisjunctionOneInwardPart(other, this, out result, out result2, out res)) return res;

                {
                    // now we know that both are outward ranges
                    // and that makes the job *incredibly* easy
                    Assert(LowerBound.TryDisjunction(other.LowerBound, out var lowCompare, out _) == CombineResult.OneComparer);
                    Assert(UpperBound.TryDisjunction(other.UpperBound, out var highCompare, out _) == CombineResult.OneComparer);

                    result = new Subrange(lowCompare, highCompare);
                    result2 = default;
                    if (result.IsInward)
                    {
                        // if the result of the disjunctions of 2 outwards is inward, then that just means that
                        //   the two sides are matching eachother, so it *should* match everything
                        result = Everything;
                        return CombineResult.Everything;
                    }
                    else
                        return CombineResult.OneSubrange;
                }
            }

            private static bool TryDisjunctionOneInwardPart(in Subrange a, in Subrange b, out Subrange result, out Subrange result2, out CombineResult retVal)
            {
                result2 = default;
                if (a.IsInward && !b.IsInward)
                {
                    // a is completely contained by b
                    if ((b.LowerBound.Matches(a.LowerBound) && b.LowerBound.Matches(a.UpperBound))
                     || (b.UpperBound.Matches(a.LowerBound) && b.UpperBound.Matches(a.UpperBound)))
                    {
                        result = a;
                        retVal = CombineResult.OneSubrange;
                        return true;
                    }

                    var meetLower = TestExactMeeting(b.LowerBound, a.LowerBound);
                    var meetUpper = TestExactMeeting(b.UpperBound, a.UpperBound);
                    // if we meet at both the upper and lower bounds, then our result is everything
                    if (meetLower && meetUpper)
                    {
                        result = Everything;
                        retVal = CombineResult.Everything;
                        return true;
                    }

                    // the edges of A are in each side of B, so it matches everything
                    if (b.LowerBound.Matches(a.LowerBound) && b.UpperBound.Matches(a.UpperBound))
                    {
                        result = Everything;
                        retVal = CombineResult.Everything;
                        return true;
                    }

                    // our lower bound exactly meets its lower bound
                    if (meetLower)
                    {
                        retVal = b.UpperBound.TryDisjunction(a.UpperBound, out _, out result);
                        Assert(retVal == CombineResult.OneSubrange || retVal == CombineResult.Everything);
                        return true;
                    }
                    // out upper bound exactly meets its upper bound
                    if (meetUpper)
                    {
                        retVal = b.LowerBound.TryDisjunction(a.LowerBound, out _, out result);
                        Assert(retVal == CombineResult.OneSubrange || retVal == CombineResult.Everything);
                        return true;
                    }

                    // lower bound is within the lower segment, upper is not
                    if (b.LowerBound.Matches(a.LowerBound) && !b.UpperBound.Matches(a.UpperBound))
                    {
                        Assert(b.LowerBound.TryDisjunction(a.UpperBound, out var compare, out _) == CombineResult.OneComparer);
                        result = new Subrange(compare, b.UpperBound);
                        retVal = CombineResult.OneSubrange;
                        return true;
                    }
                    // upper bound is within the upper segment, lower is not
                    if (b.UpperBound.Matches(a.UpperBound) && !b.LowerBound.Matches(a.LowerBound))
                    {
                        Assert(b.UpperBound.TryDisjunction(a.LowerBound, out var compare, out _) == CombineResult.OneComparer);
                        result = new Subrange(b.LowerBound, compare);
                        retVal = CombineResult.OneSubrange;
                        return true;
                    }

                    // otherwise we can't combine them into one, so our outputs are just our inputs
                    result = a;
                    result2 = b;
                    retVal = CombineResult.TwoSubranges;
                    return true;
                }

                result = default;
                retVal = default;
                return false;
            }

            private static bool TestExactMeeting(in VersionComparer a, in VersionComparer b)
                => a.CompareTo == b.CompareTo
                && ((a.Type & b.Type) & ~ComparisonType.ExactEqual) == ComparisonType.None  // they have opposite directions
                && ((a.Type ^ b.Type) & ComparisonType.ExactEqual) != ComparisonType.None; // there is exactly one equal between them
        }

        internal partial struct Subrange
        {
            public static readonly Subrange Everything;

            static Subrange()
            {
                Everything = new Subrange(
                    new VersionComparer(Version.Zero, ComparisonType.LessEqual),
                    new VersionComparer(Version.Zero, ComparisonType.Greater));
            }
        }

        #region Parser
        internal partial struct VersionComparer
        {
            public static bool TryParse(ref ReadOnlySpan<char> text, out VersionComparer comparer)
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

            private static bool TryReadCompareType(ref ReadOnlySpan<char> text, out ComparisonType type)
            {
                type = ComparisonType.None;

                var copy = text;
                if (TryTake(ref text, '>'))
                    type |= ComparisonType.Greater;
                else if (TryTake(ref text, '<'))
                    type |= ComparisonType.Less;
                if (TryTake(ref text, '='))
                    type |= ComparisonType.ExactEqual;

                if (type == ComparisonType.None)
                {
                    text = copy;
                    return false;
                }

                return true;
            }

            public StringBuilder ToString(StringBuilder sb)
            {
                if (Type == ComparisonType.None) return sb.Append("default");

                if ((Type & ComparisonType.Greater) != ComparisonType.None)
                    sb.Append(">");
                if ((Type & ComparisonType.Less) != ComparisonType.None)
                    sb.Append("<");
                if ((Type & ComparisonType.ExactEqual) != ComparisonType.None)
                    sb.Append("=");
                if ((Type & ~ComparisonType._All) != ComparisonType.None)
                    sb.Append("!Invalid!");

                return CompareTo.ToString(sb);
            }

            public override string ToString()
                => ToString(new StringBuilder()).ToString();
        }
        internal partial struct Subrange
        {
            public static bool TryParse(ref ReadOnlySpan<char> text, out Subrange subrange)
            {
                var copy = text;

                // first we check for a ^ range
                if (TryReadCaretRange(ref text, out subrange)) return true;

                // otherwise we just try read two VersionComparers in a row
                if (!VersionComparer.TryParse(ref text, out var lower))
                {
                    text = copy;
                    subrange = default;
                    return false;
                }
                text = text.TrimStart();
                if (!VersionComparer.TryParse(ref text, out var upper))
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

                subrange = new Subrange(lower, upper);
                return true;
            }

            private static bool TryReadCaretRange(ref ReadOnlySpan<char> text, out Subrange range)
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

                range = new Subrange(new VersionComparer(lower, ComparisonType.GreaterEqual), new VersionComparer(upper, ComparisonType.Less));
                return true;
            }

            public StringBuilder ToString(StringBuilder sb)
                => UpperBound.ToString(LowerBound.ToString(sb).Append(" "));
            public override string ToString()
                => ToString(new StringBuilder()).ToString();
        }
        #endregion
    }
}
