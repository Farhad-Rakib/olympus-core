# Caching Guide

This guide shows a pragmatic pattern to add caching for any data in the application using the `IDistributedCache` abstraction (works with in-memory or Redis). It covers read-through (read from cache, on miss query DB, set cache), invalidation, key design, TTL, and multi-node invalidation.

When to cache

- Read-heavy, relatively static data: permissions, menus, site settings, lookup tables.
- Expensive queries or aggregation results.
- Frequently requested user profile fragments or role→permission sets.

Key principles

- Use `IDistributedCache` so backend can be swapped (in-memory for dev, Redis for prod).
- Cache by a stable key naming convention: `{prefix}:{id|name}`. e.g. `permissions:all`, `menus:root`, `sitesettings:Palette.Default`, `user:{id}:claims`.
- Set an explicit TTL (AbsoluteExpirationRelativeToNow) to limit stale data.
- Invalidate cache on writes (create/update/delete) — do not rely solely on TTL for consistency.
- For multi-node deployments, prefer pub/sub or Redis keyspace notifications to broadcast invalidations.
- Consider background refresh (refresh-before-expire) to avoid cold-start spikes.

Simple read-through pattern (pseudo-code)

1. Try to read from cache.
2. If hit, deserialize and return.
3. If miss, acquire a lightweight lock (optional) to avoid stampede.
4. Query the DB.
5. Serialize and set the cache with TTL.
6. Release lock and return.

C# example: helper extension (simplified)

```csharp
public static async Task<T?> GetOrCreateAsync<T>(this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T?>> factory)
{
    var data = await cache.GetStringAsync(key);
    if (!string.IsNullOrEmpty(data))
        return JsonSerializer.Deserialize<T>(data);

    // Optionally implement a distributed lock here (Redis SETNX or RedLock) to avoid stampede
    var result = await factory();
    if (result is null)
        return default;

    var json = JsonSerializer.Serialize(result);
    var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
    await cache.SetStringAsync(key, json, options);
    return result;
}
```

Full example: SiteSettings (pattern used in this repo)

- Key: `sitesettings:{key}`
- TTL: 30m (configurable)
- On create/update/delete: call `cache.RemoveAsync("sitesettings:{key}")` after persisting change.

Invalidation strategies

- Immediate invalidation: remove the cache entry in the same transaction or immediately after database commit.
- Event-driven invalidation: publish an event or Redis pub/sub message on changes; other nodes subscribe and remove their local caches.
- Key versioning: include a version token in keys (e.g., `menus:v2:root`) and bump version on major changes.

Avoiding thundering herd

- Short-term locking: use a distributed lock before doing expensive DB work on cache miss (e.g., RedLock, Redis SET with NX).
- Staggered TTLs: add jitter to TTLs to avoid simultaneous expirations.
- Background refresh: refresh keys in background just before expiry.

Metrics and monitoring

- Track cache hits and misses (logs or metrics) to validate TTL and benefit.
- Monitor Redis memory usage and eviction events (if using Redis).

Multi-node considerations

- Use pub/sub to broadcast invalidations so each node can remove cached data.
- Alternatively rely on Redis TTL + `volatile-*` eviction policies, but explicit invalidation is preferred for correctness.

Configuration and defaults

- TTLs should be driven by configuration (e.g., `appsettings.json`): `Caching:Permissions:TtlMinutes`.
- Provide `Caching:UseRedis` flag and `ConnectionStrings:Redis` for production.

Install Redis on a server

Use one of the following options to provision Redis on your server. Then point `ConnectionStrings:Redis` to that host and port and set `Caching:UseRedis=true`.

Option A: Docker (recommended)

```bash
docker run -d \
    --name redis \
    -p 6379:6379 \
    -v redis_data:/data \
    redis:7-alpine \
    redis-server --appendonly yes
```

Option B: Ubuntu (apt)

```bash
sudo apt update
sudo apt install -y redis-server
sudo systemctl enable --now redis-server
```

Option C: macOS (Homebrew)

```bash
brew install redis
brew services start redis
```

Verify from the server:

```bash
redis-cli ping
```

Checklist when adding caching for an entity

1. Identify the read path and where to insert `GetOrCreateAsync` (service layer recommended).
2. Choose a cache key and TTL and add them to configuration.
3. Implement cache reads in service `Get`/`List` methods.
4. Invalidate cache in `Create`, `Update`, `Delete` methods after DB commit.
5. Add unit tests for cache behavior (hit/miss/invalidation).
6. Add logging for cache hits/misses.
7. Enable Redis in staging and monitor before production.

Examples used in repository

- `permissions:all` — cached permissions list (see `PermissionService`).
- `menus:root` — good candidate for menu tree caching in `MenuService`.
- `sitesettings:{key}` — site setting by key; used by `SiteSettingService`.

Further improvements

- Use a typed cache wrapper (e.g., `ICache<T>`) to centralize serialization and policies.
- Implement distributed locks for heavy DB computations.
- Implement a cache-aside background refresher to keep hot keys warm.
- Add Prometheus or application metrics for cache performance.

If you want, I can:

- Implement `GetOrCreateAsync` helper and apply it to `MenuService` and `SiteSettingService` (MVP), or
- Add Redis configuration flags and a small `IHostedService` to warm-up caches on startup.

Tell me which action to take next and I will implement it.
