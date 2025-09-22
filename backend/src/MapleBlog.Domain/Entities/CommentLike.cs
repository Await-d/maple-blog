using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 评论点赞实体
/// </summary>
public class CommentLike : BaseEntity
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [Required]
    public Guid CommentId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    // 导航属性

    /// <summary>
    /// 评论
    /// </summary>
    public virtual Comment? Comment { get; set; }

    /// <summary>
    /// 用户
    /// </summary>
    public virtual User? User { get; set; }
}