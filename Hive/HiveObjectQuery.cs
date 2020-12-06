namespace Hive
{
    public record HiveObjectQuery<T>(T? Value, string? Message, int StatusCode);
}