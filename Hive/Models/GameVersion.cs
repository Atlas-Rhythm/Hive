using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Hive.Models
{
    public class GameVersion
    {
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        public Instant CreationTime { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public virtual ICollection<Mod> SupportedMods { get; set; } = new List<Mod>();

        #region DB Schema stuff

        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We use Guid as a name. Could be changed in the future.")]
        public Guid Guid { get; set; }

        #endregion DB Schema stuff

        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            b.Entity<GameVersion>()
                .HasIndex(v => new { v.Name })
                .IsUnique();

            b.Entity<GameVersion>()
                .HasMany(m => m.SupportedMods)
                .WithMany(v => v.SupportedVersions)
                .UsingEntity(b =>
                {
                });
        }
    }
}