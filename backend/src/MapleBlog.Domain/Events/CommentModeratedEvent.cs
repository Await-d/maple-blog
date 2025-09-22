using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Events;

/// <summary>
/// 评论审核事件
/// </summary>
public record CommentModeratedEvent : DomainEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public override string EventName => "CommentModerated";

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 评论作者ID
    /// </summary>
    public Guid CommentAuthorId { get; init; }

    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 审核者ID
    /// </summary>
    public Guid ModeratorId { get; init; }

    /// <summary>
    /// 原状态
    /// </summary>
    public CommentStatus PreviousStatus { get; init; }

    /// <summary>
    /// 新状态
    /// </summary>
    public CommentStatus NewStatus { get; init; }

    /// <summary>
    /// 审核原因
    /// </summary>
    public string? ModerationReason { get; init; }

    /// <summary>
    /// 审核动作
    /// </summary>
    public ModerationAction Action { get; init; }

    /// <summary>
    /// 是否自动审核
    /// </summary>
    public bool IsAutoModeration { get; init; }

    /// <summary>
    /// 审核者信息
    /// </summary>
    public Moderator ModeratorInfo { get; init; }

    /// <summary>
    /// 评论信息
    /// </summary>
    public ModeratedComment Comment { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="commentAuthorId">评论作者ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="previousStatus">原状态</param>
    /// <param name="newStatus">新状态</param>
    /// <param name="moderationReason">审核原因</param>
    /// <param name="action">审核动作</param>
    /// <param name="isAutoModeration">是否自动审核</param>
    /// <param name="moderatorInfo">审核者信息</param>
    /// <param name="comment">评论信息</param>
    public CommentModeratedEvent(
        Guid commentId,
        Guid commentAuthorId,
        Guid postId,
        Guid moderatorId,
        CommentStatus previousStatus,
        CommentStatus newStatus,
        string? moderationReason,
        ModerationAction action,
        bool isAutoModeration,
        Moderator moderatorInfo,
        ModeratedComment comment)
    {
        CommentId = commentId;
        CommentAuthorId = commentAuthorId;
        PostId = postId;
        ModeratorId = moderatorId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ModerationReason = moderationReason;
        Action = action;
        IsAutoModeration = isAutoModeration;
        ModeratorInfo = moderatorInfo;
        Comment = comment;
    }

    /// <summary>
    /// 从评论实体创建事件
    /// </summary>
    /// <param name="comment">评论实体</param>
    /// <param name="previousStatus">原状态</param>
    /// <param name="moderator">审核者</param>
    /// <param name="isAutoModeration">是否自动审核</param>
    /// <returns>评论审核事件</returns>
    public static CommentModeratedEvent FromComment(
        Comment comment,
        CommentStatus previousStatus,
        User? moderator = null,
        bool isAutoModeration = false)
    {
        var moderatorInfo = new Moderator(
            comment.ModeratedBy ?? Guid.Empty,
            moderator?.UserName ?? (isAutoModeration ? "系统" : "未知"),
            moderator?.Email,
            isAutoModeration
        );

        var commentInfo = new ModeratedComment(
            comment.Id,
            comment.Content?.ProcessedContent ?? string.Empty,
            comment.RawContent,
            comment.AuthorId,
            comment.PostId,
            comment.ParentId,
            comment.Level,
            comment.ReportCount
        );

        var action = GetModerationAction(previousStatus, comment.Status);

        return new CommentModeratedEvent(
            commentId: comment.Id,
            commentAuthorId: comment.AuthorId,
            postId: comment.PostId,
            moderatorId: comment.ModeratedBy ?? Guid.Empty,
            previousStatus: previousStatus,
            newStatus: comment.Status,
            moderationReason: comment.ModerationReason,
            action: action,
            isAutoModeration: isAutoModeration,
            moderatorInfo: moderatorInfo,
            comment: commentInfo
        );
    }

    /// <summary>
    /// 检查是否需要通知评论作者
    /// </summary>
    /// <returns>是否需要通知</returns>
    public bool ShouldNotifyAuthor()
    {
        // 当评论被隐藏或标记为垃圾时通知作者
        return NewStatus is CommentStatus.Hidden or CommentStatus.Spam;
    }

    /// <summary>
    /// 检查是否为惩罚性审核
    /// </summary>
    /// <returns>是否为惩罚性审核</returns>
    public bool IsPunitive()
    {
        return Action is ModerationAction.Hide or ModerationAction.MarkAsSpam or ModerationAction.Delete;
    }

    /// <summary>
    /// 检查是否为恢复性审核
    /// </summary>
    /// <returns>是否为恢复性审核</returns>
    public bool IsRestorative()
    {
        return Action is ModerationAction.Approve or ModerationAction.Restore;
    }

    /// <summary>
    /// 获取审核动作描述
    /// </summary>
    /// <returns>动作描述</returns>
    public string GetActionDescription()
    {
        return Action switch
        {
            ModerationAction.Approve => "批准发布",
            ModerationAction.Hide => "隐藏评论",
            ModerationAction.MarkAsSpam => "标记为垃圾",
            ModerationAction.Delete => "删除评论",
            ModerationAction.Restore => "恢复评论",
            ModerationAction.Review => "标记待审核",
            _ => "未知动作"
        };
    }

    /// <summary>
    /// 根据状态变化确定审核动作
    /// </summary>
    /// <param name="previousStatus">原状态</param>
    /// <param name="newStatus">新状态</param>
    /// <returns>审核动作</returns>
    private static ModerationAction GetModerationAction(CommentStatus previousStatus, CommentStatus newStatus)
    {
        return (previousStatus, newStatus) switch
        {
            (CommentStatus.Pending, CommentStatus.Published) => ModerationAction.Approve,
            (CommentStatus.Published, CommentStatus.Hidden) => ModerationAction.Hide,
            (CommentStatus.Published, CommentStatus.Spam) => ModerationAction.MarkAsSpam,
            (CommentStatus.Hidden, CommentStatus.Published) => ModerationAction.Restore,
            (CommentStatus.Spam, CommentStatus.Published) => ModerationAction.Restore,
            (_, CommentStatus.Pending) => ModerationAction.Review,
            _ => ModerationAction.Review
        };
    }
}


/// <summary>
/// 审核者信息
/// </summary>
/// <param name="ModeratorId">审核者ID</param>
/// <param name="Username">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="IsAutoModerator">是否为自动审核</param>
public record Moderator(
    Guid ModeratorId,
    string Username,
    string? Email = null,
    bool IsAutoModerator = false
);

/// <summary>
/// 被审核的评论信息
/// </summary>
/// <param name="CommentId">评论ID</param>
/// <param name="Content">评论内容</param>
/// <param name="RawContent">原始内容</param>
/// <param name="AuthorId">作者ID</param>
/// <param name="PostId">文章ID</param>
/// <param name="ParentId">父评论ID</param>
/// <param name="Level">评论层级</param>
/// <param name="ReportCount">举报数</param>
public record ModeratedComment(
    Guid CommentId,
    string Content,
    string RawContent,
    Guid AuthorId,
    Guid PostId,
    Guid? ParentId,
    int Level,
    int ReportCount
);