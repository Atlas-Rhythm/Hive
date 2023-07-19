using System.Collections.Immutable;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// Mod data used for updating a specific localized version of a mod.
    /// </summary>
    public class SerializedModUpdate
    {
        /// <summary>
        /// The ID of the <see cref="Mod"/>.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// The version of the <see cref="Mod"/>.
        /// </summary>
        public string Version { get; init; } = null!;

        /// <summary>
        /// The <see cref="SerializedLocalizedModInfo"/> of the <see cref="Mod"/>.
        /// </summary>
        public SerializedLocalizedModInfo? LocalizedModInfo { get; init; } = null!;

        /// <summary>
        /// The supported game versions of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<string> SupportedGameVersions { get; init; } = null!;

        /// <summary>
        /// The dependencies (a list of <see cref="ModReference"/> objects) of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<ModReference> Dependencies { get; init; } = null!;

        /// <summary>
        /// The conflicts (a list of <see cref="ModReference"/> objects) of the <see cref="Mod"/>
        /// </summary>
        public ImmutableList<ModReference> ConflictsWith { get; init; } = null!;

        /// <summary>
        /// The links of the <see cref="Mod"/>
        /// </summary>
        public ImmutableList<Link> Links { get; init; } = null!;
    }
}
