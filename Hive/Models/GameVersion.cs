using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Hive.Models
{
    public class GameVersion
    {
        public string Name { get; set; } = null!;

        // like Mod's
        public JsonElement AdditionalData { get; set; }

        public Instant CreationTime { get; set; }

        public virtual ICollection<Mod> SupportedMods { get; set; } = new List<Mod>();

        #region DB Schema stuff
        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        #endregion

        public static void Configure(ModelBuilder b)
        {
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