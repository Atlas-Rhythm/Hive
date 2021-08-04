using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using static Hive.Models.ArbitraryAdditionalData;

namespace Hive.Models.ReadOnly
{
    /// <summary>
    /// A readonly copy of <see cref="Channel"/>, for passing into Hive plugins.
    /// </summary>
    public readonly struct ReadOnlyChannel : IEquatable<ReadOnlyChannel>
    {
        /// <summary>
        /// The name of the <see cref="Channel"/>.
        /// </summary>
        public readonly string Name { get; }

        /// <summary>
        /// The additional data from the <see cref="Channel"/>.
        /// </summary>
        [JsonConverter(typeof(ArbitraryAdditionalDataConverter))]
        public readonly ArbitraryAdditionalData AdditionalData { get; }

        /// <summary>
        /// Create a read-only version of a <see cref="Channel"/>.
        /// </summary>
        /// <param name="origin"></param>
        public ReadOnlyChannel([DisallowNull] Channel origin)
        {
            if (origin is null) throw new ArgumentNullException(nameof(origin));

            Name = origin.Name;
            AdditionalData = origin.AdditionalData;
        }

        /// <inheritdoc/>
        public bool Equals(ReadOnlyChannel other) => Name == other.Name;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is ReadOnlyChannel readOnlyChannel && Equals(readOnlyChannel);

        /// <inheritdoc/>
        public override int GetHashCode() => Name.GetHashCode(StringComparison.InvariantCulture);

        /// <summary>
        /// Performs the <see cref="Equals(ReadOnlyChannel)"/> comparison on the channels.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the left channel is equal to the right.</returns>
        public static bool operator ==(ReadOnlyChannel left, ReadOnlyChannel right) => left.Equals(right);

        /// <summary>
        /// Performs an <see cref="Equals(ReadOnlyChannel)"/> comparison on the channels and negates the result.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>If the left channel is not equal to the right.</returns>
        public static bool operator !=(ReadOnlyChannel left, ReadOnlyChannel right) => !(left == right);
    }
}
