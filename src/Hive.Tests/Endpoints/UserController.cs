using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using DryIoc;
using Hive.Controllers;
using Hive.Models;
using Hive.Permissions;
using Hive.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using static Hive.Tests.TestHelpers;

namespace Hive.Tests.Endpoints
{
    public class UserController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        public UserController(ITestOutputHelper helper) : base(new PartialContext
        {
            Users = new[]
            {
                new User
                {
                    Username = "test",
                    AlternativeId = "1234"
                }, new User
                {
                    Username = "test2",
                    AlternativeId = "4321"
                }
            }
        })
        {
            this.helper = helper;
        }

        private IContainer CreateController(IEnumerable<IUserPlugin> plugins, string ruleRename, string ruleInfo)
        {
            var container = DIHelper.ConfigureServices(Options, _ => { }, helper, new UserControllerRuleProvider(ruleRename, ruleInfo));

            container.RegisterInstance(plugins);
            container.Register<Controllers.UserController>(Reuse.Scoped);

            return container;
        }

        [Fact]
        public async Task CanGetSelfUserInfo()
        {
            var serviceProvider = CreateController(new[] { new HiveUserPlugin() }, "next(true)", "next(true)");

            using var scope = serviceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService<Controllers.UserController>();
            controller.ControllerContext.HttpContext = CreateMockRequest(new MemoryStream());
            // Get mock request user info, which will have Bearer: asdf
            // Should resolve to the test user.
            var data = await controller.GetUserInfo(null);
            Assert.NotNull(data);
            var result = data.Result as OkObjectResult;
            Assert.NotNull(result);
            var value = result!.Value as Dictionary<string, object>;
            Assert.NotNull(value);
            Assert.True(value!.ContainsKey("username"));
            Assert.Equal("test", value!["username"]);
        }

        private class UserControllerRuleProvider : IRuleProvider
        {
            private readonly string renamePermissionRule;
            private readonly string infoPermissionRule;

            public UserControllerRuleProvider(string renamePermissionRule, string infoPermissionRule)
            {
                this.renamePermissionRule = renamePermissionRule;
                this.infoPermissionRule = infoPermissionRule;
            }

            public bool HasRuleChangedSince(StringView name, Instant time) => true;

            public bool HasRuleChangedSince(Rule rule, Instant time) => true;

            public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
            {
                var nameString = name.ToString();
                switch (nameString)
                {
                    case "hive.user.rename":
                        gotten = new Rule(nameString, renamePermissionRule);
                        return true;

                    case "hive.user.info":
                        gotten = new Rule(nameString, infoPermissionRule);
                        return true;

                    default:
                        gotten = null;
                        return false;
                }
            }
        }
    }
}
