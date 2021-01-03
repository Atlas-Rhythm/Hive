using System.Collections.Generic;

namespace Hive.Models
{
    /// <summary>
    /// The result of a dependency resolution.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The type of these are ALWAYS List<T> and may need to be modified.")]
    public class DependencyResolutionResult
    {
        /// <summary>
        /// The message from the dependency resolution.
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// The list of input mods, as well as additional <see cref="Mod"/> objects necessary for a successful resolution.
        /// </summary>
        public List<Mod> AdditionalMods { get; } = new List<Mod>();

        /// <summary>
        /// The list of <see cref="ModReference"/>s that do not exist in the database.
        /// </summary>
        public List<ModReference> MissingMods { get; } = new List<ModReference>();

        /// <summary>
        /// The list of mod IDs that are conflicting and should be removed for a successful resolution.
        /// </summary>
        public List<string> ConflictingMods { get; } = new List<string>();

        /// <summary>
        /// The list of <see cref="ModReference"/> objects that have version mismatches.
        /// </summary>
        public List<ModReference> VersionMismatches { get; } = new List<ModReference>();
    }
}
