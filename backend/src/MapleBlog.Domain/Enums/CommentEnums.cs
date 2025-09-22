namespace MapleBlog.Domain.Enums;

/// <summary>
/// 评论状态枚举
/// </summary>
public enum CommentStatus
{
    /// <summary>
    /// 待审核
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已批准
    /// </summary>
    Approved = 1,

    /// <summary>
    /// 已隐藏
    /// </summary>
    Hidden = 2,

    /// <summary>
    /// 标记为垃圾
    /// </summary>
    Spam = 3,

    /// <summary>
    /// 已删除
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 5,

    /// <summary>
    /// 已发布
    /// </summary>
    Published = 6
}

/// <summary>
/// 评论排序方式枚举
/// </summary>
public enum CommentSortBy
{
    /// <summary>
    /// 按创建时间升序
    /// </summary>
    CreatedAtAsc,

    /// <summary>
    /// 按创建时间降序
    /// </summary>
    CreatedAtDesc,

    /// <summary>
    /// 按点赞数降序
    /// </summary>
    LikeCountDesc,

    /// <summary>
    /// 按回复数降序
    /// </summary>
    ReplyCountDesc,

    /// <summary>
    /// 按线程路径（层次结构）
    /// </summary>
    ThreadPath,

    /// <summary>
    /// 按热度（综合点赞和回复）
    /// </summary>
    Popularity
}

/// <summary>
/// 评论操作类型枚举
/// </summary>
public enum CommentAction
{
    /// <summary>
    /// 查看
    /// </summary>
    View,

    /// <summary>
    /// 编辑
    /// </summary>
    Edit,

    /// <summary>
    /// 删除
    /// </summary>
    Delete,

    /// <summary>
    /// 回复
    /// </summary>
    Reply,

    /// <summary>
    /// 点赞
    /// </summary>
    Like,

    /// <summary>
    /// 举报
    /// </summary>
    Report,

    /// <summary>
    /// 审核
    /// </summary>
    Moderate
}

/// <summary>
/// 评论举报原因枚举
/// </summary>
public enum CommentReportReason
{
    /// <summary>
    /// 垃圾信息
    /// </summary>
    Spam = 0,

    /// <summary>
    /// 侮辱或骚扰
    /// </summary>
    Harassment = 1,

    /// <summary>
    /// 仇恨言论
    /// </summary>
    HateSpeech = 2,

    /// <summary>
    /// 不当内容
    /// </summary>
    InappropriateContent = 3,

    /// <summary>
    /// 虚假信息
    /// </summary>
    Misinformation = 4,

    /// <summary>
    /// 版权侵犯
    /// </summary>
    CopyrightViolation = 5,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99
}

/// <summary>
/// 评论举报状态枚举
/// </summary>
public enum CommentReportStatus
{
    /// <summary>
    /// 待处理
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 处理中
    /// </summary>
    InReview = 1,

    /// <summary>
    /// 已解决
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// 已驳回
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// 已关闭
    /// </summary>
    Closed = 4
}

/// <summary>
/// 评论举报处理动作枚举
/// </summary>
public enum CommentReportAction
{
    /// <summary>
    /// 无动作
    /// </summary>
    NoAction = 0,

    /// <summary>
    /// 隐藏评论
    /// </summary>
    HideComment = 1,

    /// <summary>
    /// 删除评论
    /// </summary>
    DeleteComment = 2,

    /// <summary>
    /// 警告用户
    /// </summary>
    WarnUser = 3,

    /// <summary>
    /// 禁言用户
    /// </summary>
    MuteUser = 4,

    /// <summary>
    /// 封禁用户
    /// </summary>
    BanUser = 5
}

/// <summary>
/// 审核动作枚举
/// </summary>
public enum ModerationAction
{
    /// <summary>
    /// 批准
    /// </summary>
    Approve = 0,

    /// <summary>
    /// 隐藏
    /// </summary>
    Hide = 1,

    /// <summary>
    /// 标记为垃圾
    /// </summary>
    MarkAsSpam = 2,

    /// <summary>
    /// 删除
    /// </summary>
    Delete = 3,

    /// <summary>
    /// 恢复
    /// </summary>
    Restore = 4,

    /// <summary>
    /// 待审核
    /// </summary>
    Review = 5
}

/// <summary>
/// 删除类型枚举
/// </summary>
public enum DeletionType
{
    /// <summary>
    /// 作者删除
    /// </summary>
    AuthorDelete = 0,

    /// <summary>
    /// 管理员删除
    /// </summary>
    AdminDelete = 1,

    /// <summary>
    /// 审核员删除
    /// </summary>
    ModeratorDelete = 2,

    /// <summary>
    /// 系统删除
    /// </summary>
    SystemDelete = 3,

    /// <summary>
    /// 批量删除
    /// </summary>
    BatchDelete = 4
}

/// <summary>
/// 内容审核结果枚举
/// </summary>
public enum ModerationResult
{
    /// <summary>
    /// 通过
    /// </summary>
    Approved = 0,

    /// <summary>
    /// 需要人工审核
    /// </summary>
    RequiresHumanReview = 1,

    /// <summary>
    /// 拒绝 - 垃圾信息
    /// </summary>
    RejectedSpam = 2,

    /// <summary>
    /// 拒绝 - 不当内容
    /// </summary>
    RejectedInappropriate = 3,

    /// <summary>
    /// 拒绝 - 仇恨言论
    /// </summary>
    RejectedHateSpeech = 4,

    /// <summary>
    /// 拒绝 - 敏感词
    /// </summary>
    RejectedSensitiveWords = 5
}

/// <summary>
/// 敏感词过滤级别枚举
/// </summary>
public enum SensitivityLevel
{
    /// <summary>
    /// 低 - 只过滤明显的敏感词
    /// </summary>
    Low = 0,

    /// <summary>
    /// 中等 - 过滤常见敏感词
    /// </summary>
    Medium = 1,

    /// <summary>
    /// 高 - 严格过滤
    /// </summary>
    High = 2,

    /// <summary>
    /// 极高 - 最严格过滤
    /// </summary>
    VeryHigh = 3
}

/// <summary>
/// 评论质量等级枚举
/// </summary>
public enum CommentQuality
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 低质量
    /// </summary>
    Poor = 1,

    /// <summary>
    /// 一般
    /// </summary>
    Fair = 2,

    /// <summary>
    /// 良好
    /// </summary>
    Good = 3,

    /// <summary>
    /// 优秀
    /// </summary>
    Excellent = 4
}

/// <summary>
/// 通知类型枚举（评论相关）
/// </summary>
public enum CommentNotificationType
{
    /// <summary>
    /// 新评论
    /// </summary>
    NewComment = 0,

    /// <summary>
    /// 评论回复
    /// </summary>
    CommentReply = 1,

    /// <summary>
    /// 评论点赞
    /// </summary>
    CommentLiked = 2,

    /// <summary>
    /// 评论被举报
    /// </summary>
    CommentReported = 3,

    /// <summary>
    /// 评论被审核
    /// </summary>
    CommentModerated = 4,

    /// <summary>
    /// 评论被删除
    /// </summary>
    CommentDeleted = 5,

    /// <summary>
    /// 评论提及
    /// </summary>
    CommentMention = 6
}

