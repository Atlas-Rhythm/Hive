﻿using System.Linq;
using System.Text.RegularExpressions;
using Hive.Versioning.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Versioning.Tests
{
    public class VersionTestFixture
    {
        public Regex SemVerRegex { get; } = new Regex(
            @"^(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<prerelease>(?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public void Validate(bool matches, string text, Version? ver)
        {
            var match = SemVerRegex.Match(text);
            Assert.Equal(matches, match.Success);

            if (matches)
            {
                Assert.NotNull(ver);

                Assert.Equal(match.Groups["major"].Value, ver!.Major.ToString());
                Assert.Equal(match.Groups["minor"].Value, ver!.Minor.ToString());
                Assert.Equal(match.Groups["patch"].Value, ver!.Patch.ToString());

                var pre = match.Groups["prerelease"];
                Assert.Equal(pre.Success, ver!.PreReleaseIds.Any());
                if (pre.Success)
                    Assert.Equal(pre.Value, string.Join(".", ver!.PreReleaseIds));

                var build = match.Groups["buildmetadata"];
                Assert.Equal(build.Success, ver!.BuildIds.Any());
                if (build.Success)
                    Assert.Equal(build.Value, string.Join(".", ver!.BuildIds));
            }
        }
    }

    public class VersionTests : IClassFixture<VersionTestFixture>
    {
        private readonly VersionTestFixture fixture;
        private readonly ITestOutputHelper output;

        public VersionTests(VersionTestFixture fix, ITestOutputHelper output)
            => (fixture, this.output) = (fix, output);

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
        [InlineData("1.0.0-0A.is.legal")]
        //[InlineData("99999999999999999999999.999999999999999999.99999999999999999")] // right shape, but too long for this impl
        //[InlineData("10.110.11111111111111111111111")] // right shape, but too long for this impl
        public void SemverValid(string text)
        {
            Assert.True(Version.TryParse(text, out var ver));
            _ = ver;
            Assert.Equal(text, ver!.ToString());

            fixture.Validate(true, text, ver);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("1.2")]
        [InlineData("1.")]
        [InlineData("1.2.")]
        [InlineData("1.2.3.")]
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
        [InlineData("10.2.3\0")]
        [InlineData("1.2.3\0")]
        [InlineData("0.0.0-a.3\x8c")]
        [InlineData("1.0.0-amethinglongCbuild.1-aef.1-.1-aef.1\x1a")]
        [InlineData("1.0.0-66\x8e")]
        [InlineData("0.0.3111111111111111111111111\x0a\x0a")] // what afl found was actually rather longer, but this hits the issue i think
        [InlineData("1.0.0-ala.3=")]
        [InlineData("1.0.0-a--Z0.߀xa--Z")]
        [InlineData("1.0.0-Zl.10--Z.0-Zl.buZl.1-ZD0-Zl.buiC.Z.0.0-Zl.1DbpD.p.l.10-Zl.1-Z.0-Zl.buiC.Z.0aa--Zl.1Dbp.0-Zl.1DiC.߀0-Zl.1DiC.0.0-p.0-Zl.1Zl.1D-d.1")]
        [InlineData("1.0.0-߀ta")]
        [InlineData("1.0.0-rc.1߀uild.1")]
        //[InlineData("10.11111111111111111111111112.3\x0a")] // right shape, but too long
        //[InlineData("22222222222222222222210.2.3\x0a")] // same as above
        public void SemverInvalid(string text)
        {
            var errors = new ParserErrorState<VersionParseAction>(text);
            Assert.False(Version.TryParse(ref errors, text, out var ver));
            var message = ErrorMessages.GetVersionErrorMessage(ref errors);
            output.WriteLine(message);
            errors.Dispose();
            _ = ver;

            fixture.Validate(false, text, ver);
        }

        // TODO: Version comparison tests
    }
}
