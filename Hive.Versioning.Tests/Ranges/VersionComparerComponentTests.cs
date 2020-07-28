using Hive.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Hive.Versioning.VersionRange;

namespace Hive.Versioning.Tests.Ranges
{
    public class VersionComparerComponentTests
    {
        private static VersionComparer CreateComparer(string vers, string types)
        {
            var type = Enum.Parse<ComparisonType>(types);
            return new VersionComparer(new Version(vers), type);
        }

        [Theory]
        [MemberData(nameof(ComparerMatchingValues))]
        public void TestComparerMatching(string comparerBase, string typeString, string compareToStr, bool matches)
        {
            var comparer = CreateComparer(comparerBase, typeString);

            var compareTo = new Version(compareToStr);

            Assert.Equal(matches, comparer.Matches(compareTo));
        }

        [Theory]
        [MemberData(nameof(ComparerMatchingValues))]
        public void TestComparerInvert(string comparerBase, string typeString, string compareToStr, bool matches)
        {
            var comparer = CreateComparer(comparerBase, typeString);

            var compareTo = new Version(compareToStr);

            var inversionResult = comparer.Invert(out var newComparer, out var newRange);

            Assert.NotEqual(ComparerCombineResult.Invalid, inversionResult);
            Assert.Equal(comparer.Type == ComparisonType.ExactEqual
                ? ComparerCombineResult.Subrange
                : ComparerCombineResult.SingleComparer,
                inversionResult);

            if (inversionResult == VersionRange.ComparerCombineResult.SingleComparer)
                Assert.Equal(!matches, newComparer.Matches(compareTo));
            else
                Assert.Equal(!matches, newRange.Matches(compareTo));
        }

        public static readonly object[][] ComparerMatchingValues = new[]
        {
            new object[] { "1.0.0", nameof(ComparisonType.GreaterEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(ComparisonType.GreaterEqual), "1.0.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.GreaterEqual), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.GreaterEqual), "0.1.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.Greater), "1.0.0", false },
            new object[] { "1.0.0", nameof(ComparisonType.Greater), "1.0.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.Greater), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.Greater), "0.1.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.LessEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(ComparisonType.LessEqual), "1.0.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.LessEqual), "1.0.0-pre.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.LessEqual), "0.1.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.Less), "1.0.0", false },
            new object[] { "1.0.0", nameof(ComparisonType.Less), "1.0.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.Less), "1.0.0-pre.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.Less), "0.1.1", true },
            new object[] { "1.0.0", nameof(ComparisonType.ExactEqual), "1.0.0", true },
            new object[] { "1.0.0", nameof(ComparisonType.ExactEqual), "1.0.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.ExactEqual), "1.0.0-pre.1", false },
            new object[] { "1.0.0", nameof(ComparisonType.ExactEqual), "0.1.1", false },
        };

        [Theory]
        [InlineData("1.0.0", nameof(ComparisonType.Greater), "1.0.1", nameof(ComparisonType.GreaterEqual), "1.0.0", nameof(ComparisonType.Greater))]
        [InlineData("1.0.1", nameof(ComparisonType.Greater), "1.0.0", nameof(ComparisonType.GreaterEqual), "1.0.0", nameof(ComparisonType.GreaterEqual))]
        [InlineData("1.0.0", nameof(ComparisonType.GreaterEqual), "1.0.1", nameof(ComparisonType.Greater), "1.0.0", nameof(ComparisonType.GreaterEqual))]
        [InlineData("1.0.1", nameof(ComparisonType.GreaterEqual), "1.0.0", nameof(ComparisonType.Greater), "1.0.0", nameof(ComparisonType.Greater))]
        [InlineData("1.0.0", nameof(ComparisonType.Less), "1.0.1", nameof(ComparisonType.LessEqual), "1.0.1", nameof(ComparisonType.LessEqual))]
        [InlineData("1.0.1", nameof(ComparisonType.Less), "1.0.0", nameof(ComparisonType.LessEqual), "1.0.1", nameof(ComparisonType.Less))]
        [InlineData("1.0.0", nameof(ComparisonType.LessEqual), "1.0.1", nameof(ComparisonType.Less), "1.0.1", nameof(ComparisonType.Less))]
        [InlineData("1.0.1", nameof(ComparisonType.LessEqual), "1.0.0", nameof(ComparisonType.Less), "1.0.1", nameof(ComparisonType.LessEqual))]
        [InlineData("1.0.0", nameof(ComparisonType.Greater), "1.0.1", nameof(ComparisonType.ExactEqual), "1.0.0", nameof(ComparisonType.Greater))]
        [InlineData("1.0.0", nameof(ComparisonType.Less), "0.1.1", nameof(ComparisonType.ExactEqual), "1.0.0", nameof(ComparisonType.Less))]
        [InlineData("1.0.1", nameof(ComparisonType.ExactEqual), "1.0.1", nameof(ComparisonType.ExactEqual), "1.0.1", nameof(ComparisonType.ExactEqual))]
        public void TextComparerCombineToComparer(string verAs, string typeAs, string verBs, string typeBs, string verRs, string typeRs)
        {
            var a = CreateComparer(verAs, typeAs);
            var b = CreateComparer(verBs, typeBs);

            var expect = CreateComparer(verRs, typeRs);

            var result = a.CombineWith(b, out var newComparer, out _);

            Assert.Equal(ComparerCombineResult.SingleComparer, result);
            Assert.Equal(expect.Type, newComparer.Type);
            Assert.Equal(expect.CompareTo, newComparer.CompareTo);
        }

    }
}
