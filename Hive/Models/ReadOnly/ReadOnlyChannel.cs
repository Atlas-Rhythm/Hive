using System;
using System.Text.Json;

namespace Hive.Models.ReadOnly
{
    /// <summary>
    /// A readonly copy of <see cref="Channel"/>, for passing into Hive plugins.
    /// </summary>
    public readonly struct ReadOnlyChannel : IEquatable<ReadOnlyChannel>
    {
        public readonly string Name { get; }

        public readonly JsonElement? AdditionalData { get; }

        public ReadOnlyChannel(Channel origin)
        {
            if (origin is null)
            {
                throw new ArgumentException($"Given channel is null.");
            }

            Name = origin.Name;
            AdditionalData = origin.AdditionalData;
        }

        public bool Equals(ReadOnlyChannel other) => Name == other.Name;

        public override bool Equals(object? obj) => obj is ReadOnlyChannel readOnlyChannel && Equals(readOnlyChannel);

        public override int GetHashCode() => Name.GetHashCode(StringComparison.InvariantCulture);

        public static bool operator ==(ReadOnlyChannel left, ReadOnlyChannel right) => left.Equals(right);

        public static bool operator !=(ReadOnlyChannel left, ReadOnlyChannel right) => !(left == right);
    }
}
