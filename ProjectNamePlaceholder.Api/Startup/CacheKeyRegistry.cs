using System.Collections.Concurrent;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class CacheKeyRegistry
{
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Keys => _keys.Keys.ToList();

    public void Register(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _keys.TryAdd(key, 0);
    }

    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _keys.TryRemove(key, out _);
    }

    public void Clear() => _keys.Clear();
}