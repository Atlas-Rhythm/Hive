using System.Collections.Generic;

namespace Hive.Models
{
    public class DependencyResolutionResult
    {
        public string Message { get; set; } = null!;
        public List<Mod> AdditionalMods { get; } = new List<Mod>();
        public List<ModReference> MissingMods { get; } = new List<ModReference>();
        public List<ModReference> ConflictingMods { get; } = new List<ModReference>();
        public List<ModReference> VersionMismatches { get; } = new List<ModReference>();
    }
}
