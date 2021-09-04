using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace Hive.Converters
{
    /// <summary>
    /// Bare wrapper over <see cref="NodaConverters.InstantConverter"/>
    /// </summary>
    public class NodaInstantWrapper : JsonConverter<Instant>
    {
        /// <inheritdoc/>
        public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => NodaConverters.InstantConverter.Read(ref reader, typeToConvert, options);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options) => NodaConverters.InstantConverter.Write(writer, value, options);
    }
}
