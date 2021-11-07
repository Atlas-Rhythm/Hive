using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Configuration
{
    /// <summary>
    /// Represents configuration options for Restriction options.
    /// </summary>
    public class RestrictionOptions
    {
        /// <summary>
        /// The config header to look under for this configuration instance.
        /// </summary>
        public const string ConfigHeader = "Restrictions";

        /// <summary>
        /// The collection of restricted routes to deny access to.
        /// </summary>
        [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Needed for proper deserialization")]
        public List<string> RestrictedRoutes { get; private set; } = new();
    }
}
