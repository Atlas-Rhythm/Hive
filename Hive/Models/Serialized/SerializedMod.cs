using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Version = Hive.Versioning.Version;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of a <see cref="Mod"/>.
    /// </summary>
    public class SerializedMod
    {
        public string ID { get; init; } = null!;

        public Version Version { get; init; } = null!;

        public string UpdatedAt { get; init; } = null!;

        public string EditedAt { get; init; } = null!;

        public string UploaderUsername { get; init; } = null!;

        public string ChannelName { get; init; } = null!;

        public string DownloadLink { get; init; } = null!;

        public SerializedLocalizedModInfo LocalizedModInfo { get; init; } = null!;

        public IReadOnlyList<string> Authors { get; init; } = new List<string>();

        public IReadOnlyList<string> Contributors { get; init; } = new List<string>();

        public IReadOnlyList<string> SupportedGameVersions { get; init; } = new List<string>();

        public IReadOnlyList<(string, string)> Links { get; init; } = new List<(string, string)>();

        public IReadOnlyList<ModReference> Dependencies { get; init; } = new List<ModReference>();

        public IReadOnlyList<ModReference> ConflictsWith { get; init; } = new List<ModReference>();

        // all AdditionalData fields are public, yet readonly.
        public JsonElement AdditionalData { get; init; }

        public static SerializedMod Serialize(Mod toSerialize, LocalizedModInfo localizedModInfo)
        {
            if (toSerialize is null) throw new ArgumentException($"{nameof(toSerialize)} is null.");
            var serialized = new SerializedMod()
            {
                ID = toSerialize.ReadableID,
                Version = toSerialize.Version,
                UpdatedAt = toSerialize.UploadedAt.ToString(),
                EditedAt = toSerialize.EditedAt?.ToString()!,
                UploaderUsername = toSerialize.Uploader.Name!,
                ChannelName = toSerialize.Channel.Name,
                DownloadLink = toSerialize.DownloadLink.ToString(),
                LocalizedModInfo = SerializedLocalizedModInfo.Serialize(localizedModInfo),
                AdditionalData = toSerialize.AdditionalData,
                Authors = toSerialize.Authors.Select(x => x.Name!).ToList(),
                Contributors = toSerialize.Contributors.Select(x => x.Name!).ToList(),
                SupportedGameVersions = toSerialize.SupportedVersions.Select(x => x.Name!).ToList(),
                Links = toSerialize.Links.Select(x => (x.Name, x.Url.ToString()))!.ToList(),
                Dependencies = toSerialize.Dependencies.ToList(),
                ConflictsWith = toSerialize.Conflicts.ToList()
            };
            return serialized;
        }
    }
}
