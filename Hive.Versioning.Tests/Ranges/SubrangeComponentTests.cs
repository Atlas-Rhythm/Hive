using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Hive.Versioning.VersionRange;

namespace Hive.Versioning.Tests.Ranges
{
    public class SubrangeComponentTests
    {
        private static VersionComparer ParseComparer(string input)
        {
            ReadOnlySpan<char> text = input;
            Assert.True(VersionComparer.TryParse(ref text, out var comparer));
            return comparer;
        }

        private static Subrange CreateSubrange(string lower, string upper)
            => new Subrange(ParseComparer(lower), ParseComparer(upper));

        [Theory]
        [InlineData(">=1.0.0", "<2.0.0", true)]
        [InlineData(">1.0.0", "<=2.0.0", true)]
        [InlineData("<=1.0.0", ">2.0.0", false)]
        [InlineData("<1.0.0", ">=2.0.0", false)]
        public void TestInwardness(string lowerS, string upperS, bool inward)
        {
            var range = CreateSubrange(lowerS, upperS);
            Assert.Equal(inward, range.IsInward);
        }

        [Theory]
        [InlineData("=1.0.0", ">1.0.0")]
        [InlineData(">=1.0.0", "=2.0.0")]
        [InlineData("<2.0.0", ">=1.0.0")]
        public void InvalidRanges(string lowerS, string upperS)
        {
            Assert.Throws<ArgumentException>(() => CreateSubrange(lowerS, upperS));
        }

        [Theory]
        [MemberData(nameof(MatchesData))]
        public void TestMatches(string lowerS, string upperS, string versionS, bool matches)
        {
            var sr = CreateSubrange(lowerS, upperS);
            var version = new Version(versionS);

            Assert.Equal(matches, sr.Matches(version));
        }

        [Theory]
        [MemberData(nameof(MatchesData))]
        public void TestInvert(string lowerS, string upperS, string versionS, bool matches)
        {
            var sr = CreateSubrange(lowerS, upperS);
            var version = new Version(versionS);

            sr = sr.Invert();

            Assert.Equal(!matches, sr.Matches(version));
        }

        public static readonly object[][] MatchesData = new[]
        {
            new object[] { ">=1.0.0", "<2.0.0", "1.0.0-pre.1", false },
            new object[] { ">=1.0.0", "<2.0.0", "1.0.0", true },
            new object[] { ">=1.0.0", "<2.0.0", "1.0.1", true },
            new object[] { ">=1.0.0", "<2.0.0", "1.1.0", true },
            new object[] { ">=1.0.0", "<2.0.0", "2.0.0", false },
            new object[] { "<1.0.0", ">=2.0.0", "1.0.0-pre.1", true},
            new object[] { "<1.0.0", ">=2.0.0", "1.0.0", false },
            new object[] { "<1.0.0", ">=2.0.0", "1.0.1", false },
            new object[] { "<1.0.0", ">=2.0.0", "1.1.0", false },
            new object[] { "<1.0.0", ">=2.0.0", "2.0.0", true },
        };

        [Theory]
        [InlineData(">=1.0.0", "<2.0.0", ">=1.0.1", "<2.0.0-pre.1", true, ">=1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">=1.0.1", "<2.0.0", true, ">=1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">=1.0.0", "<2.0.0-pre.1", true, ">=1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">1.0.0", "<2.0.0-pre.1", true, ">=1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">1.0.0", "<2.0.0", true, ">=1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">1.0.0", "<=2.0.0", true, ">=1.0.0", "<=2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">=1.0.1", "<2.0.1", true, ">=1.0.0", "<2.0.1")]
        [InlineData(">=1.0.0", "<2.0.0", ">=1.0.0-pre.1", "<2.0.0-pre.1", true, ">=1.0.0-pre.1", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">1.0.0-pre.1", "<2.0.0-pre.1", true, ">1.0.0-pre.1", "<2.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">=2.0.0", "<3.0.0", true, ">=1.0.0", "<3.0.0")]
        [InlineData(">=2.0.0", "<3.0.0", ">=1.0.0", "<2.0.0", true, ">=1.0.0", "<3.0.0")]
        [InlineData(">=1.0.0", "<2.0.0", ">2.0.0", "<3.0.0", false, "", "")]
        public void TestTryCombineWith(string lowerA, string upperA, string lowerB, string upperB, bool succeeds, string lowerR, string upperR)
        {
            var ra = CreateSubrange(lowerA, upperA);
            var rb = CreateSubrange(lowerB, upperB);

            Assert.Equal(succeeds, ra.TryCombineWith(rb, out var result));
            if (succeeds)
            {
                var expect = CreateSubrange(lowerR, upperR);
                Assert.Equal(expect.IsInward, result.IsInward);
                Assert.Equal(expect.LowerBound.Type, result.LowerBound.Type);
                Assert.Equal(expect.LowerBound.CompareTo, result.LowerBound.CompareTo);
                Assert.Equal(expect.UpperBound.Type, result.UpperBound.Type);
                Assert.Equal(expect.UpperBound.CompareTo, result.UpperBound.CompareTo);
            }
        }
    }
}
