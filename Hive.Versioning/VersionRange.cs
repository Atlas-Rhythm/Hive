using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    public partial class VersionRange : IEquatable<VersionRange>
    {
        private readonly Subrange[] subranges;
        private readonly VersionComparer? additionalComparer;

        // TODO: finish this

        public VersionRange(ReadOnlySpan<char> text)
        {
            text = text.Trim();

            if (!TryParse(ref text, out var ranges, out additionalComparer) || text.Length > 0)
                throw new ArgumentException("Input is an invalid range", nameof(text));

            subranges = ranges;
        }

        private VersionRange(Subrange[] srs, VersionComparer? comparer)
        {
            (srs, comparer) = FixupRangeList(srs, comparer);

            subranges = srs;
            additionalComparer = comparer;
        }

        public VersionRange Disjunction(VersionRange other)
        {
            VersionComparer? comparer = null;
            Subrange? subrange = null;
            if (additionalComparer != null && other.additionalComparer != null)
            {
                var result = additionalComparer.Value.TryDisjunction(other.additionalComparer.Value, out var resComp, out var resSub);
                switch (result)
                {
                    case CombineResult.OneComparer:
                        comparer = resComp;
                        break;
                    case CombineResult.OneSubrange:
                        subrange = resSub;
                        break;
                    case CombineResult.Everything:
                        return Everything;
                    case CombineResult.Unrepresentable:
                        if (additionalComparer.Value.Type == ComparisonType.ExactEqual)
                        {
                            subrange = additionalComparer.Value.ToExactEqualSubrange();
                            comparer = other.additionalComparer;
                        }
                        else
                        {
                            subrange = other.additionalComparer.Value.ToExactEqualSubrange();
                            comparer = additionalComparer;
                        }
                        break;
                    default: throw new InvalidOperationException();
                }
            }
            else if (additionalComparer != null)
                comparer = additionalComparer;
            else
                comparer = other.additionalComparer;

            var allSubranges = new Subrange[subranges.Length + other.subranges.Length + (subrange != null ? 1 : 0)];
            Array.Copy(subranges, allSubranges, subranges.Length);
            Array.Copy(other.subranges, 0, allSubranges, subranges.Length, other.subranges.Length);
            if (subrange != null)
                allSubranges[^1] = subrange.Value;

            return new VersionRange(allSubranges, comparer);
        }

        public static VersionRange operator |(VersionRange a, VersionRange b) => a.Disjunction(b);


        private static readonly Subrange[] EverythingSubranges = new[] { Subrange.Everything };

        public static VersionRange Everything { get; } = new VersionRange(EverythingSubranges, null);
        public static VersionRange Nothing { get; } = new VersionRange(Array.Empty<Subrange>(), null);

        private static (Subrange[] Ranges, VersionComparer? Comparer) FixupRangeList(Subrange[] ranges, VersionComparer? comparer)
        {
            if (ranges.Length == 0 && comparer == null)
                return (ranges, comparer);

            Array.Sort(ranges, (a, b) => a.LowerBound.CompareTo.CompareTo(b.LowerBound.CompareTo));

            var ab = new ArrayBuilder<Subrange>(ranges.Length);

            Subrange nextToInsert = default;
            for (int i = 0; i < ranges.Length; i++)
            {
                var current = ranges[i];

                if (comparer != null)
                {
                    if (current.IsInward)
                    {
                        if (current.Matches(comparer.Value))
                        {
                            if ((comparer.Value.Type & ComparisonType.Greater) != 0)
                            {
                                comparer = current.LowerBound;
                                continue;
                            }
                            if ((comparer.Value.Type & ComparisonType.Less) != 0)
                            {
                                comparer = current.UpperBound;
                                continue;
                            }
                            // if we reach here, then comparer is an ExactEqual, so we can just kill it
                            comparer = null;
                        }
                        else
                        {
                            if ((comparer.Value.Type & ComparisonType.Greater) != 0)
                            {
                                if (comparer.Value.Matches(current.LowerBound))
                                    continue; // we want to skip this current, because it already matches our comparer
                            }
                            if ((comparer.Value.Type & ComparisonType.Less) != 0)
                            {
                                if (comparer.Value.Matches(current.UpperBound))
                                    continue; // we want to skip this current, because it already matches our comparer
                            }
                            // if its an exact equal and isn't matched, do nothing
                        }
                    }
                    else
                    {
                        // current is an outward subrange
                        if ((comparer.Value.Type & ComparisonType.Greater) != 0)
                        {
                            if (comparer.Value.Matches(current.LowerBound))
                            { // if we match the lower bound, then these two match everything
                                ab.Clear();
                                return (EverythingSubranges, null);
                            }
                            else if (comparer.Value.Matches(current.UpperBound))
                            { // if we match the upper bound, then adjust current to include comparer
                                current = new Subrange(current.LowerBound, comparer.Value);
                                comparer = null;
                            }
                            else
                            {
                                // comparer is completely included in current
                                comparer = null;
                            }
                        }
                        else if ((comparer.Value.Type & ComparisonType.Less) != 0)
                        {
                            if (comparer.Value.Matches(current.UpperBound))
                            { // if we match the upper bound, then these two match everything
                                ab.Clear();
                                return (EverythingSubranges, null);
                            }
                            else if (comparer.Value.Matches(current.LowerBound))
                            { // if we match the lower bound, then adjust current to include comparer
                                current = new Subrange(comparer.Value, current.UpperBound);
                                comparer = null;
                            }
                            else
                            {
                                // comparer is completely included in current
                                comparer = null;
                            }
                        }
                        else
                        {
                            // its an exact equal
                            if (current.Matches(comparer.Value))
                                comparer = null; // so if it matches, null comparer, otherwise do nothing
                        }
                    }
                }

                if (i == 0)
                {
                    nextToInsert = current;
                    continue;
                }

                var result = nextToInsert.TryDisjunction(current, out var result1, out var result2);
                switch (result)
                {
                    case CombineResult.OneSubrange:
                        nextToInsert = result1;
                        break;
                    case CombineResult.TwoSubranges:
                        if (result1.LowerBound.CompareTo < result2.LowerBound.CompareTo)
                        {
                            ab.Add(result1);
                            nextToInsert = result2;
                        }
                        else
                        {
                            ab.Add(result2);
                            nextToInsert = result1;
                        }
                        break;
                    case CombineResult.Everything:
                        // if any combo is everything, we can skip all the ceremony and make our result everything
                        ab.Clear();
                        return (EverythingSubranges, null);
                    default: throw new InvalidOperationException();
                }
            }

            if (ranges.Length > 0)
                ab.Add(nextToInsert);

            return (ab.ToArray(), comparer);
        }

        public StringBuilder ToString(StringBuilder sb)
        {
            for (int i = 0; i < subranges.Length; i++)
            {
                subranges[i].ToString(sb);
                if (i != subranges.Length - 1)
                    sb.Append(" || ");
            }

            if (additionalComparer != null)
            {
                if (subranges.Length > 0)
                    sb.Append(" || ");
                additionalComparer.Value.ToString(sb);
            }

            return sb;
        }

        public override string ToString()
            => ToString(new StringBuilder()).ToString();

        public override bool Equals(object obj)
            => obj is VersionRange range && Equals(range);

        public bool Equals(VersionRange other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (additionalComparer == null ^ other.additionalComparer == null)
                return false;
            if (subranges.Length != other.subranges.Length)
                return false;
            if (additionalComparer != null && other.additionalComparer != null
             && !additionalComparer.Value.Equals(other.additionalComparer.Value))
                return false;

            for (var i = 0; i < subranges.Length; i++)
            {
                if (!subranges[i].Equals(other.subranges[i]))
                    return false;
            }

            return true;
        }

        public static bool operator ==(VersionRange? a, VersionRange? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(VersionRange? a, VersionRange? b) => !(a == b);

        public static bool TryParse(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out VersionRange range)
        {
            text = text.Trim();
            return TryParse(ref text, out range) && text.Length == 0;
        }

        public static bool TryParse(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out VersionRange range)
        {
            if (!TryParse(ref text, out var srs, out var compare))
            {
                range = null;
                return false;
            }

            range = new VersionRange(srs, compare);
            return true;
        }

        #region Parser
        private static bool TryParse(ref ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Subrange[] sranges, out VersionComparer? comparer)
        {
            sranges = null;
            comparer = null;

            if (!TryReadComponent(ref text, out var range, out var compare))
                return false;

            var ab = new ArrayBuilder<Subrange>();

            ReadOnlySpan<char> restoreTo;
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
                                if (compare.Value.Type == ComparisonType.ExactEqual)
                                    ab.Add(compare.Value.ToExactEqualSubrange());
                                if (comparer.Value.Type == compare.Value.Type)
                                    comparer = null;
                                break;
                            default: throw new InvalidOperationException();
                        }
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

        private static bool TryReadComponent(ref ReadOnlySpan<char> text, out Subrange? range, out VersionComparer? compare)
        {
            if (Subrange.TryParse(ref text, out var sr))
            {
                range = sr;
                compare = null;
                return true;
            }

            if (VersionComparer.TryParse(ref text, out var comp))
            {
                range = null;
                compare = comp;
                return true;
            }

            range = null;
            compare = null;
            return false;
        }
        #endregion
    }
}
