using Hive.Utilities;
using Hive.Versioning.Resources;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

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
#pragma warning restore IDE0065 // Misplaced using directive

    /// <summary>
    /// An arbitrary range of <see cref="Version"/>s, capable of matching any possible set of <see cref="Version"/>s.
    /// </summary>
    public partial class VersionRange : IEquatable<VersionRange>
    {
        private readonly Subrange[] subranges;
        private readonly VersionComparer? additionalComparer;

        /// <summary>
        /// Constructs a new <see cref="VersionRange"/> that corresponds to the text provided in <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The textual represenation of the <see cref="VersionRange"/> to create.</param>
        /// <seealso cref="TryParse(ref StringPart, out VersionRange)"/>
        /// <exception cref="ArgumentException">Thrown when<paramref name="text"/> is not a valid <see cref="VersionRange"/>.</exception>
        public VersionRange(StringPart text)
        {
            text = text.Trim();

            var errors = new ErrorState(text);
            if (!TryParseComponents(ref errors, ref text, true, out var ranges, out additionalComparer) || text.Length > 0)
                throw BuildError(ref errors, nameof(text));
            errors.Dispose();

            (ranges, additionalComparer) = FixupRangeList(ranges, additionalComparer);
            subranges = ranges;
        }

        private VersionRange(Subrange[] srs, VersionComparer? comparer)
        {
            (srs, comparer) = FixupRangeList(srs, comparer);

            subranges = srs;
            additionalComparer = comparer;
        }

        /// <summary>
        /// Creates a <see cref="VersionRange"/> which matches only the provided <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The <see cref="Version"/> to match.</param>
        /// <returns>A <see cref="VersionRange"/> matching only the provided <paramref name="version"/>.</returns>
        public static VersionRange ForVersion(Version version)
            => new(Array.Empty<Subrange>(), new VersionComparer(version, ComparisonType.ExactEqual));

        /// <summary>
        /// Computes the logical disjunction (or) of this <see cref="VersionRange"/> and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other <see cref="VersionRange"/> to compute the disjunction of.</param>
        /// <returns>The logical disjunction of <see langword="this"/> and <paramref name="other"/>.</returns>
        /// <seealso cref="operator |(VersionRange, VersionRange)"/>
        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
#if !NETSTANDARD2_0
        [SuppressMessage("Style", "IDE0056:Use index operator", Justification = "Standard 2.0 doesn't have System.Index")]
#endif
        public VersionRange Disjunction(VersionRange other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

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
            else
            {
                comparer = additionalComparer != null ? additionalComparer : other.additionalComparer;
            }

            var allSubranges = new Subrange[subranges.Length + other.subranges.Length + (subrange != null ? 1 : 0)];
            Array.Copy(subranges, allSubranges, subranges.Length);
            Array.Copy(other.subranges, 0, allSubranges, subranges.Length, other.subranges.Length);
            if (subrange != null)
                allSubranges[allSubranges.Length - 1] = subrange.Value;

            return new VersionRange(allSubranges, comparer);
        }


        /// <summary>
        /// Computes the logical disjunction (or) of the two arguments.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns>The logical disjunction of <paramref name="a"/> and <paramref name="b"/>.</returns>
        /// <seealso cref="Disjunction(VersionRange)"/>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "The named alternate is Disjunction().")]
        public static VersionRange operator |(VersionRange a, VersionRange b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            return a.Disjunction(b);
        }

        /// <summary>
        /// Computes the logical conjunction (and) of this <see cref="VersionRange"/> and <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The other <see cref="VersionRange"/> to compute the conjunction of.</param>
        /// <returns>The logical conjunction of <see langword="this"/> and <paramref name="other"/>.</returns>
        /// <seealso cref="operator &amp;(VersionRange, VersionRange)"/>
        public VersionRange Conjunction(VersionRange other)
        {
            // TODO: replace this implementation with one that doesn't allocate a load of ranges

            // the current implementation allocates 6-12 times (2-4 VersionRanges, 4-8 arrays):
            // - (potentially) allocates the inverse of `this`
            //   - +1 array during inversion
            //   - +1 array during range set fixup
            //   - +1 VersionRange object
            // - (potentially) allocates the inverse of `other`
            //   - same as above
            // - allocates for disjunction
            //   - +1 array during computation
            //   - +1 array during range set fixup
            //   - +1 VersionRange object
            // - allocates for final inversion
            //   - +1 array during inversion
            //   - +1 array during range set fixup
            //   - +1 VersionRange object

            return ~(~this | ~other);
        }


        /// <summary>
        /// Computes the logical conjunction (and) of the two arguments.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns>The logical conjunction of <paramref name="a"/> and <paramref name="b"/>.</returns>
        /// <seealso cref="Conjunction(VersionRange)"/>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "The named alternate is Conjunction().")]
        public static VersionRange operator &(VersionRange a, VersionRange b)
        {
            if (a is null) throw new ArgumentNullException(nameof(a));
            if (b is null) throw new ArgumentNullException(nameof(b));

            return a.Conjunction(b);
        }

        private VersionRange? _inverse;

        /// <summary>
        /// Gets the compliement of this <see cref="VersionRange"/>.
        /// </summary>
        /// <returns>The compliement of this <see cref="VersionRange"/>.</returns>
        /// <seealso cref="operator ~(VersionRange)"/>
        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
        [SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Don't need missing cases.")]
        public VersionRange Invert()
        {
            if (_inverse is null)
            {
                if (this == Everything)
                    _inverse = Nothing;
                else if (this == Nothing)
                    _inverse = Everything;
                else
                {
                    VersionComparer? invComparer = null;
                    if (additionalComparer != null)
                    {
                        var comparerResult = additionalComparer.Value.Invert(out var inverseComparer, out var inverseCompRange);
                        invComparer = comparerResult switch
                        {
                            CombineResult.OneComparer => inverseComparer,
                            _ => throw new InvalidOperationException(),
                        };
                    }

                    var invertedRanges = subranges.Select(r => r.Invert());
                    using var ab = new ArrayBuilder<Subrange>(subranges.Length + 2);

                    VersionComparer? lowerBound = null;
                    VersionComparer? upperBound = null;
                    foreach (var range in invertedRanges)
                    {
                        if (upperBound != null)
                        {
                            var conjResult = upperBound.Value.TryConjunction(range.LowerBound, out var comp, out var resRange);
                            switch (conjResult)
                            {
                                case CombineResult.Nothing: break;
                                case CombineResult.OneSubrange:
                                    ab.Add(resRange);
                                    break;

                                default: throw new InvalidOperationException();
                            }
                        }
                        else
                        {
                            lowerBound = range.IsInward ? range.UpperBound : range.LowerBound;
                            if (invComparer != null)
                            {
                                var conjResult = lowerBound.Value.TryConjunction(invComparer.Value, out var comp, out var resRange);
                                switch (conjResult)
                                {
                                    case CombineResult.OneComparer:
                                    case CombineResult.Nothing: break;
                                    case CombineResult.OneSubrange:
                                        ab.Add(resRange);
                                        lowerBound = null;
                                        invComparer = null;
                                        break;

                                    default: throw new InvalidOperationException();
                                }
                            }
                        }

                        upperBound = range.IsInward ? range.LowerBound : range.UpperBound;
                    }

                    if (invComparer != null && upperBound != null)
                    {
                        var conjResult = upperBound.Value.TryConjunction(invComparer.Value, out var comp, out var resRange);
                        switch (conjResult)
                        {
                            case CombineResult.OneComparer:
                            case CombineResult.Nothing: break;
                            case CombineResult.OneSubrange:
                                ab.Add(resRange);
                                upperBound = null;
                                break;

                            default: throw new InvalidOperationException();
                        }
                        invComparer = null;
                    }

                    if (lowerBound != null && upperBound != null)
                    {
                        ab.Add(lowerBound.Value.CompareTo < upperBound.Value.CompareTo
                            ? new Subrange(lowerBound.Value, upperBound.Value)
                            : new Subrange(upperBound.Value, lowerBound.Value));
                    }
                    else if (lowerBound != null)
                        invComparer = lowerBound;
                    else if (upperBound != null)
                        invComparer = upperBound;

                    _inverse = new VersionRange(ab.ToArray(), invComparer) { _inverse = this };
                }
            }

            return _inverse;
        }


        /// <summary>
        /// Computes the compliment of the argument.
        /// </summary>
        /// <param name="r">The <see cref="VersionRange"/> to compute the compliment of.</param>
        /// <returns>The compliment of <paramref name="r"/>.</returns>
        /// <seealso cref="Invert()"/>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "The named alternate is Invert().")]
        public static VersionRange operator ~(VersionRange r)
        {
            if (r is null) throw new ArgumentNullException(nameof(r));

            return r.Invert();
        }

        /// <summary>
        /// Determines whether or not a given <see cref="Version"/> matches this <see cref="VersionRange"/>.
        /// </summary>
        /// <param name="version">The <see cref="Version"/> to check.</param>
        /// <returns><see langword="true"/> if <paramref name="version"/> matches, <see langword="false"/> otherwise.</returns>
        public bool Matches(Version version)
        {
            if (version is null) throw new ArgumentNullException(nameof(version));

            if (additionalComparer?.Matches(version) ?? false)
                return true;
            foreach (var range in subranges)
            {
                if (range.Matches(version))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The <see cref="VersionRange"/> that matches all <see cref="Version"/>s.
        /// </summary>
        public static VersionRange Everything { get; } = new VersionRange(RangeParser.EverythingSubranges, null);

        /// <summary>
        /// The <see cref="VersionRange"/> that matches no <see cref="Version"/>s.
        /// </summary>
        public static VersionRange Nothing { get; } = new VersionRange(Array.Empty<Subrange>(), null);

        private static int CompareSubranges(Subrange a, Subrange b)
        {
            if (!a.IsInward && b.IsInward) return -1;
            if (a.IsInward && !b.IsInward) return 1;
            return a.LowerBound.CompareTo.CompareTo(b.LowerBound.CompareTo);
        }

        [SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Don't need missing cases.")]
        private static (Subrange[] Ranges, VersionComparer? Comparer) FixupRangeList(Subrange[] ranges, VersionComparer? comparer)
        {
            if (ranges.Length == 0 && comparer == null)
                return (ranges, comparer);

            Array.Sort(ranges, CompareSubranges);

            using var ab = new ArrayBuilder<Subrange>(ranges.Length);

            Subrange? nextToInsert = null;
            for (var i = 0; i < ranges.Length; i++)
            {
                var current = ranges[i];

                var combineWithComparerResult = CheckCombineWithComparer(ref current, ref comparer, out var returnResult);
                switch (combineWithComparerResult)
                {
                    case CombineWithComparerResult.NextIteration:
                        continue;
                    case CombineWithComparerResult.ReturnEarly:
                        ab.Clear();
                        return returnResult;

                    case CombineWithComparerResult.DoNothing: break;
                    default: throw new InvalidOperationException();
                }

                if (nextToInsert == null)
                {
                    nextToInsert = current;
                    continue;
                }

                var result = nextToInsert.Value.TryDisjunction(current, out var result1, out var result2);
                switch (result)
                {
                    case CombineResult.OneSubrange:
                        nextToInsert = result1;
                        break;

                    case CombineResult.TwoSubranges:
                        {
                            var aLowerB = result1.LowerBound.CompareTo < result2.LowerBound.CompareTo;
                            var addFirst = aLowerB ? result1 : result2;
                            nextToInsert = aLowerB ? result2 : result1;

                            combineWithComparerResult = CheckCombineWithComparer(ref addFirst, ref comparer, out returnResult);
                            switch (combineWithComparerResult)
                            {
                                case CombineWithComparerResult.NextIteration:
                                    continue;
                                case CombineWithComparerResult.ReturnEarly:
                                    ab.Clear();
                                    return returnResult;

                                case CombineWithComparerResult.DoNothing: break;
                                default: throw new InvalidOperationException();
                            }

                            ab.Add(addFirst);
                        }
                        break;

                    case CombineResult.Everything:
                        // if any combo is everything, we can skip all the ceremony and make our result everything
                        ab.Clear();
                        return (RangeParser.EverythingSubranges, null);

                    default: throw new InvalidOperationException();
                }
            }

            if (nextToInsert != null)
                ab.Add(nextToInsert.Value);

            return (ab.ToArray(), comparer);
        }

        private enum CombineWithComparerResult
        {
            DoNothing, NextIteration, ReturnEarly
        }

        private static CombineWithComparerResult CheckCombineWithComparer(ref Subrange current, ref VersionComparer? comparer, out (Subrange[], VersionComparer?) returnResult)
        {
            returnResult = default;

            if (comparer != null)
            {
                if (current.IsInward)
                {
                    if (current.Matches(comparer.Value))
                    {
                        if ((comparer.Value.Type & ComparisonType.Greater) != 0)
                        {
                            comparer = current.LowerBound;
                            return CombineWithComparerResult.NextIteration;
                        }
                        if ((comparer.Value.Type & ComparisonType.Less) != 0)
                        {
                            comparer = current.UpperBound;
                            return CombineWithComparerResult.NextIteration;
                        }
                        // if we reach here, then comparer is an ExactEqual, so we can just kill it
                        comparer = null;
                    }
                    else
                    {
                        if ((comparer.Value.Type & ComparisonType.Greater) != 0)
                        {
                            if (comparer.Value.Matches(current.LowerBound))
                                return CombineWithComparerResult.NextIteration; // we want to skip this current, because it already matches our comparer
                        }
                        if ((comparer.Value.Type & ComparisonType.Less) != 0)
                        {
                            if (comparer.Value.Matches(current.UpperBound))
                                return CombineWithComparerResult.NextIteration; // we want to skip this current, because it already matches our comparer
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
                            //ab.Clear();
                            returnResult = (RangeParser.EverythingSubranges, null);
                            return CombineWithComparerResult.ReturnEarly;
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
                            returnResult = (RangeParser.EverythingSubranges, null);
                            return CombineWithComparerResult.ReturnEarly;
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

            return CombineWithComparerResult.DoNothing;
        }

        /// <summary>
        /// Appends the string representation of this <see cref="VersionRange"/> to the provided <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
        /// <returns>The <see cref="StringBuilder"/> that was appended to.</returns>
        public StringBuilder ToString(StringBuilder sb)
        {
            if (sb is null) throw new ArgumentNullException(nameof(sb));

            // special case handling for the nothing range
            if (subranges.Length == 0 && additionalComparer is null)
            {
                return sb.Append('Z');
            }

            for (var i = 0; i < subranges.Length; i++)
            {
                _ = subranges[i].ToString(sb);
                if (i != subranges.Length - 1)
                    _ = sb.Append(" || ");
            }

            if (additionalComparer != null)
            {
                if (subranges.Length > 0)
                    _ = sb.Append(" || ");
                _ = additionalComparer.Value.ToString(sb);
            }

            return sb;
        }

        /// <summary>
        /// Gets the string representation of this <see cref="VersionRange"/>.
        /// </summary>
        /// <returns>The string representation of this <see cref="VersionRange"/>.</returns>
        public override string ToString()
            => ToString(new StringBuilder()).ToString();

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is VersionRange range && Equals(range);

        /// <summary>
        /// Determines whether this <see cref="VersionRange"/> is equivalent to another range.
        /// </summary>
        /// <param name="other">The <see cref="VersionRange"/> to compare to.</param>
        /// <returns><see langword="true"/> if they are equivalent, <see langword="false"/> otherwise.</returns>
        public bool Equals(VersionRange? other)
        {
            if (other is null) return false;
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

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(additionalComparer);
            foreach (var sr in subranges)
                hc.Add(sr);
            return hc.ToHashCode();
        }

        /// <summary>
        /// Compares two <see cref="VersionRange"/>s for equality.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns><see langword="true"/> if <paramref name="b"/> and <paramref name="b"/> are equivalent, <see langword="false"/> otherwise.</returns>
        public static bool operator ==(VersionRange? a, VersionRange? b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Determines if two <see cref="VersionRange"/>s are not equivalent.
        /// </summary>
        /// <param name="a">The first argument.</param>
        /// <param name="b">The second argument.</param>
        /// <returns><see langword="true"/> if <paramref name="b"/> and <paramref name="b"/> are not equivalent, <see langword="false"/> otherwise.</returns>
        public static bool operator !=(VersionRange? a, VersionRange? b) => !(a == b);

        #region Parsing methods
        /// <summary>
        /// Parses a string as a <see cref="VersionRange"/>.
        /// </summary>
        /// <remarks>
        /// <include file="docs.xml" path='csdocs/class[@name="VersionRange"]/syntax/*'/>
        /// </remarks>
        /// <param name="text">The string to parse.</param>
        /// <returns>The parsed <see cref="VersionRange"/>.</returns>
        /// <seealso cref="TryParse(StringPart, out VersionRange)"/>
        /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is not a valid <see cref="VersionRange"/>.</exception>
        public static VersionRange Parse(StringPart text)
        {
            var errors = new ErrorState(text); // we do want error info
            if (!TryParse(ref errors, text, out var range))
                throw BuildError(ref errors, nameof(text));
            errors.Dispose();
            return range;
        }

        /// <summary>
        /// Attempts to parse a whole string as a <see cref="VersionRange"/>.
        /// </summary>
        /// <remarks>
        /// <include file="docs.xml" path='csdocs/class[@name="VersionRange"]/syntax/*'/>
        /// </remarks>
        /// <param name="text">The string to try to parse.</param>
        /// <param name="range">The parsed <see cref="VersionRange"/>, if any.</param>
        /// <returns><see langword="true"/> if <paramref name="text"/> was successfully parsed, <see langword="false"/> otherwise.</returns>
        /// <seealso cref="TryParse(ref StringPart, out VersionRange)"/>
        public static bool TryParse(StringPart text, [MaybeNullWhen(false)] out VersionRange range)
        {
            text = text.Trim();
            var errors = new ErrorState();
            return TryParse(ref errors, ref text, true, out range) && text.Length == 0; // don't report errors
        }

        public static bool TryParse(ref ErrorState errors, StringPart text, [MaybeNullWhen(false)] out VersionRange range)
        {
            text = text.Trim();
            return TryParse(ref errors, ref text, true, out range) && text.Length == 0; // report errors
        }

        /// <summary>
        /// Attempts to parse a <see cref="VersionRange"/> from the start of the string.
        /// </summary>
        /// <remarks>
        /// <para>When this returns <see langword="true"/>, <paramref name="text"/> will begin immediately after the parsed <see cref="VersionRange"/>.
        /// When this returns <see langword="false"/>, <paramref name="text"/> will remain unchanged.</para>
        /// <include file="docs.xml" path='csdocs/class[@name="VersionRange"]/syntax/*'/>
        /// </remarks>
        /// <param name="text">The string to try to parse.</param>
        /// <param name="range">The parsed <see cref="VersionRange"/>, if any.</param>
        /// <returns><see langword="true"/> if <paramref name="text"/> was successfully parsed, <see langword="false"/> otherwise.</returns>
        [CLSCompliant(false)]
        public static bool TryParse(ref StringPart text, [MaybeNullWhen(false)] out VersionRange range)
        {
            var errors = new ErrorState();
            return TryParse(ref errors, ref text, false, out range); // don't do error reporting
        }

        [CLSCompliant(false)]
        public static bool TryParse(ref ErrorState errors, ref StringPart text, [MaybeNullWhen(false)] out VersionRange range)
            => TryParse(ref errors, ref text, false, out range);

        private static bool TryParse(ref ErrorState errors, ref StringPart text, bool checkLength, [MaybeNullWhen(false)] out VersionRange range)
        {
            range = null;

            if (!TryParseComponents(ref errors, ref text, checkLength, out var srs, out var compare))
                return false;

            // check for our everything range array as a minor optimization
            if (srs == RangeParser.EverythingSubranges)
            {
                range = Everything;
                return true;
            }
            // check for empty subranges list, signifying the nothing range
            if (srs.Length == 0 && compare is null)
            {
                range = Nothing;
                return true;
            }

            range = new VersionRange(srs, compare);
            return true;
        }

        private static bool TryParseComponents(ref ErrorState errors, ref StringPart text, bool checkLength,
            [MaybeNullWhen(false)] out Subrange[] sranges, out VersionComparer? comparer)
        {
            if (!RangeParser.TryParse(ref errors, ref text, out sranges, out comparer))
            {
                return false;
            }
            if (checkLength && text.Length > 0)
            {
                errors.Report(new(RangeParseAction.ExtraInput), text);
                return false;
            }

            return true;
        }
        #endregion

        private static Exception BuildError(ref ErrorState errors, string argumentName)
        {
            // TODO: implement
            errors.Dispose();
            return new ArgumentException(SR.Range_InputInvalid, argumentName);
        }
    }
}
