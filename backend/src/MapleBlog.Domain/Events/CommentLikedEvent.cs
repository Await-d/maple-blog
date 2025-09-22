using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Events;

/// <summary>
/// 评论点赞事件
/// </summary>
public record CommentLikedEvent : DomainEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public override string EventName => "CommentLiked";

    /// <summary>
    /// 点赞记录ID
    /// </summary>
    public Guid LikeId { get; init; }

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// 评论作者ID
    /// </summary>
    public Guid CommentAuthorId { get; init; }

    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 是否为取消点赞
    /// </summary>
    public bool IsUnlike { get; init; }

    /// <summary>
    /// 点赞后的总数
    /// </summary>
    public int NewLikeCount { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public LikeUser User { get; init; }

    /// <summary>
    /// 评论信息
    /// </summary>
    public LikeComment Comment { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="likeId">点赞记录ID</param>
    /// <param name="commentId">评论ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="commentAuthorId">评论作者ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="isUnlike">是否为取消点赞</param>
    /// <param name="newLikeCount">点赞后的总数</param>
    /// <param name="user">用户信息</param>
    /// <param name="comment">评论信息</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    public CommentLikedEvent(
        Guid likeId,
        Guid commentId,
        Guid userId,
        Guid commentAuthorId,
        Guid postId,
        bool isUnlike,
        int newLikeCount,
        LikeUser user,
        LikeComment comment,
        string? ipAddress = null,
        string? userAgent = null)
    {
        LikeId = likeId;
        CommentId = commentId;
        UserId = userId;
        CommentAuthorId = commentAuthorId;
        PostId = postId;
        IsUnlike = isUnlike;
        NewLikeCount = newLikeCount;
        User = user;
        Comment = comment;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// 从评论点赞实体创建事件
    /// </summary>
    /// <param name="commentLike">评论点赞实体</param>
    /// <param name="isUnlike">是否为取消点赞</param>
    /// <param name="newLikeCount">新的点赞总数</param>
    /// <returns>评论点赞事件</returns>
    public static CommentLikedEvent FromCommentLike(CommentLike commentLike, bool isUnlike, int newLikeCount)
    {
        var user = new LikeUser(
            commentLike.UserId,
            commentLike.User?.UserName ?? "匿名用户",
            commentLike.User?.Email,
            commentLike.User?.AvatarUrl
        );

        var comment = new LikeComment(
            commentLike.CommentId,
            commentLike.Comment?.Content?.ProcessedContent ?? string.Empty,
            commentLike.Comment?.AuthorId ?? Guid.Empty,
            commentLike.Comment?.PostId ?? Guid.Empty,
            commentLike.Comment?.ParentId,
            commentLike.Comment?.Level ?? 0
        );

        return new CommentLikedEvent(
            likeId: commentLike.Id,
            commentId: commentLike.CommentId,
            userId: commentLike.UserId,
            commentAuthorId: comment.AuthorId,
            postId: comment.PostId,
            isUnlike: isUnlike,
            newLikeCount: newLikeCount,
            user: user,
            comment: comment,
            ipAddress: commentLike.IpAddress,
            userAgent: commentLike.UserAgent
        );
    }

    /// <summary>
    /// 检查是否应该发送通知给评论作者
    /// </summary>
    /// <returns>是否应该发送通知</returns>
    public bool ShouldNotifyCommentAuthor()
    {
        // 不是取消点赞，且点赞者不是评论作者本人
        return !IsUnlike && UserId != CommentAuthorId;
    }

    /// <summary>
    /// 检查是否为热门评论（点赞数达到阈值）
    /// </summary>
    /// <param name="threshold">热门阈值</param>
    /// <returns>是否为热门评论</returns>
    public bool IsPopularComment(int threshold = 10)
    {
        return !IsUnlike && NewLikeCount >= threshold;
    }

    /// <summary>
    /// 获取操作类型描述
    /// </summary>
    /// <returns>操作类型</returns>
    public string GetActionType()
    {
        return IsUnlike ? "取消点赞" : "点赞";
    }
}

/// <summary>
/// 点赞用户信息
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Username">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="AvatarUrl">头像URL</param>
public record LikeUser(
    Guid UserId,
    string Username,
    string? Email = null,
    string? AvatarUrl = null
);

/// <summary>
/// 被点赞的评论信息
/// </summary>
/// <param name="CommentId">评论ID</param>
/// <param name="Content">评论内容</param>
/// <param name="AuthorId">作者ID</param>
/// <param name="PostId">文章ID</param>
/// <param name="ParentId">父评论ID</param>
/// <param name="Level">评论层级</param>
public record LikeComment(
    Guid CommentId,
    string Content,
    Guid AuthorId,
    Guid PostId,
    Guid? ParentId,
    int Level
);