using Hive.Utilities;
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

        public bool HasRuleChangedSince(StringView name, DateTime time)
        {
            return false;
        }

        public bool HasRuleChangedSince(Rule rule, DateTime time)
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
