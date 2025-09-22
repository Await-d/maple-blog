using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// 评论通知数据传输对象
/// </summary>
public record CommentNotificationDto
{
    /// <summary>
    /// 通知ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 接收者ID
    /// </summary>
    public Guid RecipientId { get; init; }

    /// <summary>
    /// 发送者信息
    /// </summary>
    public CommentAuthorDto? Sender { get; init; }

    /// <summary>
    /// 评论信息
    /// </summary>
    public CommentNotificationCommentDto? Comment { get; init; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public CommentNotificationType Type { get; init; }

    /// <summary>
    /// 通知标题
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 相关链接
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// 是否已读
    /// </summary>
    public bool IsRead { get; init; }

    /// <summary>
    /// 读取时间
    /// </summary>
    public DateTime? ReadAt { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// 附加数据（JSON格式）
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// 通知中的评论信息
/// </summary>
public record CommentNotificationCommentDto
{
    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 文章标题
    /// </summary>
    public string PostTitle { get; init; } = string.Empty;

    /// <summary>
    /// 文章Slug
    /// </summary>
    public string PostSlug { get; init; } = string.Empty;

    /// <summary>
    /// 评论内容（可能是摘要）
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 父评论ID
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 评论状态
    /// </summary>
    public CommentStatus Status { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// 评论通知类型
/// </summary>
public enum CommentNotificationType
{
    /// <summary>
    /// 新回复通知
    /// </summary>
    NewReply = 1,

    /// <summary>
    /// 提及通知
    /// </summary>
    Mention = 2,

    /// <summary>
    /// 评论被点赞
    /// </summary>
    CommentLiked = 3,

    /// <summary>
    /// 评论审核通过
    /// </summary>
    CommentApproved = 4,

    /// <summary>
    /// 评论审核拒绝
    /// </summary>
    CommentRejected = 5,

    /// <summary>
    /// 评论被举报
    /// </summary>
    CommentReported = 6,

    /// <summary>
    /// 评论被删除
    /// </summary>
    CommentDeleted = 7,

    /// <summary>
    /// 文章有新评论（文章作者）
    /// </summary>
    NewCommentOnPost = 8,

    /// <summary>
    /// 系统通知
    /// </summary>
    SystemNotice = 9
}

/// <summary>
/// 创建通知请求
/// </summary>
public record CommentNotificationCreateDto
{
    /// <summary>
    /// 接收者ID列表
    /// </summary>
    [Required]
    public IList<Guid> RecipientIds { get; init; } = new List<Guid>();

    /// <summary>
    /// 发送者ID
    /// </summary>
    public Guid? SenderId { get; init; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid? CommentId { get; init; }

    /// <summary>
    /// 通知类型
    /// </summary>
    [Required]
    public CommentNotificationType Type { get; init; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "通知标题不能超过200字符")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 通知内容
    /// </summary>
    [Required]
    [StringLength(1000, ErrorMessage = "通知内容不能超过1000字符")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 相关链接
    /// </summary>
    [Url(ErrorMessage = "请输入有效的URL")]
    public string? Url { get; init; }

    /// <summary>
    /// 过期时间（如果为空则不过期）
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// 是否立即发送
    /// </summary>
    public bool SendImmediately { get; init; } = true;

    /// <summary>
    /// 预定发送时间
    /// </summary>
    public DateTime? ScheduledAt { get; init; }
}

/// <summary>
/// 通知查询参数
/// </summary>
public record CommentNotificationQueryDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// 通知类型过滤
    /// </summary>
    public CommentNotificationType[] TypeFilter { get; init; } = [];

    /// <summary>
    /// 是否只显示未读
    /// </summary>
    public bool UnreadOnly { get; init; } = false;

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
/// 批量操作通知请求
/// </summary>
public record CommentNotificationBatchActionDto
{
    /// <summary>
    /// 通知ID列表
    /// </summary>
    [Required]
    public IList<Guid> NotificationIds { get; init; } = new List<Guid>();

    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    public NotificationBatchAction Action { get; init; }
}

/// <summary>
/// 通知批量操作类型
/// </summary>
public enum NotificationBatchAction
{
    /// <summary>
    /// 标记为已读
    /// </summary>
    MarkAsRead,

    /// <summary>
    /// 标记为未读
    /// </summary>
    MarkAsUnread,

    /// <summary>
    /// 删除通知
    /// </summary>
    Delete,

    /// <summary>
    /// 归档通知
    /// </summary>
    Archive
}

/// <summary>
/// 通知设置
/// </summary>
public record CommentNotificationSettingsDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool EnableEmailNotifications { get; init; } = true;

    /// <summary>
    /// 是否启用浏览器通知
    /// </summary>
    public bool EnableBrowserNotifications { get; init; } = true;

    /// <summary>
    /// 是否启用移动推送通知
    /// </summary>
    public bool EnableMobileNotifications { get; init; } = true;

    /// <summary>
    /// 新回复通知设置
    /// </summary>
    public NotificationTypeSettings NewReplySettings { get; init; } = new();

    /// <summary>
    /// 提及通知设置
    /// </summary>
    public NotificationTypeSettings MentionSettings { get; init; } = new();

    /// <summary>
    /// 点赞通知设置
    /// </summary>
    public NotificationTypeSettings LikeSettings { get; init; } = new();

    /// <summary>
    /// 审核通知设置
    /// </summary>
    public NotificationTypeSettings ModerationSettings { get; init; } = new();

    /// <summary>
    /// 安静时间开始
    /// </summary>
    public TimeSpan? QuietTimeStart { get; init; }

    /// <summary>
    /// 安静时间结束
    /// </summary>
    public TimeSpan? QuietTimeEnd { get; init; }

    /// <summary>
    /// 通知频率限制（分钟）
    /// </summary>
    public int NotificationFrequencyLimit { get; init; } = 5;
}

/// <summary>
/// 通知类型设置
/// </summary>
public record NotificationTypeSettings
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// 是否发送邮件
    /// </summary>
    public bool SendEmail { get; init; } = true;

    /// <summary>
    /// 是否发送浏览器通知
    /// </summary>
    public bool SendBrowser { get; init; } = true;

    /// <summary>
    /// 是否发送移动推送
    /// </summary>
    public bool SendMobile { get; init; } = false;
}

/// <summary>
/// 通知统计信息
/// </summary>
public record CommentNotificationStatsDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 总通知数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 未读通知数
    /// </summary>
    public int UnreadCount { get; init; }

    /// <summary>
    /// 今日新通知数
    /// </summary>
    public int TodayCount { get; init; }

    /// <summary>
    /// 按类型分组的统计
    /// </summary>
    public IDictionary<CommentNotificationType, int> TypeCounts { get; init; } = new Dictionary<CommentNotificationType, int>();

    /// <summary>
    /// 按类型分组的未读统计
    /// </summary>
    public IDictionary<CommentNotificationType, int> UnreadTypeCounts { get; init; } = new Dictionary<CommentNotificationType, int>();
}

/// <summary>
/// 实时通知推送数据
/// </summary>
public record CommentNotificationPushDto
{
    /// <summary>
    /// 通知数据
    /// </summary>
    public CommentNotificationDto Notification { get; init; } = new();

    /// <summary>
    /// 推送类型
    /// </summary>
    public NotificationPushType PushType { get; init; }

    /// <summary>
    /// 目标用户ID列表
    /// </summary>
    public IList<Guid> TargetUserIds { get; init; } = new List<Guid>();

    /// <summary>
    /// 推送渠道
    /// </summary>
    public NotificationChannel[] Channels { get; init; } = [NotificationChannel.Web];
}

/// <summary>
/// 通知推送类型
/// </summary>
public enum NotificationPushType
{
    /// <summary>
    /// 新通知
    /// </summary>
    New,

    /// <summary>
    /// 通知更新
    /// </summary>
    Update,

    /// <summary>
    /// 通知删除
    /// </summary>
    Delete,

    /// <summary>
    /// 批量通知
    /// </summary>
    Batch
}

/// <summary>
/// 通知推送渠道
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// 网页推送
    /// </summary>
    Web,

    /// <summary>
    /// 邮件推送
    /// </summary>
    Email,

    /// <summary>
    /// 移动推送
    /// </summary>
    Mobile,

    /// <summary>
    /// 短信推送
    /// </summary>
    SMS,

    /// <summary>
    /// 微信推送
    /// </summary>
    WeChat
}