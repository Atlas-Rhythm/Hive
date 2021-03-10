using System.Text.Json;

namespace Hive.Services
{
    /// <summary>
    /// Helper class for <see cref="JsonElement"/>s.
    /// </summary>
    public static class JsonElementHelper
    {
        /// <summary>
        /// Returns a <see cref="JsonElement"/> that represents an empty JSON object.
        /// </summary>
        // REVIEW: Is cloning necessary?
        public static JsonElement BlankObject { get; private set; } = JsonDocument.Parse("{}").RootElement;
    }
}
