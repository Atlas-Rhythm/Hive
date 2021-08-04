using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Hive.Converters;
using NodaTime;
using static Hive.Models.ArbitraryAdditionalData;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of a <see cref="GameVersion"/>.
    /// </summary>
    public record SerializedGameVersion
    {
        /// <summary>
        /// The name of the GameVersion
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// The <see cref="Instant"/> this GameVersion was created
        /// </summary>
        [JsonConverter(typeof(NodaInstantWrapper))]
        public Instant CreationTime { get; set; }

        /// <summary>
        /// Additional data associated with the GameVersion
        /// </summary>
        [JsonConverter(typeof(ArbitraryAdditionalDataConverter))]
        public ArbitraryAdditionalData AdditionalData { get; init; } = new();

        /// <summary>
        /// Serialize a <see cref="GameVersion"/> into a <see cref="SerializedGameVersion"/>.
        /// </summary>
        /// <param name="toSerialize">The game version to serialize.</param>
        /// <returns>The created serialized game version.</returns>
        public static SerializedGameVersion Serialize([DisallowNull] GameVersion toSerialize)
        {
            return toSerialize is null
                ? throw new ArgumentNullException(nameof(toSerialize))
                : new SerializedGameVersion()
                {
                    Name = toSerialize.Name,
                    CreationTime = toSerialize.CreationTime,
                    AdditionalData = toSerialize.AdditionalData
                };
        }
    }
}
