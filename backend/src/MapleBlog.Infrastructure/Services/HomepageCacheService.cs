using MapleBlog.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 首页缓存服务实现
/// </summary>
public class HomepageCacheService : IHomepageCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<HomepageCacheService> _logger;

    // 缓存键常量
    private const string HomepageDataCacheKey = "homepage_data";
    private const string PopularPostsCacheKey = "popular_posts";
    private const string LatestPostsCacheKey = "latest_posts";

    // 默认缓存过期时间
    private static readonly TimeSpan DefaultHomepageExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultPostsExpiration = TimeSpan.FromMinutes(10);

    public HomepageCacheService(
        IMemoryCache memoryCache,
        ILogger<HomepageCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取首页缓存数据
    /// </summary>
    /// <returns>首页数据</returns>
    public async Task<object?> GetHomepageDataAsync()
    {
        try
        {
            if (_memoryCache.TryGetValue(HomepageDataCacheKey, out object? cachedData))
            {
                _logger.LogDebug("Homepage data retrieved from cache");
                return cachedData;
            }

            _logger.LogDebug("Homepage data not found in cache");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving homepage data from cache");
            return null;
        }
    }

    /// <summary>
    /// 设置首页缓存数据
    /// </summary>
    /// <param name="data">首页数据</param>
    /// <param name="expiration">过期时间</param>
    public async Task SetHomepageDataAsync(object data, TimeSpan? expiration = null)
    {
        if (data == null)
        {
            _logger.LogWarning("Attempted to cache null homepage data");
            return;
        }

        try
        {
            var cacheExpiration = expiration ?? DefaultHomepageExpiration;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High
            };

            _memoryCache.Set(HomepageDataCacheKey, data, cacheOptions);
            _logger.LogDebug("Homepage data cached for {Expiration}", cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching homepage data");
        }
    }

    /// <summary>
    /// 清除首页缓存
    /// </summary>
    public async Task ClearHomepageCacheAsync()
    {
        try
        {
            _memoryCache.Remove(HomepageDataCacheKey);
            _logger.LogDebug("Homepage cache cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing homepage cache");
        }
    }

    /// <summary>
    /// 获取热门文章缓存
    /// </summary>
    /// <returns>热门文章</returns>
    public async Task<object?> GetPopularPostsAsync()
    {
        try
        {
            if (_memoryCache.TryGetValue(PopularPostsCacheKey, out object? cachedPosts))
            {
                _logger.LogDebug("Popular posts retrieved from cache");
                return cachedPosts;
            }

            _logger.LogDebug("Popular posts not found in cache");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving popular posts from cache");
            return null;
        }
    }

    /// <summary>
    /// 设置热门文章缓存
    /// </summary>
    /// <param name="posts">热门文章</param>
    /// <param name="expiration">过期时间</param>
    public async Task SetPopularPostsAsync(object posts, TimeSpan? expiration = null)
    {
        if (posts == null)
        {
            _logger.LogWarning("Attempted to cache null popular posts");
            return;
        }

        try
        {
            var cacheExpiration = expiration ?? DefaultPostsExpiration;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(3),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(PopularPostsCacheKey, posts, cacheOptions);
            _logger.LogDebug("Popular posts cached for {Expiration}", cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching popular posts");
        }
    }

    /// <summary>
    /// 获取最新文章缓存
    /// </summary>
    /// <returns>最新文章</returns>
    public async Task<object?> GetLatestPostsAsync()
    {
        try
        {
            if (_memoryCache.TryGetValue(LatestPostsCacheKey, out object? cachedPosts))
            {
                _logger.LogDebug("Latest posts retrieved from cache");
                return cachedPosts;
            }

            _logger.LogDebug("Latest posts not found in cache");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest posts from cache");
            return null;
        }
    }

    /// <summary>
    /// 设置最新文章缓存
    /// </summary>
    /// <param name="posts">最新文章</param>
    /// <param name="expiration">过期时间</param>
    public async Task SetLatestPostsAsync(object posts, TimeSpan? expiration = null)
    {
        if (posts == null)
        {
            _logger.LogWarning("Attempted to cache null latest posts");
            return;
        }

        try
        {
            var cacheExpiration = expiration ?? DefaultPostsExpiration;
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                SlidingExpiration = TimeSpan.FromMinutes(3),
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(LatestPostsCacheKey, posts, cacheOptions);
            _logger.LogDebug("Latest posts cached for {Expiration}", cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching latest posts");
        }
    }
}