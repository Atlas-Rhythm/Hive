using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Hive.Converters;
using System.Text.Json.Serialization;
using Hive.Versioning;
using Version = Hive.Versioning.Version;
using System.Diagnostics.CodeAnalysis;

namespace Hive.Models
{
    /// <summary>
    /// Represents a modification or library uploaded to this instance.
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// The ID of this mod. Also must be a unique key for each mod.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// The legible, unique identification of a mod that is human readible.
        /// </summary>
        public string ReadableID { get; set; } = null!;

        /// <summary>
        /// A collection of localization data for this particular mod.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<LocalizedModInfo> Localizations { get; set; } = new List<LocalizedModInfo>();

        /// <summary>
        /// The <see cref="Versioning.Version"/> of the mod.
        /// </summary>
        public Version Version { get; set; } = null!;

        /// <summary>
        /// The <see cref="Instant"/> of when this mod was uploaded.
        /// </summary>
        public Instant UploadedAt { get; set; }

        /// <summary>
        /// The <see cref="Instant"/> of when this mod was edited last, if it exists.
        /// </summary>
        public Instant? EditedAt { get; set; }

        /// <summary>
        /// The <see cref="User"/> of the uploader of this mod.
        /// </summary>
        public User Uploader { get; set; } = null!;

        /// <summary>
        /// The <see cref="User"/> objects of the authors of this mod. May be empty.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<User> Authors { get; set; } = new List<User>();

        /// <summary>
        /// The <see cref="User"/> objects of the contributors of this mod. May be empty.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<User> Contributors { get; set; } = new List<User>();

        /// <summary>
        /// The <see cref="GameVersion"/> objects that are supported by this particular mod. May be empty.
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public virtual ICollection<GameVersion> SupportedVersions { get; set; } = new List<GameVersion>();

        /// <summary>
        /// The <see cref="ModReference"/> objects that are dependencies of this particular mod. May be empty.
        /// </summary>
        [Column(TypeName = "jsonb")]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<ModReference> Dependencies { get; set; } = new List<ModReference>();

        /// <summary>
        /// The <see cref="ModReference"/> objects that are conflicts of this particular mod. May be empty.
        /// </summary>
        [Column(TypeName = "jsonb")]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<ModReference> Conflicts { get; set; } = new List<ModReference>();

        /// <summary>
        /// The <see cref="Models.Channel"/> that this mod is located in.
        /// </summary>
        public Channel Channel { get; set; } = null!;

        /// <summary>
        /// Represents extra data located within the mod.
        /// </summary>
        /// <remarks>This data is publicly read-only. Be sure not to store sensitive information as additional data.</remarks>
        [Column(TypeName = "jsonb")]
        [JsonConverter(typeof(ArbitraryAdditionalData.ArbitraryAdditionalDataConverter))]
        public ArbitraryAdditionalData AdditionalData { get; } = new();

        /// <summary>
        /// A collection of link pairs, with the name and url of each link. May be empty.
        /// </summary>
        [Column(TypeName = "jsonb")] // use jsonb here because that will let the db handle it sanely
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public IList<(string Name, Uri Url)> Links { get; set; } = new List<(string, Uri)>();

        /// <summary>
        /// The download link of the mod.
        /// </summary>
        public Uri DownloadLink { get; set; } = null!;

        /// <summary>
        /// Construct a mod with extra data.
        /// </summary>
        /// <param name="extraData"></param>
        public Mod(ArbitraryAdditionalData extraData)
        {
            AdditionalData = extraData;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public Mod() { }

        /// <summary>
        /// Add a <see cref="GameVersion"/> as a supported version.
        /// <seealso cref="SupportedVersions"/>
        /// </summary>
        /// <param name="ver">The <see cref="GameVersion"/> to add.</param>
        public void AddGameVersion([DisallowNull] GameVersion ver)
        {
            if (ver is null)
                throw new ArgumentNullException(nameof(ver));
            if (!SupportedVersions.Contains(ver))
                SupportedVersions.Add(ver);
            if (!ver.SupportedMods.Contains(this))
                ver.SupportedMods.Add(this);
        }

        /// <summary>
        /// Removes a <see cref="GameVersion"/> as a supported version.
        /// <seealso cref="SupportedVersions"/>
        /// </summary>
        /// <param name="ver">The <see cref="GameVersion"/> to remove.</param>
        public void RemoveGameVersion([DisallowNull] GameVersion ver)
        {
            if (ver is null)
                throw new ArgumentNullException(nameof(ver));
            _ = SupportedVersions.Remove(ver);
            _ = ver.SupportedMods.Remove(this);
        }

        /// <summary>
        /// Configures for EF
        /// </summary>
        /// <param name="b"></param>
        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            _ = b.Entity<Mod>()
                .HasMany(m => m.Localizations)
                .WithOne(l => l.OwningMod)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SupportedMods is set up by GameVersion
            _ = b.Entity<Mod>()
                .Property(m => m.Version)
                .HasConversion( // TODO: maybe encode this differently (say in a json structure?)
                    v => v.ToString(),
                    s => new Version(s)
                );
            _ = b.Entity<Mod>()
                .HasIndex(m => new { m.ReadableID, m.Version })
                .IsUnique();
            _ = b.Entity<Mod>()
                .Property(m => m.Uploader)
                .IsVaulthUser();
            _ = b.Entity<Mod>()
                .Property(m => m.Authors)
                .IsValidUsers();
            _ = b.Entity<Mod>()
                .Property(m => m.Contributors)
                .IsValidUsers();
        }
    }

    /// <summary>
    /// A reference of a <see cref="Mod"/>.
    /// </summary>
    public readonly struct ModReference : IEquatable<ModReference>
    {
        /// <summary>
        /// The <see cref="Mod.ReadableID"/> to refer to.
        /// </summary>
        public string ModID { get; }

        /// <summary>
        /// The versions to search at.
        /// </summary>
        [JsonConverter(typeof(VersionRangeJsonConverter))]
        public VersionRange Versions { get; }

        /// <summary>
        /// Construct a reference to a <see cref="Mod"/> to a particular readable ID with a matching <see cref="VersionRange"/>.
        /// </summary>
        /// <param name="modID"></param>
        /// <param name="versions"></param>
        [JsonConstructor]
        public ModReference(string modID, VersionRange versions)
        {
            ModID = modID;
            Versions = versions;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{ModID}@{Versions}";

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj != null && obj is ModReference r && Equals(r);

        /// <inheritdoc/>
        public bool Equals(ModReference other) => other.ModID == ModID && other.Versions == Versions;

        /// <inheritdoc/>
        public override int GetHashCode() => ModID.GetHashCode(StringComparison.InvariantCulture) ^ Versions.GetHashCode();

        /// <summary>
        /// Equals operator
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator ==(ModReference left, ModReference right) => left.Equals(right);

        /// <summary>
        /// Not equals operator
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool operator !=(ModReference left, ModReference right) => !left.Equals(right);
    }
}
