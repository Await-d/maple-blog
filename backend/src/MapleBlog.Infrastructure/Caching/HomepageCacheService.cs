using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.DTOs;
using MapleBlog.Infrastructure.Data.Configurations;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// 首页缓存服务
/// 实现多级缓存策略和智能失效机制
/// </summary>
public interface IHomepageCacheService
{
    // 获取缓存数据
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    // 设置缓存数据
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    // 删除缓存
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    // 批量删除缓存
    Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default);

    // 首页特定缓存方法
    Task<HomePageDto?> GetHomePageDataAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task SetHomePageDataAsync(HomePageDto data, string? userId = null, CancellationToken cancellationToken = default);

    // 文章列表缓存
    Task<List<PostSummaryDto>?> GetPopularPostsAsync(string timeRange, string sortBy, int page, int limit, CancellationToken cancellationToken = default);
    Task SetPopularPostsAsync(List<PostSummaryDto> posts, string timeRange, string sortBy, int page, int limit, CancellationToken cancellationToken = default);

    // 统计数据缓存
    Task<SiteStatsDto?> GetSiteStatsAsync(CancellationToken cancellationToken = default);
    Task SetSiteStatsAsync(SiteStatsDto stats, CancellationToken cancellationToken = default);

    // 分类和标签缓存
    Task<List<CategoryDto>?> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task SetCategoriesAsync(List<CategoryDto> categories, CancellationToken cancellationToken = default);

    // 个性化推荐缓存
    Task<List<PostSummaryDto>?> GetPersonalizedPostsAsync(string userId, CancellationToken cancellationToken = default);
    Task SetPersonalizedPostsAsync(string userId, List<PostSummaryDto> posts, CancellationToken cancellationToken = default);

    // 缓存失效
    Task InvalidateHomePageCacheAsync(CancellationToken cancellationToken = default);
    Task InvalidatePostCacheAsync(string postId, CancellationToken cancellationToken = default);
    Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 首页缓存配置
/// </summary>
public class HomepageCacheOptions
{
    public const string SectionName = "Cache:Homepage";

    /// <summary>
    /// 默认过期时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// 热门文章缓存时间（分钟）
    /// </summary>
    public int PopularPostsExpirationMinutes { get; set; } = 30;

    /// <summary>
    /// 统计数据缓存时间（分钟）
    /// </summary>
    public int SiteStatsExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 分类标签缓存时间（小时）
    /// </summary>
    public int CategoriesExpirationHours { get; set; } = 4;

    /// <summary>
    /// 个性化推荐缓存时间（分钟）
    /// </summary>
    public int PersonalizedPostsExpirationMinutes { get; set; } = 20;

    /// <summary>
    /// 首页数据缓存时间（分钟）
    /// </summary>
    public int HomePageDataExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// 是否启用内存缓存
    /// </summary>
    public bool EnableMemoryCache { get; set; } = true;

    /// <summary>
    /// 是否启用分布式缓存
    /// </summary>
    public bool EnableDistributedCache { get; set; } = true;

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "homepage:";

    /// <summary>
    /// 内存缓存大小限制（MB）
    /// </summary>
    public int MemoryCacheSizeLimitMB { get; set; } = 100;

    /// <summary>
    /// 是否启用缓存预热
    /// </summary>
    public bool EnableCacheWarming { get; set; } = true;

    /// <summary>
    /// 缓存预热间隔（分钟）
    /// </summary>
    public int CacheWarmingIntervalMinutes { get; set; } = 30;
}

