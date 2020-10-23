﻿using System.Globalization;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of <see cref="LocalizedModInfo"/>.
    /// </summary>
    public class SerializedLocalizedModInfo
    {
        public string Language { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string Description { get; init; } = null!;

        public string Changelog { get; init; } = null!;

        public string Credits { get; init; } = null!;

        public static SerializedLocalizedModInfo Serialize(LocalizedModInfo toSerialize)
        {
            if (toSerialize is null) return null!;
            return new SerializedLocalizedModInfo()
            {
                Language = toSerialize.Language.Name,
                Name = toSerialize.Name,
                Changelog = toSerialize.Changelog!,
                Credits = toSerialize.Credits!,
                Description = toSerialize.Description,
            };
        }
    }
}
