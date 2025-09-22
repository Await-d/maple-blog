using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 搜索历史实体
/// </summary>
public class SearchHistory : BaseEntity
{
    /// <summary>
    /// 用户ID（可选，匿名搜索为null）
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 搜索查询
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 结果数量
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// 搜索时间
    /// </summary>
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 搜索类型
    /// </summary>
    [StringLength(50)]
    public string SearchType { get; set; } = "General";

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
    public virtual User? User { get; set; }
}