namespace Hive.Models
{
    public class ModIdentifier
    {
        public string ID { get; init; } = null!;
        public string Version { get; init; } = null!;

        public override string ToString() => $"{ID}@{Version}";
    }
}
