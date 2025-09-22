using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// 评论审核数据传输对象
/// </summary>
public record CommentModerationDto
{
    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 评论作者
    /// </summary>
    public CommentAuthorDto? Author { get; init; }

    /// <summary>
    /// 文章标题
    /// </summary>
    public string PostTitle { get; init; } = string.Empty;

    /// <summary>
    /// 当前状态
    /// </summary>
    public CommentStatus Status { get; init; }

    /// <summary>
    /// 举报数
    /// </summary>
    public int ReportCount { get; init; }

    /// <summary>
    /// AI审核结果
    /// </summary>
    public ModerationResult? AIModerationResult { get; init; }

    /// <summary>
    /// AI审核置信度
    /// </summary>
    public double? AIModerationScore { get; init; }

    /// <summary>
    /// 是否包含敏感词
    /// </summary>
    public bool ContainsSensitiveWords { get; init; }

    /// <summary>
    /// 内容质量
    /// </summary>
    public CommentQuality Quality { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTime? ModeratedAt { get; init; }

    /// <summary>
    /// 审核者
    /// </summary>
    public CommentAuthorDto? Moderator { get; init; }

    /// <summary>
    /// 审核备注
    /// </summary>
    public string? ModerationNote { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 相关举报列表
    /// </summary>
    public IList<CommentReportDto> Reports { get; init; } = new List<CommentReportDto>();
}

/// <summary>
/// 评论举报信息
/// </summary>
public record CommentReportDto
{
    /// <summary>
    /// 举报ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 举报者
    /// </summary>
    public CommentAuthorDto? Reporter { get; init; }

    /// <summary>
    /// 举报原因
    /// </summary>
    public CommentReportReason Reason { get; init; }

    /// <summary>
    /// 举报描述
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 举报状态
    /// </summary>
    public CommentReportStatus Status { get; init; }

    /// <summary>
    /// 处理时间
    /// </summary>
    public DateTime? ProcessedAt { get; init; }

    /// <summary>
    /// 处理者
    /// </summary>
    public CommentAuthorDto? ProcessedBy { get; init; }

    /// <summary>
    /// 处理备注
    /// </summary>
    public string? ProcessNote { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// 审核操作请求
/// </summary>
public record CommentModerationActionDto
{
    /// <summary>
    /// 评论ID列表
    /// </summary>
    [Required]
    public IList<Guid> CommentIds { get; init; } = new List<Guid>();

    /// <summary>
    /// 审核动作
    /// </summary>
    [Required]
    public ModerationAction Action { get; init; }

    /// <summary>
    /// 审核备注
    /// </summary>
    [StringLength(500, ErrorMessage = "审核备注不能超过500字符")]
    public string? Note { get; init; }

    /// <summary>
    /// 是否发送通知给用户
    /// </summary>
    public bool SendNotification { get; init; } = true;
}

/// <summary>
/// 举报评论请求
/// </summary>
public record CommentReportRequestDto
{
    /// <summary>
    /// 举报原因
    /// </summary>
    [Required]
    public CommentReportReason Reason { get; init; }

    /// <summary>
    /// 举报描述
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "举报描述长度必须在10-500字符之间")]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 相关证据截图或链接
    /// </summary>
    public IList<string> Evidence { get; init; } = new List<string>();
}

/// <summary>
/// 处理举报请求
/// </summary>
public record CommentReportProcessDto
{
    /// <summary>
    /// 举报ID列表
    /// </summary>
    [Required]
    public IList<Guid> ReportIds { get; init; } = new List<Guid>();

    /// <summary>
    /// 处理动作
    /// </summary>
    [Required]
    public CommentReportProcessAction Action { get; init; }

    /// <summary>
    /// 处理备注
    /// </summary>
    [StringLength(500, ErrorMessage = "处理备注不能超过500字符")]
    public string? Note { get; init; }
}

/// <summary>
/// 举报处理动作
/// </summary>
public enum CommentReportProcessAction
{
    /// <summary>
    /// 驳回举报
    /// </summary>
    Dismiss,

    /// <summary>
    /// 确认举报并删除评论
    /// </summary>
    ConfirmAndDelete,

    /// <summary>
    /// 确认举报并隐藏评论
    /// </summary>
    ConfirmAndHide,

    /// <summary>
    /// 确认举报并标记为垃圾信息
    /// </summary>
    ConfirmAndMarkSpam,

    /// <summary>
    /// 需要进一步调查
    /// </summary>
    RequireInvestigation
}

/// <summary>
/// 审核队列查询参数
/// </summary>
public record CommentModerationQueryDto
{
    /// <summary>
    /// 状态过滤
    /// </summary>
    public CommentStatus[] StatusFilter { get; init; } = [CommentStatus.Pending];

    /// <summary>
    /// 是否只显示有举报的评论
    /// </summary>
    public bool OnlyReported { get; init; } = false;

    /// <summary>
    /// AI审核结果过滤
    /// </summary>
    public ModerationResult[] AIModerationFilter { get; init; } = [];

    /// <summary>
    /// 质量等级过滤
    /// </summary>
    public CommentQuality[] QualityFilter { get; init; } = [];

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 关键词搜索
    /// </summary>
    public string? Keyword { get; init; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public ModerationSortField SortBy { get; init; } = ModerationSortField.CreatedAt;

    /// <summary>
    /// 排序方向
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    /// <summary>
    /// 页码
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    [Range(1, 100, ErrorMessage = "每页大小必须在1-100之间")]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// 审核排序字段
/// </summary>
public enum ModerationSortField
{
    /// <summary>
    /// 创建时间
    /// </summary>
    CreatedAt,

    /// <summary>
    /// 举报数
    /// </summary>
    ReportCount,

    /// <summary>
    /// AI审核分数
    /// </summary>
    AIModerationScore,

    /// <summary>
    /// 内容质量
    /// </summary>
    Quality
}

/// <summary>
/// 排序方向
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// 升序
    /// </summary>
    Asc,

    /// <summary>
    /// 降序
    /// </summary>
    Desc
}

/// <summary>
/// 审核统计信息
/// </summary>
public record CommentModerationStatsDto
{
    /// <summary>
    /// 待审核评论数
    /// </summary>
    public int PendingCount { get; init; }

    /// <summary>
    /// 已举报评论数
    /// </summary>
    public int ReportedCount { get; init; }

    /// <summary>
    /// 今日新增评论数
    /// </summary>
    public int TodayCount { get; init; }

    /// <summary>
    /// 本周审核通过率
    /// </summary>
    public double WeeklyApprovalRate { get; init; }

    /// <summary>
    /// AI自动通过率
    /// </summary>
    public double AIApprovalRate { get; init; }

    /// <summary>
    /// 平均审核时长（分钟）
    /// </summary>
    public double AverageModerationTime { get; init; }

    /// <summary>
    /// 按状态分组的统计
    /// </summary>
    public IDictionary<CommentStatus, int> StatusCounts { get; init; } = new Dictionary<CommentStatus, int>();

    /// <summary>
    /// 按举报原因分组的统计
    /// </summary>
    public IDictionary<CommentReportReason, int> ReportReasonCounts { get; init; } = new Dictionary<CommentReportReason, int>();
}