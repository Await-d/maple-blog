using MapleBlog.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Admin.DTOs;

/// <summary>
/// 配置基础DTO
/// </summary>
public class ConfigurationDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Section { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    public string? Value { get; set; }
    
    [StringLength(50)]
    public string DataType { get; set; } = "string";
    
    public string? Description { get; set; }
    
    public bool IsSystem { get; set; }
    
    public bool IsEncrypted { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public int CurrentVersion { get; set; }
    
    public ConfigurationCriticality Criticality { get; set; }
    
    public bool RequiresApproval { get; set; }
    
    public bool EnableVersioning { get; set; }
    
    public int MaxVersions { get; set; }
    
    public string? Schema { get; set; }
    
    public string? DefaultValue { get; set; }
    
    public string? ValidationRules { get; set; }
    
    public string? Tags { get; set; }
    
    public string Environment { get; set; } = "Production";
    
    public string? LastChangeReason { get; set; }
    
    public string? ImpactAssessment { get; set; }
    
    public bool IsReadOnly { get; set; }
    
    public DateTime? EffectiveDate { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
    
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 配置创建DTO
/// </summary>
public class CreateConfigurationDto
{
    [Required]
    [StringLength(100)]
    public string Section { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    public string? Value { get; set; }
    
    [StringLength(50)]
    public string DataType { get; set; } = "string";
    
    public string? Description { get; set; }
    
    public bool IsSystem { get; set; } = false;
    
    public bool IsEncrypted { get; set; } = false;
    
    public int DisplayOrder { get; set; } = 0;
    
    public ConfigurationCriticality Criticality { get; set; } = ConfigurationCriticality.Low;
    
    public bool RequiresApproval { get; set; } = false;
    
    public bool EnableVersioning { get; set; } = true;
    
    public int MaxVersions { get; set; } = 10;
    
    public string? Schema { get; set; }
    
    public string? DefaultValue { get; set; }
    
    public string? ValidationRules { get; set; }
    
    public string? Tags { get; set; }
    
    public string Environment { get; set; } = "Production";
    
    public string? ImpactAssessment { get; set; }
    
    public DateTime? EffectiveDate { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    
    public string? ChangeReason { get; set; }
}

/// <summary>
/// 配置更新DTO
/// </summary>
public class UpdateConfigurationDto
{
    public string? Value { get; set; }
    
    [StringLength(50)]
    public string? DataType { get; set; }
    
    public string? Description { get; set; }
    
    public int? DisplayOrder { get; set; }
    
    public ConfigurationCriticality? Criticality { get; set; }
    
    public bool? RequiresApproval { get; set; }
    
    public bool? EnableVersioning { get; set; }
    
    public int? MaxVersions { get; set; }
    
    public string? Schema { get; set; }
    
    public string? DefaultValue { get; set; }
    
    public string? ValidationRules { get; set; }
    
    public string? Tags { get; set; }
    
    public string? ImpactAssessment { get; set; }
    
    public bool? IsReadOnly { get; set; }
    
    public DateTime? EffectiveDate { get; set; }
    
    public DateTime? ExpirationDate { get; set; }
    
    [Required]
    public string ChangeReason { get; set; } = string.Empty;
}

/// <summary>
/// 配置版本DTO
/// </summary>
public class ConfigurationVersionDto
{
    public Guid Id { get; set; }
    
    public Guid ConfigurationId { get; set; }
    
    public int Version { get; set; }
    
    public string Section { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    
    public string? Value { get; set; }
    
    public string DataType { get; set; } = "string";
    
    public string? Description { get; set; }
    
    public bool IsSystem { get; set; }
    
    public bool IsEncrypted { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public ConfigurationChangeType ChangeType { get; set; }
    
    public string? ChangeReason { get; set; }
    
    public string? ChangeDetails { get; set; }
    
    public ConfigurationApprovalStatus ApprovalStatus { get; set; }
    
    public Guid? ApprovedByUserId { get; set; }
    
    public string? ApprovedByUserName { get; set; }
    
    public DateTime? ApprovedAt { get; set; }
    
    public string? ApprovalNotes { get; set; }
    
    public bool IsCurrent { get; set; }
    
    public bool CanRollback { get; set; }
    
    public string? Checksum { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string? CreatedBy { get; set; }
}

/// <summary>
/// 配置搜索和过滤DTO
/// </summary>
public class ConfigurationSearchDto
{
    public string? Section { get; set; }
    
    public string? Key { get; set; }
    
    public string? Value { get; set; }
    
    public string? Environment { get; set; }
    
    public ConfigurationCriticality? Criticality { get; set; }
    
    public bool? IsSystem { get; set; }
    
    public bool? IsEncrypted { get; set; }
    
    public bool? RequiresApproval { get; set; }
    
    public bool? IsReadOnly { get; set; }
    
    public string? Tags { get; set; }
    
    public DateTime? CreatedAfter { get; set; }
    
    public DateTime? CreatedBefore { get; set; }
    
    public DateTime? UpdatedAfter { get; set; }
    
    public DateTime? UpdatedBefore { get; set; }
    
    public int Page { get; set; } = 1;
    
    public int PageSize { get; set; } = 20;
    
    public string? SortBy { get; set; } = "Section";
    
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// 配置批量操作DTO
/// </summary>
public class ConfigurationBatchOperationDto
{
    [Required]
    public List<Guid> ConfigurationIds { get; set; } = new();
    
    [Required]
    public string Operation { get; set; } = string.Empty; // update, delete, export, backup
    
    public Dictionary<string, object>? Parameters { get; set; }
    
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 配置验证结果DTO
/// </summary>
public class ConfigurationValidationResultDto
{
    public bool IsValid { get; set; }
    
    public List<string> Errors { get; set; } = new();
    
    public List<string> Warnings { get; set; } = new();
    
    public Dictionary<string, object>? Suggestions { get; set; }
}

/// <summary>
/// 配置回滚DTO
/// </summary>
public class ConfigurationRollbackDto
{
    [Required]
    public Guid ConfigurationId { get; set; }
    
    [Required]
    public int TargetVersion { get; set; }
    
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    public bool ForceRollback { get; set; } = false;
}

/// <summary>
/// 配置审批DTO
/// </summary>
public class ConfigurationApprovalDto
{
    [Required]
    public Guid VersionId { get; set; }
    
    [Required]
    public ConfigurationApprovalStatus Status { get; set; }
    
    public string? Notes { get; set; }
}

/// <summary>
/// 配置模板DTO
/// </summary>
public class ConfigurationTemplateDto
{
    public Guid Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public ConfigurationTemplateType Type { get; set; }
    
    public List<CreateConfigurationDto> Configurations { get; set; } = new();
    
    public Dictionary<string, object>? Metadata { get; set; }
    
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 配置备份DTO
/// </summary>
public class ConfigurationBackupDto
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string Environment { get; set; } = string.Empty;
    
    public List<string> Sections { get; set; } = new();
    
    public int ConfigurationCount { get; set; }
    
    public string BackupPath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public string Checksum { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// 配置恢复DTO
/// </summary>
public class ConfigurationRestoreDto
{
    [Required]
    public Guid BackupId { get; set; }
    
    public List<string>? Sections { get; set; }
    
    public List<string>? Keys { get; set; }
    
    public bool OverwriteExisting { get; set; } = false;
    
    public bool CreateBackupBeforeRestore { get; set; } = true;
    
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 配置导入导出DTO
/// </summary>
public class ConfigurationExportDto
{
    public List<string>? Sections { get; set; }
    
    public List<string>? Keys { get; set; }
    
    public string Environment { get; set; } = "Production";
    
    public bool IncludeSystemConfigs { get; set; } = false;
    
    public bool IncludeEncryptedConfigs { get; set; } = false;
    
    public string Format { get; set; } = "json"; // json, xml, yaml
}

/// <summary>
/// 配置导入DTO
/// </summary>
public class ConfigurationImportDto
{
    [Required]
    public string Data { get; set; } = string.Empty;
    
    public string Format { get; set; } = "json";
    
    public string TargetEnvironment { get; set; } = "Production";
    
    public bool OverwriteExisting { get; set; } = false;
    
    public bool ValidateOnly { get; set; } = false;
    
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 配置统计DTO
/// </summary>
public class ConfigurationStatisticsDto
{
    public int TotalConfigurations { get; set; }
    
    public int SystemConfigurations { get; set; }
    
    public int EncryptedConfigurations { get; set; }
    
    public int ConfigurationsRequiringApproval { get; set; }
    
    public int PendingApprovals { get; set; }
    
    public Dictionary<string, int> ConfigurationsBySection { get; set; } = new();
    
    public Dictionary<string, int> ConfigurationsByEnvironment { get; set; } = new();
    
    public Dictionary<ConfigurationCriticality, int> ConfigurationsByCriticality { get; set; } = new();
    
    public Dictionary<string, int> ConfigurationsByDataType { get; set; } = new();
    
    public int TotalVersions { get; set; }
    
    public DateTime? LastModified { get; set; }
    
    public List<ConfigurationChangeActivity> RecentActivity { get; set; } = new();
}

/// <summary>
/// 配置变更活动DTO
/// </summary>
public class ConfigurationChangeActivity
{
    public Guid ConfigurationId { get; set; }
    
    public string Section { get; set; } = string.Empty;
    
    public string Key { get; set; } = string.Empty;
    
    public ConfigurationChangeType ChangeType { get; set; }
    
    public string? ChangeReason { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public string? UserName { get; set; }
    
    public int Version { get; set; }
}