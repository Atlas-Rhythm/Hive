using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Hive.Models;
using Hive.Permissions;
using Hive.Utilities;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Hive.Tests.Endpoints
{
    public class UserController : TestDbWrapper
    {
        private readonly ITestOutputHelper helper;

        public UserController(ITestOutputHelper helper) : base(new PartialContext { Users = new[] { new User { Username = "asdf", AlternativeId = "1234" } } })
        {
            this.helper = helper;
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
