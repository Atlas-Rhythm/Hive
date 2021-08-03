using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hive.Models
{
    /// <summary>
    /// Represents a thin layer over a collection of key value pairs for use as additional data.
    /// This wrapper exists to perform JSON serialization/deserialization for arbitrary types that end up in the database.
    /// </summary>
    public class ArbitraryAdditionalData
    {
        // We can control how we serialize, so we serialize straight to string instead of holding objects.
        // This allows us to avoid strange double parses of json text when dealing with JsonElement deserialization.
        private Dictionary<string, string?> Data { get; } = new();

        /// <summary>
        /// This collection holds objects that need to be serialized and their corresponding serializer options.
        /// This allows us to be performant when adding data, since we only need to serialize all of our data when we save.
        /// Accessing the Data dictionary BEFORE a save has taken place, however, will result in non-existent data, unless we are careful.
        /// This may not be necessary at ALL.
        /// This simply optimzies the time it takes to perform multiple adds in quick succession and decreases the time for multiple adds and gets in sequence.
        /// TODO: This may not be what we want, since it optimizes for cases that do not add outside of the creation step
        /// </summary>
        private List<(string Key, object Data, Type Type, JsonSerializerOptions? Options)> ToWritePool { get; } = new();

        /// <summary>
        /// Gets the instance at a given key, deserializing to a <typeparamref name="T"/> instance.
        /// This function will serialize any pending instances before getting the value at the provided key.
        /// </summary>
        /// <typeparam name="T">The instance the value at this key should be</typeparam>
        /// <param name="key">The key to get from</param>
        /// <param name="opts">Optional deserialization options</param>
        /// <returns>The instance at this key</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="KeyNotFoundException">The key does not exist.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key.</exception>
        public T? Get<T>(string key, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            // Propagate any changes to data
            Propagate();
            var val = Data[key];
            return val is null ? default : JsonSerializer.Deserialize<T>(val, opts);
        }

        /// <summary>
        /// Attempts to get the instance at a given key, deserializing to a <typeparamref name="T"/> if it exists.
        /// This function will serialize any pending instances before attempting to get the value at the provided key.
        /// </summary>
        /// <typeparam name="T">The instance the value at this key should be</typeparam>
        /// <param name="key">The key to get from</param>
        /// <param name="data">The value to write to, if found</param>
        /// <param name="opts">Optional deserialization options</param>
        /// <returns>true if the key is found, false otherwise</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key.</exception>
        public bool TryGetValue<T>(string key, out T? data, JsonSerializerOptions? opts = null)
        {
            // Propagate any changes to data
            Propagate();
            if (!Data.TryGetValue(key, out var strData) || strData is null)
            {
                data = default;
                return false;
            }
            data = JsonSerializer.Deserialize<T>(strData, opts);
            return true;
        }

        /// <summary>
        /// Creates a new value with the provided key. Assigning to existing keys is not permitted.
        /// The value provided to this call is serialized lazily, on demand when serializing or performing a lookup.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="opts"></param>
        public void Add<T>(string key, T? data, JsonSerializerOptions? opts = null)
        {
            if (data is null)
                Data.Add(key, null);
            else
                ToWritePool.Add((key, data, data.GetType(), opts));
        }

        // Ensures the changes from the write pool are properly reflected in the dictionary
        private void Propagate()
        {
            foreach (var (key, data, type, options) in ToWritePool)
            {
                var serialized = JsonSerializer.Serialize(data, type, options);
                Data.Add(key, serialized);
            }
        }

        /// <summary>
        /// A System.Text.Json converter for <see cref="ArbitraryAdditionalData"/>
        /// </summary>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "We nest this because it uses private members.")]
        public class ArbitraryAdditionalDataConverter : JsonConverter<ArbitraryAdditionalData>
        {
            /// <inheritdoc/>
            public override ArbitraryAdditionalData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Must be able to read either a start or a null
                if (reader.TokenType == JsonTokenType.Null)
                    return null;
                else if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();
                var data = new ArbitraryAdditionalData();

                while (reader.Read())
                {
                    // Read until the end of the object, or we fail
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return data;
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var key = reader.GetString();
                        if (key is null)
                            // Keys cannot be null
                            throw new JsonException();
                        // Values can be null
                        data.Data.Add(key, reader.GetString());
                    }
                }
                // We shouldn't hit this point unless we have bad json
                throw new JsonException();
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, ArbitraryAdditionalData value, JsonSerializerOptions options)
            {
                if (writer is null)
                    throw new ArgumentNullException(nameof(writer));
                if (value is null)
                    // We do not permit serializing null instances of ArbitraryAdditionalData, they MUST exist.
                    throw new ArgumentNullException(nameof(value));
                // We must ensure that all data is propagated!
                value.Propagate();
                writer.WriteStartObject();
                var data = value.Data;
                foreach (var (key, v) in data)
                {
                    writer.WriteString(key, v);
                }
                writer.WriteEndObject();
            }
        }
    }
}
