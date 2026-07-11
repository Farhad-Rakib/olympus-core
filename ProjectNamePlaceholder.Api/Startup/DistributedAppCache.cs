using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class DistributedAppCache : IAppCache
{
    private readonly IDistributedCache _cache;
    private readonly CacheKeyRegistry _keyRegistry;
    private readonly ILogger<DistributedAppCache> _logger;

    public DistributedAppCache(IDistributedCache cache, CacheKeyRegistry keyRegistry, ILogger<DistributedAppCache> logger)
    {
        _cache = cache;
        _keyRegistry = keyRegistry;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogDebug("Cache miss for key {Key}", key);
            return default;
        }
        _logger.LogDebug("Cache hit for key {Key}", key);
        try
        {
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache value for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await _cache.SetStringAsync(key, json, options, cancellationToken);
        _keyRegistry.Register(key);
        _logger.LogDebug("Cache set for key {Key} (ttl {Ttl})", key, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        _keyRegistry.Remove(key);
        _logger.LogDebug("Cache removed for key {Key}", key);
    }
}
