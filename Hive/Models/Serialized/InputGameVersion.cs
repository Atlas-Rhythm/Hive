using System.Text.Json;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A <see cref="Hive.Models.GameVersion"/> for input to creation requests.
    /// </summary>
    public record InputGameVersion(string Name, JsonElement AdditionalData);
}
