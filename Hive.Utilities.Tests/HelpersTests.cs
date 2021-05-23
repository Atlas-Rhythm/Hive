using Xunit;

namespace Hive.Utilities.Tests
{
    public class HelpersTests
    {
        [Fact]
        public void TestValueTupleToArray()
        {
            var tuple = ("hello", 5, 17m, 'a', (object?)null);

            var arr = tuple.ToArray();

            Assert.Equal(new object?[] { tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 }, arr);
        }

        [Fact]
        public void TestLongValueTupleToArray()
        {
            var tuple = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            var arr = tuple.ToArray();

            Assert.Equal(new object?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, arr);
        }
    }
}
