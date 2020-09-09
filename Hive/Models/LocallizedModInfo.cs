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
        public CultureInfo Language { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string? Changelog { get; set; }

        public string? Credits { get; set; }

        private Mod? owningMod = null;

        [BackingField(nameof(owningMod))]
        public Mod OwningMod
        {
            get => owningMod ?? throw new InvalidOperationException();
            set
            {
                owningMod?.Localizations.Remove(this);
                owningMod = value;
                if (!owningMod.Localizations.Contains(this))
                    owningMod.Localizations.Add(this);
            }
        }

        #region DB Schema stuff

        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }

        #endregion DB Schema stuff

        public static void Configure(ModelBuilder b)
        {
            // OwningMod is configured by Mod
            /*b.Entity<LocalizedModInfo>()
                .HasIndex(l => new { l.OwningMod, l.Language })
                .IsUnique()
                .IncludeProperties(l => new { l.Name });*/
            b.Entity<LocalizedModInfo>()
                .Property(l => l.Language)
                .HasConversion(
                    c => c.Name,
                    n => new CultureInfo(n, false)
                );
        }
    }
}