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

        public SerializedLocalizedModInfo LocalizedModInfo { get; init; } = null!;

        public List<string> Authors { get; } = new List<string>();

        public List<string> Contributors { get; } = new List<string>();

        public List<string> SupportedGameVersions { get; } = new List<string>();

        public List<(string, string)> Links { get; } = new List<(string, string)>();

        public List<ModReference> Dependencies { get; } = new List<ModReference>();

        public List<ModReference> ConflictsWith { get; } = new List<ModReference>();

        // all AdditionalData fields are public, yet readonly.
        public JsonElement AdditionalData { get; init; }
    }
}
