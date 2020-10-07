using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
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
    public interface ITestStopIfReturnsEmpty
    {
        [return: StopIfReturnsEmpty]
        List<int> RemoveNumber([TakesReturnValue] List<int> input)
        {
            if (input.Count == 0) throw new ArgumentException("Input list is empty.");
            input.RemoveAt(input.Count - 1);
            return input;
        }
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
        public void TestStopIfReturnsEmpty()
        {
            List<ITestStopIfReturnsEmpty> plugins = new List<ITestStopIfReturnsEmpty>();
            for (int i = 0; i < 6; i++) // This could be any number of plugins, as long as there is more than the input list. 
            {
                var plugin = new Mock<ITestStopIfReturnsEmpty>();
                plugin.Setup(p => p.RemoveNumber(It.IsAny<List<int>>())).Returns<List<int>>((input) =>
                {
                    if (input.Count == 0) throw new ArgumentException("Input list is empty.");
                    input.RemoveAt(input.Count - 1);
                    return input;
                });
                plugins.Add(plugin.Object);
            }

            List<int> numbers = new List<int>() { 0, 1, 2 };

            var created = new Aggregate<ITestStopIfReturnsEmpty>(plugins);
            Assert.True(created.Instance.RemoveNumber(numbers).Count == 0);
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