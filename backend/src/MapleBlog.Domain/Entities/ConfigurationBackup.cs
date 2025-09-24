using System;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 配置备份实体
/// </summary>
public class ConfigurationBackup : BaseEntity
{
    /// <summary>
    /// 备份名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 备份描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 备份数据（JSON格式）
    /// </summary>
    public string BackupData { get; set; } = string.Empty;

    /// <summary>
    /// 备份时间
    /// </summary>
    public DateTime BackupTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 备份人ID
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// 备份人
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// 是否自动备份
    /// </summary>
    public bool IsAutoBackup { get; set; }

    /// <summary>
    /// 环境
    /// </summary>
    [StringLength(50)]
    public string? Environment { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    [StringLength(50)]
    public string? Version { get; set; }

    /// <summary>
    /// 配置JSON数据 (别名)
    /// </summary>
    public string ConfigurationJson => BackupData;

    /// <summary>
    /// 配置数量
    /// </summary>
    public int ConfigurationCount { get; set; }

    /// <summary>
    /// 创建者ID (别名)
    /// </summary>
    public Guid? CreatedById => CreatedByUserId;

    /// <summary>
    /// 最后恢复时间
    /// </summary>
    public DateTime? LastRestoredAt { get; set; }

    /// <summary>
    /// 恢复人ID
    /// </summary>
    public Guid? RestoredById { get; set; }
}