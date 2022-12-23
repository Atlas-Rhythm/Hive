using System;
using System.Collections.Concurrent;

namespace Hive.Services.Common;

internal class InMemoryInformationContext : IInformationContext
{
    private readonly ConcurrentDictionary<string, object> _contexts = new();

    public void SetValue<T>(string key, T value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        _ = _contexts.TryAdd(key, value);
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        if (key is null)
            throw new ArgumentNullException(nameof(key));

        var result = _contexts.TryGetValue(key, out var val);
        value = result ? (T?)val : default;
        return result;
    }
}
