using System;
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

                // if one is exact equal and the other matches it, then return the one that matches it
                if (Type == ComparisonType.ExactEqual)
                { 
                    if (other.Matches(this))
                    {
                        comparer = other;
                        return ComparerCombineResult.SingleComparer;
                    }
                    else
                    {
                        // if we *don't* match, then this is similar to when they are both equal and not the same
                        return ComparerCombineResult.Invalid;
                    }
                }
                if (other.Type == ComparisonType.ExactEqual)
                {
                    if (Matches(other))
                    {
                        comparer = this;
                        return ComparerCombineResult.SingleComparer;
                    }
                    else
                    {
                        // if we *don't* match, then this is similar to when they are both equal and not the same
                        return ComparerCombineResult.Invalid;
                    }
                }

                // at this point, we know that the versions form a range, be it inward or outward
                var thisIsMin = CompareTo < other.CompareTo;
                range = thisIsMin ? new Subrange(this, other) : new Subrange(other, this);
                return ComparerCombineResult.Subrange;
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
                    if (UpperBound.CompareTo == other.LowerBound.CompareTo)
                    {
                        if (((UpperBound.Type & other.LowerBound.Type) & ~ComparisonType.ExactEqual) == ComparisonType.None  // they have opposite directions
                         && ((UpperBound.Type ^ other.LowerBound.Type) &  ComparisonType.ExactEqual) != ComparisonType.None) // there is exactly one equal between them
                        {
                            result = new Subrange(LowerBound, other.UpperBound);
                            return true;
                        }
                    }
                    if (other.UpperBound.CompareTo == LowerBound.CompareTo)
                    {
                        if (((other.UpperBound.Type & LowerBound.Type) & ~ComparisonType.ExactEqual) == ComparisonType.None  // they have opposite directions
                         && ((other.UpperBound.Type ^ LowerBound.Type) &  ComparisonType.ExactEqual) != ComparisonType.None) // there is exactly one equal between them
                        {
                            result = new Subrange(other.LowerBound, UpperBound);
                            return true;
                        }
                    }

                    // otherwise, we can't combine them
                    result = default;
                    return false;
                }

                throw new NotImplementedException();

                if (IsInward && !other.IsInward)
                {

                }
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
