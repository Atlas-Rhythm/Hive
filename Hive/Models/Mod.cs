using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NodaTime;
using Hive.Converters;
using System.Text.Json.Serialization;
using Hive.Versioning;
using Version = Hive.Versioning.Version;

namespace Hive.Models
{
    public class Mod
    {
        // this would be the primary key for this row
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // the reason for this wierdness is that M2M, at the moment, fails if it can't find the PKey by convention

        public string ReadableID { get; set; } = null!;

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
        public virtual ICollection<GameVersion> SupportedVersions { get; set; } = new List<GameVersion>();

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

        public void AddGameVersion(GameVersion ver)
        {
            if (!SupportedVersions.Contains(ver))
                SupportedVersions.Add(ver);
            if (!ver.SupportedMods.Contains(this))
                ver.SupportedMods.Add(this);
        }

        public void RemoveGameVersion(GameVersion ver)
        {
            SupportedVersions.Remove(ver);
            ver.SupportedMods.Remove(this);
        }

        public static void Configure(ModelBuilder b)
        {
            b.Entity<Mod>()
                .HasMany(m => m.Localizations)
                .WithOne(l => l.OwningMod)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SupportedMods is set up by GameVersion
            b.Entity<Mod>()
                .Property(m => m.Version)
                .HasConversion( // TODO: maybe encode this differently (say in a json structure?)
                    v => v.ToString(),
                    s => new Version(s)
                );
            b.Entity<Mod>()
                .HasIndex(m => new { m.ReadableID, m.Version })
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

    public readonly struct ModReference
    {
        public string ModID { get; }

        [JsonConverter(typeof(VersionRangeJsonConverter))]
        public VersionRange Versions { get; }

        [JsonConstructor]
        public ModReference(string modID, VersionRange versions)
        {
            ModID = modID;
            Versions = versions;
        }
    }
}
