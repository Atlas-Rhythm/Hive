using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Hive.Models
{
    public class LocalizedModInfo
    {
        public CultureInfo Language { get; } = null!;

        public string Name { get; } = null!;

        public string Description { get; } = null!;

        public string? Changelog { get; }

        public string? Credits { get; }

        public Mod OwningMod { get; } = null!;

#if false
        #region DB Schema stuff
        // this would be a foreign key back to the Mod object this is associated with
        public Guid ModVersionGuid { get; }
        #endregion
#endif
    }
}
