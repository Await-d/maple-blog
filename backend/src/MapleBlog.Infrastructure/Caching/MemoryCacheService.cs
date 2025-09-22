using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Enhanced in-memory cache implementation using IMemoryCache
/// </summary>
public class EnhancedMemoryCacheService : IEnhancedCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EnhancedMemoryCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, CacheKeyMetadata> _cacheKeys;
    private readonly ConcurrentDictionary<string, HashSet<string>> _cacheTags;

    public EnhancedMemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<EnhancedMemoryCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
        _cacheKeys = new ConcurrentDictionary<string, CacheKeyMetadata>();
        _cacheTags = new ConcurrentDictionary<string, HashSet<string>>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return Task.FromResult<T?>(null);

        try
        {
            var cached = _memoryCache.Get<T>(key);
            if (cached != null)
            {
                // Update metadata
                if (_cacheKeys.TryGetValue(key, out var metadata))
                {
                    metadata.HitCount++;
                    metadata.LastAccessed = DateTime.UtcNow;
                }
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }

            return Task.FromResult(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return Task.CompletedTask;

        try
        {
            var cacheExpiration = expiration ?? _options.DefaultExpiration;
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5), // Sliding window for frequently accessed items
                Priority = CacheItemPriority.Normal
            };

            // Add removal callback to track keys
            cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (cacheKey, cacheValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(key, out _);
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", key, reason);
                }
            });

            _memoryCache.Set(key, value, cacheEntryOptions);

            // Track metadata
            var metadata = new CacheKeyMetadata
            {
                Key = key,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Size = EstimateSize(value),
                TimeToLive = (long)cacheExpiration.TotalSeconds
            };
            _cacheKeys.AddOrUpdate(key, metadata, (k, old) => metadata);

            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return Task.CompletedTask;

        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                Priority = CacheItemPriority.Normal
            };

            cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (cacheKey, cacheValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(key, out _);
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", key, reason);
                }
            });

            _memoryCache.Set(key, value, cacheEntryOptions);

            // Track metadata
            var metadata = new CacheKeyMetadata
            {
                Key = key,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                Size = EstimateSize(value),
                TimeToLive = (long)(absoluteExpiration - DateTime.UtcNow).TotalSeconds
            };
            _cacheKeys.AddOrUpdate(key, metadata, (k, old) => metadata);

            _logger.LogDebug("Cache set for key: {Key}, Absolute expiration: {Expiration}", key, absoluteExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration, string[] tags, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return Task.CompletedTask;

        try
        {
            // Set the value normally first
            SetAsync(key, value, expiration, cancellationToken).Wait();

            // Track tags
            foreach (var tag in tags)
            {
                _cacheTags.AddOrUpdate(tag, new HashSet<string> { key }, (t, existing) =>
                {
                    existing.Add(key);
                    return existing;
                });
            }

            // Update metadata with tags
            if (_cacheKeys.TryGetValue(key, out var metadata))
            {
                metadata.Tags = tags;
            }

            _logger.LogDebug("Cache set with tags for key: {Key}, Tags: {Tags}", key, string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with tags for key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);

            // Remove from tag tracking
            foreach (var tagSet in _cacheTags.Values)
            {
                tagSet.Remove(key);
            }

            _logger.LogDebug("Cache key removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert cache pattern to regex pattern
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var keysToRemove = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);

                // Remove from tag tracking
                foreach (var tagSet in _cacheTags.Values)
                {
                    tagSet.Remove(key);
                }
            }

            _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
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

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // For memory cache, we can't refresh expiration directly
            // This is a limitation of IMemoryCache
            if (_cacheKeys.TryGetValue(key, out var metadata))
            {
                metadata.LastAccessed = DateTime.UtcNow;
            }
            _logger.LogDebug("Refresh attempted for key: {Key} (limited support in memory cache)", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, expiration, cancellationToken);
        }
    }

    public Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        try
        {
            var keysToRemove = new HashSet<string>();

            foreach (var tag in tags)
            {
                if (_cacheTags.TryGetValue(tag, out var taggedKeys))
                {
                    foreach (var key in taggedKeys)
                    {
                        keysToRemove.Add(key);
                    }
                    // Clear the tag set
                    _cacheTags.TryRemove(tag, out _);
                }
            }

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Invalidated {Count} cache entries for tags: {Tags}", keysToRemove.Count, string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }

        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Memory cache doesn't have a direct clear all method
            var keysToRemove = _cacheKeys.Keys.ToList();
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }

            _cacheKeys.Clear();
            _cacheTags.Clear();

            _logger.LogInformation("All memory cache data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache data");
        }

        return Task.CompletedTask;
    }

    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new CacheStatistics
            {
                IsConnected = true, // Memory cache is always "connected"
                TotalKeys = _cacheKeys.Count,
                HitCount = _cacheKeys.Values.Sum(m => m.HitCount),
                MissCount = 0, // We don't track misses in memory cache easily
                TotalCommands = _cacheKeys.Values.Sum(m => m.HitCount),
                UsedMemory = _cacheKeys.Values.Sum(m => m.Size),
                UsedMemoryHuman = FormatBytes(_cacheKeys.Values.Sum(m => m.Size)),
                UptimeInSeconds = (long)(DateTime.UtcNow - DateTime.Today).TotalSeconds,
                LastUpdated = DateTime.UtcNow
            };

            stats.AdditionalMetrics["TotalTags"] = _cacheTags.Count;
            stats.AdditionalMetrics["AverageKeySize"] = stats.TotalKeys > 0 ? stats.UsedMemory / stats.TotalKeys : 0;

            return Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return Task.FromResult(new CacheStatistics { IsConnected = false, LastUpdated = DateTime.UtcNow });
        }
    }

    public Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_cacheKeys.TryGetValue(key, out var metadata))
                return Task.FromResult<CacheKeyInfo?>(null);

            var keyInfo = new CacheKeyInfo
            {
                Key = key,
                ValueType = "object", // Memory cache doesn't track specific types
                Size = metadata.Size,
                TimeToLive = metadata.TimeToLive,
                CreatedAt = metadata.CreatedAt,
                LastAccessed = metadata.LastAccessed,
                Tags = metadata.Tags ?? Array.Empty<string>(),
                HitCount = metadata.HitCount
            };

            return Task.FromResult<CacheKeyInfo?>(keyInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key info for: {Key}", key);
            return Task.FromResult<CacheKeyInfo?>(null);
        }
    }

    public Task<string[]> GetKeysAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var matchingKeys = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToArray();
            return Task.FromResult(matchingKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting keys by pattern: {Pattern}", pattern);
            return Task.FromResult(Array.Empty<string>());
        }
    }

    public async Task SetWithDependencyAsync<T>(string key, T value, TimeSpan? expiration, string[] dependentKeys, CancellationToken cancellationToken = default) where T : class
    {
        // Simple implementation: just set the value
        await SetAsync(key, value, expiration, cancellationToken);
        _logger.LogDebug("Set cache key {Key} with dependencies: {Dependencies}", key, string.Join(", ", dependentKeys));
    }

    private static long EstimateSize<T>(T value) where T : class
    {
        try
        {
            // Rough estimation based on JSON serialization
            var json = JsonSerializer.Serialize(value);
            return json.Length * 2; // Approximate byte size
        }
        catch
        {
            return 1024; // Default size estimate
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return $"{value:F2}{suffixes[suffixIndex]}";
    }

    private class CacheKeyMetadata
    {
        public string Key { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public long Size { get; set; }
        public long TimeToLive { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public long HitCount { get; set; }
    }
}

/// <summary>
/// Basic in-memory cache implementation using IMemoryCache (legacy compatibility)
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<string, byte> _cacheKeys;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ILogger<MemoryCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;
        _cacheKeys = new ConcurrentDictionary<string, byte>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return Task.FromResult<T?>(null);

        try
        {
            var cached = _memoryCache.Get<T>(key);
            if (cached != null)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
            }

            return Task.FromResult(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return Task.FromResult<T?>(null);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return Task.CompletedTask;

        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (cacheKey, cacheValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(key, out _);
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", key, reason);
                }
            });

            _memoryCache.Set(key, value, cacheEntryOptions);
            _cacheKeys.TryAdd(key, 0);

            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration ?? _options.DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled || value == null)
            return Task.CompletedTask;

        try
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration,
                Priority = CacheItemPriority.Normal
            };

            cacheEntryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (cacheKey, cacheValue, reason, state) =>
                {
                    _cacheKeys.TryRemove(key, out _);
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", key, reason);
                }
            });

            _memoryCache.Set(key, value, cacheEntryOptions);
            _cacheKeys.TryAdd(key, 0);

            _logger.LogDebug("Cache set for key: {Key}, Absolute expiration: {Expiration}", key, absoluteExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            _logger.LogDebug("Cache key removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var regexPattern = "^" + pattern.Replace("*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            var keysToRemove = _cacheKeys.Keys.Where(key => regex.IsMatch(key)).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
        }

        return Task.CompletedTask;
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

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache key existence: {Key}", key);
            return Task.FromResult(false);
        }
    }

    public Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Refresh attempted for key: {Key} (not supported in basic memory cache)", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();

        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, expiration, cancellationToken);
        }
    }
}

