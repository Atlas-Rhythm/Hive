using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Versioning.Tests.Ranges
{
    public class RangeTests
    {

        [Theory]
        [InlineData(">=1.0.0", true)]
        [InlineData(">1.0.0", true)]
        [InlineData("<2.0.0", true)]
        [InlineData("<=2.0.0", true)]
        [InlineData(">=1.0.0 <2.0.0", true)]
        [InlineData(">1.0.0 <=2.0.0", true)]
        [InlineData(">=1.0.0 || <2.0.0", true)]
        [InlineData(">1.0.0  || <=2.0.0", true)]
        [InlineData("^0.1.5", true)]
        [InlineData("=1.0.0 <=2.0.0", false)]
        public void TestParserValidation(string text, bool valid)
        {
            Assert.Equal(valid, VersionRange.TryParse(text, out _));
        }

        [Theory]
        [InlineData(">=1.0.0")]
        [InlineData(">1.0.0")]
        [InlineData("<2.0.0")]
        [InlineData("<=2.0.0")]
        [InlineData(">=1.0.0 <2.0.0")]
        [InlineData(">1.0.0 <=2.0.0")]
        [InlineData(">=1.0.0 || <2.0.0")]
        [InlineData(">1.0.0  || <=2.0.0")]
        [InlineData("^0.1.5")]
        public void TestStringificationRoundTrip(string startText)
        {
            Assert.True(VersionRange.TryParse(startText, out var range));
            var startString = range!.ToString();
            Assert.True(VersionRange.TryParse(startString, out var range2));
            var endString = range2!.ToString();
            Assert.Equal(startString, endString);
            Assert.Equal(range, range2);
        }

        [Theory]
        [InlineData(">=1.0.0 <2.0.0", ">=1.0.0 <2.0.0", true)]
        [InlineData("^1.0.0", ">=1.0.0 <2.0.0", true)]
        [InlineData("^1.0.0 || ^2.0.0", ">=1.0.0 <3.0.0", true)]
        [InlineData("^1.0.0 || ^2.0.0 || >4.0.0", ">4.0.0 || >=1.0.0 <3.0.0", true)]
        [InlineData(">4.0.0 || <5.0.0", "<1.0.0 >=1.0.0", true)]
        [InlineData("<1.0.0 || ^2.0.0 || >4.0.0", "^2.0.0 || <1.0.0 >4.0.0", true)]
        public void TestEquality(string Sa, string Sb, bool equal)
        {
            Assert.True(VersionRange.TryParse(Sa, out var a));
            Assert.True(VersionRange.TryParse(Sb, out var b));
            Assert.Equal(equal, a!.Equals(b!));
            Assert.Equal(equal, b!.Equals(a!));
        }

        [Theory]
        [InlineData("^1.0.0", "^0.1.5", "^0.1.5 || ^1.0.0")]
        [InlineData("^1.0.0 || >=3.0.0", "^0.1.5 || <2.1.5", "^0.1.5 || ^1.0.0 || <2.1.5 >=3.0.0")]
        [InlineData("^1.0.0 || >=3.0.0", "^0.1.5 || <0.1.0", "^0.1.5 || ^1.0.0 || <0.1.0 >=3.0.0")]
        public void TestDisjunction(string Sa, string Sb, string Sexpect)
        {
            Assert.True(VersionRange.TryParse(Sa, out var a));
            Assert.True(VersionRange.TryParse(Sb, out var b));
            Assert.True(VersionRange.TryParse(Sexpect, out var expect));

            var explDisjunct = a!.Disjunction(b!);
            var implDisjunct = a! | b!;
            Assert.Equal(expect, explDisjunct);
            Assert.Equal(expect, implDisjunct);
        }
    }
}
