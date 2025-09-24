using System;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 配置模板实体
/// </summary>
public class ConfigurationTemplate : BaseEntity
{
    /// <summary>
    /// 模板名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [StringLength(50)]
    public string TemplateType { get; set; } = string.Empty;

    /// <summary>
    /// 模板数据（JSON格式）
    /// </summary>
    public string TemplateData { get; set; } = string.Empty;

    /// <summary>
    /// 是否为默认模板
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否为系统模板
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    [StringLength(50)]
    public string? Version { get; set; }

    /// <summary>
    /// 配置JSON数据 (别名)
    /// </summary>
    public string ConfigurationJson => TemplateData;
}