using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hive.Plugins.Tests
{
    [Aggregable]
    public interface ITestStopIfReturns
    {
        [return: StopIfReturns(false)]
        bool Test1() => false;

        void Test2([StopIfReturns(false)] out bool ret) => ret = false;
    }

    [Aggregable]
    public interface ITestStopIfReturnsNull
    {
        [return: StopIfReturnsNull]
        List<int>? Test1();

        void Test2([StopIfReturnsNull] out List<int>? ret);
    }

    [Aggregable]
    public interface ITestCarryReturnValue
    {
        int Test1([TakesReturnValue] int x);
    }

    [Aggregable]
    public interface ITestStopIfReturnsEmptyGeneric
    {
        [return: StopIfReturnsEmpty]
        List<int> RemoveNumber([TakesReturnValue] List<int> input);

        [return: StopIfReturnsEmpty]
        Dictionary<string, int> RemoveLastKey([TakesReturnValue] Dictionary<string, int> input);
    }

    [Aggregable]
    public interface ITestStopIfReturnsEmptyNonGeneric
    {
        [return: StopIfReturnsEmpty]
        ArrayList RemoveElement([TakesReturnValue] ArrayList input);
    }

    public class TestAggregable
    {
        [Fact]
        public void TestStopIfReturns()
        {
            var retTrue1 = new Mock<ITestStopIfReturns>();
            retTrue1.Setup(m => m.Test1()).Returns(true);
            bool expected = true;
            retTrue1.Setup(m => m.Test2(out expected));

            var retFalse = new Mock<ITestStopIfReturns>();
            retFalse.Setup(m => m.Test1()).Returns(false);
            expected = false;
            retFalse.Setup(m => m.Test2(out expected));

            var retTrue2 = new Mock<ITestStopIfReturns>();
            retTrue2.Setup(m => m.Test1()).Returns(true);
            expected = true;
            retTrue2.Setup(m => m.Test2(out expected));

            var created = new Aggregate<ITestStopIfReturns>(new List<ITestStopIfReturns>(){
                retTrue1.Object, retFalse.Object, retTrue2.Object
            });
            // Should return upon the first false returned
            Assert.False(created.Instance.Test1());
            created.Instance.Test2(out var tmp);
            Assert.False(tmp);
            // Should have called retTrue1, retFalse, but not retTrue2
            retTrue1.Verify(m => m.Test1(), Times.Once);
            retFalse.Verify(m => m.Test1(), Times.Once);
            retTrue2.Verify(m => m.Test1(), Times.Never);
            tmp = true;
            retTrue1.Verify(m => m.Test2(out tmp), Times.Once);
            tmp = false;
            retFalse.Verify(m => m.Test2(out tmp), Times.Once);
            // Shouldn't call retTrue.Test2 for either true or false as our out
            retTrue2.Verify(m => m.Test2(out tmp), Times.Never);
            tmp = true;
            retTrue2.Verify(m => m.Test2(out tmp), Times.Never);
        }

        [Fact]
        public void TestStopIfReturnsNull()
        {
            var retTrue1 = new Mock<ITestStopIfReturnsNull>();
            retTrue1.Setup(m => m.Test1()).Returns(new List<int>());
            List<int>? expected1 = new List<int>();
            retTrue1.Setup(m => m.Test2(out expected1));

            var retFalse = new Mock<ITestStopIfReturnsNull>();
            retFalse.Setup(m => m.Test1()).Returns(() => null);
            List<int>? expected = null;
            retFalse.Setup(m => m.Test2(out expected));

            var retTrue2 = new Mock<ITestStopIfReturnsNull>();
            retTrue2.Setup(m => m.Test1()).Returns(new List<int>());
            List<int>? expected2 = new List<int>();
            retTrue2.Setup(m => m.Test2(out expected2));

            var created = new Aggregate<ITestStopIfReturnsNull>(new List<ITestStopIfReturnsNull>(){
                retTrue1.Object, retFalse.Object, retTrue2.Object
            });
            // Should return upon the first null returned
            Assert.Null(created.Instance.Test1());
            created.Instance.Test2(out var tmp);
            Assert.Null(tmp);
            // Should have called retTrue1, retFalse, but not retTrue2
            retTrue1.Verify(m => m.Test1(), Times.Once);
            retFalse.Verify(m => m.Test1(), Times.Once);
            retTrue2.Verify(m => m.Test1(), Times.Never);
            retTrue1.Verify(m => m.Test2(out expected1), Times.Once);
            retFalse.Verify(m => m.Test2(out expected), Times.Once);
            // Shouldn't call retTrue.Test2 at all
            retTrue2.Verify(m => m.Test2(out expected2), Times.Never);
        }

        [Fact]
        public void TestStopIfReturnsEmptyGeneric()
        {
            int numberOfElements = 3;

            List<Mock<ITestStopIfReturnsEmptyGeneric>> plugins = new List<Mock<ITestStopIfReturnsEmptyGeneric>>();
            List<int> numbers = new List<int>() { };
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            for (int i = 0; i < numberOfElements; i++)
            {
                numbers.Add(i);
                dictionary.Add(i.ToString(), i);
            }

            for (int i = 0; i < numberOfElements * 2; i++)
            {
                var plugin = new Mock<ITestStopIfReturnsEmptyGeneric>();
                // These plugins will each take away the last element, or throw an exception if it is empty.
                plugin.Setup(p => p.RemoveNumber(It.IsAny<List<int>>())).Returns<List<int>>((input) =>
                {
                    if (input.Count == 0) throw new ArgumentException("Input list is empty.");
                    input.RemoveAt(input.Count - 1);
                    return input;
                });

                plugin.Setup(p => p.RemoveLastKey(It.IsAny<Dictionary<string, int>>())).Returns<Dictionary<string, int>>((input) =>
                {
                    if (!input.Any()) throw new ArgumentException("Input dictionary is empty.");
                    input.Remove(input.Last().Key);
                    return input;
                });
                plugins.Add(plugin);
            }

            var created = new Aggregate<ITestStopIfReturnsEmptyGeneric>(plugins.Select(x => x.Object));

            // If StopIfReturnsEmpty fails, then an exception will be thrown here.
            Assert.Empty(created.Instance.RemoveNumber(numbers));
            Assert.Empty(created.Instance.RemoveLastKey(dictionary));

            // Since each plugin takes away one element, we need to ensure that only the first X plugins were fired,
            // where X is the amount of elements in the initial list.
            for (int i = 0; i < numberOfElements; i++)
            {
                plugins[i].Verify(p => p.RemoveNumber(It.IsAny<List<int>>()));
            }

            // Next, we ensure that any plugins after the short-circuit were not fired.
            // If any were fired, we would throw a regular Exception, and the test would fail here.
            for (int i = numberOfElements; i < plugins.Count; i++)
            {
                plugins[i].Verify(p => p.RemoveNumber(It.IsAny<List<int>>()), Times.Never());
            }
        }

        [Fact]
        public void TestStopIfReturnsEmptyNonGeneric()
        {
            // This is essentially the same as the Generic version, but instead dealing with ArrayLists.
            int numberOfElements = 3;
            List<Mock<ITestStopIfReturnsEmptyNonGeneric>> plugins = new List<Mock<ITestStopIfReturnsEmptyNonGeneric>>();
            ArrayList data = new ArrayList();
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            for (int i = 0; i < numberOfElements; i++)
            {
                data.Add(i);
            }

            for (int i = 0; i < numberOfElements * 2; i++)
            {
                var plugin = new Mock<ITestStopIfReturnsEmptyNonGeneric>();
                // These plugins will each take away the last element, or throw an exception if it is empty.
                plugin.Setup(p => p.RemoveElement(It.IsAny<ArrayList>())).Returns<ArrayList>((input) =>
                {
                    if (input.Count == 0) throw new ArgumentException("Input list is empty.");
                    input.RemoveAt(input.Count - 1);
                    return input;
                });
                plugins.Add(plugin);
            }

            var created = new Aggregate<ITestStopIfReturnsEmptyNonGeneric>(plugins.Select(x => x.Object));

            // If StopIfReturnsEmpty fails, then an exception will be thrown here.
            Assert.Empty(created.Instance.RemoveElement(data));
            for (int i = 0; i < numberOfElements; i++)
            {
                plugins[i].Verify(p => p.RemoveElement(It.IsAny<ArrayList>()));
            }
            for (int i = numberOfElements; i < plugins.Count; i++)
            {
                plugins[i].Verify(p => p.RemoveElement(It.IsAny<ArrayList>()), Times.Never());
            }
        }

        [Fact]
        public void TestNormal()
        {
            var test1 = new Mock<ITestCarryReturnValue>();
            test1.Setup(m => m.Test1(It.IsAny<int>())).Returns((int x) => x + 1);
            var test2 = new Mock<ITestCarryReturnValue>();
            test2.Setup(m => m.Test1(It.IsAny<int>())).Returns((int x) => x * -1);

            var created = new Aggregate<ITestCarryReturnValue>(new List<ITestCarryReturnValue>(){
                test1.Object, test2.Object
            });
            // Should go till completion, return (x + 1) * -1
            Assert.Equal(-2, created.Instance.Test1(1));
            // Should have called both functions identically once
            test1.Verify(m => m.Test1(It.IsAny<int>()), Times.Once);
            test2.Verify(m => m.Test1(It.IsAny<int>()), Times.Once);
        }
    }
}