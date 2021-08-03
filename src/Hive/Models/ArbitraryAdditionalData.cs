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
        /// <summary>
        /// A small reference type that we use for the value type of our data dictionary.
        /// This type holds the data, the type of the data, and the serializer options.
        /// </summary>
        private class InnerData
        {
            /// <summary>
            /// Object may be null, or a valid non-null instance.
            /// </summary>
            internal object? Object { get; set; }

            /// <summary>
            /// Type is always non-null. Is the last known serialization/deserialization type for this particular instance.
            /// </summary>
            internal Type Type { get; set; } = null!;

            /// <summary>
            /// Options is the last known serialization/deserialization options used on this particular instance.
            /// </summary>
            internal JsonSerializerOptions? Options { get; set; }
        }

        /// <summary>
        /// We can control how we serialize, which we perform in our converter.
        /// As such, we can hold references here and how to serialize them so that we can ensure reference type changes persist.
        /// </summary>
        private Dictionary<string, InnerData> Data { get; } = new();

        /// <summary>
        /// The data that we deserialize into, which we then back-convert into InnerData instances in Data.
        /// This dictionary is only ever the ground truth for information that hasn't been added or gotten from previously.
        /// </summary>
        private Dictionary<string, string> SerializedData { get; } = new();

        /// <summary>
        /// Gets the instance at a given key, deserializing to a <typeparamref name="T"/> instance.
        /// </summary>
        /// <typeparam name="T">The instance the value at this key should be</typeparam>
        /// <param name="key">The key to get from</param>
        /// <returns>The instance at this key</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="KeyNotFoundException">The key does not exist.</exception>
        public T? Get<T>(string key, JsonSerializerOptions? opts = null) => (T?)Get(key, typeof(T), opts);

        public object? Get(string key, Type type, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                // We do not allow null keys
                throw new ArgumentNullException(nameof(key));
            // When we get data, we first check our serialization set for a match
            if (!Data.TryGetValue(key, out var data))
            {
                // If we don't have the data already, we MUST have it in our serialized data.
                // Otherwise, we throw here via []
                var obj = JsonSerializer.Deserialize(SerializedData[key], type, opts);
                // If we have a value AND we do NOT have a value in our data, we deserialize and assign.
                Data.Add(key, new InnerData { Object = obj, Type = type, Options = opts });
                return obj;
            }
            // If we have found the value, we need to ensure the types are assignable in some way
            // Then, we ensure our data's type and serializer are set accordingly.
            var oldType = data.Type;
            if (!oldType.IsAssignableFrom(type) && !oldType.IsAssignableTo(type))
                throw new InvalidCastException($"Previously serialized type: {oldType} is not convertible in either direction to desired type: {type}!");
            data.Type = type;
            data.Options = opts;
            return data.Object;
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
            if (TryGetValue(key, out var obj, typeof(T), opts))
            {
                data = (T?)obj;
                return true;
            }
            data = default;
            return false;
        }

        public bool TryGetValue(string key, out object? data, Type type, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                // We do not allow null keys
                throw new ArgumentNullException(nameof(key));
            if (!Data.TryGetValue(key, out var innerData))
            {
                // If we don't find it in our data, search in our serialized data
                if (!SerializedData.TryGetValue(key, out var serialized))
                {
                    data = null;
                    return false;
                }
                data = JsonSerializer.Deserialize(serialized, type, opts);
                // If we have a value AND we do NOT have a value in our data, we deserialize and assign.
                Data.Add(key, new InnerData { Object = data, Type = type, Options = opts });
                return true;
            }
            var oldType = innerData.Type;
            if (!oldType.IsAssignableFrom(type) && !oldType.IsAssignableTo(type))
                throw new InvalidCastException($"Previously serialized type: {oldType} is not convertible in either direction to desired type: {type}!");
            innerData.Type = type;
            innerData.Options = opts;
            data = innerData.Object;
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
        public void Add<T>(string key, T? data, JsonSerializerOptions? opts = null) => Add(key, typeof(T), data, opts);

        public void Add(string key, Type type, object? data, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            // Before we can simply add to the dictionary, we need to make sure no existing keys exist.
            if (SerializedData.ContainsKey(key))
            {
                // We do not attempt to deserialize the instance in the serialized list because we do not know how.
                // We do not want to guess and use the provided options, we know we must throw anyways.
                throw new ArgumentException("Key already exists", nameof(key));
            }
            Data.Add(key, new InnerData { Object = data, Type = type, Options = opts });
        }

        public void Set(string key, Type type, object? data, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (!Data.TryGetValue(key, out var innerData))
            {
                // Set is a bit tricky. Because the type we are attempting to load may not necessarily be the known deserializable type.
                // We pass in two sets of options to allow differences in serialization/deserialization
                // But there is no guarantee that the type we are setting is the exact same type that already exists, should we need to deserialize.
                // We have two options:
                // 1. We can guess that the type we need to deser to is type, or
                // 2. We can avoid deser entirely and force people to call Get at least once before calling Set.
                // Open to either, there will be weirdness in either case.
                // It's also unclear if Set is even a desirable API function to have, as opposed to Get on reference types and modify inline.

                // For now, we shall simply throw, because it's far easier.
                throw new InvalidOperationException("Cannot deserialize key! Call Get with a specific type before calling Set!");
            }
            var oldType = innerData.Type;
            if (oldType is not null && !oldType.IsAssignableFrom(type) && !oldType.IsAssignableTo(type))
                throw new InvalidCastException($"Previously serialized type: {oldType} is not convertible in either direction to desired type: {type}!");
            innerData.Type = type;
            innerData.Options = opts;
            innerData.Object = data;
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
                        // Values can be null, but are instead serialized as "null"
                        var value = reader.GetString();
                        if (value is null)
                            throw new JsonException();
                        data.SerializedData.Add(key, value);
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
                // We must collect data from both our serialized data (which may have been from a previous deserialize)
                // and any new data we have provided.
                writer.WriteStartObject();
                foreach (var (key, v) in value.Data)
                {
                    writer.WriteString(key, JsonSerializer.Serialize(v.Object, v.Type, v.Options));
                }
                // After writing all guaranteed, known, instances, we need to check our serialized data and write any that haven't been written.
                foreach (var (key, v) in value.SerializedData)
                {
                    if (value.Data.ContainsKey(key))
                        continue;
                    writer.WriteString(key, v);
                }
                writer.WriteEndObject();
            }
        }
    }
}
