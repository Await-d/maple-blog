using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Redis-based enhanced cache service with advanced pattern support and monitoring
/// </summary>
public class RedisCacheService : IEnhancedCacheService, IDisposable
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly IServer _server;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
        _logger = logger;
        _options = options.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return null;

        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);

            // Update last access time for monitoring
            await _database.KeyTouchAsync(key);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var success = await _database.StringSetAsync(key, serializedValue, expiration ?? _options.DefaultExpiration);

            if (success)
            {
                _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration ?? _options.DefaultExpiration);
            }
            else
            {
                _logger.LogWarning("Failed to set cache key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var expiration = absoluteExpiration - DateTimeOffset.UtcNow;

            if (expiration > TimeSpan.Zero)
            {
                var success = await _database.StringSetAsync(key, serializedValue, expiration);

                if (success)
                {
                    _logger.LogDebug("Cache set for key: {Key}, Absolute expiration: {Expiration}", key, absoluteExpiration);
                }
                else
                {
                    _logger.LogWarning("Failed to set cache key: {Key}", key);
                }
            }
            else
            {
                _logger.LogWarning("Absolute expiration is in the past for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration, string[] tags, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return;

        try
        {
            var transaction = _database.CreateTransaction();

            // Set the main value
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            transaction.StringSetAsync(key, serializedValue, expiration ?? _options.DefaultExpiration);

            // Add key to tag sets
            foreach (var tag in tags)
            {
                var tagKey = $"tag:{tag}";
                transaction.SetAddAsync(tagKey, key);
                transaction.KeyExpireAsync(tagKey, TimeSpan.FromDays(1)); // Tags expire after 1 day
            }

            var success = await transaction.ExecuteAsync();

            if (success)
            {
                _logger.LogDebug("Cache set with tags for key: {Key}, Tags: {Tags}", key, string.Join(", ", tags));
            }
            else
            {
                _logger.LogWarning("Failed to set cache with tags for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with tags for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _database.KeyDeleteAsync(key);
            if (success)
            {
                _logger.LogDebug("Cache key removed: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache key not found for removal: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = await GetKeysAsync(pattern, cancellationToken);
            if (keys.Any())
            {
                var deletedCount = await _database.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray());
                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", deletedCount, pattern);
            }
            else
            {
                _logger.LogDebug("No cache keys found matching pattern: {Pattern}", pattern);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync factory for key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _database.KeyExistsAsync(key);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var ttl = await _database.KeyTimeToLiveAsync(key);
            if (ttl.HasValue)
            {
                // Extend TTL by touching the key
                await _database.KeyExpireAsync(key, ttl.Value);
                _logger.LogDebug("Cache key refreshed: {Key}, TTL: {TTL}", key, ttl.Value);
            }
            else
            {
                _logger.LogDebug("Cache key has no TTL or doesn't exist: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key: {Key}", key);
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();
        var keyArray = keys.ToArray();

        if (!keyArray.Any())
            return result;

        try
        {
            var values = await _database.StringGetAsync(keyArray.Select(k => (RedisKey)k).ToArray());

            for (int i = 0; i < keyArray.Length; i++)
            {
                var key = keyArray[i];
                var value = values[i];

                if (value.HasValue)
                {
                    try
                    {
                        result[key] = JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize cached value for key: {Key}", key);
                        result[key] = null;
                    }
                }
                else
                {
                    result[key] = null;
                }
            }

            _logger.LogDebug("Retrieved {Count} keys from cache", keyArray.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting multiple cache keys");

            // Fallback to individual gets
            foreach (var key in keyArray)
            {
                result[key] = await GetAsync<T>(key, cancellationToken);
            }
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!items.Any())
            return;

        try
        {
            var transaction = _database.CreateTransaction();

            foreach (var item in items)
            {
                var serializedValue = JsonSerializer.Serialize(item.Value, _jsonOptions);
                transaction.StringSetAsync(item.Key, serializedValue, expiration ?? _options.DefaultExpiration);
            }

            var success = await transaction.ExecuteAsync();

            if (success)
            {
                _logger.LogDebug("Set {Count} cache keys in transaction", items.Count);
            }
            else
            {
                _logger.LogWarning("Failed to set {Count} cache keys in transaction", items.Count);

                // Fallback to individual sets
                foreach (var item in items)
                {
                    await SetAsync(item.Key, item.Value, expiration, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting multiple cache keys");

            // Fallback to individual sets
            foreach (var item in items)
            {
                await SetAsync(item.Key, item.Value, expiration, cancellationToken);
            }
        }
    }

    public async Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToDelete = new HashSet<string>();

            foreach (var tag in tags)
            {
                var tagKey = $"tag:{tag}";
                var taggedKeys = await _database.SetMembersAsync(tagKey);

                foreach (var key in taggedKeys)
                {
                    keysToDelete.Add(key!);
                }

                // Remove the tag set itself
                keysToDelete.Add(tagKey);
            }

            if (keysToDelete.Any())
            {
                var deletedCount = await _database.KeyDeleteAsync(keysToDelete.Select(k => (RedisKey)k).ToArray());
                _logger.LogDebug("Invalidated {Count} cache entries for tags: {Tags}", deletedCount, string.Join(", ", tags));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _server.FlushDatabaseAsync();
            _logger.LogInformation("All cache data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache data");
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await _server.InfoAsync();
            var dbInfo = info.FirstOrDefault(i => i.Key == "Keyspace");

            var stats = new CacheStatistics
            {
                IsConnected = _redis.IsConnected,
                UsedMemory = GetInfoValue(info, "used_memory", 0L),
                UsedMemoryHuman = GetInfoValue(info, "used_memory_human", "0B"),
                ConnectedClients = GetInfoValue(info, "connected_clients", 0),
                TotalCommands = GetInfoValue(info, "total_commands_processed", 0L),
                HitCount = GetInfoValue(info, "keyspace_hits", 0L),
                MissCount = GetInfoValue(info, "keyspace_misses", 0L),
                UptimeInSeconds = GetInfoValue(info, "uptime_in_seconds", 0L),
                LastUpdated = DateTime.UtcNow
            };

            if (dbInfo != null)
            {
                var keyspaceInfo = string.Join(",", dbInfo.Select(kv => $"{kv.Key}={kv.Value}"));
                var match = Regex.Match(keyspaceInfo, @"keys=(\d+),expires=(\d+)");
                if (match.Success)
                {
                    stats.TotalKeys = long.Parse(match.Groups[1].Value);
                    stats.AdditionalMetrics["KeysWithExpiry"] = long.Parse(match.Groups[2].Value);
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics { IsConnected = false, LastUpdated = DateTime.UtcNow };
        }
    }

    public async Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _database.KeyExistsAsync(key);
            if (!exists)
                return null;

            var ttl = await _database.KeyTimeToLiveAsync(key);
            var keyType = await _database.KeyTypeAsync(key);
            // Memory usage is not directly available in all Redis versions
            long? memory = null;
            try
            {
                // Try to get memory usage if supported
                var result = await _database.ExecuteAsync("MEMORY", "USAGE", key);
                if (!result.IsNull)
                {
                    memory = (long)result;
                }
            }
            catch
            {
                // Memory usage command not supported, use default
                memory = null;
            }

            return new CacheKeyInfo
            {
                Key = key,
                ValueType = keyType.ToString(),
                Size = memory ?? 0,
                TimeToLive = ttl?.Seconds ?? -1,
                Tags = Array.Empty<string>() // Redis doesn't track tags directly
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key info for: {Key}", key);
            return null;
        }
    }

    public async Task<string[]> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = _server.Keys(pattern: pattern).Select(k => k.ToString()).ToArray();
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
            return Array.Empty<string>();
        }
    }

    public async Task SetWithDependencyAsync<T>(string key, T value, TimeSpan? expiration, string[] dependentKeys, CancellationToken cancellationToken = default) where T : class
    {
        // Redis doesn't have built-in key dependencies, but we can implement using Lua scripts or manual tracking
        // For now, just set the value normally
        await SetAsync(key, value, expiration, cancellationToken);
        _logger.LogDebug("Set cache key {Key} with dependencies: {Dependencies}", key, string.Join(", ", dependentKeys));
    }

    private static T GetInfoValue<T>(IGrouping<string, KeyValuePair<string, string>>[] info, string key, T defaultValue)
    {
        try
        {
            var value = info.SelectMany(g => g).FirstOrDefault(kv => kv.Key == key).Value;
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public void Dispose()
    {
        // Redis connection is managed by DI container
        // Do not dispose here as it's shared
    }
}