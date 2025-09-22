using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Domain.Events;

/// <summary>
/// 评论创建事件
/// </summary>
public record CommentCreatedEvent : DomainEvent
{
    /// <summary>
    /// 事件名称
    /// </summary>
    public override string EventName => "CommentCreated";

    /// <summary>
    /// 评论ID
    /// </summary>
    public Guid CommentId { get; init; }

    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 作者ID
    /// </summary>
    public Guid AuthorId { get; init; }

    /// <summary>
    /// 父评论ID（如果是回复）
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 线程路径
    /// </summary>
    public string ThreadPath { get; init; }

    /// <summary>
    /// 评论层级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    public CommentContent Content { get; init; }

    /// <summary>
    /// 作者信息
    /// </summary>
    public CommentAuthor Author { get; init; }

    /// <summary>
    /// 是否需要审核
    /// </summary>
    public bool RequiresModeration { get; init; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="postId">文章ID</param>
    /// <param name="authorId">作者ID</param>
    /// <param name="parentId">父评论ID</param>
    /// <param name="threadPath">线程路径</param>
    /// <param name="level">评论层级</param>
    /// <param name="content">评论内容</param>
    /// <param name="author">作者信息</param>
    /// <param name="requiresModeration">是否需要审核</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    public CommentCreatedEvent(
        Guid commentId,
        Guid postId,
        Guid authorId,
        Guid? parentId,
        string threadPath,
        int level,
        CommentContent content,
        CommentAuthor author,
        bool requiresModeration,
        string? ipAddress = null,
        string? userAgent = null)
    {
        CommentId = commentId;
        PostId = postId;
        AuthorId = authorId;
        ParentId = parentId;
        ThreadPath = threadPath;
        Level = level;
        Content = content;
        Author = author;
        RequiresModeration = requiresModeration;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    /// <summary>
    /// 从评论实体创建事件
    /// </summary>
    /// <param name="comment">评论实体</param>
    /// <returns>评论创建事件</returns>
    public static CommentCreatedEvent FromComment(Comment comment)
    {
        var content = CommentContent.Create(comment.RawContent ?? string.Empty, comment.ContentType.ToString() ?? "markdown");
        var author = new CommentAuthor(
            comment.AuthorId,
            comment.AuthorName ?? "匿名用户",
            comment.AuthorEmail,
            comment.AuthorAvatarUrl
        );

        return new CommentCreatedEvent(
            commentId: comment.Id,
            postId: comment.PostId,
            authorId: comment.AuthorId,
            parentId: comment.ParentId,
            threadPath: comment.ThreadPath?.Path ?? string.Empty,
            level: comment.Level,
            content: content,
            author: author,
            requiresModeration: content.RequiresModeration() || !comment.IsApproved,
            ipAddress: comment.IpAddress,
            userAgent: comment.UserAgent
        );
    }

    /// <summary>
    /// 检查是否为根评论
    /// </summary>
    /// <returns>是否为根评论</returns>
    public bool IsRootComment() => ParentId == null;

    /// <summary>
    /// 检查是否为回复
    /// </summary>
    /// <returns>是否为回复</returns>
    public bool IsReply() => ParentId != null;

    /// <summary>
    /// 获取提及的用户ID列表
    /// </summary>
    /// <returns>被提及的用户ID列表</returns>
    public IEnumerable<Guid> GetMentionedUserIds()
    {
        // 这里可以解析评论内容中的@用户提及
        // 暂时返回空列表，实际实现中需要解析内容
        return Enumerable.Empty<Guid>();
    }
}

/// <summary>
/// 评论作者信息
/// </summary>
/// <param name="UserId">用户ID</param>
/// <param name="Username">用户名</param>
/// <param name="Email">邮箱</param>
/// <param name="AvatarUrl">头像URL</param>
public record CommentAuthor(
    Guid UserId,
    string Username,
    string? Email = null,
    string? AvatarUrl = null
);