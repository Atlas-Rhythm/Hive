using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using DryIoc;
using GraphQL.Execution;
using Hive.Graphing;
using Hive.Models;
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
            using var scope = services.CreateScope();
            var result = await scope.ServiceProvider.ExecuteGraphAsync("{ mod(id: \"lilac\") { readableID } }");
            Assert.NotNull(result);
            Assert.NotNull(result.Errors);
            var error = Assert.Single(result.Errors);
            // Should have only one error, and the string code should be equivalent to the forbidden HttpStatusCode (as a number)
            Assert.Equal(((int)HttpStatusCode.Forbidden).ToString(CultureInfo.InvariantCulture), error.Code);
        }

        [Fact]
        public async Task SpecificModWithIDOnly()
        {
            var services = CreateProvider("next(true)", TestHelpers.CreateMockRequest(null!));
            using var scope = services.CreateScope();
            var result = await scope.ServiceProvider.ExecuteGraphAsync("{ mod(id: \"lilac\") { readableID } }");
            Assert.NotNull(result);
            Assert.Null(result.Errors);
            Assert.NotNull(result.Data);
            var execution = Assert.IsType<RootExecutionNode>(result.Data);
            Assert.NotEmpty(execution.SubFields);
            Assert.Equal("mod", execution.SubFields[0].Name);
            var mod = Assert.IsType<Mod>(execution.SubFields[0].Result);
            Assert.Equal("lilac", mod.ReadableID);
        }

        [Fact]
        public async Task SpecificModWithAllStandardSerializableFields()
        {
            var services = CreateProvider("next(true)", TestHelpers.CreateMockRequest(null!));
            using var scope = services.CreateScope();
            var result = await scope.ServiceProvider.ExecuteGraphAsync(
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

        private IContainer CreateProvider(string permissionRule, HttpContext? context)
        {
            if (context is null)
                context = new DefaultHttpContext();

            var container = DIHelper.ConfigureServices(Options, s
                => s.AddHiveGraphQL(), helper, new ModTestHelper.ModsRuleProvider(permissionRule));

            container.RegisterInstance<IHttpContextAccessor>(new DIHelper.TestAccessor(context));
            container.Register<DependencyResolverService>(Reuse.Scoped);
            container.Register<GameVersionService>(Reuse.Scoped);
            container.Register<ChannelService>(Reuse.Scoped);
            container.Register<ModService>(Reuse.Scoped);
            container.RegisterHiveGraphQL();

            return container;
        }
    }
}
