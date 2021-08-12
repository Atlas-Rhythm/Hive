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
            /// Type is always non-null. Type is the most specific known type that has been serialized/deserialized at this key.
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
        private readonly Dictionary<string, InnerData> data = new();

        /// <summary>
        /// The data that we deserialize into, which we then back-convert into InnerData instances in Data.
        /// This dictionary is only ever the ground truth for information that hasn't been added or gotten from previously.
        /// </summary>
        private readonly Dictionary<string, JsonElement> serializedData = new();

        /// <summary>
        /// Gets the instance at a given key, deserializing to a <typeparamref name="T"/> instance, if necessary.
        /// </summary>
        /// <typeparam name="T">The instance the value at this key should be.</typeparam>
        /// <param name="key">The key to get from.</param>
        /// <param name="opts">The optional deserialization options to use, if needed.</param>
        /// <returns>The instance at this key.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="KeyNotFoundException">The key does not exist.</exception>
        /// <exception cref="InvalidCastException">The existing value could not be cast to the desired type.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key, it is not valid json.</exception>
        /// <exception cref="NotSupportedException">The instance could not be deserialized from the value at this key, it is not supported.</exception>
        public T? Get<T>(string key, JsonSerializerOptions? opts = null) => (T?)Get(key, typeof(T), opts);

        /// <summary>
        /// Gets the instance at a given key, deserializing to the provided type, if necessary.
        /// </summary>
        /// <param name="key">The key to get from.</param>
        /// <param name="type">The type of the instance to get.</param>
        /// <param name="opts">The optional deserialization options to use, if needed.</param>
        /// <returns>The instance at this key.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="KeyNotFoundException">The key does not exist.</exception>
        /// <exception cref="InvalidCastException">The instance found could not be converted to the desired type.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key, it is not valid json.</exception>
        /// <exception cref="NotSupportedException">The instance could not be deserialized from the value at this key, it is not supported.</exception>
        public object? Get(string key, Type type, JsonSerializerOptions? opts = null) => !TryGetValue(key, out var obj, type, opts) ? throw new KeyNotFoundException() : obj;

        /// <summary>
        /// Attempts to get the instance at a given key, deserializing to a <typeparamref name="T"/> if necessary.
        /// This function will serialize any pending instances before attempting to get the value at the provided key.
        /// </summary>
        /// <typeparam name="T">The instance the value at this key should be.</typeparam>
        /// <param name="key">The key to get from.</param>
        /// <param name="data">The found value.</param>
        /// <param name="opts">Optional deserialization options.</param>
        /// <returns>true if the key is found, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key, it is not valid json.</exception>
        /// <exception cref="NotSupportedException">The instance could not be deserialized from the value at this key, it is not supported.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key, it is not valid json.</exception>
        /// <exception cref="NotSupportedException">The instance could not be deserialized from the value at this key, it is not supported.</exception>
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

        /// <summary>
        /// Attempts to get the instance at a given key, deserializing using the provided type if necessary.
        /// </summary>
        /// <param name="key">The key to get from.</param>
        /// <param name="data">The found value.</param>
        /// <param name="type">The type of the value to obtain.</param>
        /// <param name="opts">Optional deserialization options.</param>
        /// <returns>true if the key is found, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key.</exception>
        /// <exception cref="InvalidCastException">The instance found could not be converted to the desired type.</exception>
        /// <exception cref="JsonException">The instance could not be deserialized from the value at this key, it is not valid json.</exception>
        /// <exception cref="NotSupportedException">The instance could not be deserialized from the value at this key, it is not supported.</exception>
        public bool TryGetValue(string key, out object? data, Type type, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                // We do not allow null keys
                throw new ArgumentNullException(nameof(key));
            if (!this.data.TryGetValue(key, out var innerData))
            {
                // If we don't find it in our data, search in our serialized data
                if (!serializedData.TryGetValue(key, out var serialized))
                {
                    data = null;
                    return false;
                }
                data = JsonSerializer.Deserialize(serialized.GetRawText(), type, opts);
                // If we have a value AND we do NOT have a value in our data, we deserialize and assign.
                this.data.Add(key, new InnerData { Object = data, Type = type, Options = opts });
                return true;
            }
            var oldType = innerData.Type;
            // If the old type is assignable to the new type, then we know we deserialized something valid.
            if (oldType.IsAssignableTo(type))
            {
                data = innerData.Object;
                return true;
            }
            // Otherwise, we have a scenario where the old type is less specific than the new type.
            // In such a case, we can either change the type to match, or just blatantly fail.
            // Here, we shall blatantly fail.
            throw new InvalidCastException($"Most specific type: {oldType} is not convertible to desired type: {type}");
        }

        /// <summary>
        /// Add a collection of serialized data.
        /// </summary>
        /// <param name="serializedData">The data to add.</param>
        /// <exception cref="ArgumentNullException">The input data is null.</exception>
        /// <exception cref="ArgumentException">A key with the same name already exists.</exception>
        public void AddSerialized(IDictionary<string, JsonElement> serializedData)
        {
            if (serializedData is null)
                throw new ArgumentNullException(nameof(serializedData));
            foreach (var (k, v) in serializedData)
            {
                this.serializedData.Add(k, v);
            }
        }

        /// <summary>
        /// Creates a new value with the provided key and instance.
        /// </summary>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        /// <param name="key">The key to add the value at.</param>
        /// <param name="data">The data to add.</param>
        /// <param name="opts">Optional serialization options.</param>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">A key with the same name already exists.</exception>
        public void Add<T>(string key, T? data, JsonSerializerOptions? opts = null) => Add(key, typeof(T), data, opts);

        /// <summary>
        /// Creates a new value with the provided key, instance, and type.
        /// </summary>
        /// <param name="key">The key to add the value at.</param>
        /// <param name="type">The type of the value to add.</param>
        /// <param name="data">The data to add.</param>
        /// <param name="opts">Optional serialization options.</param>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">A key with the same name already exists.</exception>
        public void Add(string key, Type type, object? data, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            // Before we can simply add to the dictionary, we need to make sure no existing keys exist.
            if (serializedData.ContainsKey(key))
            {
                // We do not attempt to deserialize the instance in the serialized list because we do not know how.
                // We do not want to guess and use the provided options, we know we must throw anyways.
                throw new ArgumentException("Key already exists", nameof(key));
            }
            this.data.Add(key, new InnerData { Object = data, Type = type, Options = opts });
        }

        /// <summary>
        /// Assigns an existing value or creates a new value at the provided key.
        /// </summary>
        /// <typeparam name="T">The type of the value to assign/add.</typeparam>
        /// <param name="key">The key to assign/add the value at.</param>
        /// <param name="data">The data to assign/add.</param>
        /// <param name="opts">Optional serialization options.</param>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="InvalidCastException">The instance found cannot be overwritten by the provided type.</exception>
        public void Set<T>(string key, T? data, JsonSerializerOptions? opts = null) => Set(key, typeof(T), data, opts);

        /// <summary>
        /// Assigns an existing value or creates a new value at the provided key.
        /// </summary>
        /// <param name="key">The key to assign/add the value at.</param>
        /// <param name="type">The type of the value to assign.</param>
        /// <param name="data">The data to assign/add.</param>
        /// <param name="opts">Optional serialization options.</param>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="InvalidCastException">The instance found cannot be overwritten by the provided type.</exception>
        public void Set(string key, Type type, object? data, JsonSerializerOptions? opts = null)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (!this.data.TryGetValue(key, out var innerData))
            {
                // If the value does not exist, simply add it.
                this.data.Add(key, new InnerData { Type = type, Options = opts, Object = data });
                return;
            }
            // Otherwise, assign the data as needed
            var oldType = innerData.Type;
            if (oldType is not null && !oldType.IsAssignableFrom(type) && !oldType.IsAssignableTo(type))
                throw new InvalidCastException($"Previously serialized type: {oldType} is not convertible in either direction to desired type: {type}!");
            innerData.Type = type;
            innerData.Options = opts;
            innerData.Object = data;
        }

        /// <summary>
        /// Gets the converter for <see cref="ArbitraryAdditionalData"/>.
        /// </summary>
        public static JsonConverter<ArbitraryAdditionalData> Converter { get; } = new ArbitraryAdditionalDataConverter();

        /// <summary>
        /// A System.Text.Json converter for <see cref="ArbitraryAdditionalData"/>.
        /// </summary>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "We nest this because it has private members")]
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
                        var value = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                        data.serializedData.Add(key, value);
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
                foreach (var (key, v) in value.data)
                {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, v.Object, v.Type, v.Options);
                }
                // After writing all guaranteed, known, instances, we need to check our serialized data and write any that haven't been written.
                foreach (var (key, v) in value.serializedData)
                {
                    if (value.data.ContainsKey(key))
                        continue;
                    writer.WritePropertyName(key);
                    v.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
        }
    }
}
