using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 用户交互仓储实现
/// </summary>
public class UserInteractionRepository : BlogBaseRepository<UserInteraction>, IUserInteractionRepository
{
    private readonly ILogger<UserInteractionRepository> _logger;

    public UserInteractionRepository(
        BlogDbContext context,
        ILogger<UserInteractionRepository> logger) : base(context)
    {
        _logger = logger;
    }

    /// <summary>
    /// 根据用户和文章获取交互记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="interactionType">交互类型</param>
    /// <returns>交互记录</returns>
    public async Task<UserInteraction?> GetInteractionAsync(Guid userId, Guid? postId, string interactionType)
    {
        return await _context.Set<UserInteraction>()
            .FirstOrDefaultAsync(ui => ui.UserId == userId &&
                                      ui.PostId == postId &&
                                      ui.InteractionType == interactionType);
    }

    /// <summary>
    /// 获取用户的交互历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>交互历史</returns>
    public async Task<(IEnumerable<UserInteraction> Interactions, int TotalCount)> GetUserInteractionsAsync(
        Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        var query = _context.Set<UserInteraction>()
            .Where(ui => ui.UserId == userId)
            .OrderByDescending(ui => ui.CreatedAt);

        var totalCount = await query.CountAsync();
        var interactions = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (interactions, totalCount);
    }

    /// <summary>
    /// 获取文章的交互统计
    /// </summary>
    /// <param name="postId">文章ID</param>
    /// <param name="interactionType">交互类型</param>
    /// <returns>交互统计</returns>
    public async Task<int> GetInteractionCountAsync(Guid? postId, string interactionType)
    {
        return await _context.Set<UserInteraction>()
            .CountAsync(ui => ui.PostId == postId && ui.InteractionType == interactionType);
    }

    /// <summary>
    /// 记录或更新交互
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="postId">文章ID（可选）</param>
    /// <param name="interactionType">交互类型</param>
    /// <param name="duration">交互时长</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="referrer">引荐来源</param>
    /// <param name="sessionId">会话ID</param>
    /// <param name="metadata">元数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>交互记录</returns>
    public async Task<UserInteraction> RecordInteractionAsync(
        Guid userId,
        Guid? postId = null,
        string interactionType = "view",
        TimeSpan? duration = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? referrer = null,
        string? sessionId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var newInteraction = new UserInteraction
        {
            UserId = userId,
            PostId = postId,
            InteractionType = interactionType,
            Duration = duration,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Referrer = referrer,
            SessionId = sessionId,
            Metadata = metadata
        };

        await AddAsync(newInteraction, cancellationToken);
        return newInteraction;
    }

