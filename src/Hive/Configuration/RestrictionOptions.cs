using System.Collections.Generic;

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

        public IReadOnlyList<string>? RestrictedRoutes { get; private set; }
    }
}
