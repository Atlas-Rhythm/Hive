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
        [InlineData("0.0.4")]
        [InlineData("1.2.3")]
        [InlineData("1.1.2-prerelease+meta")]
        [InlineData("1.1.2+meta")]
        [InlineData("1.1.2+meta-valid")]
        [InlineData("1.0.0-alpha")]
        [InlineData("1.0.0-beta")]
        [InlineData("1.0.0-alpha.beta")]
        [InlineData("1.0.0-alpha.beta.1")]
        [InlineData("1.0.0-alpha.1")]
        [InlineData("1.0.0-alpha0.valid")]
        [InlineData("1.0.0-alpha.0valid")]
        [InlineData("1.0.0-alpha-a.b-c-somethinglong+build.1-aef.1-its-okay")]
        [InlineData("1.0.0-rc.1+build.1")]
        [InlineData("1.0.0-rc.1+build.123")]
        [InlineData("1.2.3-beta")]
        [InlineData("10.2.3-DEV-SNAPSHOT")]
        [InlineData("1.2.3-SNAPSHOT-123")]
        [InlineData("1.0.0")]
        [InlineData("2.0.0")]
        [InlineData("1.1.7")]
        [InlineData("2.0.0+build.1848")]
        [InlineData("2.0.1-alpha.1227")]
        [InlineData("1.0.0-alpha+beta")]
        [InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12+788")]
        [InlineData("1.2.3----R-S.12.9.1--.12+meta")]
        [InlineData("1.2.3----RC-SNAPSHOT.12.9.1--.12")]
        [InlineData("1.0.0+0.build.1-rc.10000aaa-kk-0.1")]
        //[InlineData("99999999999999999999999.999999999999999999.99999999999999999")]
        [InlineData("1.0.0-0A.is.legal")]
        public void SemverValid(string text)
        {
            Assert.True(Version.TryParse(text, out var ver));
            _ = ver;
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1.2")]
        [InlineData("1.2.3-0123")]
        [InlineData("1.2.3-0123.0123")]
        [InlineData("1.1.2+.123")]
        [InlineData("+invalid")]
        [InlineData("-invalid")]
        [InlineData("-invalid+invalid")]
        [InlineData("-invalid.01")]
        [InlineData("alpha")]
        [InlineData("alpha.beta")]
        [InlineData("alpha.beta.1")]
        [InlineData("alpha.1")]
        [InlineData("alpha+beta")]
        [InlineData("alpha_beta")]
        [InlineData("alpha.")]
        [InlineData("alpha..")]
        [InlineData("beta")]
        [InlineData("1.0.0-alpha_beta")]
        [InlineData("-alpha.")]
        [InlineData("1.0.0-alpha..")]
        [InlineData("1.0.0-alpha..1")]
        [InlineData("1.0.0-alpha...1")]
        [InlineData("1.0.0-alpha....1")]
        [InlineData("1.0.0-alpha.....1")]
        [InlineData("1.0.0-alpha......1")]
        [InlineData("1.0.0-alpha.......1")]
        [InlineData("01.1.1")]
        [InlineData("1.01.1")]
        [InlineData("1.1.01")]
        [InlineData("1.2.3.DEV")]
        [InlineData("1.2-SNAPSHOT")]
        [InlineData("1.2.31.2.3----RC-SNAPSHOT.12.09.1--..12+788")]
        [InlineData("1.2-RC-SNAPSHOT")]
        [InlineData("-1.0.3-gamma+b7718")]
        [InlineData("+justmeta")]
        [InlineData("9.8.7+meta+meta")]
        [InlineData("9.8.7-whatever+meta+meta")]
        [InlineData("99999999999999999999999.999999999999999999.99999999999999999----RC-SNAPSHOT.12.09.1--------------------------------..12")]
        public void SemverInvalid(string text)
        {
            Assert.False(Version.TryParse(text, out var ver));
            _ = ver;
        }
    }
}
