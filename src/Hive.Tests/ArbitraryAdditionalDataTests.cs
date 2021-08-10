using System.Collections.Generic;
using System.Text.Json;
using Hive.Models;
using Xunit;

namespace Hive.Tests
{
    public class ArbitraryAdditionalDataTests
    {
        private static readonly JsonSerializerOptions opts = new();

        static ArbitraryAdditionalDataTests()
        {
            opts.Converters.Add(new ArbitraryAdditionalData.ArbitraryAdditionalDataConverter());
        }

        [Fact]
        public void TestSimple()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", 3);
            var s = JsonSerializer.Serialize(data, opts);
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(s, opts);
            Assert.NotNull(data2);
            Assert.Equal(3, data2!.Get<int>("test"));
        }

        [Fact]
        public void TestCollection()
        {
            ArbitraryAdditionalData data = new();
            var lst = new List<int> { 1, 2, 3 };
            data.Add("test", lst);
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            Assert.Equal(lst, data2!.Get<List<int>>("test"));
        }

        [Fact]
        public void TestMultiple()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test1", 3);
            data.Add("test2", 4);
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            Assert.Equal(3, data2!.Get<int>("test1"));
            Assert.Equal(4, data2!.Get<int>("test2"));
        }

        private class A
        {
            public int X { get; set; }
        }

        [Fact]
        public void TestObject()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new A { X = 2 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            var v = data2!.Get<A>("test");
            Assert.NotNull(v);
            Assert.Equal(2, v!.X);
        }
    }
}
