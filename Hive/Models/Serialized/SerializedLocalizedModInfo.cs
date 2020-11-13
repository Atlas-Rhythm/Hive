using System;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of <see cref="LocalizedModInfo"/>.
    /// </summary>
    public record SerializedLocalizedModInfo
    {
        public string Language { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string Description { get; init; } = null!;

        public string? Changelog { get; init; }

        public string? Credits { get; init; }

        public static SerializedLocalizedModInfo Serialize(LocalizedModInfo toSerialize)
        {
            if (toSerialize is null) throw new ArgumentException($"{nameof(toSerialize)} is null.");
            return new SerializedLocalizedModInfo()
            {
                Language = toSerialize.Language,
                Name = toSerialize.Name,
                Changelog = toSerialize.Changelog,
                Credits = toSerialize.Credits,
                Description = toSerialize.Description,
            };
        }
    }
}
