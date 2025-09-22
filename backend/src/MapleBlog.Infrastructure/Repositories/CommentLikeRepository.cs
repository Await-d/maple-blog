using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 评论点赞仓储实现
/// </summary>
public class CommentLikeRepository : BlogBaseRepository<CommentLike>, ICommentLikeRepository
{
    public CommentLikeRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 检查用户是否已点赞评论
    /// </summary>
    public async Task<bool> HasLikedAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            cl => cl.CommentId == commentId && cl.UserId == userId && !cl.IsDeleted,
            cancellationToken);
    }

    /// <summary>
    /// 获取用户的点赞记录
    /// </summary>
    public async Task<CommentLike?> GetUserLikeAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cl => cl.Comment)
            .Include(cl => cl.User)
            .FirstOrDefaultAsync(
                cl => cl.CommentId == commentId && cl.UserId == userId && !cl.IsDeleted,
                cancellationToken);
    }

    /// <summary>
    /// 获取评论的所有点赞记录
    /// </summary>
    public async Task<(IEnumerable<CommentLike> Likes, int TotalCount)> GetByCommentIdAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cl => cl.User)
            .Where(cl => cl.CommentId == commentId && !cl.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var likes = await query
            .OrderByDescending(cl => cl.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (likes, totalCount);
    }

    /// <summary>
    /// 获取用户的所有点赞记录
    /// </summary>
    public async Task<(IEnumerable<CommentLike> Likes, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cl => cl.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cl => cl.Comment)
            .ThenInclude(c => c!.Author)
            .Where(cl => cl.UserId == userId && !cl.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var likes = await query
            .OrderByDescending(cl => cl.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (likes, totalCount);
    }

    /// <summary>
    /// 获取评论的点赞数量
    /// </summary>
    public async Task<int> GetLikeCountAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(
            cl => cl.CommentId == commentId && !cl.IsDeleted,
            cancellationToken);
    }

    /// <summary>
    /// 获取多个评论的点赞数量
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetLikeCountsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default)
    {
        var commentIdList = commentIds.ToList();

        return await _dbSet
            .Where(cl => commentIdList.Contains(cl.CommentId) && !cl.IsDeleted)
            .GroupBy(cl => cl.CommentId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }

    /// <summary>
    /// 检查用户对多个评论的点赞状态
    /// </summary>
    public async Task<Dictionary<Guid, bool>> GetUserLikeStatusAsync(
        IEnumerable<Guid> commentIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var commentIdList = commentIds.ToList();

        var likedComments = await _dbSet
            .Where(cl => commentIdList.Contains(cl.CommentId) && cl.UserId == userId && !cl.IsDeleted)
            .Select(cl => cl.CommentId)
            .ToListAsync(cancellationToken);

        return commentIdList.ToDictionary(
            commentId => commentId,
            commentId => likedComments.Contains(commentId));
    }

    /// <summary>
    /// 添加点赞记录
    /// </summary>
    public async Task<CommentLike> AddLikeAsync(
        Guid commentId,
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        // 检查是否已经点赞
        var existingLike = await _dbSet
            .FirstOrDefaultAsync(
                cl => cl.CommentId == commentId && cl.UserId == userId,
                cancellationToken);

        if (existingLike != null)
        {
            if (existingLike.IsDeleted)
            {
                // 恢复已删除的点赞记录
                existingLike.Restore();
                await _context.SaveChangesAsync(cancellationToken);
                return existingLike;
            }
            else
            {
                // 已经点赞，直接返回
                return existingLike;
            }
        }

        // 创建新的点赞记录
        var commentLike = new CommentLike
        {
            CommentId = commentId,
            UserId = userId,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbSet.Add(commentLike);

        // 更新评论的点赞数
        var comment = await _context.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment != null)
        {
            comment.IncreaseLikeCount();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return commentLike;
    }

    /// <summary>
    /// 移除点赞记录
    /// </summary>
    public async Task<bool> RemoveLikeAsync(
        Guid commentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var commentLike = await _dbSet
            .FirstOrDefaultAsync(
                cl => cl.CommentId == commentId && cl.UserId == userId && !cl.IsDeleted,
                cancellationToken);

        if (commentLike == null)
            return false;

        // 软删除点赞记录
        commentLike.SoftDelete();

        // 更新评论的点赞数
        var comment = await _context.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment != null)
        {
            comment.DecreaseLikeCount();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 切换点赞状态
    /// </summary>
    public async Task<bool> ToggleLikeAsync(
        Guid commentId,
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var isLiked = await HasLikedAsync(commentId, userId, cancellationToken);

        if (isLiked)
        {
            await RemoveLikeAsync(commentId, userId, cancellationToken);
            return false; // 已取消点赞
        }
        else
        {
            await AddLikeAsync(commentId, userId, ipAddress, userAgent, cancellationToken);
            return true; // 已点赞
        }
    }

    /// <summary>
    /// 获取最近的点赞活动
    /// </summary>
    public async Task<IEnumerable<CommentLike>> GetRecentLikesAsync(
        int days = 7,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _dbSet
            .Include(cl => cl.User)
            .Include(cl => cl.Comment)
            .ThenInclude(c => c!.Post)
            .Where(cl => cl.CreatedAt >= cutoffDate && !cl.IsDeleted)
            .OrderByDescending(cl => cl.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取热门评论（按点赞数排序）
    /// </summary>
    public async Task<IEnumerable<(Comment Comment, int LikeCount)>> GetMostLikedCommentsAsync(
        Guid? postId = null,
        int days = 30,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var query = _dbSet
            .Include(cl => cl.Comment)
            .ThenInclude(c => c!.Author)
            .Where(cl => cl.CreatedAt >= cutoffDate && !cl.IsDeleted);

        if (postId.HasValue)
        {
            query = query.Where(cl => cl.Comment!.PostId == postId.Value);
        }

        return await query
            .GroupBy(cl => cl.Comment)
            .Select(g => new { Comment = g.Key, LikeCount = g.Count() })
            .OrderByDescending(x => x.LikeCount)
            .Take(count)
            .Select(x => ValueTuple.Create(x.Comment!, x.LikeCount))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取点赞统计信息
    /// </summary>
    public async Task<CommentLikeStatistics> GetStatisticsAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(cl => !cl.IsDeleted);

        if (dateFrom.HasValue)
            query = query.Where(cl => cl.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(cl => cl.CreatedAt <= dateTo.Value);

        var totalLikes = await query.CountAsync(cancellationToken);
        var activeLikers = await query.Select(cl => cl.UserId).Distinct().CountAsync(cancellationToken);
        var likedCommentsCount = await query.Select(cl => cl.CommentId).Distinct().CountAsync(cancellationToken);

        var averageLikesPerComment = likedCommentsCount > 0 ? (double)totalLikes / likedCommentsCount : 0;
        var averageLikesPerUser = activeLikers > 0 ? (double)totalLikes / activeLikers : 0;

        // 最多点赞的评论
        var mostLikedComment = await query
            .GroupBy(cl => cl.CommentId)
            .Select(g => new { CommentId = g.Key, LikeCount = g.Count() })
            .OrderByDescending(x => x.LikeCount)
            .FirstOrDefaultAsync(cancellationToken);

        // 最活跃的点赞用户
        var mostActiveLiker = await query
            .GroupBy(cl => cl.UserId)
            .Select(g => new { UserId = g.Key, LikeCount = g.Count() })
            .OrderByDescending(x => x.LikeCount)
            .FirstOrDefaultAsync(cancellationToken);

        // 每日点赞趋势（最近30天）
        var trendStartDate = DateTime.UtcNow.AddDays(-30).Date;
        var dailyTrend = await query
            .Where(cl => cl.CreatedAt >= trendStartDate)
            .GroupBy(cl => cl.CreatedAt.Date)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        return new CommentLikeStatistics
        {
            TotalLikes = totalLikes,
            ActiveLikers = activeLikers,
            LikedCommentsCount = likedCommentsCount,
            AverageLikesPerComment = averageLikesPerComment,
            AverageLikesPerUser = averageLikesPerUser,
            MostLikedComment = mostLikedComment != null
                ? (mostLikedComment.CommentId, mostLikedComment.LikeCount)
                : null,
            MostActiveLiker = mostActiveLiker != null
                ? (mostActiveLiker.UserId, mostActiveLiker.LikeCount)
                : null,
            DailyLikeTrend = dailyTrend
        };
    }

    /// <summary>
    /// 批量删除点赞记录
    /// </summary>
    public async Task<int> BatchDeleteByCommentIdsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default)
    {
        var commentIdList = commentIds.ToList();

        var likes = await _dbSet
            .Where(cl => commentIdList.Contains(cl.CommentId) && !cl.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var like in likes)
        {
            like.SoftDelete();
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 清理重复的点赞记录
    /// </summary>
    public async Task<int> CleanupDuplicateLikesAsync(
        CancellationToken cancellationToken = default)
    {
        // 查找重复的点赞记录
        var duplicates = await _dbSet
            .Where(cl => !cl.IsDeleted)
            .GroupBy(cl => new { cl.CommentId, cl.UserId })
            .Where(g => g.Count() > 1)
            .Select(g => new { Key = g.Key, Likes = g.OrderBy(cl => cl.CreatedAt).Skip(1) })
            .ToListAsync(cancellationToken);

        var cleanedCount = 0;

        foreach (var duplicate in duplicates)
        {
            foreach (var extraLike in duplicate.Likes)
            {
                extraLike.SoftDelete();
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return cleanedCount;
    }
}