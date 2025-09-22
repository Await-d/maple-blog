using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 评论点赞仓储接口
/// </summary>
public interface ICommentLikeRepository : IRepository<CommentLike>
{
    /// <summary>
    /// 检查用户是否已点赞评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已点赞</returns>
    Task<bool> HasLikedAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的点赞记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞记录</returns>
    Task<CommentLike?> GetUserLikeAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论的所有点赞记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞记录列表</returns>
    Task<(IEnumerable<CommentLike> Likes, int TotalCount)> GetByCommentIdAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有点赞记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞记录列表</returns>
    Task<(IEnumerable<CommentLike> Likes, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论的点赞数量
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞数量</returns>
    Task<int> GetLikeCountAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取多个评论的点赞数量
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论ID和点赞数量的字典</returns>
    Task<Dictionary<Guid, int>> GetLikeCountsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户对多个评论的点赞状态
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论ID和是否已点赞的字典</returns>
    Task<Dictionary<Guid, bool>> GetUserLikeStatusAsync(
        IEnumerable<Guid> commentIds,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加点赞记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>点赞记录</returns>
    Task<CommentLike> AddLikeAsync(
        Guid commentId,
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除点赞记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功移除</returns>
    Task<bool> RemoveLikeAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 切换点赞状态
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的点赞状态（true表示已点赞，false表示已取消点赞）</returns>
    Task<bool> ToggleLikeAsync(
        Guid commentId,
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近的点赞活动
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="count">获取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近点赞记录</returns>
    Task<IEnumerable<CommentLike>> GetRecentLikesAsync(
        int days = 7,
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取热门评论（按点赞数排序）
    /// </summary>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="days">统计天数</param>
    /// <param name="count">获取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门评论列表</returns>
    Task<IEnumerable<(Comment Comment, int LikeCount)>> GetMostLikedCommentsAsync(
        Guid? postId = null,
        int days = 30,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取点赞统计信息
    /// </summary>
    /// <param name="dateFrom">开始日期（可选）</param>
    /// <param name="dateTo">结束日期（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<CommentLikeStatistics> GetStatisticsAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除点赞记录
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的记录数</returns>
    Task<int> BatchDeleteByCommentIdsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理重复的点赞记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数</returns>
    Task<int> CleanupDuplicateLikesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 评论点赞统计信息
/// </summary>
public record CommentLikeStatistics
{
    /// <summary>
    /// 总点赞数
    /// </summary>
    public int TotalLikes { get; init; }

    /// <summary>
    /// 活跃点赞用户数
    /// </summary>
    public int ActiveLikers { get; init; }

    /// <summary>
    /// 被点赞的评论数
    /// </summary>
    public int LikedCommentsCount { get; init; }

    /// <summary>
    /// 平均每个评论的点赞数
    /// </summary>
    public double AverageLikesPerComment { get; init; }

    /// <summary>
    /// 平均每个用户的点赞数
    /// </summary>
    public double AverageLikesPerUser { get; init; }

    /// <summary>
    /// 最多点赞的评论信息
    /// </summary>
    public (Guid CommentId, int LikeCount)? MostLikedComment { get; init; }

    /// <summary>
    /// 最活跃的点赞用户
    /// </summary>
    public (Guid UserId, int LikeCount)? MostActiveLiker { get; init; }

    /// <summary>
    /// 每日点赞趋势（最近30天）
    /// </summary>
    public Dictionary<DateTime, int> DailyLikeTrend { get; init; } = new();
}