using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 文章实体
/// </summary>
public class Post : BaseEntity
{
    /// <summary>
    /// 文章标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL友好的标识符
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// 文章摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 文章正文内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 内容类型
    /// </summary>
    [Required]
    [StringLength(20)]
    public string ContentType { get; set; } = "markdown";

    // 关联关系

    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// 作者ID
    /// </summary>
    [Required]
    public Guid AuthorId { get; set; }

    // 状态管理

    /// <summary>
    /// 文章状态
    /// </summary>
    public PostStatus Status { get; set; } = PostStatus.Draft;

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 定时发布时间
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    // 统计数据

    /// <summary>
    /// 浏览次数
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// 评论数
    /// </summary>
    public int CommentCount { get; set; } = 0;

    /// <summary>
    /// 分享数
    /// </summary>
    public int ShareCount { get; set; } = 0;

    // 内容设置

    /// <summary>
    /// 是否允许评论
    /// </summary>
    public bool AllowComments { get; set; } = true;

    /// <summary>
    /// 是否为推荐文章
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsSticky { get; set; } = false;

    // SEO优化

    /// <summary>
    /// SEO标题
    /// </summary>
    [StringLength(200)]
    public string? MetaTitle { get; set; }

    /// <summary>
    /// SEO描述
    /// </summary>
    public string? MetaDescription { get; set; }

    /// <summary>
    /// SEO关键词
    /// </summary>
    [StringLength(500)]
    public string? MetaKeywords { get; set; }

    /// <summary>
    /// 规范URL
    /// </summary>
    [StringLength(500)]
    public string? CanonicalUrl { get; set; }

    // 社交媒体

    /// <summary>
    /// Open Graph标题
    /// </summary>
    [StringLength(200)]
    public string? OgTitle { get; set; }

    /// <summary>
    /// Open Graph描述
    /// </summary>
    public string? OgDescription { get; set; }

    /// <summary>
    /// Open Graph图片URL
    /// </summary>
    [StringLength(500)]
    public string? OgImageUrl { get; set; }

    // 内容配置

    /// <summary>
    /// 预估阅读时间（分钟）
    /// </summary>
    public int? ReadingTime { get; set; }

    /// <summary>
    /// 字数统计
    /// </summary>
    public int? WordCount { get; set; }

    /// <summary>
    /// 语言代码
    /// </summary>
    [StringLength(10)]
    public string Language { get; set; } = "zh-CN";

    // 导航属性

    /// <summary>
    /// 分类
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public virtual User? Author { get; set; }

    /// <summary>
    /// 文章标签
    /// </summary>
    public virtual ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();

    /// <summary>
    /// 文章附件
    /// </summary>
    public virtual ICollection<PostAttachment> PostAttachments { get; set; } = new List<PostAttachment>();

    /// <summary>
    /// 文章版本历史
    /// </summary>
    public virtual ICollection<PostRevision> PostRevisions { get; set; } = new List<PostRevision>();

    /// <summary>
    /// 文章评论
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // 计算属性

    /// <summary>
    /// 是否已发布（计算属性）
    /// </summary>
    public bool IsPublished => Status == PostStatus.Published && !IsDeleted;

    /// <summary>
    /// 标签列表（计算属性）
    /// </summary>
    public IEnumerable<Tag> Tags => PostTags.Where(pt => pt.Tag != null).Select(pt => pt.Tag!);

    // 业务方法

    /// <summary>
    /// 发布文章
    /// </summary>
    public void Publish()
    {
        if (Status == PostStatus.Published)
            return;

        Status = PostStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdateAuditFields();
    }

    /// <summary>
    /// 取消发布文章
    /// </summary>
    public void Unpublish()
    {
        if (Status != PostStatus.Published)
            return;

        Status = PostStatus.Draft;
        UpdateAuditFields();
    }

    /// <summary>
    /// 归档文章
    /// </summary>
    public void Archive()
    {
        Status = PostStatus.Archived;
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置为私有文章
    /// </summary>
    public void SetPrivate()
    {
        Status = PostStatus.Private;
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加浏览次数
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreaseViewCount(int count = 1)
    {
        ViewCount += count;
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加点赞数
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreaseLikeCount(int count = 1)
    {
        LikeCount += count;
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加评论数
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreaseCommentCount(int count = 1)
    {
        CommentCount += count;
        UpdateAuditFields();
    }

    /// <summary>
    /// 减少评论数
    /// </summary>
    /// <param name="count">减少数量</param>
    public void DecreaseCommentCount(int count = 1)
    {
        CommentCount = Math.Max(0, CommentCount - count);
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置标题和Slug
    /// </summary>
    /// <param name="title">文章标题</param>
    /// <param name="slug">URL标识符</param>
    public void SetTitleAndSlug(string title, string? slug = null)
    {
        Title = title?.Trim() ?? string.Empty;
        Slug = slug?.Trim() ?? GenerateSlug(Title);
        UpdateAuditFields();
    }

    /// <summary>
    /// 计算阅读时间（基于字数）
    /// </summary>
    /// <param name="wordsPerMinute">每分钟阅读字数</param>
    public void CalculateReadingTime(int wordsPerMinute = 200)
    {
        if (WordCount.HasValue && WordCount.Value > 0)
        {
            ReadingTime = Math.Max(1, (int)Math.Ceiling(WordCount.Value / (double)wordsPerMinute));
        }
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查文章是否可以被公开访问
    /// </summary>
    /// <returns>是否可公开访问</returns>
    public bool IsPubliclyAccessible()
    {
        return Status == PostStatus.Published &&
               !IsDeleted &&
               (PublishedAt == null || PublishedAt <= DateTime.UtcNow);
    }

    /// <summary>
    /// 获取所有标签
    /// </summary>
    /// <returns>标签列表</returns>
    public IEnumerable<Tag> GetTags()
    {
        return PostTags.Where(pt => pt.Tag != null).Select(pt => pt.Tag!);
    }

    /// <summary>
    /// 生成URL友好的Slug
    /// </summary>
    /// <param name="title">文章标题</param>
    /// <returns>URL标识符</returns>
    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        // 简单的Slug生成逻辑，实际项目中可以使用更复杂的实现
        return title.ToLowerInvariant()
                   .Replace(" ", "-")
                   .Replace("_", "-")
                   .Trim('-');
    }
}