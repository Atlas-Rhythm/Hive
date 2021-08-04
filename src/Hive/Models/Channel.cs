using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static Hive.Models.ArbitraryAdditionalData;

namespace Hive.Models
{
    /// <summary>
    /// Represents a channel of mods.
    /// </summary>
    public class Channel : IEquatable<Channel>
    {
        /// <summary>
        /// Name of the Channel
        /// </summary>
        [Key]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Additional data associated with the Channel
        /// </summary>
        [Column(TypeName = "jsonb")]
        [JsonConverter(typeof(ArbitraryAdditionalDataConverter))]
        public ArbitraryAdditionalData AdditionalData { get; } = new();

        /// <summary>
        /// Equality comparison
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Channel? other) => other is not null && other.Name == Name;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => Equals(obj as Channel);

        /// <inheritdoc/>
        public override int GetHashCode() => Name.GetHashCode(StringComparison.InvariantCulture);
    }
}
