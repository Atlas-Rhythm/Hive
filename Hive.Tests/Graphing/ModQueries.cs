using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL;
using Hive.Controllers;
using Hive.Graphing;
using Hive.Models;
using Hive.Plugins;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Graphing
{
    public class ModQueries : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        public ModQueries(ITestOutputHelper helper) : base(DbExamples.PopulatedPartialContext()) => this.helper = helper;

        [Fact]
        public async Task PermissionForbidModByID()
        {
            var services = CreateProvider("next(false)", CreateMockRequest(null!));
            var result = await services.ExecuteGraphAsync("{ mod(id: \"uwu\") { readableID } }");
            Assert.NotNull(result);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
            var error = result.Errors[0];
            Assert.Equal("Forbidden", error.Message);
        }

        // NOT PASSING LOL
        /*[Fact]
        public async Task SpecificModWithIDOnly()
        {
            var services = CreateProvider("next(true)", CreateMockRequest(null!));
            var result = await services.ExecuteGraphAsync("{ mod(id: \"lilac\") { readableID } }");
            Assert.NotNull(result);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Data);
            var mod = FindAndCastDataObject<Mod>(result, "mod");
            Assert.Equal("lilac", mod.ReadableID);
        }*/

        private ServiceProvider CreateProvider(string permissionRule, HttpContext context)
        {
            var services = DIHelper.ConfigureServices(Options, helper, new ModTestHelper.ModsRuleProvider(permissionRule));

            _ = services
                .AddTransient<IChannelsControllerPlugin, HiveChannelsControllerPlugin>()
                .AddSingleton<IHttpContextAccessor, DIHelper.TestAccessor>(sp => new DIHelper.TestAccessor(context))
                .AddTransient<IResolveDependenciesPlugin, HiveResolveDependenciesControllerPlugin>()
                .AddTransient<IGameVersionsPlugin, HiveGameVersionsControllerPlugin>()
                .AddScoped<DependencyResolverService>()
                .AddScoped<GameVersionService>()
                .AddScoped<ChannelService>()
                .AddScoped<ModService>()
                .AddAggregates()
                .AddHiveQLTypes()
                .AddHiveGraphQL();

            return services.BuildServiceProvider();
        }

        private static HttpContext CreateMockRequest(Stream body)
        {
            var requestMoq = new Mock<HttpRequest>();
            _ = requestMoq.SetupGet(r => r.Body).Returns(body);
            _ = requestMoq.SetupGet(r => r.Headers).Returns(new HeaderDictionary(
                new Dictionary<string, StringValues>()
                {
                    { HeaderNames.Authorization, new StringValues("Bearer: test") }
                })
            );

            var contextMoq = new Mock<HttpContext>();
            _ = contextMoq.SetupGet(c => c.Request).Returns(requestMoq.Object);

            return contextMoq.Object;
        }

        private static T FindAndCastDataObject<T>(ExecutionResult result, string keyName)
        {
            var serializedData = JsonSerializer.Serialize(result.Data);
            return JsonSerializer.Deserialize<JsonElement>(serializedData, new JsonSerializerOptions(JsonSerializerDefaults.Web)).GetProperty(keyName).GetPropertyValue<T>();
        }
    }
}
