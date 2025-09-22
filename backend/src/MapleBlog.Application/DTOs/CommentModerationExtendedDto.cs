using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// 评论审核历史记录
/// </summary>
public record CommentModerationHistoryDto
{
    /// <summary>
    /// 历史记录ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 审核者信息
    /// </summary>
    public CommentAuthorDto? Moderator { get; init; }

    /// <summary>
    /// 审核动作
    /// </summary>
    public ModerationAction Action { get; init; }

    /// <summary>
    /// 审核前状态
    /// </summary>
    public CommentStatus PreviousStatus { get; init; }

    /// <summary>
    /// 审核后状态
    /// </summary>
    public CommentStatus NewStatus { get; init; }

    /// <summary>
    /// 审核备注
    /// </summary>
    public string? Note { get; init; }

    /// <summary>
    /// 审核原因
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }
}

/// <summary>
/// 用户审核统计
/// </summary>
public record UserModerationStatsDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// 通过审核的评论数
    /// </summary>
    public int ApprovedComments { get; init; }

    /// <summary>
    /// 被拒绝的评论数
    /// </summary>
    public int RejectedComments { get; init; }

    /// <summary>
    /// 被举报的评论数
    /// </summary>
    public int ReportedComments { get; init; }

    /// <summary>
    /// 垃圾信息评论数
    /// </summary>
    public int SpamComments { get; init; }

    /// <summary>
    /// 审核通过率
    /// </summary>
    public double ApprovalRate { get; init; }

    /// <summary>
    /// 当前信任度评分
    /// </summary>
    public double CurrentTrustScore { get; init; }

    /// <summary>
    /// 最近30天的评论数
    /// </summary>
    public int RecentCommentCount { get; init; }

    /// <summary>
    /// 最后被举报时间
    /// </summary>
    public DateTime? LastReportedAt { get; init; }

    /// <summary>
    /// 最后审核时间
    /// </summary>
    public DateTime? LastModeratedAt { get; init; }

    /// <summary>
    /// 按状态分组的统计
    /// </summary>
    public IDictionary<CommentStatus, int> StatusCounts { get; init; } = new Dictionary<CommentStatus, int>();

    /// <summary>
    /// 月度趋势数据
    /// </summary>
    public IList<MonthlyModerationTrendDto> MonthlyTrend { get; init; } = new List<MonthlyModerationTrendDto>();
}

/// <summary>
/// 月度审核趋势
/// </summary>
public record MonthlyModerationTrendDto
{
    /// <summary>
    /// 年月（格式：YYYY-MM）
    /// </summary>
    public string Month { get; init; } = string.Empty;

    /// <summary>
    /// 评论总数
    /// </summary>
    public int TotalComments { get; init; }

    /// <summary>
    /// 通过数量
    /// </summary>
    public int ApprovedCount { get; init; }

    /// <summary>
    /// 拒绝数量
    /// </summary>
    public int RejectedCount { get; init; }

    /// <summary>
    /// 举报数量
    /// </summary>
    public int ReportedCount { get; init; }

    /// <summary>
    /// 通过率
    /// </summary>
    public double ApprovalRate { get; init; }
}

