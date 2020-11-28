namespace Hive.Models
{
    /// <summary>
    /// An identifier for a particular mod ID and version.
    /// </summary>
    /// <seealso cref="Mod"/>
    public class ModIdentifier
    {
        /// <summary>
        /// The ID of the mod.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// The Version of the mod.
        /// </summary>
        public string Version { get; init; } = null!;
    }
}