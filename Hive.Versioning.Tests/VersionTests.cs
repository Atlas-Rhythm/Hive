using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hive.Versioning.Tests
{
    public class VersionTests
    {
        [Theory]
        [InlineData("0.0.4", true, 0, 5)]
        [InlineData("1.2.3", true, 0, 5)]
        [InlineData("1.1.2-prerelease+meta", true, 0, 21)]
        [InlineData("1.1.2+meta", true, 0, 10)]
        [InlineData("1.1.2+meta-valid", true, 0, 16)]
        [InlineData("1.0.0-alpha", true, 0, 11)]
        [InlineData("1.0.0-beta", true, 0, 10)]
        [InlineData("1.0.0-alpha.beta", true, 0, 16)]
        [InlineData("1.0.0-alpha.beta.1", true, 0, 18)]
        [InlineData("1.0.0-alpha.1", true, 0, 13)]
        [InlineData("1.0.0-alpha0.valid", true, 0, 18)]
        [InlineData("1.0.0-alpha.0valid", true, 0, 18)]
        [InlineData("1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay", true, 0, 54)]
        [InlineData("1.0.0-rc.1+build.1", true, 0, 18)]
        [InlineData("1.0.0-rc.1+build.123", true, 0, 20)]
        [InlineData("1.2.3-beta", true, 0, 10)]
        [InlineData("10.2.3-DEV-SNAPSHOT", true, 0, 19)]
        [InlineData("1.2.3-SNAPSHOT-123", true, 0, 18)]
        [InlineData("1.0.0", true, 0, 5)]
        [InlineData("2.0.0", true, 0, 5)]
        [InlineData("1.1.7", true, 0, 5)]
        [InlineData("2.0.0+build.1848", true, 0, 16)]
        [InlineData("2.0.1-alpha.1227", true, 0, 16)]
        [InlineData("1.0.0-alpha+beta", true, 0, 16)]
        [InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12+788", true, 0, 36)]
        [InlineData("1.2.3----R-S.12.9.1--.12+meta", true, 0, 29)]
        [InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12", true, 0, 32)]
        [InlineData("1.0.0+0.build.1-rc.10000aaa-kk-0.1", true, 0, 34)]
        //[InlineData("99999999999999999999999.999999999999999999.99999999999999999", true, 0, 60)]
        [InlineData("1.0.0-0A.is.legal", true, 0, 17)]
        public void SemverValid(string text, bool matches, int __, int ___)
        {
            Assert.True(Version.TryParse(text, out var ver));
            _ = ver;
        }

        [Theory]
        [InlineData("1", false, 0, 0)]
        [InlineData("1.2", false, 0, 0)]
        [InlineData("1.2.3-0123", false, 0, 0)]
        [InlineData("1.2.3-0123.0123", false, 0, 0)]
        [InlineData("1.1.2+.123", false, 0, 0)]
        [InlineData("+invalid", false, 0, 0)]
        [InlineData("-invalid", false, 0, 0)]
        [InlineData("-invalid+invalid", false, 0, 0)]
        [InlineData("-invalid.01", false, 0, 0)]
        [InlineData("alpha", false, 0, 0)]
        [InlineData("alpha.beta", false, 0, 0)]
        [InlineData("alpha.beta.1", false, 0, 0)]
        [InlineData("alpha.1", false, 0, 0)]
        [InlineData("alpha+beta", false, 0, 0)]
        [InlineData("alpha_beta", false, 0, 0)]
        [InlineData("alpha.", false, 0, 0)]
        [InlineData("alpha..", false, 0, 0)]
        [InlineData("beta", false, 0, 0)]
        [InlineData("1.0.0-alpha_beta", false, 0, 0)]
        [InlineData("-alpha.", false, 0, 0)]
        [InlineData("1.0.0-alpha..", false, 0, 0)]
        [InlineData("1.0.0-alpha..1", false, 0, 0)]
        [InlineData("1.0.0-alpha...1", false, 0, 0)]
        [InlineData("1.0.0-alpha....1", false, 0, 0)]
        [InlineData("1.0.0-alpha.....1", false, 0, 0)]
        [InlineData("1.0.0-alpha......1", false, 0, 0)]
        [InlineData("1.0.0-alpha.......1", false, 0, 0)]
        [InlineData("01.1.1", false, 0, 0)]
        [InlineData("1.01.1", false, 0, 0)]
        [InlineData("1.1.01", false, 0, 0)]
        [InlineData("1.2.3.DEV", false, 0, 0)]
        [InlineData("1.2-SNAPSHOT", false, 0, 0)]
        [InlineData("1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788", false, 0, 0)]
        [InlineData("1.2-RC-SNAPSHOT", false, 0, 0)]
        [InlineData("-1.0.3-gamma+b7718", false, 0, 0)]
        [InlineData("+justmeta", false, 0, 0)]
        [InlineData("9.8.7+meta+meta", false, 0, 0)]
        [InlineData("9.8.7-whatever+meta+meta", false, 0, 0)]
        [InlineData("99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12", false, 0, 0)]
        public void SemverInvalid(string text, bool valid, int __, int ___)
        {
            Assert.False(Version.TryParse(text, out var ver));
            _ = ver;
        }
    }
}