/// <summary>
/// 用户信任度历史
/// </summary>
public record UserTrustScoreHistoryDto
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 之前的信任度
    /// </summary>
    public double PreviousScore { get; init; }

    /// <summary>
    /// 新的信任度
    /// </summary>
    public double NewScore { get; init; }

    /// <summary>
    /// 变化原因
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// 操作者
    /// </summary>
    public CommentAuthorDto? Operator { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// 审核设置
/// </summary>
public record ModerationSettings
{
    /// <summary>
    /// 是否启用AI审核
    /// </summary>
    public bool EnableAIModeration { get; init; } = true;

    /// <summary>
    /// 垃圾信息阈值
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "阈值必须在0.0-1.0之间")]
    public double SpamThreshold { get; init; } = 0.7;

    /// <summary>
    /// 毒性内容阈值
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "阈值必须在0.0-1.0之间")]
    public double ToxicityThreshold { get; init; } = 0.8;

    /// <summary>
    /// 仇恨言论阈值
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "阈值必须在0.0-1.0之间")]
    public double HateSpeechThreshold { get; init; } = 0.9;

    /// <summary>
    /// 自动批准高信任度用户评论
    /// </summary>
    public bool AutoApproveHighTrustUsers { get; init; } = true;

    /// <summary>
    /// 高信任度用户阈值
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "阈值必须在0.0-1.0之间")]
    public double HighTrustUserThreshold { get; init; } = 0.8;

    /// <summary>
    /// 自动拒绝低信任度用户评论
    /// </summary>
    public bool AutoRejectLowTrustUsers { get; init; } = false;

    /// <summary>
    /// 低信任度用户阈值
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "阈值必须在0.0-1.0之间")]
    public double LowTrustUserThreshold { get; init; } = 0.3;

    /// <summary>
    /// 启用敏感词过滤
    /// </summary>
    public bool EnableSensitiveWordFilter { get; init; } = true;

    /// <summary>
    /// 新用户评论需要审核
    /// </summary>
    public bool RequireNewUserModeration { get; init; } = true;

    /// <summary>
    /// 新用户定义天数
    /// </summary>
    [Range(1, 365, ErrorMessage = "天数必须在1-365之间")]
    public int NewUserDays { get; init; } = 30;

    /// <summary>
    /// 包含链接的评论需要审核
    /// </summary>
    public bool RequireLinkModeration { get; init; } = true;

    /// <summary>
    /// 最大审核队列大小
    /// </summary>
    [Range(100, 10000, ErrorMessage = "队列大小必须在100-10000之间")]
    public int MaxQueueSize { get; init; } = 1000;

    /// <summary>
    /// 自动审核批次大小
    /// </summary>
    [Range(10, 100, ErrorMessage = "批次大小必须在10-100之间")]
    public int AutoModerationBatchSize { get; init; } = 50;

    /// <summary>
    /// 审核缓存过期时间（分钟）
    /// </summary>
    [Range(5, 1440, ErrorMessage = "缓存时间必须在5-1440分钟之间")]
    public int ModerationCacheMinutes { get; init; } = 60;
}

