namespace Hive.Models
{
    /// <summary>
    /// An ID and version pair to identify a <see cref="Mod"/>
    /// </summary>
    public class ModIdentifier
    {
        /// <summary>
        /// The ID to identify a <see cref="Mod"/> with.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// The Version to identify a <see cref="Mod"/> with.
        /// </summary>
        public string Version { get; init; } = null!;
    }
}
