using Hive.Utilities;
using NodaTime;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Permissions
{
    /// <summary>
    /// A structure that is used to cache parsed actions and related information.
    /// </summary>
    /// <seealso cref="PermissionsManager{TContext}.CanDo(StringView, TContext, ref PermissionActionParseState)"/>
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "There is no point in overriding this behavior for this type, since we never compare it.")]
    public struct PermissionActionParseState
    {
        internal struct SearchEntry
        {
            public StringView Name;
            public Rule? Rule;
            public Instant CheckedAt;

            public SearchEntry(StringView name)
            {
                Name = name;
                Rule = null;
                CheckedAt = Instant.MinValue;
            }
        }

        internal Type? ContextType;
        internal SearchEntry[]? SearchOrder;

        /// <summary>
        /// Resets this cache to its default state. This will cause additional calls that use it to re-parse,
        /// and possibly re-compile the rules.
        /// </summary>
        public void Reset()
        {
            SearchOrder = null;
            ContextType = null;
        }
    }
}
