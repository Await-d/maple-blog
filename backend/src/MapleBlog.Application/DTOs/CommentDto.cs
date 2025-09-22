using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// 评论数据传输对象
/// </summary>
public record CommentDto
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
    /// 作者ID
    /// </summary>
    public Guid AuthorId { get; init; }

    /// <summary>
    /// 作者信息
    /// </summary>
    public CommentAuthorDto? Author { get; init; }

    /// <summary>
    /// 父评论ID
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 渲染后的HTML内容
    /// </summary>
    public string RenderedContent { get; init; } = string.Empty;

    /// <summary>
    /// 评论状态
    /// </summary>
    public CommentStatus Status { get; init; }

    /// <summary>
    /// 层级深度
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// 线程路径
    /// </summary>
    public string ThreadPath { get; init; } = string.Empty;

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; init; }

    /// <summary>
    /// 回复数
    /// </summary>
    public int ReplyCount { get; init; }

    /// <summary>
    /// 当前用户是否已点赞
    /// </summary>
    public bool IsLiked { get; init; }

    /// <summary>
    /// 当前用户是否可以编辑
    /// </summary>
    public bool CanEdit { get; init; }

    /// <summary>
    /// 当前用户是否可以删除
    /// </summary>
    public bool CanDelete { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 子评论列表（用于嵌套显示）
    /// </summary>
    public IList<CommentDto> Replies { get; init; } = new List<CommentDto>();
}

/// <summary>
/// 评论作者信息
/// </summary>
public record CommentAuthorDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// 是否为VIP用户
    /// </summary>
    public bool IsVip { get; init; }
}

/// <summary>
/// 创建评论请求
/// </summary>
public record CommentCreateDto
{
    /// <summary>
    /// 文章ID
    /// </summary>
    [Required]
    public Guid PostId { get; init; }

    /// <summary>
    /// 父评论ID（回复时使用）
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "评论内容长度必须在1-2000字符之间")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 提及的用户ID列表
    /// </summary>
    public IList<Guid> MentionedUsers { get; init; } = new List<Guid>();

    /// <summary>
    /// 客户端信息（用于反垃圾）
    /// </summary>
    public CommentClientInfoDto? ClientInfo { get; init; }
}

/// <summary>
/// 更新评论请求
/// </summary>
public record CommentUpdateDto
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "评论内容长度必须在1-2000字符之间")]
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// 提及的用户ID列表
    /// </summary>
    public IList<Guid> MentionedUsers { get; init; } = new List<Guid>();
}

/// <summary>
/// 客户端信息
/// </summary>
public record CommentClientInfoDto
{
    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// 引用页面
    /// </summary>
    public string? Referer { get; init; }
}

/// <summary>
/// 评论列表查询参数
/// </summary>
public record CommentQueryDto
{
    /// <summary>
    /// 文章ID
    /// </summary>
    [Required]
    public Guid PostId { get; init; }

    /// <summary>
    /// 父评论ID（获取特定评论的回复时使用）
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// 排序方式
    /// </summary>
    public CommentSortOrder SortOrder { get; init; } = CommentSortOrder.CreatedAtDesc;

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

    /// <summary>
    /// 是否只获取根评论
    /// </summary>
    public bool RootOnly { get; init; } = false;

    /// <summary>
    /// 包含状态过滤
    /// </summary>
    public CommentStatus[] IncludeStatus { get; init; } = [CommentStatus.Approved];
}

/// <summary>
/// 评论排序方式
/// </summary>
public enum CommentSortOrder
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
    /// 热度排序（综合点赞、回复、时间）
    /// </summary>
    HotScore
}

/// <summary>
/// 评论分页结果
/// </summary>
public record CommentPagedResultDto
{
    /// <summary>
    /// 评论列表
    /// </summary>
    public IList<CommentDto> Comments { get; init; } = new List<CommentDto>();

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

/// <summary>
/// 评论统计信息
/// </summary>
public record CommentStatsDto
{
    /// <summary>
    /// 文章ID
    /// </summary>
    public Guid PostId { get; init; }

    /// <summary>
    /// 总评论数
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 根评论数
    /// </summary>
    public int RootCommentCount { get; init; }

    /// <summary>
    /// 回复数
    /// </summary>
    public int ReplyCount { get; init; }

    /// <summary>
    /// 参与用户数
    /// </summary>
    public int ParticipantCount { get; init; }

    /// <summary>
    /// 最新评论时间
    /// </summary>
    public DateTime? LatestCommentAt { get; init; }

    /// <summary>
    /// 最新评论作者
    /// </summary>
    public CommentAuthorDto? LatestCommentAuthor { get; init; }
}