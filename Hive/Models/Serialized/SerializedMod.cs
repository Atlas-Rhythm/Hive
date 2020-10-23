﻿using Hive.Converters;
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
    public class SerializedMod
    {
        public string ID { get; init; } = null!;

        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; init; } = null!;

        public string UploadedAt { get; init; } = null!;

        public string EditedAt { get; init; } = null!;

        public string UploaderUsername { get; init; } = null!;

        public string ChannelName { get; init; } = null!;

        public string DownloadLink { get; init; } = null!;

        public SerializedLocalizedModInfo LocalizedModInfo { get; init; } = null!;

        public IImmutableList<string> Authors { get; init; } = null!;

        public IImmutableList<string> Contributors { get; init; } = null!;

        public IImmutableList<string> SupportedGameVersions { get; init; } = null!;

        public IImmutableList<(string, string)> Links { get; init; } = null!;

        public IImmutableList<ModReference> Dependencies { get; init; } = null!;

        public IImmutableList<ModReference> ConflictsWith { get; init; } = null!;

        // all AdditionalData fields are public, yet readonly.
        public JsonElement AdditionalData { get; init; }

        public static SerializedMod Serialize(Mod toSerialize, LocalizedModInfo localizedModInfo)
        {
            if (toSerialize is null) throw new ArgumentException($"{nameof(toSerialize)} is null.");
            var serialized = new SerializedMod()
            {
                ID = toSerialize.ReadableID,
                Version = toSerialize.Version,
                UploadedAt = toSerialize.UploadedAt.ToString(),
                EditedAt = toSerialize.EditedAt?.ToString()!,
                UploaderUsername = toSerialize.Uploader.Name!,
                ChannelName = toSerialize.Channel.Name,
                DownloadLink = toSerialize.DownloadLink.ToString(),
                LocalizedModInfo = SerializedLocalizedModInfo.Serialize(localizedModInfo),
                AdditionalData = toSerialize.AdditionalData,
                Authors = toSerialize.Authors.Select(x => x.Name!).ToImmutableList(),
                Contributors = toSerialize.Contributors.Select(x => x.Name!).ToImmutableList(),
                SupportedGameVersions = toSerialize.SupportedVersions.Select(x => x.Name!).ToImmutableList(),
                Links = toSerialize.Links.Select(x => (x.Name, x.Url.ToString()))!.ToImmutableList(),
                Dependencies = toSerialize.Dependencies.ToImmutableList(),
                ConflictsWith = toSerialize.Conflicts.ToImmutableList()
            };
            return serialized;
        }
    }
}
