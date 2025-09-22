using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 评论仓储实现
/// 提供评论的数据访问和复杂查询功能
/// </summary>
public class CommentRepository : BlogBaseRepository<Comment>, ICommentRepository
{
    public CommentRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 根据文章ID获取评论列表
    /// </summary>
    public async Task<IEnumerable<Comment>> GetByPostIdAsync(
        Guid postId,
        bool includeReplies = true,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Where(c => c.PostId == postId && !c.IsDeleted);

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        if (!includeReplies)
        {
            query = query.Where(c => c.ParentId == null);
        }

        return await query
            .OrderBy(c => c.ThreadPath.GetSortWeight())
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 分页获取文章评论
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPagedByPostIdAsync(
        Guid postId,
        int pageNumber = 1,
        int pageSize = 20,
        bool onlyApproved = true,
        CommentSortBy sortBy = CommentSortBy.CreatedAtDesc,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Where(c => c.PostId == postId && !c.IsDeleted);

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        // 应用排序
        query = ApplySorting(query, sortBy);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取指定评论的直接回复
    /// </summary>
    public async Task<IEnumerable<Comment>> GetRepliesAsync(
        Guid parentId,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Where(c => c.ParentId == parentId && !c.IsDeleted);

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        return await query
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取指定评论的所有后代回复（递归）
    /// </summary>
    public async Task<IEnumerable<Comment>> GetDescendantsAsync(
        Guid commentId,
        int? maxDepth = null,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        var parentComment = await _dbSet
            .Where(c => c.Id == commentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (parentComment?.ThreadPath == null)
            return Enumerable.Empty<Comment>();

        var descendantPattern = parentComment.ThreadPath.GetDescendantQueryPattern();

        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Where(c => EF.Functions.Like(c.ThreadPath.Path, descendantPattern) && !c.IsDeleted);

        if (maxDepth.HasValue)
        {
            query = query.Where(c => c.ThreadPath.Depth <= parentComment.ThreadPath.Depth + maxDepth.Value);
        }

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        return await query
            .OrderBy(c => c.ThreadPath.GetSortWeight())
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据线程路径获取评论
    /// </summary>
    public async Task<Comment?> GetByThreadPathAsync(
        ThreadPath threadPath,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .FirstOrDefaultAsync(c => c.ThreadPath.Path == threadPath.Path, cancellationToken);
    }

    /// <summary>
    /// 获取用户的评论列表
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => c.AuthorId == userId && !c.IsDeleted);

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取待审核的评论列表
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPendingModerationAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => c.Status == CommentStatus.Pending && !c.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .OrderBy(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取被举报的评论列表
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetReportedCommentsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        int minReportCount = 1,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Include(c => c.Reports)
            .Where(c => c.ReportCount >= minReportCount && !c.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .OrderByDescending(c => c.ReportCount)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 搜索评论
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchAsync(
        string keyword,
        Guid? postId = null,
        Guid? userId = null,
        CommentStatus? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => !c.IsDeleted);

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(c =>
                c.Content.ProcessedContent.Contains(keyword) ||
                c.Content.RawContent.Contains(keyword) ||
                (c.Author != null && c.Author.UserName.Contains(keyword)));
        }

        // 文章筛选
        if (postId.HasValue)
        {
            query = query.Where(c => c.PostId == postId.Value);
        }

        // 用户筛选
        if (userId.HasValue)
        {
            query = query.Where(c => c.AuthorId == userId.Value);
        }

        // 状态筛选
        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        // 日期范围筛选
        if (dateFrom.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= dateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取评论的完整线程
    /// </summary>
    public async Task<IEnumerable<Comment>> GetThreadAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var comment = await _dbSet
            .Where(c => c.Id == commentId)
            .Select(c => new { c.ThreadPath, c.PostId })
            .FirstOrDefaultAsync(cancellationToken);

        if (comment?.ThreadPath == null)
            return Enumerable.Empty<Comment>();

        // 获取线程中的所有祖先和后代
        var rootId = comment.ThreadPath.RootId;
        var rootPattern = rootId.ToString() + "%";

        return await _dbSet
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .Where(c => c.PostId == comment.PostId &&
                       EF.Functions.Like(c.ThreadPath.Path, rootPattern) &&
                       !c.IsDeleted)
            .OrderBy(c => c.ThreadPath.GetSortWeight())
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取热门评论
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetPopularCommentsAsync(
        Guid? postId = null,
        int days = 7,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => c.CreatedAt >= cutoffDate &&
                       c.Status == CommentStatus.Approved &&
                       !c.IsDeleted);

        if (postId.HasValue)
        {
            query = query.Where(c => c.PostId == postId.Value);
        }

        // 按热度排序（点赞数 + 回复数）
        query = query.OrderByDescending(c => c.LikeCount + c.ReplyCount)
                     .ThenByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取最新评论
    /// </summary>
    public async Task<IEnumerable<Comment>> GetLatestCommentsAsync(
        int count = 10,
        bool onlyApproved = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => !c.IsDeleted);

        if (onlyApproved)
        {
            query = query.Where(c => c.Status == CommentStatus.Approved);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 统计评论数据
    /// </summary>
    public async Task<CommentStatistics> GetStatisticsAsync(
        Guid? postId = null,
        Guid? userId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable().Where(c => !c.IsDeleted);

        if (postId.HasValue)
            query = query.Where(c => c.PostId == postId.Value);

        if (userId.HasValue)
            query = query.Where(c => c.AuthorId == userId.Value);

        if (dateFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(c => c.CreatedAt <= dateTo.Value);

        var totalComments = await query.CountAsync(cancellationToken);
        var publishedComments = await query.CountAsync(c => c.Status == CommentStatus.Approved, cancellationToken);
        var pendingComments = await query.CountAsync(c => c.Status == CommentStatus.Pending, cancellationToken);
        var spamComments = await query.CountAsync(c => c.Status == CommentStatus.Spam, cancellationToken);
        var hiddenComments = await query.CountAsync(c => c.Status == CommentStatus.Hidden, cancellationToken);
        var rootComments = await query.CountAsync(c => c.ParentId == null, cancellationToken);
        var replyComments = await query.CountAsync(c => c.ParentId != null, cancellationToken);

        var averageLength = await query
            .Where(c => c.Content.ProcessedContent.Length > 0)
            .AverageAsync(c => (double?)c.Content.ProcessedContent.Length, cancellationToken) ?? 0;

        var maxNestingLevel = await query.MaxAsync(c => (int?)c.ThreadPath.Depth, cancellationToken) ?? 0;

        var activeCommenters = await query
            .Select(c => c.AuthorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalLikes = await query.SumAsync(c => c.LikeCount, cancellationToken);
        var totalReports = await query.SumAsync(c => c.ReportCount, cancellationToken);

        // 计算平均每篇文章评论数
        var postCount = postId.HasValue ? 1 : await query.Select(c => c.PostId).Distinct().CountAsync(cancellationToken);
        var averageCommentsPerPost = postCount > 0 ? (double)totalComments / postCount : 0;

        return new CommentStatistics
        {
            TotalComments = totalComments,
            PublishedComments = publishedComments,
            PendingComments = pendingComments,
            SpamComments = spamComments,
            HiddenComments = hiddenComments,
            RootComments = rootComments,
            ReplyComments = replyComments,
            AverageCommentsPerPost = averageCommentsPerPost,
            AverageCommentLength = averageLength,
            MaxNestingLevel = maxNestingLevel,
            ActiveCommenters = activeCommenters,
            TotalLikes = totalLikes,
            TotalReports = totalReports
        };
    }

    /// <summary>
    /// 批量更新评论状态
    /// </summary>
    public async Task<int> BatchUpdateStatusAsync(
        IEnumerable<Guid> commentIds,
        CommentStatus status,
        Guid moderatorId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var comments = await _dbSet
            .Where(c => commentIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        foreach (var comment in comments)
        {
            var action = GetModerationActionFromStatus(status);
            comment.Moderate(moderatorId, action, reason);
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 重建评论的线程路径
    /// </summary>
    public async Task<int> RebuildThreadPathsAsync(
        Guid? postId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (postId.HasValue)
            query = query.Where(c => c.PostId == postId.Value);

        var comments = await query.ToListAsync(cancellationToken);
        var updatedCount = 0;

        // 先处理根评论
        var rootComments = comments.Where(c => c.ParentId == null).ToList();
        foreach (var rootComment in rootComments)
        {
            var expectedThreadPath = ThreadPath.CreateRoot(rootComment.Id);
            if (rootComment.ThreadPath.Path != expectedThreadPath.Path)
            {
                rootComment.ThreadPath = expectedThreadPath;
                updatedCount++;
            }
        }

        // 递归处理子评论
        foreach (var rootComment in rootComments)
        {
            updatedCount += await RebuildChildrenPaths(rootComment, comments, cancellationToken);
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return updatedCount;
    }

    /// <summary>
    /// 检查用户是否对评论有权限
    /// </summary>
    public async Task<bool> CheckPermissionAsync(
        Guid commentId,
        Guid userId,
        CommentAction action,
        CancellationToken cancellationToken = default)
    {
        var comment = await _dbSet
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment == null)
            return false;

        return action switch
        {
            CommentAction.View => comment.IsPubliclyVisible() || comment.AuthorId == userId,
            CommentAction.Edit => comment.CanBeEditedBy(userId),
            CommentAction.Delete => comment.CanBeDeletedBy(userId),
            CommentAction.Reply => comment.IsPubliclyVisible(),
            CommentAction.Like => comment.IsPubliclyVisible() && comment.AuthorId != userId,
            CommentAction.Report => comment.IsPubliclyVisible() && comment.AuthorId != userId,
            CommentAction.Moderate => true, // 这里应该检查用户角色权限
            _ => false
        };
    }

    /// <summary>
    /// 应用排序逻辑
    /// </summary>
    private static IQueryable<Comment> ApplySorting(IQueryable<Comment> query, CommentSortBy sortBy)
    {
        return sortBy switch
        {
            CommentSortBy.CreatedAtAsc => query.OrderBy(c => c.CreatedAt),
            CommentSortBy.CreatedAtDesc => query.OrderByDescending(c => c.CreatedAt),
            CommentSortBy.LikeCountDesc => query.OrderByDescending(c => c.LikeCount).ThenByDescending(c => c.CreatedAt),
            CommentSortBy.ReplyCountDesc => query.OrderByDescending(c => c.ReplyCount).ThenByDescending(c => c.CreatedAt),
            CommentSortBy.ThreadPath => query.OrderBy(c => c.ThreadPath.GetSortWeight()).ThenBy(c => c.CreatedAt),
            CommentSortBy.Popularity => query.OrderByDescending(c => c.LikeCount + c.ReplyCount).ThenByDescending(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };
    }

    /// <summary>
    /// 递归重建子评论的线程路径
    /// </summary>
    private async Task<int> RebuildChildrenPaths(Comment parent, List<Comment> allComments, CancellationToken cancellationToken)
    {
        var children = allComments.Where(c => c.ParentId == parent.Id).ToList();
        var updatedCount = 0;

        foreach (var child in children)
        {
            var expectedPath = parent.ThreadPath.CreateChildPath(child.Id);

            if (child.ThreadPath.Path != expectedPath.Path)
            {
                child.ThreadPath = expectedPath;
                updatedCount++;
            }

            // 递归处理子评论的子评论
            updatedCount += await RebuildChildrenPaths(child, allComments, cancellationToken);
        }

        return updatedCount;
    }

    /// <summary>
    /// 根据状态获取审核动作
    /// </summary>
    private static ModerationAction GetModerationActionFromStatus(CommentStatus status)
    {
        return status switch
        {
            CommentStatus.Approved => ModerationAction.Approve,
            CommentStatus.Hidden => ModerationAction.Hide,
            CommentStatus.Spam => ModerationAction.MarkAsSpam,
            CommentStatus.Deleted => ModerationAction.Delete,
            CommentStatus.Rejected => ModerationAction.Hide,
            CommentStatus.Pending => ModerationAction.Review,
            _ => ModerationAction.Review
        };
    }

    // 以下是缺失的接口方法实现

    /// <summary>
    /// 获取包含详细信息的评论
    /// </summary>
    public async Task<Comment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Include(c => c.Parent)
            .Include(c => c.Replies)
            .Include(c => c.Likes)
            .Include(c => c.Reports)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// 获取评论查询对象
    /// </summary>
    public async Task<IQueryable<Comment>> GetCommentsQueryableAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return _dbSet
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Likes);
    }

    /// <summary>
    /// 根据ID列表获取评论
    /// </summary>
    public async Task<IEnumerable<Comment>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (!idList.Any())
            return Enumerable.Empty<Comment>();

        return await _dbSet
            .Where(c => idList.Contains(c.Id) && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Post)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取根评论（分页）
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> GetRootCommentsAsync(Guid postId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(c => c.PostId == postId && c.ParentId == null && !c.IsDeleted && c.Status == CommentStatus.Approved);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .OrderBy(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取用户评论查询对象
    /// </summary>
    public async Task<IQueryable<Comment>> GetUserCommentsQueryableAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbSet
            .Where(c => c.AuthorId == userId && !c.IsDeleted)
            .Include(c => c.Post)
            .Include(c => c.Likes);
    }

    /// <summary>
    /// 搜索评论
    /// </summary>
    public async Task<(IEnumerable<Comment> Comments, int TotalCount)> SearchCommentsAsync(
        string searchTerm,
        Guid? postId = null,
        Guid? userId = null,
        CommentStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable()
            .Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => c.Content.ProcessedContent.Contains(searchTerm) ||
                                   c.Content.RawContent.Contains(searchTerm));
        }

        if (postId.HasValue)
            query = query.Where(c => c.PostId == postId.Value);

        if (userId.HasValue)
            query = query.Where(c => c.AuthorId == userId.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var comments = await query
            .Include(c => c.Author)
            .Include(c => c.Post)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (comments, totalCount);
    }

    /// <summary>
    /// 获取文章评论统计
    /// </summary>
    public async Task<CommentStatistics> GetPostCommentStatsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable().Where(c => c.PostId == postId && !c.IsDeleted);

        var totalComments = await query.CountAsync(cancellationToken);
        var publishedComments = await query.CountAsync(c => c.Status == CommentStatus.Approved, cancellationToken);
        var pendingComments = await query.CountAsync(c => c.Status == CommentStatus.Pending, cancellationToken);
        var spamComments = await query.CountAsync(c => c.Status == CommentStatus.Spam, cancellationToken);
        var hiddenComments = await query.CountAsync(c => c.Status == CommentStatus.Hidden, cancellationToken);
        var rootComments = await query.CountAsync(c => c.ParentId == null, cancellationToken);
        var replyComments = await query.CountAsync(c => c.ParentId != null, cancellationToken);

        var averageLength = await query
            .Where(c => c.Content.ProcessedContent.Length > 0)
            .AverageAsync(c => (double?)c.Content.ProcessedContent.Length, cancellationToken) ?? 0;

        var maxNestingLevel = await query.MaxAsync(c => (int?)c.ThreadPath.Depth, cancellationToken) ?? 0;

        var activeCommenters = await query
            .Select(c => c.AuthorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var totalLikes = await query.SumAsync(c => c.LikeCount, cancellationToken);
        var totalReports = await query.SumAsync(c => c.ReportCount, cancellationToken);

        return new CommentStatistics
        {
            TotalComments = totalComments,
            PublishedComments = publishedComments,
            PendingComments = pendingComments,
            SpamComments = spamComments,
            HiddenComments = hiddenComments,
            RootComments = rootComments,
            ReplyComments = replyComments,
            AverageCommentsPerPost = totalComments, // For single post, this equals total comments
            AverageCommentLength = averageLength,
            MaxNestingLevel = maxNestingLevel,
            ActiveCommenters = activeCommenters,
            TotalLikes = totalLikes,
            TotalReports = totalReports
        };
    }

    /// <summary>
    /// 获取用户评论统计
    /// </summary>
    public async Task<CommentStatistics> GetUserCommentStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable().Where(c => c.AuthorId == userId && !c.IsDeleted);

        var totalComments = await query.CountAsync(cancellationToken);
        var publishedComments = await query.CountAsync(c => c.Status == CommentStatus.Approved, cancellationToken);
        var pendingComments = await query.CountAsync(c => c.Status == CommentStatus.Pending, cancellationToken);
        var spamComments = await query.CountAsync(c => c.Status == CommentStatus.Spam, cancellationToken);
        var hiddenComments = await query.CountAsync(c => c.Status == CommentStatus.Hidden, cancellationToken);
        var rootComments = await query.CountAsync(c => c.ParentId == null, cancellationToken);
        var replyComments = await query.CountAsync(c => c.ParentId != null, cancellationToken);

        var averageLength = await query
            .Where(c => c.Content.ProcessedContent.Length > 0)
            .AverageAsync(c => (double?)c.Content.ProcessedContent.Length, cancellationToken) ?? 0;

        var maxNestingLevel = await query.MaxAsync(c => (int?)c.ThreadPath.Depth, cancellationToken) ?? 0;

        var totalLikes = await query.SumAsync(c => c.LikeCount, cancellationToken);
        var totalReports = await query.SumAsync(c => c.ReportCount, cancellationToken);

        var postCount = await query.Select(c => c.PostId).Distinct().CountAsync(cancellationToken);
        var averageCommentsPerPost = postCount > 0 ? (double)totalComments / postCount : 0;

        return new CommentStatistics
        {
            TotalComments = totalComments,
            PublishedComments = publishedComments,
            PendingComments = pendingComments,
            SpamComments = spamComments,
            HiddenComments = hiddenComments,
            RootComments = rootComments,
            ReplyComments = replyComments,
            AverageCommentsPerPost = averageCommentsPerPost,
            AverageCommentLength = averageLength,
            MaxNestingLevel = maxNestingLevel,
            ActiveCommenters = 1, // For single user stats, this is always 1
            TotalLikes = totalLikes,
            TotalReports = totalReports
        };
    }

    /// <summary>
    /// 获取基础查询对象
    /// </summary>
    public IQueryable<Comment> GetQueryable()
    {
        return _dbSet.Where(c => !c.IsDeleted);
    }

    /// <summary>
    /// 获取文章评论
    /// </summary>
    public async Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.PostId == postId && !c.IsDeleted && c.Status == CommentStatus.Approved)
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .OrderBy(c => c.ThreadPath.GetSortWeight())
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子评论
    /// </summary>
    public async Task<IEnumerable<Comment>> GetChildCommentsAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.ParentId == parentId && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Likes)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 按状态统计
    /// </summary>
    public async Task<int> CountByStatusAsync(CommentStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(c => c.Status == status && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// 统计被举报评论数量
    /// </summary>
    public async Task<int> CountReportedCommentsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(c => c.ReportCount > 0 && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// 按日期统计
    /// </summary>
    public async Task<int> CountByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startDate = date.Date;
        var endDate = startDate.AddDays(1);
        return await _dbSet.CountAsync(c => c.CreatedAt >= startDate && c.CreatedAt < endDate && !c.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// 获取周度审核统计
    /// </summary>
    public async Task<object> GetWeeklyModerationStatsAsync(CancellationToken cancellationToken = default)
    {
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var totalModerated = await _dbSet.CountAsync(c => c.ModeratedAt.HasValue && c.ModeratedAt >= weekAgo, cancellationToken);
        var approved = await _dbSet.CountAsync(c => c.Status == CommentStatus.Approved && c.ModeratedAt.HasValue && c.ModeratedAt >= weekAgo, cancellationToken);
        var rejected = await _dbSet.CountAsync(c => c.Status == CommentStatus.Rejected && c.ModeratedAt.HasValue && c.ModeratedAt >= weekAgo, cancellationToken);

        return new
        {
            TotalModerated = totalModerated,
            Approved = approved,
            Rejected = rejected,
            Period = "Last 7 days"
        };
    }

    /// <summary>
    /// 获取AI审核统计
    /// </summary>
    public async Task<object> GetAIModerationStatsAsync(CancellationToken cancellationToken = default)
    {
        var aiModerated = await _dbSet.CountAsync(c => c.AIModerated, cancellationToken);
        var aiApproved = await _dbSet.CountAsync(c => c.AIModerated && c.Status == CommentStatus.Approved, cancellationToken);
        var aiRejected = await _dbSet.CountAsync(c => c.AIModerated && c.Status == CommentStatus.Rejected, cancellationToken);

        return new
        {
            TotalAIModerated = aiModerated,
            AIApproved = aiApproved,
            AIRejected = aiRejected,
            AIAccuracy = aiModerated > 0 ? (double)aiApproved / aiModerated * 100 : 0
        };
    }

    /// <summary>
    /// 获取用户审核统计
    /// </summary>
    public async Task<object> GetUserModerationStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userModerated = await _dbSet.CountAsync(c => c.ModeratedBy == userId, cancellationToken);
        var approved = await _dbSet.CountAsync(c => c.ModeratedBy == userId && c.Status == CommentStatus.Approved, cancellationToken);
        var rejected = await _dbSet.CountAsync(c => c.ModeratedBy == userId && c.Status == CommentStatus.Rejected, cancellationToken);

        return new
        {
            TotalModerated = userModerated,
            Approved = approved,
            Rejected = rejected,
            ApprovalRate = userModerated > 0 ? (double)approved / userModerated * 100 : 0
        };
    }
}