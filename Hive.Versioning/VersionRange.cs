using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hive.Versioning.ParseHelpers;

namespace Hive.Versioning
{
    public partial class VersionRange
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
            subranges = srs;
            additionalComparer = comparer;
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
                        var res = comparer.Value.TryConjunction(compare.Value, out var newComparer, out var sr);
                        switch (res)
                        {
                            case CombineResult.OneComparer:
                                comparer = newComparer;
                                break;
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
