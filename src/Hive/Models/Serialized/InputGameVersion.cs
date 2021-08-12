namespace Hive.Models.Serialized
{
    /// <summary>
    /// A <see cref="Hive.Models.GameVersion"/> for input to creation requests.
    /// <param name="Name">Name of the game version to create.</param>
    /// <param name="AdditionalData">Additional data to provide.</param>
    /// </summary>
    public record InputGameVersion(string Name, ArbitraryAdditionalData AdditionalData);
}
