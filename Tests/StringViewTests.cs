using Hive.Permissions.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Hive.Permissions.Tests
{
    public class StringViewTests
    {
        private static StringView SV(string s) => s;

        [Theory]
        [InlineData(""), InlineData("hi"), InlineData("lo")]
        public void BasicCtor(string text)
        {
            var sv = new StringView(text);

            Assert.Equal(text, sv.BaseString);
            Assert.Equal(0, sv.Start);
            Assert.Equal(text.Length, sv.Length);
        }

        [Theory]
        [MemberData(nameof(BasicEqualsCases), DisableDiscoveryEnumeration = false)]
        public void BasicEquals(StringView sv1, StringView sv2, bool equal)
        {
            Assert.Equal(equal, sv1 == sv2);
            if (equal)
                Assert.Equal(sv1.GetHashCode(), sv2.GetHashCode());
        }

        public static object[][] BasicEqualsCases = new object[][]
        {
            new object[] { SV("hello"), SV("hello"), true },
            new object[] { SV("hello"), SV("hellu"), false },
            new object[] { SV("hello"), SV("hello "), false },
            new object[] { SV("hello"), new StringView("  hello", 2, 5), true },
            new object[] { SV("hello"), new StringView("hello  ", 0, 5), true },
        };

        [Theory]
        [MemberData(nameof(BasicEqualsCases), DisableDiscoveryEnumeration = false)]
        public void EnumerationEquals(StringView sv1, StringView sv2, bool equal)
        {
            var l1 = sv1.ToList().AsEnumerable();
            var l2 = sv2.ToList().AsEnumerable();
            if (equal)
                Assert.Equal(l1, l2);
            else
                Assert.False(l1.SequenceEqual(l2));
        }

        [Theory]
        [InlineData("hello", 3, "lo")]
        [InlineData("hello", 2, "llo")]
        [InlineData("hello", 1, "ello")]
        [InlineData("hello", 0, "hello")]
        public void SubstringNoLen(string source, int start, string expect)
        {
            Assert.Equal(expect, new StringView(source).Substring(start));
        }

        [Theory]
        [MemberData(nameof(SubstringLenCases), DisableDiscoveryEnumeration = false)]
        public void SubstringLen(string source, int start, int len, string expect)
        {
            Assert.Equal(expect, new StringView(source).Substring(start, len));
        }

        public static object[][] SubstringLenCases = new object[][]
        {
            new object[] { "hello", 3, 2, "lo" },
            new object[] { "hello", 3, 1, "l" },
            new object[] { "hello", 2, 3, "llo" },
            new object[] { "hello", 2, 2, "ll" },
            new object[] { "hello", 2, 1, "l" },
            new object[] { "hello", 1, 4, "ello" },
            new object[] { "hello", 1, 3, "ell" },
            new object[] { "hello", 1, 2, "el" },
            new object[] { "hello", 1, 1, "e" },
            new object[] { "hello", 0, 5, "hello" },
            new object[] { "hello", 0, 4, "hell" },
            new object[] { "hello", 0, 3, "hel" },
            new object[] { "hello", 0, 2, "he" },
            new object[] { "hello", 0, 1, "h" },
        };

        [Theory]
        [MemberData(nameof(SubstringLenCases), DisableDiscoveryEnumeration = false)]
        public void Indexer(string source, int start, int len, string _)
        {
            StringView sv = new StringView(source, start, len);
            for (int i = start - 2; i <= start + len; i++)
            {
                if (i < start || i >= start + len)
                    Assert.Throws<IndexOutOfRangeException>(() => sv[i - start]);
                else
                    Assert.Equal(source[i], sv[i - start]);
            }
        }

        [Theory]
        [MemberData(nameof(SplitCases))]
        public void Split(StringView source, StringView splTok, bool ignoreEmpty, StringView[] expect)
        {
            Assert.Equal(expect.AsEnumerable(), source.Split(splTok, ignoreEmpty).ToArray());
        }

        public static object[][] SplitCases = new object[][]
        {
            new object[] { SV("hi.lo"), SV("."), false, new StringView[] { "hi", "lo" } },
            new object[] { SV("hi..lo"), SV("."), false, new StringView[] { "hi", "", "lo" } },
            new object[] { SV("hi.lo."), SV("."), false, new StringView[] { "hi", "lo", "" } },
            new object[] { SV("hi.lo"), SV("."), true, new StringView[] { "hi", "lo" } },
            new object[] { SV("hi..lo"), SV("."), true, new StringView[] { "hi", "lo" } },
            new object[] { SV("hi.lo."), SV("."), true, new StringView[] { "hi", "lo" } },
            new object[] { SV("hi.lo"), SV(".."), true, new StringView[] { "hi.lo" } },
            new object[] { SV("hi..lo"), SV(".."), true, new StringView[] { "hi", "lo" } },
            new object[] { SV("hi.lo."), SV(".."), true, new StringView[] { "hi.lo." } },
            new object[] { SV("hi"), SV(" . "), false, new StringView[] { "hi" } },
            new object[] { SV("hi"), SV(" . "), true, new StringView[] { "hi" } },
            new object[] { SV(""), SV(" . "), false, new StringView[] { "" } },
            new object[] { SV(""), SV(" . "), true, new StringView[] { } },
        };

        [Theory]
        [MemberData(nameof(ConcatCases))]
        public void Concat(StringView a, StringView b, StringView c, StringView expect)
        {
            Assert.Equal(expect, StringView.Concat(a, b, c));
        }

        public static object[][] ConcatCases = new object[][]
        {
            new object[] { new StringView("abc", 0, 1), SV("b"), SV("c"), SV("abc") },
            new object[] { SV("a"), new StringView("abc", 1, 1), SV("c"), SV("abc") },
            new object[] { SV("a"), new StringView("ab", 1, 1), new StringView("abc", 2, 1), SV("abc") },

            new object[] { new StringView("ab", 0, 1), SV("b"), SV("c"), SV("abc") },
            new object[] { SV("a"), new StringView("ab", 1, 1), SV("c"), SV("abc") },
            new object[] { SV("a"), SV("b"), SV("c"), SV("abc") },
        };
    }
}
