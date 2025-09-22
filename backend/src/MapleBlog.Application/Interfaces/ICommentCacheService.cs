using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 评论缓存服务接口
    /// </summary>
    public interface ICommentCacheService
    {
        /// <summary>
        /// 缓存评论
        /// </summary>
        Task CacheCommentAsync(Comment comment, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存的评论
        /// </summary>
        Task<Comment?> GetCachedCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 缓存评论列表
        /// </summary>
        Task CacheCommentsAsync(string cacheKey, IEnumerable<Comment> comments, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存的评论列表
        /// </summary>
        Task<IEnumerable<Comment>?> GetCachedCommentsAsync(string cacheKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除评论缓存
        /// </summary>
        Task RemoveCommentCacheAsync(Guid commentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除文章评论缓存
        /// </summary>
        Task RemovePostCommentsCacheAsync(Guid postId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清空所有评论缓存
        /// </summary>
        Task ClearAllCommentCacheAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 缓存评论统计信息
        /// </summary>
        Task CacheCommentStatsAsync(Guid postId, object stats, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存的评论统计信息
        /// </summary>
        Task<T?> GetCachedCommentStatsAsync<T>(Guid postId, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 预热评论缓存
        /// </summary>
        Task WarmUpCommentCacheAsync(Guid postId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存键
        /// </summary>
        string GetCommentCacheKey(Guid commentId);

        /// <summary>
        /// 获取文章评论缓存键
        /// </summary>
        string GetPostCommentsCacheKey(Guid postId, int page = 1, int pageSize = 20);

        /// <summary>
        /// 获取评论统计缓存键
        /// </summary>
        string GetCommentStatsCacheKey(Guid postId);

        /// <summary>
        /// 移除文章评论缓存
        /// </summary>
        Task RemovePostCommentsAsync(Guid postId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除评论缓存
        /// </summary>
        Task RemoveCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取评论缓存
        /// </summary>
        Task<Comment?> GetCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置评论缓存
        /// </summary>
        Task SetCommentAsync(Comment comment, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取评论页缓存
        /// </summary>
        Task<object?> GetCommentPageAsync(string cacheKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// 设置评论页缓存
        /// </summary>
        Task SetCommentPageAsync(string cacheKey, object data, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        Task CleanupExpiredCacheAsync(CancellationToken cancellationToken = default);
    }
}