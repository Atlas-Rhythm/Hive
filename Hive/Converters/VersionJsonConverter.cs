using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Version = Hive.Versioning.Version;

namespace Hive.Converters
{
    /// <summary>
    /// A <see cref="JsonConverter{T}"/> for <see cref="Version"/>.
    /// </summary>
    public class VersionJsonConverter : JsonConverter<Version>
    {
        /// <inheritdoc/>
        [return: MaybeNull]
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.Null
                ? null
                : reader.TokenType != JsonTokenType.String ? throw new JsonException() : new Version(reader.GetString()!);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            writer.WriteStringValue(value.ToString());
        }
    }
}
