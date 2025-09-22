namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Abstraction for caching operations with support for distributed caching
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached item by key
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a cached item with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a cached item with absolute expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, DateTimeOffset absoluteExpiration, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached item
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple cached items by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets a cached item using a factory function
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if a key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the expiration time of a cached item
    /// </summary>
    Task RefreshAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple cached items by keys
    /// </summary>
    Task<Dictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets multiple cached items
    /// </summary>
    Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Cache key builder for consistent key generation
/// </summary>
public static class CacheKeys
{
    private const string Separator = ":";

    // Post related keys
    public static string Post(Guid id) => $"post{Separator}{id}";
    public static string PostBySlug(string slug) => $"post{Separator}slug{Separator}{slug}";
    public static string PostList(int page, int pageSize, string? categoryId = null, string? search = null) =>
        $"posts{Separator}page{Separator}{page}{Separator}size{Separator}{pageSize}" +
        (categoryId != null ? $"{Separator}cat{Separator}{categoryId}" : "") +
        (search != null ? $"{Separator}search{Separator}{search.ToLowerInvariant()}" : "");

    // Category related keys
    public static string Category(Guid id) => $"category{Separator}{id}";
    public static string CategoryBySlug(string slug) => $"category{Separator}slug{Separator}{slug}";
    public static string CategoryList() => $"categories{Separator}all";
    public static string CategoryTree() => $"categories{Separator}tree";

    // Tag related keys
    public static string Tag(Guid id) => $"tag{Separator}{id}";
    public static string TagBySlug(string slug) => $"tag{Separator}slug{Separator}{slug}";
    public static string TagList() => $"tags{Separator}all";
    public static string PopularTags() => $"tags{Separator}popular";

    // User related keys
    public static string User(Guid id) => $"user{Separator}{id}";
    public static string UserByUsername(string username) => $"user{Separator}username{Separator}{username}";

    // Statistics keys
    public static string SiteStats() => $"stats{Separator}site";
    public static string AuthorStats(Guid authorId) => $"stats{Separator}author{Separator}{authorId}";

    // Patterns for bulk operations
    public static string PostPattern() => $"post{Separator}*";
    public static string CategoryPattern() => $"category{Separator}*";
    public static string TagPattern() => $"tag{Separator}*";
    public static string UserPattern() => $"user{Separator}*";
}

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Default expiration times for different cache categories
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan ShortExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan LongExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Specific expiration times
    /// </summary>
    public TimeSpan PostExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan CategoryExpiration { get; set; } = TimeSpan.FromHours(2);
    public TimeSpan TagExpiration { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan UserExpiration { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan StatsExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Enable/disable caching
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Redis connection string (if using Redis)
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Cache provider type
    /// </summary>
    public CacheProviderType ProviderType { get; set; } = CacheProviderType.Memory;
}

public enum CacheProviderType
{
    Memory,
    Redis,
    SqlServer
}

/// <summary>
/// Enhanced cache service with advanced features
/// </summary>
public interface IEnhancedCacheService : ICacheService
{
    /// <summary>
    /// Sets a cached item with tags for invalidation
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration, string[] tags, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Invalidates cache items by tags
    /// </summary>
    Task InvalidateByTagsAsync(string[] tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all cache items
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache key information
    /// </summary>
    Task<CacheKeyInfo?> GetKeyInfoAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all keys matching pattern
    /// </summary>
    Task<string[]> GetKeysAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set cache with dependency on other keys
    /// </summary>
    Task SetWithDependencyAsync<T>(string key, T value, TimeSpan? expiration, string[] dependentKeys, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Cache statistics model
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Whether cache is connected and operational
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Total number of cached keys
    /// </summary>
    public long TotalKeys { get; set; }

    /// <summary>
    /// Used memory in bytes
    /// </summary>
    public long UsedMemory { get; set; }

    /// <summary>
    /// Used memory in human-readable format
    /// </summary>
    public string UsedMemoryHuman { get; set; } = string.Empty;

    /// <summary>
    /// Cache hit count
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Cache miss count
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;

    /// <summary>
    /// Total commands processed
    /// </summary>
    public long TotalCommands { get; set; }

    /// <summary>
    /// Connected clients count (Redis specific)
    /// </summary>
    public int ConnectedClients { get; set; }

    /// <summary>
    /// Uptime in seconds
    /// </summary>
    public long UptimeInSeconds { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metrics
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Cache key information model
/// </summary>
public class CacheKeyInfo
{
    /// <summary>
    /// Cache key
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Value type
    /// </summary>
    public string ValueType { get; set; } = string.Empty;

    /// <summary>
    /// Value size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Time to live in seconds (-1 if no expiration)
    /// </summary>
    public long TimeToLive { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Last accessed timestamp
    /// </summary>
    public DateTime? LastAccessed { get; set; }

    /// <summary>
    /// Associated tags
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Hit count for this key
    /// </summary>
    public long HitCount { get; set; }
}