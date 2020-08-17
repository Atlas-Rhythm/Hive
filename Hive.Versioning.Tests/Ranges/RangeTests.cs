﻿using System;
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
        public void TestStringificationRoundTrip(string startText)
        {
            Assert.True(VersionRange.TryParse(startText, out var range));
            var startString = range!.ToString();
            Assert.True(VersionRange.TryParse(startString, out var range2));
            var endString = range2!.ToString();
            Assert.Equal(startString, endString);
        }
    }
}