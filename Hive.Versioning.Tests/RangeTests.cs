using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Versioning.Tests
{
    public class RangeTests
    {
        [Theory]
        [MemberData(nameof(ComparerMatchingValues))]
        public void TestComparerMatching(string comparerBase, string typeString, string compareToStr, bool matches)
        {
            var comparer = new VersionRange.VersionComparer(new Version(comparerBase), Enum.Parse<VersionRange.ComparisonType>(typeString));

            var compareTo = new Version(compareToStr);

            Assert.Equal(matches, comparer.Matches(compareTo));
        }

        [Theory]
        [MemberData(nameof(ComparerMatchingValues))]
        public void TestComparerInvert(string comparerBase, string typeString, string compareToStr, bool matches)
        {
            var type = Enum.Parse<VersionRange.ComparisonType>(typeString);
            var comparer = new VersionRange.VersionComparer(new Version(comparerBase), type);

            var compareTo = new Version(compareToStr);

            var inversionResult = comparer.Invert(out var newComparer, out var newRange);

            Assert.NotEqual(VersionRange.ComparerCombineResult.Invalid, inversionResult);
            Assert.Equal(type == VersionRange.ComparisonType.ExactEqual
                ? VersionRange.ComparerCombineResult.Subrange
                : VersionRange.ComparerCombineResult.SingleComparer,
                inversionResult);

            if (inversionResult == VersionRange.ComparerCombineResult.SingleComparer)
                Assert.Equal(!matches, newComparer.Matches(compareTo));
            else
                Assert.Equal(!matches, newRange.Matches(compareTo));
        }

        public static object[][] ComparerMatchingValues = new[]
        {
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "0.1.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.0", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Greater), "0.1.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.0-pre.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "0.1.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.0", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.0-pre.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.Less), "0.1.1", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "0.1.1", false },
        };

    }
}
