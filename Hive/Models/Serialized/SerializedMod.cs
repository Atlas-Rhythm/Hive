using Hive.Versioning;
using System.Collections.Generic;
using System.Text.Json;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of a <see cref="Mod"/>.
    /// </summary>
    public class SerializedMod
    {
        public string Name { get; init; } = null!;

        public Version Version { get; init; } = null!;

        public string UpdatedAt { get; init; } = null!;

        public string EditedAt { get; init; } = null!;

        public string UploaderUsername { get; init; } = null!;

        public string ChannelName { get; init; } = null!;

        public string DownloadLink { get; init; } = null!;

        public IList<string> Authors { get; } = new List<string>();

        public IList<string> Contributors { get; } = new List<string>();

        public IList<string> SupportedGameVersions { get; } = new List<string>();

        public IList<(string, string)> Links { get; } = new List<(string, string)>();

        public IList<ModReference> Dependencies { get; } = new List<ModReference>();

        public IList<ModReference> ConflictsWith { get; } = new List<ModReference>();

        // all AdditionalData fields are public, yet readonly.
        public JsonElement AdditionalData { get; init; }
    }
}
