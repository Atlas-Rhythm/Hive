using SemVer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Version = SemVer.Version;
using VerRange = SemVer.Range;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NodaTime;
using Hive.Converters;
using System.Text.Json.Serialization;

namespace Hive.Models
{
    public class Mod
    {
        public string ID { get; set; } = null!;

        // one to many
        public IList<LocalizedModInfo> Localizations { get; set; } = new List<LocalizedModInfo>();

        // this would ideally be a SemVer version object from somewhere
        public Version Version { get; set; } = null!;

        public Instant UploadedAt { get; set; }

        public Instant? EditedAt { get; set; }
        
        // many to one
        public User Uploader { get; set; } = null!;

        // many to many
        public IList<User> Authors { get; set; } = new List<User>();

        // many to many
        public IList<User> Contributors { get; set; } = new List<User>();

        // many to many (this needs to use a join type, and needs modification to be put into EF)
        public IList<GameVersion> SupportedVersions { get; set; } = new List<GameVersion>();

        [Column(TypeName = "jsonb")]
        public IList<ModReference> Dependencies { get; set; } = new List<ModReference>();

        [Column(TypeName = "jsonb")]
        public IList<ModReference> Conflicts { get; set; } = new List<ModReference>();

        // many to one
        public Channel Channel { get; set; } = null!;

        // this would be a JSON string, encoding arbitrary data (this should be some type that better represents that JSON data though)
        public JsonElement AdditionalData { get; set; }

        [Column(TypeName = "jsonb")] // use jsonb here because that will let the db handle it sanely
        public IList<(string Name, Uri Url)> Links { get; set; } = new List<(string, Uri)>();

        public Uri DownloadLink { get; set; } = null!;

        #region DB Schema stuff
        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        #endregion

        public static void Configure(ModelBuilder b)
        {
            b.Entity<Mod>()
                .HasMany(m => m.Localizations)
                .WithOne(l => l.OwningMod)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            b.Entity<Mod>()
                .HasMany(m => m.SupportedVersions)
                .WithMany(v => v.SupportedMods)
                .UsingEntity<GameVersion_Mod_Joiner>(
                    rb => rb.HasOne(j => j.Version).WithMany().OnDelete(DeleteBehavior.Cascade),
                    lb => lb.HasOne(j => j.Mod).WithMany().OnDelete(DeleteBehavior.Cascade)
                )
                .HasNoKey();
            b.Entity<Mod>()
                .Property(m => m.Version)
                .HasConversion( // TODO: maybe encode this differently (say in a json structure?)
                    v => v.ToString(),
                    s => new Version(s, false)
                );
            b.Entity<Mod>()
                .HasIndex(m => new { m.ID, m.Version })
                .IsUnique();
            b.Entity<Mod>()
                .Property(m => m.Uploader)
                .IsVaulthUser();
            b.Entity<Mod>()
                .Property(m => m.Authors)
                .IsVaulthUsers();
            b.Entity<Mod>()
                .Property(m => m.Contributors)
                .IsVaulthUsers();
        }
    }

    public struct ModReference
    {
        public string ModID { get; }

        [JsonConverter(typeof(JsonVersionRangeConverter))]
        public VerRange Versions { get; }

        public ModReference(string id, VerRange range)
        {
            ModID = id;
            Versions = range;
        }
    }
}
