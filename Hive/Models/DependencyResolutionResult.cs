using System.Collections.Generic;

namespace Hive.Models
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "The type of these are ALWAYS List<T> and may need to be modified.")]
    public class DependencyResolutionResult
    {
        public string Message { get; set; } = null!;

        public List<Mod> AdditionalMods { get; } = new List<Mod>();
        public List<ModReference> MissingMods { get; } = new List<ModReference>();
        public List<string> ConflictingMods { get; } = new List<string>();
        public List<ModReference> VersionMismatches { get; } = new List<ModReference>();
    }
}