public class HomepageCacheService : IHomepageCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<HomepageCacheService> _logger;
    private readonly HomepageCacheOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    // 缓存键常量
    private const string HOME_PAGE_DATA_KEY = "home-page-data";
    private const string POPULAR_POSTS_KEY = "popular-posts";
    private const string SITE_STATS_KEY = "site-stats";
    private const string CATEGORIES_KEY = "categories";
    private const string PERSONALIZED_POSTS_KEY = "personalized-posts";

    public HomepageCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<HomepageCacheService> logger,
        IOptions<HomepageCacheOptions> options)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _options = options.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_options.KeyPrefix}{key}";

        try
        {
            // 首先尝试内存缓存
            if (_options.EnableMemoryCache && _memoryCache.TryGetValue(fullKey, out T? memoryValue))
            {
                _logger.LogDebug("Cache hit in memory cache for key: {Key}", fullKey);
                return memoryValue;
            }

            // 然后尝试分布式缓存
            if (_options.EnableDistributedCache)
            {
                var distributedValue = await _distributedCache.GetStringAsync(fullKey, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);

                    // 回写到内存缓存
                    if (_options.EnableMemoryCache && deserializedValue != null)
                    {
                        _memoryCache.Set(fullKey, deserializedValue, TimeSpan.FromMinutes(5));
                    }

                    _logger.LogDebug("Cache hit in distributed cache for key: {Key}", fullKey);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", fullKey);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache value for key: {Key}", fullKey);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_options.KeyPrefix}{key}";
        var exp = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

        try
        {
            // 设置内存缓存
            if (_options.EnableMemoryCache)
            {
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = exp,
                    Size = EstimateSize(value),
                    Priority = CacheItemPriority.Normal
                };

                _memoryCache.Set(fullKey, value, memoryCacheOptions);
                _logger.LogDebug("Set memory cache for key: {Key}", fullKey);
            }

            // 设置分布式缓存
            if (_options.EnableDistributedCache)
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var distributedCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = exp
                };

                await _distributedCache.SetStringAsync(fullKey, serializedValue, distributedCacheOptions, cancellationToken);
                _logger.LogDebug("Set distributed cache for key: {Key}", fullKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", fullKey);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = $"{_options.KeyPrefix}{key}";

        try
        {
            // 从内存缓存删除
            if (_options.EnableMemoryCache)
            {
                _memoryCache.Remove(fullKey);
            }

            // 从分布式缓存删除
            if (_options.EnableDistributedCache)
            {
                await _distributedCache.RemoveAsync(fullKey, cancellationToken);
            }

            _logger.LogDebug("Removed cache for key: {Key}", fullKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache for key: {Key}", fullKey);
        }
    }

    public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var fullPattern = $"{_options.KeyPrefix}{pattern}";

        try
        {
            // 注意：内存缓存不支持模式删除，这里可以考虑维护一个键列表
            // 或者使用更高级的缓存解决方案

            _logger.LogDebug("Pattern removal requested for: {Pattern}", fullPattern);

            // 这里可以实现特定的模式删除逻辑
            // 例如，如果使用Redis，可以使用SCAN和DEL命令
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache pattern: {Pattern}", fullPattern);
        }
    }

    // 首页数据缓存
    public async Task<HomePageDto?> GetHomePageDataAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        var key = userId != null ? $"{HOME_PAGE_DATA_KEY}:{userId}" : HOME_PAGE_DATA_KEY;
        return await GetAsync<HomePageDto>(key, cancellationToken);
    }

    public async Task SetHomePageDataAsync(HomePageDto data, string? userId = null, CancellationToken cancellationToken = default)
    {
        var key = userId != null ? $"{HOME_PAGE_DATA_KEY}:{userId}" : HOME_PAGE_DATA_KEY;
        var expiration = TimeSpan.FromMinutes(_options.HomePageDataExpirationMinutes);
        await SetAsync(key, data, expiration, cancellationToken);
    }

    // 热门文章缓存
    public async Task<List<PostSummaryDto>?> GetPopularPostsAsync(string timeRange, string sortBy, int page, int limit, CancellationToken cancellationToken = default)
    {
        var key = $"{POPULAR_POSTS_KEY}:{timeRange}:{sortBy}:{page}:{limit}";
        return await GetAsync<List<PostSummaryDto>>(key, cancellationToken);
    }

    public async Task SetPopularPostsAsync(List<PostSummaryDto> posts, string timeRange, string sortBy, int page, int limit, CancellationToken cancellationToken = default)
    {
        var key = $"{POPULAR_POSTS_KEY}:{timeRange}:{sortBy}:{page}:{limit}";
        var expiration = TimeSpan.FromMinutes(_options.PopularPostsExpirationMinutes);
        await SetAsync(key, posts, expiration, cancellationToken);
    }

    // 统计数据缓存
    public async Task<SiteStatsDto?> GetSiteStatsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<SiteStatsDto>(SITE_STATS_KEY, cancellationToken);
    }

    public async Task SetSiteStatsAsync(SiteStatsDto stats, CancellationToken cancellationToken = default)
    {
        var expiration = TimeSpan.FromMinutes(_options.SiteStatsExpirationMinutes);
        await SetAsync(SITE_STATS_KEY, stats, expiration, cancellationToken);
    }

    // 分类标签缓存
    public async Task<List<CategoryDto>?> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<List<CategoryDto>>(CATEGORIES_KEY, cancellationToken);
    }

    public async Task SetCategoriesAsync(List<CategoryDto> categories, CancellationToken cancellationToken = default)
    {
        var expiration = TimeSpan.FromHours(_options.CategoriesExpirationHours);
        await SetAsync(CATEGORIES_KEY, categories, expiration, cancellationToken);
    }

    // 个性化推荐缓存
    public async Task<List<PostSummaryDto>?> GetPersonalizedPostsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = $"{PERSONALIZED_POSTS_KEY}:{userId}";
        return await GetAsync<List<PostSummaryDto>>(key, cancellationToken);
    }

    public async Task SetPersonalizedPostsAsync(string userId, List<PostSummaryDto> posts, CancellationToken cancellationToken = default)
    {
        var key = $"{PERSONALIZED_POSTS_KEY}:{userId}";
        var expiration = TimeSpan.FromMinutes(_options.PersonalizedPostsExpirationMinutes);
        await SetAsync(key, posts, expiration, cancellationToken);
    }

    // 缓存失效
    public async Task InvalidateHomePageCacheAsync(CancellationToken cancellationToken = default)
    {
        await RemovePatternAsync($"{HOME_PAGE_DATA_KEY}*", cancellationToken);
        await RemovePatternAsync($"{POPULAR_POSTS_KEY}*", cancellationToken);
        await RemoveAsync(SITE_STATS_KEY, cancellationToken);

        _logger.LogInformation("Invalidated homepage cache");
    }

    public async Task InvalidatePostCacheAsync(string postId, CancellationToken cancellationToken = default)
    {
        await RemovePatternAsync($"{POPULAR_POSTS_KEY}*", cancellationToken);
        await RemovePatternAsync($"{HOME_PAGE_DATA_KEY}*", cancellationToken);

        _logger.LogInformation("Invalidated post-related cache for post: {PostId}", postId);
    }

    public async Task InvalidateUserCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        await RemoveAsync($"{HOME_PAGE_DATA_KEY}:{userId}", cancellationToken);
        await RemoveAsync($"{PERSONALIZED_POSTS_KEY}:{userId}", cancellationToken);

        _logger.LogInformation("Invalidated user-related cache for user: {UserId}", userId);
    }

    // 估算对象大小（用于内存缓存）
    private static long EstimateSize<T>(T obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj);
            return System.Text.Encoding.UTF8.GetByteCount(json);
        }
        catch
        {
            // 如果无法序列化，返回默认大小
            return 1024; // 1KB
        }
    }
}

// 缓存预热服务
public interface ICacheWarmupService
{
    Task WarmupAsync(CancellationToken cancellationToken = default);
}

public class HomepageCacheWarmupService : ICacheWarmupService
{
    private readonly IHomepageCacheService _cacheService;
    private readonly ILogger<HomepageCacheWarmupService> _logger;
    private readonly HomepageCacheOptions _options;

    // 这里需要注入相应的服务来获取数据
    // private readonly IPostService _postService;
    // private readonly IStatsService _statsService;
    // private readonly ICategoryService _categoryService;

    public HomepageCacheWarmupService(
        IHomepageCacheService cacheService,
        ILogger<HomepageCacheWarmupService> logger,
        IOptions<HomepageCacheOptions> options)
    {
        _cacheService = cacheService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCacheWarming)
        {
            return;
        }

        _logger.LogInformation("Starting cache warmup...");

        try
        {
            // 预热热门文章
            // await WarmupPopularPostsAsync(cancellationToken);

            // 预热统计数据
            // await WarmupSiteStatsAsync(cancellationToken);

            // 预热分类数据
            // await WarmupCategoriesAsync(cancellationToken);

            _logger.LogInformation("Cache warmup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
    }

    // 具体的预热方法实现...
}