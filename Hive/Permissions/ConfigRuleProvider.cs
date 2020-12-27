using Hive.Utilities;
using NodaTime;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Permissions
{
    public class ConfigRuleProvider : IRuleProvider
    {
        // TODO: implement

        public ConfigRuleProvider()
        {
        }

        public bool HasRuleChangedSince(StringView name, Instant time) => false;

        public bool HasRuleChangedSince(Rule rule, Instant time) => false;

        public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
        {
            gotten = new Rule(name.ToString(), "next(true)");
            return true;
        }
    }
}
