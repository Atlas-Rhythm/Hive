using System;
using System.Collections.Generic;
using System.IO;
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
        public void TestKeyFail()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new B { X = 2 });
            // Key not found
            Assert.Throws<KeyNotFoundException>(() => data.Get<B>("asdf"));
            // Null key
            Assert.Throws<ArgumentNullException>(() => data.Get<B>(null!));
            Assert.Throws<ArgumentNullException>(() => data.Add(null!, 2));
            // Key exists
            Assert.Throws<ArgumentException>(() => data.Add("test", 23));
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

        private class B : A
        {
        }

        [Fact]
        public void TestInherit()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new B { X = 2 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            var v = data2!.Get<A>("test");
            Assert.NotNull(v);
            Assert.Equal(2, v!.X);
        }

        [Fact]
        public void TestFailInherit()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new A { X = 2 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            // Call get with A, only types allowed from now on are types that are convertible from A.
            var v = data2!.Get<A>("test");
            // B is not convertible from A.
            Assert.Throws<InvalidCastException>(() => data2!.Get<B>("test"));
        }

        [Fact]
        public void TestFailConversion()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new A { X = 2 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            Assert.Throws<JsonException>(() => data2!.Get<int>("test"));
        }

        [Fact]
        public void TestConversionMultiple()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new B { X = 2 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            // First ask for a B
            var v = data2!.Get<B>("test");
            Assert.NotNull(v);
            Assert.Equal(2, v!.X);
            // Then ask for an A, should be permitted
            var v2 = data2!.Get<A>("test");
            Assert.NotNull(v2);
            Assert.Equal(2, v2!.X);
        }

        [Fact]
        public void TestSet()
        {
            ArbitraryAdditionalData data = new();
            data.Add("test", new B { X = 2 });
            data.Set("test", new B { X = 5 });
            data.Set("test3", new A { X = 1 });
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            var v = data2!.Get<B>("test");
            Assert.NotNull(v);
            Assert.Equal(5, v!.X);
            var v2 = data2!.Get<A>("test3");
            Assert.NotNull(v2);
            Assert.Equal(1, v2!.X);
        }

        [Fact]
        public void TestOptions()
        {
            ArbitraryAdditionalData data = new();
            JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            data.Add("test", new A { X = 2 }, options);
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            // This should return an instance with nothing set.
            Assert.Equal(0, data2!.Get<A>("test")!.X);
            // The above value is cached, so in order to avoid using it again, serialize and deserialize again.
            data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            // This should succeed, since we provide the correct conversion options
            var v = data2!.Get<A>("test", options);
            Assert.NotNull(v);
            Assert.Equal(2, v!.X);
        }

        [Fact]
        public void TestNull()
        {
            ArbitraryAdditionalData data = new();
            data.Add<A>("test", null);
            var data2 = JsonSerializer.Deserialize<ArbitraryAdditionalData>(JsonSerializer.Serialize(data, opts), opts);
            Assert.NotNull(data2);
            // This should return null
            Assert.Null(data2!.Get<A>("test"));
        }
    }
}
