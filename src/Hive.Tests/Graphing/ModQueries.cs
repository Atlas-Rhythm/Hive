using System.Threading.Tasks;
using GraphQL.Execution;
using Hive.Graphing;
using Hive.Models;
using Hive.Plugins;
using Hive.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
            var services = CreateProvider("next(false)", TestHelpers.CreateMockRequest(null!));
            var result = await services.ExecuteGraphAsync("{ mod(id: \"lilac\") { readableID } }");
            Assert.NotNull(result);
            Assert.NotNull(result.Errors);
            Assert.NotEmpty(result.Errors);
            var error = result.Errors[0];
            Assert.Equal("Forbidden", error.Message);
        }

        [Fact]
        public async Task SpecificModWithIDOnly()
        {
            var services = CreateProvider("next(true)", TestHelpers.CreateMockRequest(null!));
            var result = await services.ExecuteGraphAsync("{ mod(id: \"lilac\") { readableID } }");
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            Assert.IsType<RootExecutionNode>(result.Data);
            var execution = (result.Data as RootExecutionNode)!;
            Assert.NotEmpty(execution.SubFields);
            Assert.Equal("mod", execution.SubFields[0].Name);
            Assert.IsType<Mod>(execution.SubFields[0].Result);
            var mod = (execution.SubFields[0].Result as Mod)!;
            Assert.Equal("lilac", mod.ReadableID);
        }

        [Fact]
        public async Task SpecificModWithAllStandardSerializableFields()
        {
            var services = CreateProvider("next(true)", TestHelpers.CreateMockRequest(null!));
            var result = await services.ExecuteGraphAsync(
                @"{
                    mod(id: ""lilac"") {
                    authors { username }
                    conflicts { modID versionRange }
                    contributors { username }
                    dependencies { modID versionRange }
                    downloadLink
                    id
                    localizations { changelog credits description name }
                    readableID
                    supportedVersions { name }
                    uploader { username }
                    }
                }");
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            Assert.IsType<RootExecutionNode>(result.Data);
            var execution = (result.Data as RootExecutionNode)!;
            Assert.NotEmpty(execution.SubFields);
            Assert.Equal("mod", execution.SubFields[0].Name);
            Assert.IsType<Mod>(execution.SubFields[0].Result);
            var mod = (execution.SubFields[0].Result as Mod)!;

            Assert.Equal("lilac", mod.ReadableID);
            Assert.Equal("raftario best modder", mod.Uploader.Username);
        }

        private ServiceProvider CreateProvider(string permissionRule, HttpContext? context)
        {
            if (context is null)
                context = new DefaultHttpContext();

            var services = DIHelper.ConfigureServices(Options, helper, new ModTestHelper.ModsRuleProvider(permissionRule));

            _ = services
                .AddTransient<IChannelsControllerPlugin, HiveChannelsControllerPlugin>()
                .AddSingleton<IHttpContextAccessor, DIHelper.TestAccessor>(sp => new DIHelper.TestAccessor(context))
                .AddTransient<IResolveDependenciesPlugin, HiveResolveDependenciesControllerPlugin>()
                .AddTransient<IGameVersionsPlugin, HiveGameVersionsControllerPlugin>()
                .AddTransient<IModsPlugin, HiveModsControllerPlugin>()
                .AddScoped<DependencyResolverService>()
                .AddScoped<GameVersionService>()
                .AddScoped<ChannelService>()
                .AddScoped<ModService>()
                .AddAggregates()
                .AddHiveGraphQL();

            return services.BuildServiceProvider();
        }
    }
}
