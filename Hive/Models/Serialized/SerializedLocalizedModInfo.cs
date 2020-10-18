using System.Globalization;

namespace Hive.Models.Serialized
{
    /// <summary>
    /// A serializer-friendly version of <see cref="LocalizedModInfo"/>.
    /// </summary>
    public class SerializedLocalizedModInfo
    {
        public CultureInfo Language { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string Description { get; init; } = null!;

        public string Changelog { get; init; } = null!;

        public string Credits { get; init; } = null!;
    }
}
