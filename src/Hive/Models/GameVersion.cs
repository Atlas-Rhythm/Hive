using Hive.Converters;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using static Hive.Models.ArbitraryAdditionalData;

namespace Hive.Models
{
    /// <summary>
    /// Represents a particular version of a game
    /// </summary>
    public class GameVersion : IEquatable<GameVersion>
    {
        /// <summary>
        /// The name of the GameVersion
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Additional data associated with the GameVersion
        /// </summary>
        [Column(TypeName = "jsonb")]
        [JsonConverter(typeof(ArbitraryAdditionalDataConverter))]
        public ArbitraryAdditionalData AdditionalData { get; } = new();

        /// <summary>
        /// The <see cref="Instant"/> this GameVersion was created
        /// </summary>
        [JsonConverter(typeof(NodaInstantWrapper))]
        public Instant CreationTime { get; set; }

        /// <summary>
        /// The collection of mods that are supported by this GameVersion
        /// </summary>
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF wants a setter")]
        public virtual ICollection<Mod> SupportedMods { get; set; } = new List<Mod>();

        #region DB Schema stuff

        /// <summary>
        /// The Guid of this GameVersion, used as a primary key
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "We use Guid as a name. Could be changed in the future.")]
        public Guid Guid { get; set; }

        #endregion DB Schema stuff

        /// <summary>
        /// Construct a game version with additional data
        /// </summary>
        /// <param name="extraData">The additional data to assign.</param>
        public GameVersion(ArbitraryAdditionalData extraData)
        {
            AdditionalData = extraData;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GameVersion() { }

        /// <summary>
        /// Configure for EF
        /// </summary>
        /// <param name="b"></param>
        public static void Configure([DisallowNull] ModelBuilder b)
        {
            if (b is null)
                throw new ArgumentNullException(nameof(b));
            _ = b.Entity<GameVersion>()
                .HasIndex(v => new { v.Name })
                .IsUnique();

            _ = b.Entity<GameVersion>()
                .HasMany(m => m.SupportedMods)
                .WithMany(v => v.SupportedVersions)
                .UsingEntity(b =>
                {
                });
        }

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(GameVersion? other) => other is not null && other.Guid == Guid;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as GameVersion);

        /// <inheritdoc/>
        public override int GetHashCode() => Guid.GetHashCode();
    }
}