/// <summary>
/// 自动审核规则
/// </summary>
public record AutoModerationRuleDto
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 规则名称
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "规则名称不能超过100字符")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 规则描述
    /// </summary>
    [StringLength(500, ErrorMessage = "规则描述不能超过500字符")]
    public string? Description { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// 优先级（数字越大优先级越高）
    /// </summary>
    [Range(1, 100, ErrorMessage = "优先级必须在1-100之间")]
    public int Priority { get; init; } = 50;

    /// <summary>
    /// 规则条件
    /// </summary>
    public IList<ModerationRuleConditionDto> Conditions { get; init; } = new List<ModerationRuleConditionDto>();

    /// <summary>
    /// 匹配时的动作
    /// </summary>
    public ModerationAction Action { get; init; }

    /// <summary>
    /// 动作原因
    /// </summary>
    [StringLength(200, ErrorMessage = "动作原因不能超过200字符")]
    public string? ActionReason { get; init; }

    /// <summary>
    /// 是否发送通知
    /// </summary>
    public bool SendNotification { get; init; } = true;

    /// <summary>
    /// 规则创建者
    /// </summary>
    public CommentAuthorDto? Creator { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 规则匹配统计
    /// </summary>
    public RuleStatisticsDto? Statistics { get; init; }
}

/// <summary>
/// 审核规则条件
/// </summary>
public record ModerationRuleConditionDto
{
    /// <summary>
    /// 条件ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 条件类型
    /// </summary>
    public RuleConditionType Type { get; init; }

    /// <summary>
    /// 操作符
    /// </summary>
    public RuleOperator Operator { get; init; }

    /// <summary>
    /// 目标值
    /// </summary>
    [Required]
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 是否大小写敏感
    /// </summary>
    public bool CaseSensitive { get; init; } = false;

    /// <summary>
    /// 是否使用正则表达式
    /// </summary>
    public bool UseRegex { get; init; } = false;

    /// <summary>
    /// 条件权重
    /// </summary>
    [Range(0.1, 10.0, ErrorMessage = "条件权重必须在0.1-10.0之间")]
    public double Weight { get; init; } = 1.0;
}

/// <summary>
/// 规则条件类型
/// </summary>
public enum RuleConditionType
{
    /// <summary>
    /// 评论内容
    /// </summary>
    Content,

    /// <summary>
    /// 用户信任度
    /// </summary>
    UserTrustScore,

    /// <summary>
    /// 用户注册天数
    /// </summary>
    UserAge,

    /// <summary>
    /// 用户评论数
    /// </summary>
    UserCommentCount,

    /// <summary>
    /// IP地址
    /// </summary>
    IpAddress,

    /// <summary>
    /// 用户代理
    /// </summary>
    UserAgent,

    /// <summary>
    /// 评论长度
    /// </summary>
    ContentLength,

    /// <summary>
    /// 包含链接数量
    /// </summary>
    LinkCount,

    /// <summary>
    /// AI审核分数
    /// </summary>
    AIModerationScore,

    /// <summary>
    /// 敏感词数量
    /// </summary>
    SensitiveWordCount
}

/// <summary>
/// 规则操作符
/// </summary>
public enum RuleOperator
{
    /// <summary>
    /// 等于
    /// </summary>
    Equals,

    /// <summary>
    /// 不等于
    /// </summary>
    NotEquals,

    /// <summary>
    /// 包含
    /// </summary>
    Contains,

    /// <summary>
    /// 不包含
    /// </summary>
    NotContains,

    /// <summary>
    /// 开头是
    /// </summary>
    StartsWith,

    /// <summary>
    /// 结尾是
    /// </summary>
    EndsWith,

    /// <summary>
    /// 大于
    /// </summary>
    GreaterThan,

    /// <summary>
    /// 大于等于
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// 小于
    /// </summary>
    LessThan,

    /// <summary>
    /// 小于等于
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// 在范围内
    /// </summary>
    InRange,

    /// <summary>
    /// 不在范围内
    /// </summary>
    NotInRange,

    /// <summary>
    /// 匹配正则表达式
    /// </summary>
    MatchesRegex,

    /// <summary>
    /// 不匹配正则表达式
    /// </summary>
    NotMatchesRegex
}

/// <summary>
/// 规则统计信息
/// </summary>
public record RuleStatisticsDto
{
    /// <summary>
    /// 规则ID
    /// </summary>
    public Guid RuleId { get; init; }

    /// <summary>
    /// 总匹配次数
    /// </summary>
    public int TotalMatches { get; init; }

    /// <summary>
    /// 本周匹配次数
    /// </summary>
    public int WeeklyMatches { get; init; }

    /// <summary>
    /// 今日匹配次数
    /// </summary>
    public int DailyMatches { get; init; }

    /// <summary>
    /// 最后匹配时间
    /// </summary>
    public DateTime? LastMatchedAt { get; init; }

    /// <summary>
    /// 平均置信度
    /// </summary>
    public double AverageConfidence { get; init; }

    /// <summary>
    /// 误报率（如果有反馈）
    /// </summary>
    public double FalsePositiveRate { get; init; }

    /// <summary>
    /// 规则效率评分
    /// </summary>
    public double EfficiencyScore { get; init; }
}

/// <summary>
/// 通用分页结果
/// </summary>
public record CommentPagedResultDto<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public IList<T> Items { get; init; } = new List<T>();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage { get; init; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage { get; init; }
}