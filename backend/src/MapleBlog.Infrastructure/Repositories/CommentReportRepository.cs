using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories;

/// <summary>
/// 评论举报仓储实现
/// </summary>
public class CommentReportRepository : BlogBaseRepository<CommentReport>, ICommentReportRepository
{
    public CommentReportRepository(BlogDbContext context) : base(context)
    {
    }

    /// <summary>
    /// 检查用户是否已举报评论
    /// </summary>
    public async Task<bool> HasReportedAsync(
        Guid commentId,
        Guid reporterId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            cr => cr.CommentId == commentId && cr.ReporterId == reporterId && !cr.IsDeleted,
            cancellationToken);
    }

    /// <summary>
    /// 获取用户的举报记录
    /// </summary>
    public async Task<CommentReport?> GetUserReportAsync(
        Guid commentId,
        Guid reporterId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Comment)
            .Include(cr => cr.Reporter)
            .FirstOrDefaultAsync(
                cr => cr.CommentId == commentId && cr.ReporterId == reporterId && !cr.IsDeleted,
                cancellationToken);
    }

    /// <summary>
    /// 获取评论的所有举报记录
    /// </summary>
    public async Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByCommentIdAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Reviewer)
            .Where(cr => cr.CommentId == commentId && !cr.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    /// <summary>
    /// 获取用户的所有举报记录
    /// </summary>
    public async Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByReporterIdAsync(
        Guid reporterId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reviewer)
            .Where(cr => cr.ReporterId == reporterId && !cr.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    /// <summary>
    /// 获取待处理的举报记录
    /// </summary>
    public async Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetPendingReportsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reporter)
            .Where(cr => cr.Status == CommentReportStatus.Pending && !cr.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderBy(cr => cr.CreatedAt) // 先进先出处理
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    /// <summary>
    /// 根据举报状态获取举报记录
    /// </summary>
    public async Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByStatusAsync(
        CommentReportStatus status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Reviewer)
            .Where(cr => cr.Status == status && !cr.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    /// <summary>
    /// 根据举报原因获取举报记录
    /// </summary>
    public async Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByReasonAsync(
        CommentReportReason reason,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Reviewer)
            .Where(cr => cr.Reason == reason && !cr.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var reports = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (reports, totalCount);
    }

    /// <summary>
    /// 获取评论的举报数量
    /// </summary>
    public async Task<int> GetReportCountAsync(
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(
            cr => cr.CommentId == commentId && !cr.IsDeleted,
            cancellationToken);
    }

    /// <summary>
    /// 获取多个评论的举报数量
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetReportCountsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default)
    {
        var commentIdList = commentIds.ToList();

        return await _dbSet
            .Where(cr => commentIdList.Contains(cr.CommentId) && !cr.IsDeleted)
            .GroupBy(cr => cr.CommentId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);
    }

    /// <summary>
    /// 获取被大量举报的评论
    /// </summary>
    public async Task<(IEnumerable<(Comment Comment, int ReportCount)> Comments, int TotalCount)> GetHeavilyReportedCommentsAsync(
        int minReportCount = 3,
        int days = 30,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var reportedComments = await _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Where(cr => cr.CreatedAt >= cutoffDate && !cr.IsDeleted)
            .GroupBy(cr => cr.Comment)
            .Select(g => new { Comment = g.Key, ReportCount = g.Count() })
            .Where(x => x.ReportCount >= minReportCount)
            .OrderByDescending(x => x.ReportCount)
            .ToListAsync(cancellationToken);

        var totalCount = reportedComments.Count;

        var pagedResults = reportedComments
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => (x.Comment!, x.ReportCount))
            .ToList();

        return (pagedResults, totalCount);
    }

    /// <summary>
    /// 创建举报记录
    /// </summary>
    public async Task<CommentReport> CreateReportAsync(
        Guid commentId,
        Guid reporterId,
        CommentReportReason reason,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        // 检查是否已经举报过
        var existingReport = await _dbSet
            .FirstOrDefaultAsync(
                cr => cr.CommentId == commentId && cr.ReporterId == reporterId,
                cancellationToken);

        if (existingReport != null)
        {
            if (existingReport.IsDeleted)
            {
                // 恢复已删除的举报记录
                existingReport.Restore();
                existingReport.Reason = reason;
                existingReport.Description = description;
                existingReport.Status = CommentReportStatus.Pending;
                await _context.SaveChangesAsync(cancellationToken);
                return existingReport;
            }
            else
            {
                // 已经举报过，直接返回
                return existingReport;
            }
        }

        // 创建新的举报记录
        var commentReport = new CommentReport
        {
            CommentId = commentId,
            ReporterId = reporterId,
            Reason = reason,
            Description = description,
            Status = CommentReportStatus.Pending,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _dbSet.Add(commentReport);

        // 更新评论的举报数
        var comment = await _context.Set<Comment>()
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment != null)
        {
            comment.IncreaseReportCount();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return commentReport;
    }

    /// <summary>
    /// 处理举报记录
    /// </summary>
    public async Task<bool> ProcessReportAsync(
        Guid reportId,
        Guid reviewerId,
        CommentReportStatus status,
        string? resolution = null,
        CommentReportAction? action = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _dbSet.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report == null)
            return false;

        report.Process(
            reviewerId,
            status,
            resolution,
            action
        );

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// 批量处理举报记录
    /// </summary>
    public async Task<int> BatchProcessReportsAsync(
        IEnumerable<Guid> reportIds,
        Guid reviewerId,
        CommentReportStatus status,
        string? resolution = null,
        CommentReportAction? action = null,
        CancellationToken cancellationToken = default)
    {
        var reportIdList = reportIds.ToList();

        var reports = await _dbSet
            .Where(r => reportIdList.Contains(r.Id))
            .ToListAsync(cancellationToken);

        foreach (var report in reports)
        {
            report.Process(
                reviewerId,
                status,
                resolution,
                action
            );
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 获取举报统计信息
    /// </summary>
    public async Task<CommentReportStatistics> GetStatisticsAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(cr => !cr.IsDeleted);

        if (dateFrom.HasValue)
            query = query.Where(cr => cr.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(cr => cr.CreatedAt <= dateTo.Value);

        var totalReports = await query.CountAsync(cancellationToken);
        var pendingReports = await query.CountAsync(cr => cr.Status == CommentReportStatus.Pending, cancellationToken);
        var resolvedReports = await query.CountAsync(cr => cr.Status == CommentReportStatus.Resolved, cancellationToken);
        var rejectedReports = await query.CountAsync(cr => cr.Status == CommentReportStatus.Rejected, cancellationToken);

        var activeReporters = await query.Select(cr => cr.ReporterId).Distinct().CountAsync(cancellationToken);
        var reportedCommentsCount = await query.Select(cr => cr.CommentId).Distinct().CountAsync(cancellationToken);

        // 计算平均处理时间
        var processedReports = await query
            .Where(cr => cr.ReviewedAt.HasValue)
            .Select(cr => new { cr.CreatedAt, cr.ReviewedAt })
            .ToListAsync(cancellationToken);

        var averageProcessingTimeHours = processedReports.Any()
            ? processedReports.Average(r => (r.ReviewedAt!.Value - r.CreatedAt).TotalHours)
            : 0;

        // 各举报原因的分布
        var reasonDistribution = await query
            .GroupBy(cr => cr.Reason)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        // 各处理状态的分布
        var statusDistribution = await query
            .GroupBy(cr => cr.Status)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        // 各处理动作的分布
        var actionDistribution = await query
            .Where(cr => cr.Action.HasValue)
            .GroupBy(cr => cr.Action!.Value)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        // 每日举报趋势（最近30天）
        var trendStartDate = DateTime.UtcNow.AddDays(-30).Date;
        var dailyTrend = await query
            .Where(cr => cr.CreatedAt >= trendStartDate)
            .GroupBy(cr => cr.CreatedAt.Date)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count(),
                cancellationToken);

        // 最活跃的举报者
        var topReporters = await query
            .GroupBy(cr => cr.ReporterId)
            .Select(g => new { UserId = g.Key, ReportCount = g.Count() })
            .OrderByDescending(x => x.ReportCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        // 被举报最多的评论
        var mostReportedComments = await query
            .GroupBy(cr => cr.CommentId)
            .Select(g => new { CommentId = g.Key, ReportCount = g.Count() })
            .OrderByDescending(x => x.ReportCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new CommentReportStatistics
        {
            TotalReports = totalReports,
            PendingReports = pendingReports,
            ResolvedReports = resolvedReports,
            RejectedReports = rejectedReports,
            ActiveReporters = activeReporters,
            ReportedCommentsCount = reportedCommentsCount,
            AverageProcessingTimeHours = averageProcessingTimeHours,
            ReasonDistribution = reasonDistribution,
            StatusDistribution = statusDistribution,
            ActionDistribution = actionDistribution,
            DailyReportTrend = dailyTrend,
            TopReporters = topReporters.Select(x => (x.UserId, x.ReportCount)),
            MostReportedComments = mostReportedComments.Select(x => (x.CommentId, x.ReportCount))
        };
    }

    /// <summary>
    /// 获取最近的举报活动
    /// </summary>
    public async Task<IEnumerable<CommentReport>> GetRecentReportsAsync(
        int days = 7,
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _dbSet
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Where(cr => cr.CreatedAt >= cutoffDate && !cr.IsDeleted)
            .OrderByDescending(cr => cr.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 自动清理过期的已处理举报记录
    /// </summary>
    public async Task<int> CleanupProcessedReportsAsync(
        int daysToKeep = 365,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var expiredReports = await _dbSet
            .Where(cr => cr.Status != CommentReportStatus.Pending &&
                        cr.ReviewedAt.HasValue &&
                        cr.ReviewedAt.Value < cutoffDate &&
                        !cr.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var report in expiredReports)
        {
            report.SoftDelete();
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// 检测可疑的举报模式
    /// </summary>
    public async Task<IEnumerable<(Guid ReporterId, int ReportCount, string SuspiciousReason)>> DetectSuspiciousReportPatternsAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var suspiciousPatterns = new List<(Guid, int, string)>();

        // 检测大量举报的用户
        var heavyReporters = await _dbSet
            .Where(cr => cr.CreatedAt >= cutoffDate && !cr.IsDeleted)
            .GroupBy(cr => cr.ReporterId)
            .Select(g => new { ReporterId = g.Key, ReportCount = g.Count() })
            .Where(x => x.ReportCount > 50) // 超过50次举报
            .ToListAsync(cancellationToken);

        foreach (var reporter in heavyReporters)
        {
            suspiciousPatterns.Add((reporter.ReporterId, reporter.ReportCount, "举报次数异常频繁"));
        }

        // 检测总是举报相同用户的情况
        var targetedReporters = await _dbSet
            .Include(cr => cr.Comment)
            .Where(cr => cr.CreatedAt >= cutoffDate && !cr.IsDeleted)
            .GroupBy(cr => new { cr.ReporterId, TargetUserId = cr.Comment!.AuthorId })
            .Select(g => new { g.Key.ReporterId, g.Key.TargetUserId, ReportCount = g.Count() })
            .Where(x => x.ReportCount > 10) // 对同一用户举报超过10次
            .GroupBy(x => x.ReporterId)
            .Select(g => new { ReporterId = g.Key, MaxTargetReports = g.Max(x => x.ReportCount) })
            .Where(x => x.MaxTargetReports > 10)
            .ToListAsync(cancellationToken);

        foreach (var reporter in targetedReporters)
        {
            suspiciousPatterns.Add((reporter.ReporterId, reporter.MaxTargetReports, "针对性举报模式"));
        }

        return suspiciousPatterns;
    }

    /// <summary>
    /// 获取举报查询对象
    /// </summary>
    public IQueryable<CommentReport> GetReportsQueryable()
    {
        return _dbSet.AsNoTracking()
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Reviewer)
            .Where(cr => !cr.IsDeleted);
    }

    /// <summary>
    /// 根据ID获取包含详细信息的举报记录
    /// </summary>
    public async Task<CommentReport?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Post)
            .Include(cr => cr.Comment)
            .ThenInclude(c => c!.Author)
            .Include(cr => cr.Reporter)
            .Include(cr => cr.Reviewer)
            .FirstOrDefaultAsync(cr => cr.Id == id && !cr.IsDeleted, cancellationToken);
    }

    /// <summary>
    /// 根据举报原因统计数量
    /// </summary>
    public async Task<int> CountByReasonAsync(CommentReportReason reason, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cr => cr.Reason == reason && !cr.IsDeleted, cancellationToken);
    }
}