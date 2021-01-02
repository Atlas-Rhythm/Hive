using System;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of <see cref="LocalizedModInfo"/>.
    /// </summary>
    public class SerializedLocalizedModInfo
    {
        /// <summary>
        /// The language ID of the <see cref="LocalizedModInfo"/>.
        /// </summary>
        public string Language { get; init; } = null!;

        /// <summary>
        /// The name of the <see cref="Mod"/> described by a <see cref="LocalizedModInfo"/>.
        /// </summary>
        public string Name { get; init; } = null!;

        /// <summary>
        /// The description of the <see cref="Mod"/> described by a <see cref="LocalizedModInfo"/>.
        /// </summary>
        public string Description { get; init; } = null!;

        /// <summary>
        /// The changelog of the <see cref="Mod"/> described by a <see cref="LocalizedModInfo"/>.
        /// </summary>
        public string Changelog { get; init; } = null!;

        /// <summary>
        /// The credits of the <see cref="Mod"/> described by a <see cref="LocalizedModInfo"/>.
        /// </summary>
        public string Credits { get; init; } = null!;

        /// <summary>
        /// Serialize a <see cref="LocalizedModInfo"/> into a <see cref="SerializedLocalizedModInfo"/>.
        /// </summary>
        /// <param name="toSerialize">The localized information to serialize.</param>
        /// <returns>The created serialized localized information.</returns>
        public static SerializedLocalizedModInfo Serialize(LocalizedModInfo toSerialize)
        {
            return toSerialize is null
                ? throw new ArgumentNullException(nameof(toSerialize))
                : new SerializedLocalizedModInfo()
                {
                    Language = toSerialize.Language,
                    Name = toSerialize.Name,
                    Changelog = toSerialize.Changelog!,
                    Credits = toSerialize.Credits!,
                    Description = toSerialize.Description,
                };
        }
    }
}
