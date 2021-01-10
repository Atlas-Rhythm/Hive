using Hive.Converters;
using NodaTime;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// The ID of the <see cref="Mod"/>.
        /// </summary>
        public string ID { get; init; } = null!;

        /// <summary>
        /// The <see cref="Versioning.Version"/> of the <see cref="Mod"/>.
        /// </summary>
        [JsonConverter(typeof(VersionJsonConverter))]
        public Version Version { get; init; } = null!;

        /// <summary>
        /// The <see cref="string"/> timestamp of when this <see cref="Mod"/> was uploaded.
        /// </summary>
        public Instant UploadedAt { get; init; }

        /// <summary>
        /// The <see cref="string"/> timestamp of when this <see cref="Mod"/> was last edited.
        /// </summary>
        public Instant? EditedAt { get; init; }

        /// <summary>
        /// The username of the uploader.
        /// </summary>
        public string UploaderUsername { get; init; } = null!;

        /// <summary>
        /// The channel name of the <see cref="Mod"/>.
        /// </summary>
        public string ChannelName { get; init; } = null!;

        /// <summary>
        /// The download link of the <see cref="Mod"/>, as a <see cref="string"/>.
        /// </summary>
        public string DownloadLink { get; init; } = null!;

        /// <summary>
        /// The <see cref="SerializedLocalizedModInfo"/> of the <see cref="Mod"/>.
        /// </summary>
        public SerializedLocalizedModInfo LocalizedModInfo { get; init; } = null!;

        /// <summary>
        /// The authors of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<string> Authors { get; init; } = null!;

        /// <summary>
        /// The contributors of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<string> Contributors { get; init; } = null!;

        /// <summary>
        /// The supported game versions of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<string> SupportedGameVersions { get; init; } = null!;

        /// <summary>
        /// The links provided of the <see cref="Mod"/>, with the left hand side as the name and the right hand side as the url.
        /// </summary>
        public ImmutableList<(string, string)> Links { get; init; } = null!;

        /// <summary>
        /// The dependencies (a list of <see cref="ModReference"/> objects) of the <see cref="Mod"/>.
        /// </summary>
        public ImmutableList<ModReference> Dependencies { get; init; } = null!;

        /// <summary>
        /// The conflicts (a list of <see cref="ModReference"/> objects) of the <see cref="Mod"/>
        /// </summary>
        public ImmutableList<ModReference> ConflictsWith { get; init; } = null!;

        /// <summary>
        /// The additional data associated with this <see cref="Mod"/>.
        /// All properties are public and readonly for all people who get mods.
        /// </summary>
        public JsonElement AdditionalData { get; init; }

        /// <summary>
        /// Serialize a <see cref="Mod"/> with an associated <see cref="Models.LocalizedModInfo"/> into a <see cref="SerializedMod"/>.
        /// </summary>
        /// <param name="toSerialize">The mod to serialize.</param>
        /// <param name="localizedModInfo">The localized information to serialize.</param>
        /// <returns>The created serialized mod.</returns>
        public static SerializedMod Serialize([DisallowNull] Mod toSerialize, [DisallowNull] LocalizedModInfo localizedModInfo)
        {
            if (toSerialize is null) throw new ArgumentNullException(nameof(toSerialize));
            if (localizedModInfo is null) throw new ArgumentNullException(nameof(localizedModInfo));
            var serialized = new SerializedMod()
            {
                ID = toSerialize.ReadableID,
                Version = toSerialize.Version,
                UploadedAt = toSerialize.UploadedAt,
                EditedAt = toSerialize.EditedAt,
                UploaderUsername = toSerialize.Uploader?.Username!,
                ChannelName = toSerialize.Channel?.Name!,
                DownloadLink = toSerialize.DownloadLink?.ToString()!,
                LocalizedModInfo = SerializedLocalizedModInfo.Serialize(localizedModInfo),
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
