using SemVer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VerRange = SemVer.Range;

namespace Hive.Converters
{
    public class JsonVersionRangeConverter : JsonConverter<VerRange>
    {
        [return: MaybeNull]
        public override VerRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            return new VerRange(reader.GetString()!, false);
        }

        public override void Write(Utf8JsonWriter writer, VerRange value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
