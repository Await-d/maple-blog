using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 评论缓存服务实现
/// </summary>
public class CommentCacheService : ICommentCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CommentCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    // 缓存键前缀
    private const string CommentKeyPrefix = "comment:";
    private const string PostCommentsKeyPrefix = "post_comments:";
    private const string UserLikeStatusKeyPrefix = "user_likes:";
    private const string CommentLikeCountsKeyPrefix = "comment_like_counts:";
    private const string PopularCommentsKeyPrefix = "popular_comments:";
    private const string LatestCommentsKeyPrefix = "latest_comments:";
    private const string CommentThreadKeyPrefix = "comment_thread:";

    // 默认缓存时间
    private static readonly TimeSpan DefaultCommentExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan DefaultListExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultStatisticsExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultPopularExpiration = TimeSpan.FromHours(1);

    public CommentCacheService(IDistributedCache cache, ILogger<CommentCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region ICommentCacheService Implementation

    public async Task CacheCommentAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await SetCommentAsync(comment, cancellationToken: cancellationToken);
    }

    public async Task<Comment?> GetCachedCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        return await GetCommentAsync(commentId, cancellationToken);
    }

    public async Task CacheCommentsAsync(string cacheKey, IEnumerable<Comment> comments, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(comments, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultListExpiration
            };

            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache comments with key {CacheKey}", cacheKey);
        }
    }

    public async Task<IEnumerable<Comment>?> GetCachedCommentsAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<List<Comment>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached comments with key {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task RemoveCommentCacheAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await RemoveCommentAsync(commentId, cancellationToken);
    }

    public async Task RemovePostCommentsCacheAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        await RemovePostCommentsAsync(postId, cancellationToken);
    }

    public async Task ClearAllCommentCacheAsync(CancellationToken cancellationToken = default)
    {
        await InvalidateAllCommentCachesAsync(cancellationToken);
    }

    public async Task CacheCommentStatsAsync(Guid postId, object stats, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentStatsCacheKey(postId);
            var json = JsonSerializer.Serialize(stats, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultStatisticsExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache comment stats for post {PostId}", postId);
        }
    }

    public async Task<T?> GetCachedCommentStatsAsync<T>(Guid postId, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var key = GetCommentStatsCacheKey(postId);
            var cached = await _cache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<T>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached comment stats for post {PostId}", postId);
            return null;
        }
    }

    public async Task WarmUpCommentCacheAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        // This would typically load comments from database and cache them
        // For now, this is a placeholder implementation
        await Task.CompletedTask;
    }

    public string GetCommentCacheKey(Guid commentId)
    {
        return GetCommentKey(commentId);
    }

    public string GetPostCommentsCacheKey(Guid postId, int page = 1, int pageSize = 20)
    {
        return $"{PostCommentsKeyPrefix}{postId}:page_{page}:size_{pageSize}";
    }

    public string GetCommentStatsCacheKey(Guid postId)
    {
        return $"comment_stats:{postId}";
    }

    public async Task<Comment?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentKey(commentId);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<Comment>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get comment {CommentId} from cache", commentId);
            return null;
        }
    }

    public async Task SetCommentAsync(Comment comment, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentKey(comment.Id);
            var json = JsonSerializer.Serialize(comment, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultCommentExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set comment {CommentId} in cache", comment.Id);
        }
    }

    public async Task<object?> GetCommentPageAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<object>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get comment page with key {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetCommentPageAsync(string cacheKey, object data, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? DefaultListExpiration
            };

            await _cache.SetStringAsync(cacheKey, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set comment page with key {CacheKey}", cacheKey);
        }
    }

    public async Task CleanupExpiredCacheAsync(CancellationToken cancellationToken = default)
    {
        // Redis automatically handles expired keys, so this is a no-op for Redis
        // For other cache providers, implement cleanup logic here
        await Task.CompletedTask;
    }

    public async Task RemoveCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentKey(commentId);
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove comment {CommentId} from cache", commentId);
        }
    }

    #endregion

    #region 评论列表缓存

    public async Task<IEnumerable<Comment>?> GetPostCommentsAsync(Guid postId, bool onlyApproved = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetPostCommentsKey(postId, onlyApproved);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<List<Comment>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get comments for post {PostId} from cache", postId);
            return null;
        }
    }

    public async Task SetPostCommentsAsync(Guid postId, IEnumerable<Comment> comments, bool onlyApproved = true, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetPostCommentsKey(postId, onlyApproved);
            var json = JsonSerializer.Serialize(comments, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultListExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set comments for post {PostId} in cache", postId);
        }
    }

    public async Task RemovePostCommentsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 移除所有相关的post评论缓存
            var approvedKey = GetPostCommentsKey(postId, true);
            var allKey = GetPostCommentsKey(postId, false);

            await Task.WhenAll(
                _cache.RemoveAsync(approvedKey, cancellationToken),
                _cache.RemoveAsync(allKey, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove comments for post {PostId} from cache", postId);
        }
    }

    #endregion

    #region 用户点赞状态缓存

    public async Task<Dictionary<Guid, bool>?> GetUserLikeStatusAsync(Guid userId, IEnumerable<Guid> commentIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetUserLikeStatusKey(userId);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            var allLikeStatus = JsonSerializer.Deserialize<Dictionary<Guid, bool>>(cached, JsonOptions);
            if (allLikeStatus == null)
                return null;

            // 只返回请求的评论ID的状态
            var result = new Dictionary<Guid, bool>();
            foreach (var commentId in commentIds)
            {
                if (allLikeStatus.TryGetValue(commentId, out var isLiked))
                {
                    result[commentId] = isLiked;
                }
            }

            return result.Any() ? result : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get user {UserId} like status from cache", userId);
            return null;
        }
    }

    public async Task SetUserLikeStatusAsync(Guid userId, Dictionary<Guid, bool> likeStatus, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetUserLikeStatusKey(userId);
            var json = JsonSerializer.Serialize(likeStatus, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultStatisticsExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set user {UserId} like status in cache", userId);
        }
    }

    public async Task UpdateUserLikeStatusAsync(Guid userId, Guid commentId, bool isLiked, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetUserLikeStatusKey(userId);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            var likeStatus = string.IsNullOrEmpty(cached)
                ? new Dictionary<Guid, bool>()
                : JsonSerializer.Deserialize<Dictionary<Guid, bool>>(cached, JsonOptions) ?? new Dictionary<Guid, bool>();

            likeStatus[commentId] = isLiked;

            await SetUserLikeStatusAsync(userId, likeStatus, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update user {UserId} like status for comment {CommentId}", userId, commentId);
        }
    }

    public async Task RemoveUserLikeStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetUserLikeStatusKey(userId);
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove user {UserId} like status from cache", userId);
        }
    }

    #endregion

    #region 评论统计缓存

    public async Task<Dictionary<Guid, int>?> GetCommentLikeCountsAsync(IEnumerable<Guid> commentIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentLikeCountsKey();
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            var allCounts = JsonSerializer.Deserialize<Dictionary<Guid, int>>(cached, JsonOptions);
            if (allCounts == null)
                return null;

            // 只返回请求的评论ID的计数
            var result = new Dictionary<Guid, int>();
            foreach (var commentId in commentIds)
            {
                if (allCounts.TryGetValue(commentId, out var count))
                {
                    result[commentId] = count;
                }
            }

            return result.Any() ? result : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get comment like counts from cache");
            return null;
        }
    }

    public async Task SetCommentLikeCountsAsync(Dictionary<Guid, int> likeCounts, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentLikeCountsKey();
            var json = JsonSerializer.Serialize(likeCounts, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultStatisticsExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set comment like counts in cache");
        }
    }

    public async Task UpdateCommentLikeCountAsync(Guid commentId, int newCount, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentLikeCountsKey();
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            var likeCounts = string.IsNullOrEmpty(cached)
                ? new Dictionary<Guid, int>()
                : JsonSerializer.Deserialize<Dictionary<Guid, int>>(cached, JsonOptions) ?? new Dictionary<Guid, int>();

            likeCounts[commentId] = newCount;

            await SetCommentLikeCountsAsync(likeCounts, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update like count for comment {CommentId}", commentId);
        }
    }

    #endregion

    #region 热门评论缓存

    public async Task<IEnumerable<Comment>?> GetPopularCommentsAsync(Guid? postId = null, int days = 7, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetPopularCommentsKey(postId, days);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<List<Comment>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get popular comments from cache");
            return null;
        }
    }

    public async Task SetPopularCommentsAsync(IEnumerable<Comment> comments, Guid? postId = null, int days = 7, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetPopularCommentsKey(postId, days);
            var json = JsonSerializer.Serialize(comments, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultPopularExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set popular comments in cache");
        }
    }

    public async Task RemovePopularCommentsAsync(Guid? postId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 移除特定post或全局的热门评论缓存
            var tasks = new List<Task>();

            if (postId.HasValue)
            {
                var key = GetPopularCommentsKey(postId, 7);
                tasks.Add(_cache.RemoveAsync(key, cancellationToken));
            }
            else
            {
                // 移除全局热门评论缓存
                for (int days = 1; days <= 30; days += 6) // 1, 7, 13, 19, 25
                {
                    var key = GetPopularCommentsKey(null, days);
                    tasks.Add(_cache.RemoveAsync(key, cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove popular comments from cache");
        }
    }

    #endregion

    #region 最新评论缓存

    public async Task<IEnumerable<Comment>?> GetLatestCommentsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetLatestCommentsKey(count);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<List<Comment>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get latest comments from cache");
            return null;
        }
    }

    public async Task SetLatestCommentsAsync(IEnumerable<Comment> comments, int count = 10, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetLatestCommentsKey(count);
            var json = JsonSerializer.Serialize(comments, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultListExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set latest comments in cache");
        }
    }

    public async Task RemoveLatestCommentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 移除常见数量的最新评论缓存
            var tasks = new[]
            {
                _cache.RemoveAsync(GetLatestCommentsKey(5), cancellationToken),
                _cache.RemoveAsync(GetLatestCommentsKey(10), cancellationToken),
                _cache.RemoveAsync(GetLatestCommentsKey(20), cancellationToken),
                _cache.RemoveAsync(GetLatestCommentsKey(50), cancellationToken)
            };

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove latest comments from cache");
        }
    }

    #endregion

    #region 评论线程缓存

    public async Task<IEnumerable<Comment>?> GetCommentThreadAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentThreadKey(commentId);
            var cached = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cached))
                return null;

            return JsonSerializer.Deserialize<List<Comment>>(cached, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get comment thread for {CommentId} from cache", commentId);
            return null;
        }
    }

    public async Task SetCommentThreadAsync(Guid commentId, IEnumerable<Comment> thread, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentThreadKey(commentId);
            var json = JsonSerializer.Serialize(thread, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? DefaultListExpiration
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set comment thread for {CommentId} in cache", commentId);
        }
    }

    public async Task RemoveCommentThreadAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetCommentThreadKey(commentId);
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove comment thread for {CommentId} from cache", commentId);
        }
    }

    #endregion

    #region 缓存失效方法

    public async Task InvalidateCommentCacheAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.WhenAll(
                RemoveCommentAsync(commentId, cancellationToken),
                RemoveCommentThreadAsync(commentId, cancellationToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for comment {CommentId}", commentId);
        }
    }

    public async Task InvalidatePostCommentCacheAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.WhenAll(
                RemovePostCommentsAsync(postId, cancellationToken),
                RemovePopularCommentsAsync(postId, cancellationToken),
                RemoveLatestCommentsAsync(cancellationToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for post {PostId} comments", postId);
        }
    }

    public async Task InvalidateUserCacheAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await RemoveUserLikeStatusAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    public async Task InvalidateAllCommentCachesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.WhenAll(
                RemovePopularCommentsAsync(cancellationToken: cancellationToken),
                RemoveLatestCommentsAsync(cancellationToken)
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate all comment caches");
        }
    }

    #endregion

    #region 私有辅助方法

    private static string GetCommentKey(Guid commentId) => $"{CommentKeyPrefix}{commentId}";

    private static string GetPostCommentsKey(Guid postId, bool onlyApproved) =>
        $"{PostCommentsKeyPrefix}{postId}:approved_{onlyApproved}";

    private static string GetUserLikeStatusKey(Guid userId) => $"{UserLikeStatusKeyPrefix}{userId}";

    private static string GetCommentLikeCountsKey() => $"{CommentLikeCountsKeyPrefix}all";

    private static string GetPopularCommentsKey(Guid? postId, int days) =>
        $"{PopularCommentsKeyPrefix}{(postId?.ToString() ?? "global")}:days_{days}";

    private static string GetLatestCommentsKey(int count) => $"{LatestCommentsKeyPrefix}count_{count}";

    private static string GetCommentThreadKey(Guid commentId) => $"{CommentThreadKeyPrefix}{commentId}";

    #endregion
}