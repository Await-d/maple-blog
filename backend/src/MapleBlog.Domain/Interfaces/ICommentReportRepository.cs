using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 评论举报仓储接口
/// </summary>
public interface ICommentReportRepository : IRepository<CommentReport>
{
    /// <summary>
    /// 检查用户是否已举报评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否已举报</returns>
    Task<bool> HasReportedAsync(
        Guid commentId,
        Guid reporterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的举报记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录</returns>
    Task<CommentReport?> GetUserReportAsync(
        Guid commentId,
        Guid reporterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论的所有举报记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录列表</returns>
    Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByCommentIdAsync(
        Guid commentId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有举报记录
    /// </summary>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录列表</returns>
    Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByReporterIdAsync(
        Guid reporterId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待处理的举报记录
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待处理举报记录</returns>
    Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetPendingReportsAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据举报状态获取举报记录
    /// </summary>
    /// <param name="status">举报状态</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录列表</returns>
    Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByStatusAsync(
        CommentReportStatus status,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据举报原因获取举报记录
    /// </summary>
    /// <param name="reason">举报原因</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录列表</returns>
    Task<(IEnumerable<CommentReport> Reports, int TotalCount)> GetByReasonAsync(
        CommentReportReason reason,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取评论的举报数量
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报数量</returns>
    Task<int> GetReportCountAsync(
        Guid commentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取多个评论的举报数量
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>评论ID和举报数量的字典</returns>
    Task<Dictionary<Guid, int>> GetReportCountsAsync(
        IEnumerable<Guid> commentIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取被大量举报的评论
    /// </summary>
    /// <param name="minReportCount">最小举报数量</param>
    /// <param name="days">天数范围</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>被大量举报的评论</returns>
    Task<(IEnumerable<(Comment Comment, int ReportCount)> Comments, int TotalCount)> GetHeavilyReportedCommentsAsync(
        int minReportCount = 3,
        int days = 30,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建举报记录
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="reporterId">举报者ID</param>
    /// <param name="reason">举报原因</param>
    /// <param name="description">详细描述</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报记录</returns>
    Task<CommentReport> CreateReportAsync(
        Guid commentId,
        Guid reporterId,
        CommentReportReason reason,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理举报记录
    /// </summary>
    /// <param name="reportId">举报记录ID</param>
    /// <param name="reviewerId">处理者ID</param>
    /// <param name="status">处理状态</param>
    /// <param name="resolution">处理结果</param>
    /// <param name="action">处理动作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否处理成功</returns>
    Task<bool> ProcessReportAsync(
        Guid reportId,
        Guid reviewerId,
        CommentReportStatus status,
        string? resolution = null,
        CommentReportAction? action = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量处理举报记录
    /// </summary>
    /// <param name="reportIds">举报记录ID列表</param>
    /// <param name="reviewerId">处理者ID</param>
    /// <param name="status">处理状态</param>
    /// <param name="resolution">处理结果</param>
    /// <param name="action">处理动作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理成功的数量</returns>
    Task<int> BatchProcessReportsAsync(
        IEnumerable<Guid> reportIds,
        Guid reviewerId,
        CommentReportStatus status,
        string? resolution = null,
        CommentReportAction? action = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取举报统计信息
    /// </summary>
    /// <param name="dateFrom">开始日期（可选）</param>
    /// <param name="dateTo">结束日期（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计信息</returns>
    Task<CommentReportStatistics> GetStatisticsAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近的举报活动
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="count">获取数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近举报记录</returns>
    Task<IEnumerable<CommentReport>> GetRecentReportsAsync(
        int days = 7,
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 自动清理过期的已处理举报记录
    /// </summary>
    /// <param name="daysToKeep">保留天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的记录数</returns>
    Task<int> CleanupProcessedReportsAsync(
        int daysToKeep = 365,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检测可疑的举报模式
    /// </summary>
    /// <param name="days">检测天数范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可疑举报者列表</returns>
    Task<IEnumerable<(Guid ReporterId, int ReportCount, string SuspiciousReason)>> DetectSuspiciousReportPatternsAsync(
        int days = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取举报记录查询接口
    /// </summary>
    /// <returns>举报记录查询接口</returns>
    IQueryable<CommentReport> GetReportsQueryable();

    /// <summary>
    /// 根据ID获取举报记录及详细信息
    /// </summary>
    /// <param name="reportId">举报记录ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含详细信息的举报记录</returns>
    Task<CommentReport?> GetByIdWithDetailsAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按举报原因统计数量
    /// </summary>
    /// <param name="reason">举报原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数量</returns>
    Task<int> CountByReasonAsync(CommentReportReason reason, CancellationToken cancellationToken = default);
}


/// <summary>
/// 评论举报统计信息
/// </summary>
public record CommentReportStatistics
{
    /// <summary>
    /// 总举报数
    /// </summary>
    public int TotalReports { get; init; }

    /// <summary>
    /// 待处理举报数
    /// </summary>
    public int PendingReports { get; init; }

    /// <summary>
    /// 已解决举报数
    /// </summary>
    public int ResolvedReports { get; init; }

    /// <summary>
    /// 已驳回举报数
    /// </summary>
    public int RejectedReports { get; init; }

    /// <summary>
    /// 活跃举报者数量
    /// </summary>
    public int ActiveReporters { get; init; }

    /// <summary>
    /// 被举报的评论数
    /// </summary>
    public int ReportedCommentsCount { get; init; }

    /// <summary>
    /// 平均处理时间（小时）
    /// </summary>
    public double AverageProcessingTimeHours { get; init; }

    /// <summary>
    /// 各举报原因的分布
    /// </summary>
    public Dictionary<CommentReportReason, int> ReasonDistribution { get; init; } = new();

    /// <summary>
    /// 各处理状态的分布
    /// </summary>
    public Dictionary<CommentReportStatus, int> StatusDistribution { get; init; } = new();

    /// <summary>
    /// 各处理动作的分布
    /// </summary>
    public Dictionary<CommentReportAction, int> ActionDistribution { get; init; } = new();

    /// <summary>
    /// 每日举报趋势（最近30天）
    /// </summary>
    public Dictionary<DateTime, int> DailyReportTrend { get; init; } = new();

    /// <summary>
    /// 最活跃的举报者
    /// </summary>
    public IEnumerable<(Guid UserId, int ReportCount)> TopReporters { get; init; } = new List<(Guid, int)>();

    /// <summary>
    /// 被举报最多的评论
    /// </summary>
    public IEnumerable<(Guid CommentId, int ReportCount)> MostReportedComments { get; init; } = new List<(Guid, int)>();
}