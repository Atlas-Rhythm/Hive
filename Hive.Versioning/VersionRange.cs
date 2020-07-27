using System;
using System.Collections.Generic;
using System.Text;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    public class VersionRange
    {



        [Flags]
        private enum ComparisonType
        {
            None = 0,
            ExactEqual = 1,
            Greater = 2,
            GreaterEqual = Greater | ExactEqual,
            Less = 4,
            LessEqual = Less | ExactEqual,
        }

        private enum ComparerCombineResult
        {
            SingleComparer,
            Subrange
        }

        private partial struct VersionComparer
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
        }

        private partial struct Subrange
        {
            public readonly VersionComparer LowerBound;
            public readonly VersionComparer UpperBound;
            public readonly bool IsInward;

            public Subrange(VersionComparer lower, VersionComparer upper)
            {
                if (lower.Type == ComparisonType.ExactEqual || upper.Type == ComparisonType.ExactEqual)
                    throw new ArgumentException("Subrange cannot take ExactEqual as one of its bounds");

                LowerBound = lower;
                UpperBound = upper;

                IsInward = lower.Matches(upper.CompareTo) && upper.Matches(lower.CompareTo);
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
                { // we're combining inward ranges, so our job is fairly simple
                    if (Matches(other.LowerBound.CompareTo) && LowerBound.CompareTo < other.UpperBound.CompareTo)
                    { // other's lower bound is within our range, and its upper is above

                    }
                }
            }
        }


        #region Parser
        private partial struct VersionComparer 
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
        private partial struct Subrange
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
