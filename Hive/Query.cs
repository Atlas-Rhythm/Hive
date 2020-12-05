namespace Hive
{
    public record Query<T>(T? Value, string? Message, int StatusCode);
}