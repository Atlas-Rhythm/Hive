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
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.0", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "1.0.0-pre.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.GreaterEqual), "0.1.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.0", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Greater), "1.0.0-pre.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Greater), "0.1.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.0", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "1.0.0-pre.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.LessEqual), "0.1.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.0", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Less), "1.0.0-pre.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.Less), "0.1.1", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.0", true)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "1.0.0-pre.1", false)]
        [InlineData("1.0.0", nameof(VersionRange.ComparisonType.ExactEqual), "0.1.1", false)]
        public void TestComparerMatching(string comparerBase, string typeString, string compareToStr, bool matches)
        {
            var comparer = new VersionRange.VersionComparer(new Version(comparerBase), Enum.Parse<VersionRange.ComparisonType>(typeString));

            var compareTo = new Version(compareToStr);

            Assert.Equal(matches, comparer.Matches(compareTo));
        }

    }
}
