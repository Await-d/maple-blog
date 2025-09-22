using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Cache manager for handling cache operations and invalidation
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Get cache service instance
    /// </summary>
    ICacheService Cache { get; }

    /// <summary>
    /// Invalidate cache by content type
    /// </summary>
    Task InvalidateAsync(string contentType, object? entityId = null);

    /// <summary>
    /// Invalidate cache by pattern
    /// </summary>
    Task InvalidateByPatternAsync(string pattern);

    /// <summary>
    /// Invalidate cache by tags
    /// </summary>
    Task InvalidateByTagsAsync(params string[] tags);

    /// <summary>
    /// Warm up cache for specific content
    /// </summary>
    Task WarmUpAsync(string contentType, params object[] parameters);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics?> GetStatisticsAsync();

    /// <summary>
    /// Subscribe to cache invalidation events
    /// </summary>
    void Subscribe(string eventType, Func<CacheInvalidationEvent, Task> handler);

    /// <summary>
    /// Publish cache invalidation event
    /// </summary>
    Task PublishInvalidationAsync(CacheInvalidationEvent eventArgs);
}

/// <summary>
/// Cache invalidation event arguments
/// </summary>
public class CacheInvalidationEvent
{
    public string ContentType { get; set; } = string.Empty;
    public object? EntityId { get; set; }
    public string[] Patterns { get; set; } = Array.Empty<string>();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = string.Empty;
    public bool EnableWarming { get; set; } = true;
}

/// <summary>
/// Implementation of cache manager
/// </summary>
public class CacheManager : ICacheManager
{
    private readonly ICacheService _cacheService;
    private readonly IEnhancedCacheService? _enhancedCacheService;
    private readonly IResponseCacheConfigurationService _configurationService;
    private readonly ILogger<CacheManager> _logger;
    private readonly ResponseCacheConfiguration _configuration;
    private readonly ConcurrentDictionary<string, List<Func<CacheInvalidationEvent, Task>>> _eventHandlers;

    public CacheManager(
        ICacheService cacheService,
        IResponseCacheConfigurationService configurationService,
        ILogger<CacheManager> logger,
        IOptions<ResponseCacheConfiguration> configuration)
    {
        _cacheService = cacheService;
        _enhancedCacheService = cacheService as IEnhancedCacheService;
        _configurationService = configurationService;
        _logger = logger;
        _configuration = configuration.Value;
        _eventHandlers = new ConcurrentDictionary<string, List<Func<CacheInvalidationEvent, Task>>>();
    }

    public ICacheService Cache => _cacheService;

    public async Task InvalidateAsync(string contentType, object? entityId = null)
    {
        try
        {
            var patterns = _configurationService.GetInvalidationPatterns(contentType);
            var tags = GetInvalidationTags(contentType, entityId);

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = contentType,
                EntityId = entityId,
                Patterns = patterns,
                Tags = tags,
                Source = "CacheManager.InvalidateAsync"
            };

            await ExecuteInvalidationAsync(eventArgs);
            await PublishInvalidationAsync(eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for content type: {ContentType}, EntityId: {EntityId}", contentType, entityId);
        }
    }

