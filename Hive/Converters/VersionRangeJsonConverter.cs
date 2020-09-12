using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Hive.Versioning;

namespace Hive.Converters
{
    public class VersionRangeJsonConverter : JsonConverter<VersionRange>
    {
        [return: MaybeNull]
        public override VersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            return new VersionRange(reader.GetString()!);
        }

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