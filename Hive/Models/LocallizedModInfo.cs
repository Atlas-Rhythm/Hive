using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        #region DB Schema stuff
        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; }
        #endregion

        public static void Configure(ModelBuilder b)
        {
            // OwningMod is configured by Mod
            b.Entity<LocalizedModInfo>()
                .HasIndex(l => new { l.OwningMod, l.Language })
                .IsUnique()
                .IncludeProperties(l => new { l.Name });
            b.Entity<LocalizedModInfo>()
                .Property(l => l.Language)
                .HasConversion(
                    c => c.Name,
                    n => new CultureInfo(n, false)
                );
        }
    }
}
