using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using StackExchange.Redis;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class RedisCacheAdminService
{
    private readonly IConfiguration _configuration;
    private readonly CachingOptions _cachingOptions;
    private readonly ILogger<RedisCacheAdminService> _logger;

    public RedisCacheAdminService(IConfiguration configuration, IOptions<CachingOptions> cachingOptions, ILogger<RedisCacheAdminService> logger)
    {
        _configuration = configuration;
        _cachingOptions = cachingOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RedisCacheEntry>> GetEntriesAsync(string? pattern, CancellationToken cancellationToken = default)
    {
        using var connection = await ConnectAsync(cancellationToken);
        var searchPattern = string.IsNullOrWhiteSpace(pattern)
            ? $"{_cachingOptions.RedisInstanceName}*"
            : pattern;

        var database = connection.GetDatabase();
        var databaseIndex = database.Database;
        var keys = GetKeys(connection, databaseIndex, searchPattern, cancellationToken);
        var results = new List<RedisCacheEntry>(keys.Count);

        foreach (var key in keys.OrderBy(key => key.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            var hashEntries = await database.HashGetAllAsync(key);
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in hashEntries)
            {
                fields[entry.Name.ToString()] = entry.Value.ToString();
            }

            fields.TryGetValue("data", out var value);
            var ttl = await database.KeyTimeToLiveAsync(key);

            results.Add(new RedisCacheEntry(
                key.ToString(),
                value,
                fields,
                ttl?.TotalSeconds));
        }

        _logger.LogDebug("Retrieved {Count} cache entries using pattern {Pattern}", results.Count, searchPattern);
        return results;
    }

    public async Task<long> FlushAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await ConnectAsync(cancellationToken);
        var searchPattern = $"{_cachingOptions.RedisInstanceName}*";
        var database = connection.GetDatabase();
        var databaseIndex = database.Database;
        var keys = GetKeys(connection, databaseIndex, searchPattern, cancellationToken);

        if (keys.Count == 0)
        {
            _logger.LogDebug("No cache keys matched flush pattern {Pattern}", searchPattern);
            return 0;
        }

        var deleted = await database.KeyDeleteAsync(keys.ToArray());
        _logger.LogInformation("Flushed {DeletedCount} cache keys using pattern {Pattern}", deleted, searchPattern);
        return deleted;
    }

    private async Task<ConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken)
    {
        if (!_cachingOptions.UseRedis)
        {
            throw new InvalidOperationException("Redis cache is not enabled. Set Caching:UseRedis to true and configure ConnectionStrings:Redis.");
        }

        var connectionString = _configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Redis connection string is missing. Set ConnectionStrings:Redis.");
        }

        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = false;

        var connection = await ConnectionMultiplexer.ConnectAsync(options);
        cancellationToken.ThrowIfCancellationRequested();
        return connection;
    }

    private static HashSet<RedisKey> GetKeys(ConnectionMultiplexer connection, int databaseIndex, string pattern, CancellationToken cancellationToken)
    {
        var keys = new HashSet<RedisKey>();

        foreach (var endpoint in connection.GetEndPoints())
        {
            cancellationToken.ThrowIfCancellationRequested();

            IServer? server;
            try
            {
                server = connection.GetServer(endpoint);
            }
            catch
            {
                continue;
            }

            if (!server.IsConnected)
            {
                continue;
            }

            try
            {
                foreach (var key in server.Keys(databaseIndex, pattern))
                {
                    keys.Add(key);
                }
            }
            catch
            {
                continue;
            }
        }

        return keys;
    }

    public sealed record RedisCacheEntry(string Key, string? Value, IReadOnlyDictionary<string, string> Fields, double? TimeToLiveSeconds);
}