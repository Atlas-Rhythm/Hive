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

        public override void Write(Utf8JsonWriter writer, VersionRange value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
