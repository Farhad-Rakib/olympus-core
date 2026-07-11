using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class DistributedCacheAdminService
{
    private readonly IDistributedCache _cache;
    private readonly CacheKeyRegistry _keyRegistry;
    private readonly ILogger<DistributedCacheAdminService> _logger;

    public DistributedCacheAdminService(IDistributedCache cache, CacheKeyRegistry keyRegistry, ILogger<DistributedCacheAdminService> logger)
    {
        _cache = cache;
        _keyRegistry = keyRegistry;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DistributedCacheEntry>> GetEntriesAsync(string? pattern, CancellationToken cancellationToken = default)
    {
        var keys = FilterKeys(pattern);
        var results = new List<DistributedCacheEntry>(keys.Count);

        foreach (var key in keys.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            results.Add(new DistributedCacheEntry(key, value));
        }

        _logger.LogDebug("Retrieved {Count} distributed cache entries using pattern {Pattern}", results.Count, pattern ?? "*");
        return results;
    }

    public async Task<long> FlushAsync(CancellationToken cancellationToken = default)
    {
        var keys = _keyRegistry.Keys.ToList();
        if (keys.Count == 0)
        {
            return 0;
        }

        foreach (var key in keys)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _keyRegistry.Remove(key);
        }

        _logger.LogInformation("Flushed {DeletedCount} distributed cache entries", keys.Count);
        return keys.Count;
    }

    private List<string> FilterKeys(string? pattern)
    {
        var keys = _keyRegistry.Keys;
        if (string.IsNullOrWhiteSpace(pattern) || pattern == "*")
        {
            return keys.ToList();
        }

        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return keys.Where(key => regex.IsMatch(key)).ToList();
    }

    public sealed record DistributedCacheEntry(string Key, string? Value);
}