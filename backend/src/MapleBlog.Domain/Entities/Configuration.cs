using System;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 配置实体
/// </summary>
public class Configuration : BaseEntity
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 配置节
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// 配置描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 环境名称
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid? CreatedById { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    public Guid? UpdatedById { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    public string DataType { get; set; } = "string";
}

/// <summary>
/// 配置变更活动
/// </summary>
public class ConfigurationChangeActivity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Action { get; set; } = string.Empty;
    public string ConfigurationKey { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedById { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}