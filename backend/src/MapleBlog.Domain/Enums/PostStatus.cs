namespace MapleBlog.Domain.Enums;

/// <summary>
/// 文章状态枚举
/// </summary>
public enum PostStatus
{
    /// <summary>
    /// 草稿
    /// </summary>
    Draft = 0,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 1,

    /// <summary>
    /// 已归档
    /// </summary>
    Archived = 2,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// 定时发布
    /// </summary>
    Scheduled = 4,

    /// <summary>
    /// 私有
    /// </summary>
    Private = 5
}

/// <summary>
/// 审计操作类型枚举
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// 创建
    /// </summary>
    Create = 0,

    /// <summary>
    /// 更新
    /// </summary>
    Update = 1,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 2,

    /// <summary>
    /// 登录
    /// </summary>
    Login = 3,

    /// <summary>
    /// 注销
    /// </summary>
    Logout = 4,

    /// <summary>
    /// 查看
    /// </summary>
    View = 5
}

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 评论回复
    /// </summary>
    CommentReply = 0,

    /// <summary>
    /// 文章点赞
    /// </summary>
    PostLiked = 1,

    /// <summary>
    /// 系统消息
    /// </summary>
    SystemMessage = 2,

    /// <summary>
    /// 用户关注
    /// </summary>
    UserFollowed = 3,

    /// <summary>
    /// 评论点赞
    /// </summary>
    CommentLiked = 4
}

/// <summary>
/// 通知优先级枚举
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// 低
    /// </summary>
    Low = 0,

    /// <summary>
    /// 正常
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 高
    /// </summary>
    High = 2,

    /// <summary>
    /// 紧急
    /// </summary>
    Urgent = 3
}

/// <summary>
/// 日志严重程度枚举
/// </summary>
public enum LogSeverity
{
    /// <summary>
    /// 调试
    /// </summary>
    Debug = 0,

    /// <summary>
    /// 信息
    /// </summary>
    Information = 1,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 3,

    /// <summary>
    /// 严重错误
    /// </summary>
    Critical = 4
}

/// <summary>
/// 内容格式类型枚举
/// </summary>
public enum ContentFormatType
{
    /// <summary>
    /// Markdown格式
    /// </summary>
    Markdown = 0,

    /// <summary>
    /// HTML格式
    /// </summary>
    Html = 1,

    /// <summary>
    /// 纯文本格式
    /// </summary>
    PlainText = 2,

    /// <summary>
    /// 富文本格式
    /// </summary>
    RichText = 3
}

/// <summary>
/// 标签受欢迎程度枚举
/// </summary>
public enum TagPopularityLevel
{
    /// <summary>
    /// 未使用 (0次)
    /// </summary>
    Unused = 0,

    /// <summary>
    /// 很少使用 (1-5次)
    /// </summary>
    Rare = 1,

    /// <summary>
    /// 偶尔使用 (6-15次)
    /// </summary>
    Occasional = 2,

    /// <summary>
    /// 常用 (16-50次)
    /// </summary>
    Common = 3,

    /// <summary>
    /// 受欢迎 (51-100次)
    /// </summary>
    Popular = 4,

    /// <summary>
    /// 非常受欢迎 (100次以上)
    /// </summary>
    VeryPopular = 5,

    /// <summary>
    /// 低使用率 (别名)
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中等使用率 (别名)
    /// </summary>
    Medium = 3,

    /// <summary>
    /// 高使用率 (别名)
    /// </summary>
    High = 4,

    /// <summary>
    /// 极高使用率 (别名)
    /// </summary>
    VeryHigh = 5
}

