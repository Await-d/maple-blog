using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Infrastructure.Caching;
using MapleBlog.Domain.Interfaces;
using System.Collections.Concurrent;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Service for intelligent cache invalidation based on content changes
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidate cache when a post is created, updated, or deleted
    /// </summary>
    Task InvalidatePostCacheAsync(Guid postId, string operation = "update");

    /// <summary>
    /// Invalidate cache when a category is created, updated, or deleted
    /// </summary>
    Task InvalidateCategoryCacheAsync(Guid categoryId, string operation = "update");

    /// <summary>
    /// Invalidate cache when a tag is created, updated, or deleted
    /// </summary>
    Task InvalidateTagCacheAsync(Guid tagId, string operation = "update");

    /// <summary>
    /// Invalidate cache when a user is updated
    /// </summary>
    Task InvalidateUserCacheAsync(Guid userId, string operation = "update");

    /// <summary>
    /// Invalidate cache when comments are modified
    /// </summary>
    Task InvalidateCommentCacheAsync(Guid postId, Guid commentId, string operation = "update");

    /// <summary>
    /// Schedule cache warming for specific content
    /// </summary>
    Task ScheduleCacheWarmingAsync(string contentType, object[] parameters, TimeSpan delay);

    /// <summary>
    /// Subscribe to cache invalidation events
    /// </summary>
    void Subscribe(string eventType, Func<CacheInvalidationEvent, Task> handler);
}

