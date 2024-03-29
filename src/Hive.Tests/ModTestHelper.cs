﻿using System.Diagnostics.CodeAnalysis;
using Hive.Models;
using Hive.Permissions;
using Hive.Services.Common;
using Hive.Utilities;
using NodaTime;

namespace Hive.Tests
{
    public static class ModTestHelper
    {
        // This plugin will filter out a mod if it's in the beta channel. Super super basic but works.
        public class BetaModsFilterPlugin : IModsPlugin
        {
            public bool GetSpecificModAdditionalChecks(User? user, Mod contextMod) => contextMod.Channel.Name != "Beta";
        }

        // This is taken from GameVersionsController to have a configurable permission rule.
        public class ModsRuleProvider : IRuleProvider
        {
            private readonly string permissionRule;

            public ModsRuleProvider(string permissionRule) => this.permissionRule = permissionRule;

            public bool HasRuleChangedSince(StringView name, Instant time) => true;

            public bool HasRuleChangedSince(Rule rule, Instant time) => true;

            public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
            {
                var nameString = name.ToString();
                switch (nameString)
                {
                    case "hive":
                        gotten = new Rule(nameString, "next(false)");
                        return true;

                    default:
                        gotten = new Rule(nameString, permissionRule);
                        return true;
                }
            }
        }
    }
}
