using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 配置版本实体 - 用于配置版本控制和历史追踪
/// </summary>
public class ConfigurationVersion : BaseEntity
{
    /// <summary>
    /// 配置ID
    /// </summary>
    public Guid ConfigurationId { get; set; }
    
    /// <summary>
    /// 配置实体导航属性
    /// </summary>
    public virtual SystemConfiguration Configuration { get; set; } = null!;

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 配置分组 (快照)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// 配置键 (快照)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值 (快照)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 数据类型 (快照)
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 描述 (快照)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否为系统配置 (快照)
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// 是否加密 (快照)
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// 显示顺序 (快照)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// 变更类型
    /// </summary>
    public ConfigurationChangeType ChangeType { get; set; }

    /// <summary>
    /// 变更原因
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// 变更内容JSON
    /// </summary>
    public string? ChangeDetails { get; set; }

    /// <summary>
    /// 审批状态
    /// </summary>
    public ConfigurationApprovalStatus ApprovalStatus { get; set; } = ConfigurationApprovalStatus.Pending;

    /// <summary>
    /// 版本状态
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// 审批人ID
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// 审批人导航属性
    /// </summary>
    public virtual User? ApprovedByUser { get; set; }

    /// <summary>
    /// 审批人ID (别名)
    /// </summary>
    public Guid? ApprovedById => ApprovedByUserId;

    /// <summary>
    /// 拒绝人ID
    /// </summary>
    public Guid? RejectedById { get; set; }

    /// <summary>
    /// 拒绝时间
    /// </summary>
    public DateTime? RejectedAt { get; set; }

    /// <summary>
    /// 拒绝原因
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// 创建人ID
    /// </summary>
    public Guid? CreatedById { get; set; }
    
    /// <summary>
    /// 创建人导航属性
    /// </summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>
    /// 审批时间
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// 审批备注
    /// </summary>
    public string? ApprovalNotes { get; set; }

    /// <summary>
    /// 是否为当前版本
    /// </summary>
    public bool IsCurrent { get; set; } = false;

    /// <summary>
    /// 是否可回滚
    /// </summary>
    public bool CanRollback { get; set; } = true;

    /// <summary>
    /// 校验和 (用于完整性验证)
    /// </summary>
    [StringLength(64)]
    public string? Checksum { get; set; }
    
    /// <summary>
    /// 环境名称
    /// </summary>
    public string? Environment { get; set; }
    
    /// <summary>
    /// 旧值（变更前的值）
    /// </summary>
    public string? OldValue { get; set; }
    
    /// <summary>
    /// 新值（Value的别名）
    /// </summary>
    public string? NewValue => Value;

    // 业务方法

    /// <summary>
    /// 审批配置变更
    /// </summary>
    /// <param name="approvedByUserId">审批人ID</param>
    /// <param name="notes">审批备注</param>
    public void Approve(Guid approvedByUserId, string? notes = null)
    {
        ApprovalStatus = ConfigurationApprovalStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = notes;
        UpdateAuditFields();
    }

    /// <summary>
    /// 拒绝配置变更
    /// </summary>
    /// <param name="rejectedByUserId">拒绝人ID</param>
    /// <param name="reason">拒绝原因</param>
    public void Reject(Guid rejectedByUserId, string? reason = null)
    {
        ApprovalStatus = ConfigurationApprovalStatus.Rejected;
        ApprovedByUserId = rejectedByUserId;
        ApprovedAt = DateTime.UtcNow;
        ApprovalNotes = reason;
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置为当前版本
    /// </summary>
    public void SetAsCurrent()
    {
        IsCurrent = true;
        UpdateAuditFields();
    }

    /// <summary>
    /// 取消当前版本标记
    /// </summary>
    public void UnsetAsCurrent()
    {
        IsCurrent = false;
        UpdateAuditFields();
    }

    /// <summary>
    /// 计算校验和
    /// </summary>
    /// <returns>校验和</returns>
    public string CalculateChecksum()
    {
        var content = $"{Section}|{Key}|{Value}|{DataType}|{Description}|{IsSystem}|{IsEncrypted}|{DisplayOrder}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 验证校验和
    /// </summary>
    /// <returns>是否有效</returns>
    public bool ValidateChecksum()
    {
        return Checksum == CalculateChecksum();
    }

    /// <summary>
    /// 创建配置版本
    /// </summary>
    /// <param name="configuration">配置实体</param>
    /// <param name="version">版本号</param>
    /// <param name="changeType">变更类型</param>
    /// <param name="changeReason">变更原因</param>
    /// <param name="changeDetails">变更详情</param>
    /// <returns>配置版本</returns>
    public static ConfigurationVersion Create(SystemConfiguration configuration, int version, 
        ConfigurationChangeType changeType, string? changeReason = null, string? changeDetails = null)
    {
        var configVersion = new ConfigurationVersion
        {
            ConfigurationId = configuration.Id,
            Version = version,
            Section = configuration.Section,
            Key = configuration.Key,
            Value = configuration.Value,
            DataType = configuration.DataType,
            Description = configuration.Description,
            IsSystem = configuration.IsSystem,
            IsEncrypted = configuration.IsEncrypted,
            DisplayOrder = configuration.DisplayOrder,
            ChangeType = changeType,
            ChangeReason = changeReason,
            ChangeDetails = changeDetails
        };
        
        configVersion.Checksum = configVersion.CalculateChecksum();
        return configVersion;
    }
}