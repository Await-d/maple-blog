using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Events;

/// <summary>
/// 评论删除事件
/// </summary>
public record CommentDeletedEvent : DomainEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public override string EventName => "CommentDeleted";

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
    /// 删除者ID
    /// </summary>
    public Guid DeletedBy { get; init; }

    /// <summary>
    /// 父评论ID
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 子评论数量
    /// </summary>
    public int ChildrenCount { get; init; }

    /// <summary>
    /// 是否为软删除
    /// </summary>
    public bool IsSoftDelete { get; init; }

    /// <summary>
    /// 删除原因
    /// </summary>
    public string? DeletionReason { get; init; }

    /// <summary>
    /// 删除类型
    /// </summary>
    public DeletionType Type { get; init; }

    /// <summary>
    /// 评论信息
    /// </summary>
    public DeletedComment Comment { get; init; }

    /// <summary>
    /// 删除者信息
    /// </summary>
    public CommentDeleter Deleter { get; init; }

    /// <summary>
    /// 受影响的子评论ID列表
    /// </summary>
    public IEnumerable<Guid> AffectedChildrenIds { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="commentAuthorId">评论作者ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="deletedBy">删除者ID</param>
    /// <param name="parentId">父评论ID</param>
    /// <param name="childrenCount">子评论数量</param>
    /// <param name="isSoftDelete">是否为软删除</param>
    /// <param name="deletionReason">删除原因</param>
    /// <param name="type">删除类型</param>
    /// <param name="comment">评论信息</param>
    /// <param name="deleter">删除者信息</param>
    /// <param name="affectedChildrenIds">受影响的子评论ID</param>
    public CommentDeletedEvent(
        Guid commentId,
        Guid commentAuthorId,
        Guid postId,
        Guid deletedBy,
        Guid? parentId,
        int childrenCount,
        bool isSoftDelete,
        string? deletionReason,
        DeletionType type,
        DeletedComment comment,
        CommentDeleter deleter,
        IEnumerable<Guid> affectedChildrenIds)
    {
        CommentId = commentId;
        CommentAuthorId = commentAuthorId;
        PostId = postId;
        DeletedBy = deletedBy;
        ParentId = parentId;
        ChildrenCount = childrenCount;
        IsSoftDelete = isSoftDelete;
        DeletionReason = deletionReason;
        Type = type;
        Comment = comment;
        Deleter = deleter;
        AffectedChildrenIds = affectedChildrenIds;
    }

    /// <summary>
    /// 从评论实体创建删除事件
    /// </summary>
    /// <param name="comment">评论实体</param>
    /// <param name="deletedBy">删除者ID</param>
    /// <param name="deleter">删除者用户</param>
    /// <param name="deletionReason">删除原因</param>
    /// <param name="affectedChildrenIds">受影响的子评论ID</param>
    /// <param name="isSoftDelete">是否为软删除</param>
    /// <returns>评论删除事件</returns>
    public static CommentDeletedEvent FromComment(
        Comment comment,
        Guid deletedBy,
        User? deleter = null,
        string? deletionReason = null,
        IEnumerable<Guid>? affectedChildrenIds = null,
        bool isSoftDelete = true)
    {
        var deleterInfo = new CommentDeleter(
            deletedBy,
            deleter?.UserName ?? "未知用户",
            deleter?.Email,
            deletedBy == comment.AuthorId
        );

        var commentInfo = new DeletedComment(
            comment.Id,
            comment.Content?.ProcessedContent ?? string.Empty,
            comment.RawContent ?? string.Empty,
            comment.AuthorId,
            comment.PostId,
            comment.ParentId,
            comment.Level,
            comment.LikeCount,
            comment.ReplyCount,
            comment.Status,
            comment.CreatedAt
        );

        var type = GetDeletionType(deletedBy, comment.AuthorId, deleter);

        return new CommentDeletedEvent(
            commentId: comment.Id,
            commentAuthorId: comment.AuthorId,
            postId: comment.PostId,
            deletedBy: deletedBy,
            parentId: comment.ParentId,
            childrenCount: comment.Children?.Count ?? 0,
            isSoftDelete: isSoftDelete,
            deletionReason: deletionReason,
            type: type,
            comment: commentInfo,
            deleter: deleterInfo,
            affectedChildrenIds: affectedChildrenIds ?? Enumerable.Empty<Guid>()
        );
    }

    /// <summary>
    /// 检查是否为作者自删
    /// </summary>
    /// <returns>是否为作者自删</returns>
    public bool IsAuthorDeletion()
    {
        return DeletedBy == CommentAuthorId;
    }

    /// <summary>
    /// 检查是否为管理员删除
    /// </summary>
    /// <returns>是否为管理员删除</returns>
    public bool IsAdminDeletion()
    {
        return Type is DeletionType.AdminDelete or DeletionType.ModeratorDelete;
    }

    /// <summary>
    /// 检查是否为系统自动删除
    /// </summary>
    /// <returns>是否为系统自动删除</returns>
    public bool IsSystemDeletion()
    {
        return Type == DeletionType.SystemDelete;
    }

    /// <summary>
    /// 检查是否有子评论受影响
    /// </summary>
    /// <returns>是否有子评论</returns>
    public bool HasAffectedChildren()
    {
        return ChildrenCount > 0 || AffectedChildrenIds.Any();
    }

    /// <summary>
    /// 检查是否需要通知相关用户
    /// </summary>
    /// <returns>是否需要通知</returns>
    public bool ShouldNotifyUsers()
    {
        // 管理员删除或有子评论的情况下需要通知
        return IsAdminDeletion() || HasAffectedChildren();
    }

    /// <summary>
    /// 获取删除类型描述
    /// </summary>
    /// <returns>删除类型描述</returns>
    public string GetDeletionTypeDescription()
    {
        return Type switch
        {
            DeletionType.AuthorDelete => "作者删除",
            DeletionType.AdminDelete => "管理员删除",
            DeletionType.ModeratorDelete => "审核员删除",
            DeletionType.SystemDelete => "系统删除",
            DeletionType.BatchDelete => "批量删除",
            _ => "未知删除"
        };
    }

    /// <summary>
    /// 根据删除者信息确定删除类型
    /// </summary>
    /// <param name="deletedBy">删除者ID</param>
    /// <param name="authorId">作者ID</param>
    /// <param name="deleter">删除者用户</param>
    /// <returns>删除类型</returns>
    private static DeletionType GetDeletionType(Guid deletedBy, Guid authorId, User? deleter)
    {
        if (deletedBy == authorId)
            return DeletionType.AuthorDelete;

        if (deleter == null)
            return DeletionType.SystemDelete;

        // 这里可以根据用户角色判断是管理员还是审核员
        // 暂时简化处理
        return DeletionType.AdminDelete;
    }
}


/// <summary>
/// 删除者信息
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Username">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="IsAuthor">是否为评论作者</param>
public record CommentDeleter(
    Guid UserId,
    string Username,
    string? Email = null,
    bool IsAuthor = false
);

/// <summary>
/// 被删除的评论信息
/// </summary>
/// <param name="CommentId">评论ID</param>
/// <param name="Content">评论内容</param>
/// <param name="RawContent">原始内容</param>
/// <param name="AuthorId">作者ID</param>
/// <param name="PostId">文章ID</param>
/// <param name="ParentId">父评论ID</param>
/// <param name="Level">评论层级</param>
/// <param name="LikeCount">点赞数</param>
/// <param name="ReplyCount">回复数</param>
/// <param name="Status">状态</param>
/// <param name="CreatedAt">创建时间</param>
public record DeletedComment(
    Guid CommentId,
    string Content,
    string RawContent,
    Guid AuthorId,
    Guid PostId,
    Guid? ParentId,
    int Level,
    int LikeCount,
    int ReplyCount,
    CommentStatus Status,
    DateTime CreatedAt
);