/// <summary>
/// Distributed cache service using IDistributedCache (Redis, SQL Server, etc.)
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly CacheOptions _options;

    public DistributedCacheService(
        Microsoft.Extensions.Caching.Distributed.IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger,
        IOptions<CacheOptions> options)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.Enabled)
            return null;

        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (cachedValue == null)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(cachedValue);
            _logger.LogDebug("Cache hit for key: {Key}", key);
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
            var serializedValue = JsonSerializer.Serialize(value);
            var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5)
            };

            await _distributedCache.SetStringAsync(key, serializedValue, cacheOptions, cancellationToken);
            _logger.LogDebug("Cache set for key: {Key}, Expiration: {Expiration}", key, expiration ?? _options.DefaultExpiration);
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
            var serializedValue = JsonSerializer.Serialize(value);
            var cacheOptions = new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, cacheOptions, cancellationToken);
            _logger.LogDebug("Cache set for key: {Key}, Absolute expiration: {Expiration}", key, absoluteExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache key removed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based removal is not directly supported by IDistributedCache
        _logger.LogWarning("Pattern-based cache removal is not implemented for distributed cache: {Pattern}", pattern);
        await Task.CompletedTask;
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
            var value = await _distributedCache.GetAsync(key, cancellationToken);
            return value != null;
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
            await _distributedCache.RefreshAsync(key, cancellationToken);
            _logger.LogDebug("Cache key refreshed: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key: {Key}", key);
        }
    }

    public async Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class
    {
        var result = new Dictionary<string, T?>();

        // For better performance, this could be optimized with Redis MGET
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            result[key] = value;
        }

        return result;
    }

    public async Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        // For better performance, this could be optimized with Redis MSET
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, expiration, cancellationToken);
        }
    }
}