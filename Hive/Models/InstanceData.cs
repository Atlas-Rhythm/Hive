using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hive.Models
{
    /// <summary>
    /// Represents a collection of interesting data on an instance. Modifyable by plugins.
    /// </summary>
    public class InstanceData
    {
        // TODO:
        // Interesting data includes:
        // Number and list of plugins
        // Various configuration members
        // Authentication server
        // CDN server
        // Other data

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }
    }
}