    public async Task InvalidateByPatternAsync(string pattern)
    {
        try
        {
            await _cacheService.RemoveByPatternAsync(pattern);

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "pattern",
                Patterns = new[] { pattern },
                Source = "CacheManager.InvalidateByPatternAsync",
                EnableWarming = false
            };

            await PublishInvalidationAsync(eventArgs);

            _logger.LogInformation("Cache invalidated by pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by pattern: {Pattern}", pattern);
        }
    }

    public async Task InvalidateByTagsAsync(params string[] tags)
    {
        try
        {
            if (_enhancedCacheService != null)
            {
                await _enhancedCacheService.InvalidateByTagsAsync(tags);
            }
            else
            {
                // Fallback: invalidate by patterns derived from tags
                foreach (var tag in tags)
                {
                    await _cacheService.RemoveByPatternAsync($"*{tag}*");
                }
            }

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "tags",
                Tags = tags,
                Source = "CacheManager.InvalidateByTagsAsync",
                EnableWarming = false
            };

            await PublishInvalidationAsync(eventArgs);

            _logger.LogInformation("Cache invalidated by tags: {Tags}", string.Join(", ", tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache by tags: {Tags}", string.Join(", ", tags));
        }
    }

    public async Task WarmUpAsync(string contentType, params object[] parameters)
    {
        try
        {
            if (!_configuration.Invalidation.EnableCacheWarming)
            {
                _logger.LogDebug("Cache warming is disabled");
                return;
            }

            // Delay warming to allow for data changes to propagate
            await Task.Delay(_configuration.Invalidation.CacheWarmingDelay);

            await ExecuteWarmUpAsync(contentType, parameters);

            _logger.LogInformation("Cache warmed up for content type: {ContentType}", contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up cache for content type: {ContentType}", contentType);
        }
    }

    public async Task ClearAllAsync()
    {
        try
        {
            if (_enhancedCacheService != null)
            {
                await _enhancedCacheService.ClearAllAsync();
            }
            else
            {
                // Fallback: clear by common patterns
                var patterns = new[] { "post:*", "posts:*", "category:*", "categories:*", "tag:*", "tags:*", "user:*", "stats:*" };
                foreach (var pattern in patterns)
                {
                    await _cacheService.RemoveByPatternAsync(pattern);
                }
            }

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "all",
                Source = "CacheManager.ClearAllAsync",
                EnableWarming = false
            };

            await PublishInvalidationAsync(eventArgs);

            _logger.LogInformation("All cache data cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all cache data");
        }
    }

    public async Task<CacheStatistics?> GetStatisticsAsync()
    {
        try
        {
            if (_enhancedCacheService != null)
            {
                return await _enhancedCacheService.GetStatisticsAsync();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return null;
        }
    }

    public void Subscribe(string eventType, Func<CacheInvalidationEvent, Task> handler)
    {
        _eventHandlers.AddOrUpdate(
            eventType,
            new List<Func<CacheInvalidationEvent, Task>> { handler },
            (key, existing) =>
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogDebug("Subscribed to cache invalidation events for type: {EventType}", eventType);
    }

    public async Task PublishInvalidationAsync(CacheInvalidationEvent eventArgs)
    {
        try
        {
            var tasks = new List<Task>();

            // Notify all general handlers
            if (_eventHandlers.TryGetValue("*", out var generalHandlers))
            {
                tasks.AddRange(generalHandlers.Select(handler => handler(eventArgs)));
            }

            // Notify specific content type handlers
            if (_eventHandlers.TryGetValue(eventArgs.ContentType, out var specificHandlers))
            {
                tasks.AddRange(specificHandlers.Select(handler => handler(eventArgs)));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
                _logger.LogDebug("Published cache invalidation event for content type: {ContentType}", eventArgs.ContentType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing cache invalidation event for content type: {ContentType}", eventArgs.ContentType);
        }
    }

    private async Task ExecuteInvalidationAsync(CacheInvalidationEvent eventArgs)
    {
        try
        {
            // Invalidate by patterns
            foreach (var pattern in eventArgs.Patterns)
            {
                await _cacheService.RemoveByPatternAsync(pattern);
            }

            // Invalidate by tags
            if (eventArgs.Tags.Any() && _enhancedCacheService != null)
            {
                await _enhancedCacheService.InvalidateByTagsAsync(eventArgs.Tags);
            }

            _logger.LogDebug("Executed cache invalidation for content type: {ContentType}, Patterns: {Patterns}, Tags: {Tags}",
                eventArgs.ContentType,
                string.Join(", ", eventArgs.Patterns),
                string.Join(", ", eventArgs.Tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing cache invalidation for content type: {ContentType}", eventArgs.ContentType);
        }
    }

    private async Task ExecuteWarmUpAsync(string contentType, params object[] parameters)
    {
        try
        {
            // Content-specific warming logic
            switch (contentType.ToLowerInvariant())
            {
                case "post":
                    await WarmUpPostCacheAsync(parameters);
                    break;

                case "posts":
                    await WarmUpPostsCacheAsync(parameters);
                    break;

                case "category":
                    await WarmUpCategoryCacheAsync(parameters);
                    break;

                case "categories":
                    await WarmUpCategoriesCacheAsync();
                    break;

                case "tag":
                    await WarmUpTagCacheAsync(parameters);
                    break;

                case "tags":
                    await WarmUpTagsCacheAsync();
                    break;

                case "homepage":
                    await WarmUpHomepageCacheAsync();
                    break;

                default:
                    _logger.LogDebug("No specific warm-up logic for content type: {ContentType}", contentType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing cache warm-up for content type: {ContentType}", contentType);
        }
    }

    private async Task WarmUpPostCacheAsync(object[] parameters)
    {
        // Warm up specific post cache
        if (parameters.Length > 0 && parameters[0] is Guid postId)
        {
            var key = CacheKeys.Post(postId);
            // Pre-load post data (this would require injecting appropriate services)
            _logger.LogDebug("Warming up post cache for ID: {PostId}", postId);
        }
    }

    private async Task WarmUpPostsCacheAsync(object[] parameters)
    {
        // Warm up posts list cache for common scenarios
        var commonPages = new[] { 1, 2, 3 };
        var pageSize = 10;

        foreach (var page in commonPages)
        {
            var key = CacheKeys.PostList(page, pageSize);
            // Pre-load posts data
            _logger.LogDebug("Warming up posts cache for page: {Page}", page);
        }

        await Task.CompletedTask;
    }

    private async Task WarmUpCategoryCacheAsync(object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Guid categoryId)
        {
            var key = CacheKeys.Category(categoryId);
            // Pre-load category data
            _logger.LogDebug("Warming up category cache for ID: {CategoryId}", categoryId);
        }

        await Task.CompletedTask;
    }

    private async Task WarmUpCategoriesCacheAsync()
    {
        var key = CacheKeys.CategoryList();
        // Pre-load all categories
        _logger.LogDebug("Warming up categories cache");
        await Task.CompletedTask;
    }

    private async Task WarmUpTagCacheAsync(object[] parameters)
    {
        if (parameters.Length > 0 && parameters[0] is Guid tagId)
        {
            var key = CacheKeys.Tag(tagId);
            // Pre-load tag data
            _logger.LogDebug("Warming up tag cache for ID: {TagId}", tagId);
        }

        await Task.CompletedTask;
    }

    private async Task WarmUpTagsCacheAsync()
    {
        var key = CacheKeys.TagList();
        // Pre-load all tags
        _logger.LogDebug("Warming up tags cache");
        await Task.CompletedTask;
    }

    private async Task WarmUpHomepageCacheAsync()
    {
        // Warm up homepage related caches
        _logger.LogDebug("Warming up homepage cache");
        await Task.CompletedTask;
    }

    private string[] GetInvalidationTags(string contentType, object? entityId)
    {
        var tags = new List<string> { contentType };

        if (entityId != null)
        {
            tags.Add($"{contentType}:{entityId}");
        }

        // Add related tags based on content type
        switch (contentType.ToLowerInvariant())
        {
            case "post":
                tags.AddRange(new[] { "posts", "homepage", "stats" });
                break;

            case "category":
                tags.AddRange(new[] { "categories", "posts", "homepage" });
                break;

            case "tag":
                tags.AddRange(new[] { "tags", "posts", "homepage" });
                break;

            case "user":
                tags.AddRange(new[] { "users", "posts", "comments" });
                break;
        }

        return tags.ToArray();
    }
}

/// <summary>
/// Background service for cache maintenance and monitoring
/// </summary>
public class CacheMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheMaintenanceService> _logger;
    private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(1);

    public CacheMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<CacheMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cache maintenance service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync();
                await Task.Delay(_maintenanceInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache maintenance");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Cache maintenance service stopped");
    }

    private async Task PerformMaintenanceAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var cacheManager = scope.ServiceProvider.GetRequiredService<ICacheManager>();

        try
        {
            // Get cache statistics
            var stats = await cacheManager.GetStatisticsAsync();
            if (stats != null)
            {
                _logger.LogInformation(
                    "Cache Statistics - Connected: {Connected}, Memory: {Memory}, Keys: {Keys}, Hit Ratio: {HitRatio:P2}",
                    stats.IsConnected,
                    stats.UsedMemoryHuman,
                    stats.TotalKeys,
                    stats.HitRatio);

                // Log warnings for poor performance
                if (stats.HitRatio < 0.8)
                {
                    _logger.LogWarning("Cache hit ratio is below 80%: {HitRatio:P2}", stats.HitRatio);
                }
            }

            // Perform any cleanup or optimization tasks here
            _logger.LogDebug("Cache maintenance completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing cache maintenance");
        }
    }
}