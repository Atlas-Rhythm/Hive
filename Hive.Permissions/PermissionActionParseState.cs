using Hive.Permissions.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hive.Permissions
{
    /// <summary>
    /// A structure that is used to cache parsed actions and related information.
    /// </summary>
    /// <seealso cref="PermissionsManager{TContext}.CanDo(StringView, TContext, ref PermissionActionParseState)"/>
    public struct PermissionActionParseState
    {
        internal struct SearchEntry
        {
            public StringView Name;
            public Rule? Rule;
            public DateTime CheckedAt;
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
