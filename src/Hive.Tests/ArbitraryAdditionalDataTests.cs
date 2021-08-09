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
    }
}
