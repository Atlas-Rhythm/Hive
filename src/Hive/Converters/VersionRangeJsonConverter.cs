using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hive.Versioning;

namespace Hive.Converters
{
    /// <summary>
    /// A <see cref="JsonConverter{T}"/> for <see cref="VersionRange"/>
    /// </summary>
    public class VersionRangeJsonConverter : JsonConverter<VersionRange>
    {
        /// <inheritdoc/>
        [return: MaybeNull]
        public override VersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.Null
                ? null
                : reader.TokenType != JsonTokenType.String ? throw new JsonException() : new VersionRange(reader.GetString()!);
        }

        /// <inheritdoc/>
        public override void Write([DisallowNull] Utf8JsonWriter writer, [DisallowNull] VersionRange value, JsonSerializerOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            writer.WriteStringValue(value.ToString());
        }
    }
}
