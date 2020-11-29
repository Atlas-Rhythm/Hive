using Hive.Converters;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Version = Hive.Versioning.Version;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of a <see cref="Mod"/>.
    /// </summary>
    public record SerializedMod
    {
        public string ID { get; init; } = null!;

        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; init; } = null!;

        public Instant UploadedAt { get; init; }

        public Instant? EditedAt { get; init; }

        public string UploaderUsername { get; init; } = null!;

        public string ChannelName { get; init; } = null!;

        public string DownloadLink { get; init; } = null!;

        public SerializedLocalizedModInfo? LocalizedModInfo { get; init; } = null!;

        public ImmutableList<string> Authors { get; init; } = null!;

        public ImmutableList<string> Contributors { get; init; } = null!;

        public ImmutableList<string> SupportedGameVersions { get; init; } = null!;

        public ImmutableList<(string, string)> Links { get; init; } = null!;

        public ImmutableList<ModReference> Dependencies { get; init; } = null!;

        public ImmutableList<ModReference> ConflictsWith { get; init; } = null!;

        // all AdditionalData fields are public, yet readonly.
        public JsonElement AdditionalData { get; init; }

        public static SerializedMod Serialize(Mod toSerialize, LocalizedModInfo? localizedModInfo)
        {
            if (toSerialize is null) throw new ArgumentException($"{nameof(toSerialize)} is null.");
            var serialized = new SerializedMod()
            {
                ID = toSerialize.ReadableID,
                Version = toSerialize.Version,
                UploadedAt = toSerialize.UploadedAt,
                EditedAt = toSerialize.EditedAt,
                UploaderUsername = toSerialize.Uploader?.Username!,
                ChannelName = toSerialize.Channel?.Name!,
                DownloadLink = toSerialize.DownloadLink?.ToString()!,
                LocalizedModInfo = localizedModInfo is not null ? SerializedLocalizedModInfo.Serialize(localizedModInfo) : null,
                AdditionalData = toSerialize.AdditionalData,
                Authors = toSerialize.Authors.Select(x => x.Username).ToImmutableList(),
                Contributors = toSerialize.Contributors.Select(x => x.Username).ToImmutableList(),
                SupportedGameVersions = toSerialize.SupportedVersions.Select(x => x.Name!).ToImmutableList(),
                Links = toSerialize.Links.Select(x => (x.Name, x.Url.ToString()))!.ToImmutableList(),
                Dependencies = toSerialize.Dependencies.ToImmutableList(),
                ConflictsWith = toSerialize.Conflicts.ToImmutableList()
            };
            return serialized;
        }
    }
}