    public async Task<IReadOnlyList<UserInteraction>> GetUserInteractionsAsync(
        Guid userId,
        string? interactionType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().Where(ui => ui.UserId == userId);

        if (!string.IsNullOrEmpty(interactionType))
            query = query.Where(ui => ui.InteractionType == interactionType);

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ui => ui.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(ui => ui.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserInteractedPostIdsAsync(
        Guid userId,
        IEnumerable<string>? interactionTypes = null,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>()
            .Where(ui => ui.UserId == userId && ui.PostId.HasValue);

        if (interactionTypes != null && interactionTypes.Any())
            query = query.Where(ui => interactionTypes.Contains(ui.InteractionType));

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        return await query
            .Select(ui => ui.PostId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserInteraction>> GetPostInteractionsAsync(
        Guid postId,
        string? interactionType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>()
            .Where(ui => ui.PostId == postId);

        if (!string.IsNullOrEmpty(interactionType))
            query = query.Where(ui => ui.InteractionType == interactionType);

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ui => ui.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(ui => ui.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserInteractionStats> GetUserInteractionStatsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().Where(ui => ui.UserId == userId);

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ui => ui.CreatedAt <= toDate.Value);

        var interactions = await query.ToListAsync(cancellationToken);

        return new UserInteractionStats
        {
            UserId = userId,
            TotalInteractions = interactions.Count,
            ViewCount = interactions.Count(i => i.InteractionType == "view"),
            LikeCount = interactions.Count(i => i.InteractionType == "like"),
            CommentCount = interactions.Count(i => i.InteractionType == "comment"),
            ShareCount = interactions.Count(i => i.InteractionType == "share"),
            TotalReadingTime = TimeSpan.FromMilliseconds(interactions.Where(i => i.Duration.HasValue).Sum(i => i.Duration!.Value.TotalMilliseconds)),
            FirstInteraction = interactions.MinBy(i => i.CreatedAt)?.CreatedAt,
            LastInteraction = interactions.MaxBy(i => i.CreatedAt)?.CreatedAt,
            UniquePostsInteracted = interactions.Where(i => i.PostId.HasValue).Select(i => i.PostId!.Value).Distinct().Count(),
            EngagementScore = CalculateEngagementScore(interactions)
        };
    }

    public async Task<PostInteractionStats> GetPostInteractionStatsAsync(
        Guid postId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().Where(ui => ui.PostId == postId);

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ui => ui.CreatedAt <= toDate.Value);

        var interactions = await query.ToListAsync(cancellationToken);

        return new PostInteractionStats
        {
            PostId = postId,
            TotalInteractions = interactions.Count,
            UniqueUsers = interactions.Select(i => i.UserId).Distinct().Count(),
            ViewCount = interactions.Count(i => i.InteractionType == "view"),
            LikeCount = interactions.Count(i => i.InteractionType == "like"),
            CommentCount = interactions.Count(i => i.InteractionType == "comment"),
            ShareCount = interactions.Count(i => i.InteractionType == "share"),
            AverageReadingTime = interactions.Where(i => i.Duration.HasValue).Any()
                ? TimeSpan.FromMilliseconds(interactions.Where(i => i.Duration.HasValue).Average(i => i.Duration!.Value.TotalMilliseconds))
                : TimeSpan.Zero,
            FirstInteraction = interactions.MinBy(i => i.CreatedAt)?.CreatedAt,
            LastInteraction = interactions.MaxBy(i => i.CreatedAt)?.CreatedAt,
            EngagementRate = CalculatePostEngagementRate(interactions)
        };
    }

    public async Task<IReadOnlyList<UserEngagementSummary>> GetMostEngagedUsersAsync(
        int count = 10,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        var userStats = await query
            .GroupBy(ui => ui.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                InteractionCount = g.Count(),
                LastActive = g.Max(ui => ui.CreatedAt),
                TotalReadingTime = g.Where(ui => ui.Duration.HasValue).Sum(ui => ui.Duration!.Value.TotalMilliseconds),
                UniquePostsRead = g.Where(ui => ui.PostId.HasValue).Select(ui => ui.PostId!.Value).Distinct().Count()
            })
            .OrderByDescending(s => s.InteractionCount)
            .Take(count)
            .ToListAsync(cancellationToken);

        return userStats.Select(s => new UserEngagementSummary
        {
            UserId = s.UserId,
            UserName = "",
            InteractionCount = s.InteractionCount,
            EngagementScore = s.InteractionCount * 0.5 + s.UniquePostsRead * 2.0,
            LastActive = s.LastActive,
            TotalReadingTime = TimeSpan.FromMilliseconds(s.TotalReadingTime),
            UniquePostsRead = s.UniquePostsRead
        }).ToList();
    }

    public async Task<IReadOnlyList<InteractionTrend>> GetInteractionTrendsAsync(
        string granularity = "day",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? interactionType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(ui => ui.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(ui => ui.CreatedAt <= toDate.Value);

        if (!string.IsNullOrEmpty(interactionType))
            query = query.Where(ui => ui.InteractionType == interactionType);

        var trends = await query
            .GroupBy(ui => new { Date = ui.CreatedAt.Date, ui.InteractionType })
            .Select(g => new InteractionTrend
            {
                Date = g.Key.Date,
                InteractionCount = g.Count(),
                UniqueUsers = g.Select(ui => ui.UserId).Distinct().Count(),
                InteractionType = g.Key.InteractionType
            })
            .OrderBy(t => t.Date)
            .ToListAsync(cancellationToken);

        return trends;
    }

    public async Task<int> CleanupOldInteractionsAsync(
        DateTime olderThan,
        IEnumerable<string>? keepInteractionTypes = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserInteraction>().Where(ui => ui.CreatedAt < olderThan);

        if (keepInteractionTypes != null && keepInteractionTypes.Any())
            query = query.Where(ui => !keepInteractionTypes.Contains(ui.InteractionType));

        var toDelete = await query.ToListAsync(cancellationToken);
        _context.Set<UserInteraction>().RemoveRange(toDelete);

        return toDelete.Count;
    }

    private static double CalculateEngagementScore(List<UserInteraction> interactions)
    {
        var viewWeight = 1.0;
        var likeWeight = 3.0;
        var commentWeight = 5.0;
        var shareWeight = 7.0;

        var views = interactions.Count(i => i.InteractionType == "view");
        var likes = interactions.Count(i => i.InteractionType == "like");
        var comments = interactions.Count(i => i.InteractionType == "comment");
        var shares = interactions.Count(i => i.InteractionType == "share");

        return (views * viewWeight) + (likes * likeWeight) + (comments * commentWeight) + (shares * shareWeight);
    }

    private static double CalculatePostEngagementRate(List<UserInteraction> interactions)
    {
        if (!interactions.Any()) return 0.0;

        var views = interactions.Count(i => i.InteractionType == "view");
        var engagements = interactions.Count(i => i.InteractionType != "view");

        return views > 0 ? (double)engagements / views * 100 : 0.0;
    }
}