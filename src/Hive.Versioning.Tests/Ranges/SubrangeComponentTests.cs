using System;
using Xunit;
using Hive.Versioning.Parsing;
using static Hive.Versioning.VersionRange;

#if !NETCOREAPP3_1
using StringPart = System.ReadOnlySpan<char>;
#else
using StringPart = Hive.Utilities.StringView;
#endif


namespace Hive.Versioning.Tests.Ranges
{
    public class SubrangeComponentTests
    {
        private static VersionComparer ParseComparer(string input)
        {
            StringPart text = input;
            var errors = new ParserErrorState<AnyParseAction>();
            Assert.True(RangeParser.TryParseComparer(ref errors, ref text, out var comparer));
            return comparer;
        }

        private static Subrange ParseSubrange(string input, bool valid = true)
        {
            StringPart text = input;
            var errors = new ParserErrorState<AnyParseAction>();
            Assert.Equal(valid, RangeParser.TryReadComponent(ref errors, ref text, true, out var range, out _)
                && range is not null);
            return range ?? default;
        }

        private static Subrange CreateSubrange(string lower, string upper)
            => new(ParseComparer(lower), ParseComparer(upper));

        [Theory]
        [InlineData(">1.0.0 <2.0.0", true, ">1.0.0", "<2.0.0")]
        [InlineData(">=1.0.0 <2.0.0", true, ">=1.0.0", "<2.0.0")]
        [InlineData("^1.0.0", true, ">=1.0.0", "~<2.0.0")]
        [InlineData("^1.0.1", true, ">=1.0.1", "~<2.0.0")]
        [InlineData("^1.1.0", true, ">=1.1.0", "~<2.0.0")]
        [InlineData("^0.1.0", true, ">=0.1.0", "~<0.2.0")]
        [InlineData("^0.1.5", true, ">=0.1.5", "~<0.2.0")]
        [InlineData("^0.0.1", true, ">=0.0.1", "~<0.0.2")]
        [InlineData("<2.0.0 >1.0.0", false, ">1.0.0", "<2.0.0")]
        [InlineData("<2.0.0 >=1.0.0", false, ">=1.0.0", "<2.0.0")]
        public void TestParse(string text, bool valid, string lowerS, string upperS)
        {
            var actual = ParseSubrange(text, valid);
            if (valid)
            {
                var expect = CreateSubrange(lowerS, upperS);
                CheckEqual(expect, actual);
            }
        }

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
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.1 <2.0.0-pre.1", nameof(CombineResult.OneSubrange), ">=1.0.1 <2.0.0-pre.1", null)]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.1 <2.0.0", nameof(CombineResult.OneSubrange), ">=1.0.1 <2.0.0", null)]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.0 <2.0.0", nameof(CombineResult.OneSubrange), ">=1.0.0 <2.0.0", null)]
        [InlineData(">=1.0.0 <2.0.0", ">1.0.0 <2.0.0", nameof(CombineResult.OneSubrange), ">1.0.0 <2.0.0", null)]
        [InlineData("<1.0.0 >=2.0.0", ">=1.0.0 <=2.0.0", nameof(CombineResult.OneSubrange), ">=2.0.0 <=2.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", ">1.0.0 <2.0.0", nameof(CombineResult.Nothing), null, null)]
        [InlineData("<1.0.0 >2.0.0", ">0.1.0 <2.0.0", nameof(CombineResult.OneSubrange), ">0.1.0 <1.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", ">1.0.0 <3.0.0", nameof(CombineResult.OneSubrange), ">2.0.0 <3.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", ">0.1.0 <3.0.0", nameof(CombineResult.TwoSubranges), ">0.1.0 <1.0.0", ">2.0.0 <3.0.0")]
        [InlineData("<1.0.0 >2.0.0", "<1.0.0 >2.0.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", "<1.1.0 >2.1.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.1.0", null)]
        [InlineData("<1.1.0 >2.1.0", "<1.0.0 >2.0.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.1.0", null)]
        [InlineData("<1.0.0 >2.0.0", "<3.0.0 >4.0.0", nameof(CombineResult.TwoSubranges), "<1.0.0 >4.0.0", ">2.0.0 <3.0.0")]
        public void TestTryConjunction(string Sa, string Sb, string SresultType, string? Sresult1, string? Sresult2)
        {
            var a = ParseSubrange(Sa);
            var b = ParseSubrange(Sb);
            var expectResult = Enum.Parse<CombineResult>(SresultType);
            var expect1 = Sresult1 == null ? null : new Subrange?(ParseSubrange(Sresult1));
            var expect2 = Sresult2 == null ? null : new Subrange?(ParseSubrange(Sresult2));

            var result = a.TryConjunction(b, out var result1, out var result2);
            Assert.Equal(expectResult, result);
            if (expect1 != null)
                CheckEqual(expect1.Value, result1);
            if (expect2 != null)
                CheckEqual(expect2.Value, result2);
        }

        [Theory]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.1 <2.0.0-pre.1", nameof(CombineResult.OneSubrange), ">=1.0.0 <2.0.0", null)]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.1 <2.0.0", nameof(CombineResult.OneSubrange), ">=1.0.0 <2.0.0", null)]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.0 <2.0.0", nameof(CombineResult.OneSubrange), ">=1.0.0 <2.0.0", null)]
        [InlineData(">=1.0.0 <2.0.0", ">1.0.0 <2.0.0", nameof(CombineResult.OneSubrange), ">=1.0.0 <2.0.0", null)]
        [InlineData("<1.0.0 >=2.0.0", ">=1.0.0 <=2.0.0", nameof(CombineResult.Everything), null, null)]
        [InlineData("<1.0.0 >2.0.0", ">1.0.0 <2.0.0", nameof(CombineResult.TwoSubranges), ">1.0.0 <2.0.0", "<1.0.0 >2.0.0")]
        [InlineData("<1.0.0 >2.0.0", ">0.1.0 <2.0.0", nameof(CombineResult.OneSubrange), "<2.0.0 >2.0.0", null)]
        [InlineData("^1.0.0", "<2.1.5 >=3.0.0", nameof(CombineResult.OneSubrange), "<2.1.5 >=3.0.0", null)]
        /*[InlineData("<1.0.0 >2.0.0", ">1.0.0 <3.0.0", nameof(CombineResult.OneSubrange), ">2.0.0 <3.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", ">0.1.0 <3.0.0", nameof(CombineResult.TwoSubranges), ">0.1.0 <1.0.0", ">2.0.0 <3.0.0")]
        [InlineData("<1.0.0 >2.0.0", "<1.0.0 >2.0.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.0.0", null)]
        [InlineData("<1.0.0 >2.0.0", "<1.1.0 >2.1.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.1.0", null)]
        [InlineData("<1.1.0 >2.1.0", "<1.0.0 >2.0.0", nameof(CombineResult.OneSubrange), "<1.0.0 >2.1.0", null)]
        [InlineData("<1.0.0 >2.0.0", "<3.0.0 >4.0.0", nameof(CombineResult.TwoSubranges), "<1.0.0 >4.0.0", ">2.0.0 <3.0.0")]*/
        public void TestTryDisjunction(string Sa, string Sb, string SresultType, string? Sresult1, string? Sresult2)
        {
            var a = ParseSubrange(Sa);
            var b = ParseSubrange(Sb);
            var expectResult = Enum.Parse<CombineResult>(SresultType);
            var expect1 = Sresult1 == null ? null : new Subrange?(ParseSubrange(Sresult1));
            var expect2 = Sresult2 == null ? null : new Subrange?(ParseSubrange(Sresult2));

            var result = a.TryDisjunction(b, out var result1, out var result2);
            Assert.Equal(expectResult, result);
            if (expect1 != null)
                CheckEqual(expect1.Value, result1);
            if (expect2 != null)
                CheckEqual(expect2.Value, result2);
        }

        private static void CheckEqual(in Subrange expect, in Subrange result)
        {
            Assert.Equal(expect.IsInward, result.IsInward);
            Assert.Equal(expect.LowerBound.Type, result.LowerBound.Type);
            Assert.Equal(expect.LowerBound.CompareTo, result.LowerBound.CompareTo);
            Assert.Equal(expect.UpperBound.Type, result.UpperBound.Type);
            Assert.Equal(expect.UpperBound.CompareTo, result.UpperBound.CompareTo);
        }
    }
}
