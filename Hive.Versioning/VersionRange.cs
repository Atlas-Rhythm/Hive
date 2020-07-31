﻿using System;
using System.Collections.Generic;
using System.Text;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    public class VersionRange
    {



        [Flags]
        internal enum ComparisonType
        {
            None = 0,
            ExactEqual = 1,
            Greater = 2,
            GreaterEqual = Greater | ExactEqual,
            Less = 4,
            LessEqual = Less | ExactEqual,
        }

        internal enum ComparerCombineResult
        {
            SingleComparer,
            Subrange,
            Invalid
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

            public ComparerCombineResult Invert(out VersionComparer comparer, out Subrange range)
            {
                switch (Type)
                {
                    case ComparisonType.ExactEqual:
                        comparer = default;
                        range = new Subrange(new VersionComparer(CompareTo, ComparisonType.Less), new VersionComparer(CompareTo, ComparisonType.Greater));
                        return ComparerCombineResult.Subrange;
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
                        return ComparerCombineResult.SingleComparer;
                }
            }

            public ComparerCombineResult CombineWith(in VersionComparer other, out VersionComparer comparer, out Subrange range)
            {
                comparer = default;
                range = default;

                if ((Type & other.Type & ~ComparisonType.ExactEqual) != ComparisonType.None) // they're pointing the same direction
                { // both face in the same direction
                    // so we want to pick the one that encapsulates the other completely
                    comparer = Matches(other) ? this : other;
                    return ComparerCombineResult.SingleComparer;
                }

                if (Type == ComparisonType.ExactEqual && other.Type == ComparisonType.ExactEqual)
                {
                    if (CompareTo == other.CompareTo)
                    {
                        comparer = this;
                        return ComparerCombineResult.SingleComparer;
                    }
                    else
                    {
                        // there is no way to create a Subrange or VersionComparer that has only the specified members
                        return ComparerCombineResult.Invalid;
                    }
                }

                if (CombineWithEqualPart(this, other, out comparer, out var res)) return res;
                if (CombineWithEqualPart(other, this, out comparer, out res)) return res;

                // at this point, we know that the versions form a range, be it inward or outward
                var thisIsMin = CompareTo < other.CompareTo;
                range = thisIsMin ? new Subrange(this, other) : new Subrange(other, this);
                return ComparerCombineResult.Subrange;
            }

            private static bool CombineWithEqualPart(in VersionComparer a, in VersionComparer b, out VersionComparer comp, out ComparerCombineResult res)
            {
                // if one is exact equal and the other matches it, then return the one that matches it
                if (a.Type == ComparisonType.ExactEqual)
                {
                    if (b.Matches(a))
                    {
                        comp = a;
                        res = ComparerCombineResult.SingleComparer;
                        return true;
                    }
                    else
                    {
                        // if we *don't* match, then this is similar to when they are both equal and not the same
                        comp = default;
                        res = ComparerCombineResult.Invalid;
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

                LowerBound = lower;
                UpperBound = upper;

                IsInward = lower.Matches(upper) && upper.Matches(lower);
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
                var lowResult = LowerBound.Invert(out var lower, out _);
                var highResult = UpperBound.Invert(out var upper, out _);
                if (lowResult != ComparerCombineResult.SingleComparer || highResult != ComparerCombineResult.SingleComparer)
                    throw new InvalidOperationException("Subrange somehow managed to have one of its bounds be an equality comparison");
                return new Subrange(lower, upper);
            }

            public bool TryCombineWith(in Subrange other, out Subrange result)
            {
                if (IsInward && other.IsInward)
                { 
                    // we're combining inward ranges, so our job is fairly simple
                    // either, there is overlap
                    if (Matches(other.LowerBound) || other.Matches(LowerBound))
                    {
                        var lowResult = LowerBound.CombineWith(other.LowerBound, out var lower, out _);
                        var highResult = UpperBound.CombineWith(other.UpperBound, out var upper, out _);
                        if (lowResult != ComparerCombineResult.SingleComparer || highResult != ComparerCombineResult.SingleComparer)
                            throw new InvalidOperationException("I'm pretty sure this case is literally impossible");

                        result = new Subrange(lower, upper);
                        return true;
                    }
                    // or the edges meet and leave no gap
                    if (TestExactMeeting(UpperBound, other.LowerBound))
                    {
                        result = new Subrange(LowerBound, other.UpperBound);
                        return true;
                    }
                    if (TestExactMeeting(other.UpperBound, LowerBound))
                    {
                        result = new Subrange(other.LowerBound, UpperBound);
                        return true;
                    }

                    // otherwise, we can't combine them
                    result = default;
                    return false;
                }

                // handle the case where there is exactly one inward range
                if (TryCombineWithOneInPart(this, other, out result, out var res)) return res;
                if (TryCombineWithOneInPart(other, this, out result, out res)) return res;

                {
                    // here, they are both outward ranges
                    // this case is *really* simple
                    // just combine the bounds, and if they are in the wrong order, return everything
                    var lowResult = LowerBound.CombineWith(other.LowerBound, out var lower, out _);
                    var highResult = UpperBound.CombineWith(other.UpperBound, out var upper, out _);
                    if (lowResult != ComparerCombineResult.SingleComparer || highResult != ComparerCombineResult.SingleComparer)
                        throw new InvalidOperationException("The lower and upper bounds were somehow in the wrong order");

                    if (lower.Matches(upper) || upper.Matches(lower))
                        result = Everything;
                    else
                        result = new Subrange(lower, upper);
                    return true;
                }
            }

            private static bool TestExactMeeting(in VersionComparer a, in VersionComparer b)
                => a.CompareTo == b.CompareTo
                && ((a.Type & b.Type) & ~ComparisonType.ExactEqual) == ComparisonType.None  // they have opposite directions
                && ((a.Type ^ b.Type) &  ComparisonType.ExactEqual) != ComparisonType.None; // there is exactly one equal between them

            private static bool TryCombineWithOneInPart(in Subrange a, in Subrange b, out Subrange result, out bool retVal)
            {
                if (a.IsInward && !b.IsInward)
                {
                    // this is completely contained by b
                    if ((b.LowerBound.Matches(a.LowerBound) && b.LowerBound.Matches(a.UpperBound))
                     || (b.UpperBound.Matches(a.LowerBound) && b.UpperBound.Matches(a.UpperBound)))
                    {
                        result = b;
                        retVal = true;
                        return true;
                    }

                    var meetLower = TestExactMeeting(b.LowerBound, a.LowerBound);
                    var meetUpper = TestExactMeeting(b.UpperBound, a.UpperBound);
                    // if we meet at both the upper and lower bounds, then our result is everything
                    if (meetLower && meetUpper)
                    {
                        result = Everything;
                        retVal = true;
                        return true;
                    }
                    // our lower bound exactly meets its lower bound
                    if (meetLower)
                    {
                        result = new Subrange(a.UpperBound, b.UpperBound);
                        retVal = true;
                        return true;
                    }
                    // out upper bound exactly meets its upper bound
                    if (meetUpper)
                    {
                        result = new Subrange(a.LowerBound, b.LowerBound);
                        retVal = true;
                        return true;
                    }
                    // lower bound is within the lower segment, upper is not
                    if (b.LowerBound.Matches(a.LowerBound) && !b.UpperBound.Matches(a.UpperBound))
                    {
                        var lowResult = b.LowerBound.CombineWith(a.UpperBound, out var lower, out _);
                        if (lowResult != ComparerCombineResult.SingleComparer)
                            throw new InvalidOperationException();

                        result = new Subrange(lower, b.UpperBound);
                        retVal = true;
                        return true;
                    }
                    // upper bound is within the upper segment, lower is not
                    if (b.UpperBound.Matches(a.UpperBound) && !b.LowerBound.Matches(a.LowerBound))
                    {
                        var highResult = b.UpperBound.CombineWith(a.LowerBound, out var upper, out _);
                        if (highResult != ComparerCombineResult.SingleComparer)
                            throw new InvalidOperationException();

                        result = new Subrange(b.LowerBound, upper);
                        retVal = true;
                        return true;
                    }

                    // otherwise we can't combine them
                    result = default;
                    retVal = false;
                    return true;
                }

                result = default;
                retVal = default;
                return false;
            }
        }

        internal partial struct Subrange
        {
            public static readonly Subrange Everything;
            public static readonly Subrange Nothing;

            static Subrange()
            {
                Everything = new Subrange(
                    new VersionComparer(Version.Zero, ComparisonType.LessEqual), 
                    new VersionComparer(Version.Zero, ComparisonType.Greater));
                Nothing = Everything.Invert();
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
        }
        internal partial struct Subrange
        {
            public static bool TryParse(ref ReadOnlySpan<char> text, out Subrange subrange)
            {
                // TODO:
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
