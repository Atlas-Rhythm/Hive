using Hive.Utilities;
using NodaTime;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Permissions
{
    /// <summary>
    ///
    /// </summary>
    public class ConfigRuleProvider : IRuleProvider
    {
        // TODO: implement

        /// <summary>
        ///
        /// </summary>
        public ConfigRuleProvider()
        {
        }

        /// <inheritdoc/>
        public bool HasRuleChangedSince(StringView name, Instant time) => false;

        /// <inheritdoc/>
        public bool HasRuleChangedSince(Rule rule, Instant time) => false;

        /// <inheritdoc/>
        public bool TryGetRule(StringView name, [MaybeNullWhen(false)] out Rule gotten)
        {
            gotten = new Rule(name.ToString(), "next(true)");
            return true;
        }
    }
}
