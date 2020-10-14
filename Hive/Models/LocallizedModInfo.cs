using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
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

        private Mod? owningMod;

        [BackingField(nameof(owningMod))]
        [DisallowNull]
        [NotNull]
        public Mod OwningMod
        {
            get => owningMod ?? throw new InvalidOperationException();
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                owningMod?.Localizations.Remove(this);
                owningMod = value;
                if (!owningMod.Localizations.Contains(this))
                    owningMod.Localizations.Add(this);
            }
        }

        #region DB Schema stuff

        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We use Guid as a name. Could be changed in the future.")]
        public Guid Guid { get; set; }

        #endregion DB Schema stuff

        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
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