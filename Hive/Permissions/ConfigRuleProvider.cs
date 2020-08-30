using Hive.Utilities;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Permissions
{
    public class ConfigRuleProvider : IRuleProvider
    {
        // TODO: implement

        public ConfigRuleProvider() { }

        public bool HasRuleChangedSince(StringView name, Instant time)
        {
            return false;
        }

        public bool HasRuleChangedSince(Rule rule, Instant time)
        {
            return false;
        }

        public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
        {
            gotten = new Rule(name.ToString(), "next(true)");
            return true;
        }
    }
}
