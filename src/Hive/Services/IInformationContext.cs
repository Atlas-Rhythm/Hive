namespace Hive.Services;

/// <summary>
/// Stores arbitrary information for any container, in any context.
/// </summary>
public interface IInformationContext
{
    /// <summary>
    /// Checks and possibly returns a value in the context
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve.</typeparam>
    /// <param name="key">The key to retrieve the object by.</param>
    /// <param name="value">The object.</param>
    /// <returns>Whether or not the object exists in this context.</returns>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>
    /// Sets a value within the context.
    /// </summary>
    /// <typeparam name="T">The type of the object to store.</typeparam>
    /// <param name="key">They key to store the object under.</param>
    /// <param name="value">The object.</param>
    void SetValue<T>(string key, T value);
}
