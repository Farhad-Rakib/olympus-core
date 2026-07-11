# Permission Caching and Redis

The application now supports caching permission lists using the `IDistributedCache` abstraction. By default the project registers an in-memory distributed cache (`AddDistributedMemoryCache`) so caching works out-of-the-box.

How caching works

- Permissions are cached as a single key `permissions:all` serialized as JSON. The TTL is configured in code (default 30 minutes).
- On permission create/update/delete the cache is invalidated.
- The caching implementation uses `IDistributedCache`, so it can be backed by Redis for production.

Enable Redis

1. Add the package to your API project (replace with the version you prefer):

```bash
dotnet add ProjectNamePlaceholder.Api package Microsoft.Extensions.Caching.StackExchangeRedis
```

2. In `Program.cs`, replace the in-memory registration with Redis registration:

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "olympus:";
});
```

3. Optionally tune TTL and eviction via Redis configuration. The cache uses `AbsoluteExpirationRelativeToNow` when setting the key, which becomes a Redis TTL.

Eviction policy

- Redis eviction is controlled by the Redis server configuration (e.g., `maxmemory-policy`). The app sets per-key TTLs; when Redis evicts keys depends on server-level policies (LRU, LFU, volatile-lru, etc.).
- Recommended production setup: configure Redis memory limits and `maxmemory-policy` appropriate to your workload (e.g., `volatile-lru` or `allkeys-lru`).

Monitoring and metrics

- Log cache hits/misses to help tune TTL.
- Consider exposing a startup log with the number of permissions inserted and whether cache is enabled.

If you want, I can:
- Replace the in-memory registration with Redis registration and add a `Permissions:UseRedis` config flag.
- Add cache hit/miss logging and metrics.
- Implement a background refresh of the cache to avoid cold-start misses.