/// <summary>
/// Implementation of intelligent cache invalidation service
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly ResponseCacheConfiguration _configuration;
    private readonly ConcurrentQueue<(string ContentType, object[] Parameters, DateTime ScheduledTime)> _warmingQueue;
    private readonly ConcurrentDictionary<string, List<Func<CacheInvalidationEvent, Task>>> _eventSubscribers;

    public CacheInvalidationService(
        ICacheManager cacheManager,
        ILogger<CacheInvalidationService> logger,
        IOptions<ResponseCacheConfiguration> configuration)
    {
        _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.Value;
        _warmingQueue = new ConcurrentQueue<(string, object[], DateTime)>();
        _eventSubscribers = new ConcurrentDictionary<string, List<Func<CacheInvalidationEvent, Task>>>();

        // Subscribe to cache manager events
        _cacheManager.Subscribe("*", OnCacheInvalidationEvent);
    }

    public async Task InvalidatePostCacheAsync(Guid postId, string operation = "update")
    {
        try
        {
            _logger.LogInformation("Invalidating post cache for Post ID: {PostId}, Operation: {Operation}", postId, operation);

            // Invalidate specific post cache
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.Post(postId));
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.PostBySlug("*"));

            // Invalidate post lists that might contain this post
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.PostPattern());
            await _cacheManager.InvalidateByPatternAsync("posts:*");

            // Invalidate related content
            await _cacheManager.InvalidateByPatternAsync("homepage:*");
            await _cacheManager.InvalidateByPatternAsync("stats:*");
            await _cacheManager.InvalidateByPatternAsync("category:*");
            await _cacheManager.InvalidateByPatternAsync("tag:*");

            // Invalidate search results and archives
            await _cacheManager.InvalidateByPatternAsync("search:*");
            await _cacheManager.InvalidateByPatternAsync("archive:*");

            // Schedule cache warming if enabled
            if (_configuration.Invalidation.EnableCacheWarming)
            {
                await ScheduleCacheWarmingAsync("post", new object[] { postId }, _configuration.Invalidation.CacheWarmingDelay);
                await ScheduleCacheWarmingAsync("posts", Array.Empty<object>(), _configuration.Invalidation.CacheWarmingDelay);
                await ScheduleCacheWarmingAsync("homepage", Array.Empty<object>(), _configuration.Invalidation.CacheWarmingDelay);
            }

            // Publish invalidation event
            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "post",
                EntityId = postId,
                Patterns = _configuration.Invalidation.PostInvalidationPatterns,
                Tags = new[] { "post", $"post:{postId}", "posts", "homepage", "stats" },
                Source = "CacheInvalidationService.InvalidatePostCacheAsync"
            };

            await PublishEventAsync(eventArgs);

            _logger.LogInformation("Post cache invalidation completed for Post ID: {PostId}", postId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating post cache for Post ID: {PostId}", postId);
        }
    }

    public async Task InvalidateCategoryCacheAsync(Guid categoryId, string operation = "update")
    {
        try
        {
            _logger.LogInformation("Invalidating category cache for Category ID: {CategoryId}, Operation: {Operation}", categoryId, operation);

            // Invalidate specific category cache
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.Category(categoryId));
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.CategoryBySlug("*"));

            // Invalidate category lists and trees
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.CategoryList());
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.CategoryTree());
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.CategoryPattern());

            // Invalidate posts in this category
            await _cacheManager.InvalidateByPatternAsync($"posts:*cat*{categoryId}*");

            // Invalidate related content
            await _cacheManager.InvalidateByPatternAsync("homepage:*");
            await _cacheManager.InvalidateByPatternAsync("stats:*");

            // Schedule cache warming
            if (_configuration.Invalidation.EnableCacheWarming)
            {
                await ScheduleCacheWarmingAsync("category", new object[] { categoryId }, _configuration.Invalidation.CacheWarmingDelay);
                await ScheduleCacheWarmingAsync("categories", Array.Empty<object>(), _configuration.Invalidation.CacheWarmingDelay);
            }

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "category",
                EntityId = categoryId,
                Patterns = _configuration.Invalidation.CategoryInvalidationPatterns,
                Tags = new[] { "category", $"category:{categoryId}", "categories", "posts", "homepage" },
                Source = "CacheInvalidationService.InvalidateCategoryCacheAsync"
            };

            await PublishEventAsync(eventArgs);

            _logger.LogInformation("Category cache invalidation completed for Category ID: {CategoryId}", categoryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating category cache for Category ID: {CategoryId}", categoryId);
        }
    }

    public async Task InvalidateTagCacheAsync(Guid tagId, string operation = "update")
    {
        try
        {
            _logger.LogInformation("Invalidating tag cache for Tag ID: {TagId}, Operation: {Operation}", tagId, operation);

            // Invalidate specific tag cache
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.Tag(tagId));
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.TagBySlug("*"));

            // Invalidate tag lists
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.TagList());
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.PopularTags());
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.TagPattern());

            // Invalidate posts with this tag
            await _cacheManager.InvalidateByPatternAsync($"posts:*tag*{tagId}*");

            // Invalidate related content
            await _cacheManager.InvalidateByPatternAsync("homepage:*");
            await _cacheManager.InvalidateByPatternAsync("stats:*");

            // Schedule cache warming
            if (_configuration.Invalidation.EnableCacheWarming)
            {
                await ScheduleCacheWarmingAsync("tag", new object[] { tagId }, _configuration.Invalidation.CacheWarmingDelay);
                await ScheduleCacheWarmingAsync("tags", Array.Empty<object>(), _configuration.Invalidation.CacheWarmingDelay);
            }

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "tag",
                EntityId = tagId,
                Patterns = _configuration.Invalidation.TagInvalidationPatterns,
                Tags = new[] { "tag", $"tag:{tagId}", "tags", "posts", "homepage" },
                Source = "CacheInvalidationService.InvalidateTagCacheAsync"
            };

            await PublishEventAsync(eventArgs);

            _logger.LogInformation("Tag cache invalidation completed for Tag ID: {TagId}", tagId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating tag cache for Tag ID: {TagId}", tagId);
        }
    }

    public async Task InvalidateUserCacheAsync(Guid userId, string operation = "update")
    {
        try
        {
            _logger.LogInformation("Invalidating user cache for User ID: {UserId}, Operation: {Operation}", userId, operation);

            // Invalidate specific user cache
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.User(userId));
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.UserByUsername("*"));

            // Invalidate user-related statistics
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.AuthorStats(userId));

            // Invalidate posts by this user (if they're an author)
            await _cacheManager.InvalidateByPatternAsync($"posts:*author*{userId}*");

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "user",
                EntityId = userId,
                Patterns = new[] { "user:*", "posts:*" },
                Tags = new[] { "user", $"user:{userId}", "posts", "stats" },
                Source = "CacheInvalidationService.InvalidateUserCacheAsync",
                EnableWarming = false // User data doesn't need warming
            };

            await PublishEventAsync(eventArgs);

            _logger.LogInformation("User cache invalidation completed for User ID: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user cache for User ID: {UserId}", userId);
        }
    }

    public async Task InvalidateCommentCacheAsync(Guid postId, Guid commentId, string operation = "update")
    {
        try
        {
            _logger.LogInformation("Invalidating comment cache for Post ID: {PostId}, Comment ID: {CommentId}, Operation: {Operation}",
                postId, commentId, operation);

            // Invalidate the specific post cache since comments are part of post details
            await _cacheManager.InvalidateByPatternAsync(CacheKeys.Post(postId));

            // Invalidate comment-related patterns
            await _cacheManager.InvalidateByPatternAsync($"comments:*post:{postId}*");
            await _cacheManager.InvalidateByPatternAsync($"comment:{commentId}*");

            // Invalidate statistics that might include comment counts
            await _cacheManager.InvalidateByPatternAsync("stats:*");

            var eventArgs = new CacheInvalidationEvent
            {
                ContentType = "comment",
                EntityId = commentId,
                Patterns = new[] { $"post:{postId}", $"comment:{commentId}", "stats:*" },
                Tags = new[] { "comment", $"comment:{commentId}", $"post:{postId}", "stats" },
                Source = "CacheInvalidationService.InvalidateCommentCacheAsync",
                EnableWarming = false
            };

            await PublishEventAsync(eventArgs);

            _logger.LogInformation("Comment cache invalidation completed for Comment ID: {CommentId}", commentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating comment cache for Comment ID: {CommentId}", commentId);
        }
    }

    public async Task ScheduleCacheWarmingAsync(string contentType, object[] parameters, TimeSpan delay)
    {
        try
        {
            var scheduledTime = DateTime.UtcNow.Add(delay);
            _warmingQueue.Enqueue((contentType, parameters, scheduledTime));

            _logger.LogDebug("Scheduled cache warming for content type: {ContentType}, delay: {Delay}",
                contentType, delay);

            // For immediate execution in this implementation
            // In a production system, you might use a background service or task scheduler
            if (delay <= TimeSpan.FromSeconds(30))
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await _cacheManager.WarmUpAsync(contentType, parameters);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling cache warming for content type: {ContentType}", contentType);
        }
    }

    public void Subscribe(string eventType, Func<CacheInvalidationEvent, Task> handler)
    {
        _eventSubscribers.AddOrUpdate(
            eventType,
            new List<Func<CacheInvalidationEvent, Task>> { handler },
            (key, existing) =>
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogDebug("New subscriber added for event type: {EventType}", eventType);
    }

    private async Task PublishEventAsync(CacheInvalidationEvent eventArgs)
    {
        try
        {
            var tasks = new List<Task>();

            // Notify all general subscribers
            if (_eventSubscribers.TryGetValue("*", out var generalSubscribers))
            {
                tasks.AddRange(generalSubscribers.Select(handler => handler(eventArgs)));
            }

            // Notify content-type specific subscribers
            if (_eventSubscribers.TryGetValue(eventArgs.ContentType, out var specificSubscribers))
            {
                tasks.AddRange(specificSubscribers.Select(handler => handler(eventArgs)));
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

    private async Task OnCacheInvalidationEvent(CacheInvalidationEvent eventArgs)
    {
        try
        {
            _logger.LogDebug("Received cache invalidation event: ContentType={ContentType}, Source={Source}",
                eventArgs.ContentType, eventArgs.Source);

            // Handle cross-cutting concerns
            if (eventArgs.ContentType == "post" && eventArgs.EnableWarming)
            {
                // Update related statistics caches
                await Task.Delay(TimeSpan.FromSeconds(5)); // Small delay for data consistency
                await _cacheManager.InvalidateByPatternAsync("stats:*");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling cache invalidation event for content type: {ContentType}", eventArgs.ContentType);
        }
    }
}

/// <summary>
/// Extension methods for dependency injection
/// </summary>
public static class CacheInvalidationServiceExtensions
{
    /// <summary>
    /// Add cache invalidation services to the service collection
    /// </summary>
    public static IServiceCollection AddCacheInvalidationServices(this IServiceCollection services)
    {
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
        return services;
    }
}