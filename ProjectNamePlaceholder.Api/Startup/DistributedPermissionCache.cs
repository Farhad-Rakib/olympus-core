using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ProjectNamePlaceholder.Application.Permissions;
using ProjectNamePlaceholder.Application.Permissions.Dtos;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class DistributedPermissionCache : IPermissionCache
{
    private const string CacheKey = "permissions:all";
    private readonly IDistributedCache _cache;
    private readonly CacheKeyRegistry _keyRegistry;
    private readonly ILogger<DistributedPermissionCache> _logger;

    public DistributedPermissionCache(IDistributedCache cache, CacheKeyRegistry keyRegistry, ILogger<DistributedPermissionCache> logger)
    {
        _cache = cache;
        _keyRegistry = keyRegistry;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(CacheKey, cancellationToken);
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogDebug("Permission cache miss for key {Key}", CacheKey);
            return Array.Empty<PermissionDto>();
        }
        _logger.LogDebug("Permission cache hit for key {Key}", CacheKey);
        try
        {
            var list = JsonSerializer.Deserialize<List<PermissionDto>>(data);
            return list ?? new List<PermissionDto>();
        }
        catch
        {
            return new List<PermissionDto>();
        }
    }

    public async Task SetAllAsync(IReadOnlyList<PermissionDto> permissions, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(permissions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await _cache.SetStringAsync(CacheKey, data, options, cancellationToken);
        _keyRegistry.Register(CacheKey);
        _logger.LogDebug("Permission cache set for key {Key} (ttl {Ttl})", CacheKey, ttl);
    }

    public async Task RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKey, cancellationToken);
        _keyRegistry.Remove(CacheKey);
        _logger.LogDebug("Permission cache removed for key {Key}", CacheKey);
    }
}
