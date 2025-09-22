using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 文章版本实体
/// </summary>
public class PostRevision : BaseEntity
{
    /// <summary>
    /// 文章ID
    /// </summary>
    [Required]
    public Guid PostId { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int RevisionNumber { get; set; }

    /// <summary>
    /// 文章标题
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章内容
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 文章摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    [StringLength(500)]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// 是否为主要版本
    /// </summary>
    public bool IsMajorRevision { get; set; } = false;

    // 导航属性

    /// <summary>
    /// 文章
    /// </summary>
    public virtual Post? Post { get; set; }

    /// <summary>
    /// 创建者（编辑者）
    /// </summary>
    public virtual User? Editor { get; set; }

    // 业务方法

    /// <summary>
    /// 设置为主要版本
    /// </summary>
    /// <param name="changeReason">变更原因</param>
    public void SetAsMajorRevision(string? changeReason = null)
    {
        IsMajorRevision = true;
        ChangeReason = changeReason;
        UpdateAuditFields();
    }

    /// <summary>
    /// 创建版本
    /// </summary>
    /// <param name="post">文章</param>
    /// <param name="revisionNumber">版本号</param>
    /// <param name="changeReason">变更原因</param>
    /// <param name="isMajor">是否为主要版本</param>
    /// <returns>版本实体</returns>
    public static PostRevision Create(Post post, int revisionNumber, string? changeReason = null, bool isMajor = false)
    {
        return new PostRevision
        {
            PostId = post.Id,
            RevisionNumber = revisionNumber,
            Title = post.Title,
            Content = post.Content,
            Summary = post.Summary,
            ChangeReason = changeReason,
            IsMajorRevision = isMajor
        };
    }
}