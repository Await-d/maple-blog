using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 系统配置实体
/// </summary>
/// <summary>
/// 系统配置实体 - 增强版配置管理，支持版本控制、审批流程和回滚机制
/// </summary>
public class SystemConfiguration : BaseEntity
{
    /// <summary>
    /// 配置分组
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 数据类型
    /// </summary>
    [StringLength(50)]
    public string DataType { get; set; } = "string";

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否为系统配置
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// 是否加密
    /// </summary>
    public bool IsEncrypted { get; set; } = false;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// 当前版本号
    /// </summary>
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// 配置重要性级别
    /// </summary>
    public ConfigurationCriticality Criticality { get; set; } = ConfigurationCriticality.Low;

    /// <summary>
    /// 是否需要审批
    /// </summary>
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// 是否启用版本控制
    /// </summary>
    public bool EnableVersioning { get; set; } = true;

    /// <summary>
    /// 最大保留版本数
    /// </summary>
    public int MaxVersions { get; set; } = 10;

    /// <summary>
    /// 配置模式 (JSON Schema)
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 验证规则 (正则表达式或JSON)
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 配置类别标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 配置分类/类别
    /// </summary>
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 是否为公开配置
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// 配置环境 (Development, Staging, Production)
    /// </summary>
    [StringLength(50)]
    public string Environment { get; set; } = "Production";

    /// <summary>
    /// 最后修改原因
    /// </summary>
    public string? LastChangeReason { get; set; }

    /// <summary>
    /// 配置变更影响评估
    /// </summary>
    public string? ImpactAssessment { get; set; }

    /// <summary>
    /// 是否只读
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public Guid? CreatedById { get; set; }

    /// <summary>
    /// 更新者ID
    /// </summary>
    public Guid? UpdatedById { get; set; }

    /// <summary>
    /// 配置版本历史
    /// </summary>
    public virtual ICollection<ConfigurationVersion> Versions { get; set; } = new List<ConfigurationVersion>();

    // 业务方法

    /// <summary>
    /// 设置配置值并创建版本
    /// </summary>
    /// <param name="value">配置值</param>
    /// <param name="changeReason">变更原因</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>是否需要审批</returns>
    public bool SetValue(string? value, string? changeReason = null, string? dataType = null, Guid? userId = null)
    {
        var oldValue = Value;
        Value = value;
        if (!string.IsNullOrEmpty(dataType))
        {
            DataType = dataType;
        }

        LastChangeReason = changeReason;

        // 如果启用版本控制，创建新版本
        if (EnableVersioning)
        {
            CreateVersion(ConfigurationChangeType.Update, changeReason);
        }

        UpdateAuditFields();

        // 根据重要性级别判断是否需要审批
        return RequiresApproval || Criticality >= ConfigurationCriticality.Medium;
    }

    /// <summary>
    /// 创建配置版本
    /// </summary>
    /// <param name="changeType">变更类型</param>
    /// <param name="changeReason">变更原因</param>
    /// <param name="changeDetails">变更详情</param>
    public void CreateVersion(ConfigurationChangeType changeType, string? changeReason = null, string? changeDetails = null)
    {
        CurrentVersion++;
        
        // 取消当前版本标记
        foreach (var version in Versions.Where(v => v.IsCurrent))
        {
            version.UnsetAsCurrent();
        }

        var newVersion = ConfigurationVersion.Create(this, CurrentVersion, changeType, changeReason, changeDetails);
        newVersion.SetAsCurrent();
        
        Versions.Add(newVersion);

        // 清理超过最大版本数的历史版本
        CleanupOldVersions();
    }

    /// <summary>
    /// 清理旧版本
    /// </summary>
    private void CleanupOldVersions()
    {
        if (MaxVersions <= 0) return;

        var versionsToRemove = Versions
            .Where(v => !v.IsCurrent)
            .OrderByDescending(v => v.Version)
            .Skip(MaxVersions - 1)
            .ToList();

        foreach (var version in versionsToRemove)
        {
            Versions.Remove(version);
        }
    }

    /// <summary>
    /// 回滚到指定版本
    /// </summary>
    /// <param name="targetVersion">目标版本号</param>
    /// <param name="rollbackReason">回滚原因</param>
    /// <returns>是否成功</returns>
    public bool RollbackToVersion(int targetVersion, string? rollbackReason = null)
    {
        var targetVersionEntity = Versions.FirstOrDefault(v => v.Version == targetVersion && v.CanRollback);
        if (targetVersionEntity == null) return false;

        // 保存当前状态为回滚前版本
        CreateVersion(ConfigurationChangeType.Rollback, rollbackReason, 
            $"Rollback from version {CurrentVersion} to version {targetVersion}");

        // 恢复目标版本的值
        Value = targetVersionEntity.Value;
        DataType = targetVersionEntity.DataType;
        Description = targetVersionEntity.Description;
        IsSystem = targetVersionEntity.IsSystem;
        IsEncrypted = targetVersionEntity.IsEncrypted;
        DisplayOrder = targetVersionEntity.DisplayOrder;
        LastChangeReason = rollbackReason ?? $"Rollback to version {targetVersion}";

        UpdateAuditFields();
        return true;
    }

    /// <summary>
    /// 验证配置值
    /// </summary>
    /// <param name="value">要验证的值</param>
    /// <returns>验证结果</returns>
    public (bool IsValid, string? ErrorMessage) ValidateValue(string? value)
    {
        // 数据类型验证
        var typeValidation = ValidateDataType(value);
        if (!typeValidation.IsValid)
        {
            return typeValidation;
        }

        // 自定义验证规则
        if (!string.IsNullOrEmpty(ValidationRules))
        {
            var customValidation = ValidateCustomRules(value);
            if (!customValidation.IsValid)
            {
                return customValidation;
            }
        }

        // Schema 验证
        if (!string.IsNullOrEmpty(Schema))
        {
            var schemaValidation = ValidateSchema(value);
            if (!schemaValidation.IsValid)
            {
                return schemaValidation;
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 验证数据类型
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateDataType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return (true, null);

        return DataType.ToLower() switch
        {
            "bool" or "boolean" => bool.TryParse(value, out _) ? (true, null) : (false, "Invalid boolean value"),
            "int" or "integer" => int.TryParse(value, out _) ? (true, null) : (false, "Invalid integer value"),
            "decimal" or "double" => decimal.TryParse(value, out _) ? (true, null) : (false, "Invalid decimal value"),
            "datetime" => DateTime.TryParse(value, out _) ? (true, null) : (false, "Invalid datetime value"),
            "email" => IsValidEmail(value) ? (true, null) : (false, "Invalid email format"),
            "url" => Uri.TryCreate(value, UriKind.Absolute, out _) ? (true, null) : (false, "Invalid URL format"),
            "json" => IsValidJson(value) ? (true, null) : (false, "Invalid JSON format"),
            _ => (true, null) // String or unknown types pass through
        };
    }

    /// <summary>
    /// 验证自定义规则
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateCustomRules(string? value)
    {
        try
        {
            if (ValidationRules!.StartsWith("{"))
            {
                // JSON格式的复杂验证规则
                return ValidateJsonRules(value);
            }
            else
            {
                // 正则表达式验证
                var regex = new System.Text.RegularExpressions.Regex(ValidationRules);
                return regex.IsMatch(value ?? "") ? (true, null) : (false, "Value does not match validation pattern");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Validation rule error: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证JSON规则
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateJsonRules(string? value)
    {
        // 这里可以实现更复杂的JSON规则验证逻辑
        // 例如长度限制、值范围、依赖关系等
        return (true, null);
    }

    /// <summary>
    /// 验证Schema
    /// </summary>
    private (bool IsValid, string? ErrorMessage) ValidateSchema(string? value)
    {
        // 这里可以集成JSON Schema验证库
        // 例如 Newtonsoft.Json.Schema 或 JsonSchema.Net
        return (true, null);
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证JSON格式
    /// </summary>
    private bool IsValidJson(string json)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取布尔值
    /// </summary>
    /// <param name="defaultValue">默认值</param>
    /// <returns>布尔值</returns>
    public bool GetBoolValue(bool defaultValue = false)
    {
        if (bool.TryParse(Value, out var result))
            return result;
        if (bool.TryParse(DefaultValue, out var defaultResult))
            return defaultResult;
        return defaultValue;
    }

    /// <summary>
    /// 获取整数值
    /// </summary>
    /// <param name="defaultValue">默认值</param>
    /// <returns>整数值</returns>
    public int GetIntValue(int defaultValue = 0)
    {
        if (int.TryParse(Value, out var result))
            return result;
        if (int.TryParse(DefaultValue, out var defaultResult))
            return defaultResult;
        return defaultValue;
    }

    /// <summary>
    /// 获取小数值
    /// </summary>
    /// <param name="defaultValue">默认值</param>
    /// <returns>小数值</returns>
    public decimal GetDecimalValue(decimal defaultValue = 0m)
    {
        if (decimal.TryParse(Value, out var result))
            return result;
        if (decimal.TryParse(DefaultValue, out var defaultResult))
            return defaultResult;
        return defaultValue;
    }

    /// <summary>
    /// 获取日期时间值
    /// </summary>
    /// <param name="defaultValue">默认值</param>
    /// <returns>日期时间值</returns>
    public DateTime? GetDateTimeValue(DateTime? defaultValue = null)
    {
        if (DateTime.TryParse(Value, out var result))
            return result;
        if (DateTime.TryParse(DefaultValue, out var defaultResult))
            return defaultResult;
        return defaultValue;
    }

    /// <summary>
    /// 获取字符串值
    /// </summary>
    /// <param name="defaultValue">默认值</param>
    /// <returns>字符串值</returns>
    public string GetStringValue(string defaultValue = "")
    {
        return Value ?? DefaultValue ?? defaultValue;
    }

    /// <summary>
    /// 获取JSON对象值
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="defaultValue">默认值</param>
    /// <returns>反序列化的对象</returns>
    public T? GetJsonValue<T>(T? defaultValue = default) where T : class
    {
        try
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Value);
            }
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(DefaultValue);
            }
        }
        catch
        {
            // 序列化失败时返回默认值
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置为加密配置
    /// </summary>
    public void SetAsEncrypted()
    {
        IsEncrypted = true;
        Criticality = ConfigurationCriticality.High;
        RequiresApproval = true;
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置为系统配置
    /// </summary>
    public void SetAsSystem()
    {
        IsSystem = true;
        IsReadOnly = true;
        Criticality = ConfigurationCriticality.Critical;
        RequiresApproval = true;
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置重要性级别
    /// </summary>
    /// <param name="criticality">重要性级别</param>
    public void SetCriticality(ConfigurationCriticality criticality)
    {
        Criticality = criticality;
        RequiresApproval = criticality >= ConfigurationCriticality.Medium;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查配置是否有效
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsEffective()
    {
        var now = DateTime.UtcNow;
        
        if (EffectiveDate.HasValue && now < EffectiveDate.Value)
            return false;
            
        if (ExpirationDate.HasValue && now > ExpirationDate.Value)
            return false;
            
        return true;
    }

    /// <summary>
    /// 克隆配置用于不同环境
    /// </summary>
    /// <param name="targetEnvironment">目标环境</param>
    /// <returns>克隆的配置</returns>
    public SystemConfiguration CloneForEnvironment(string targetEnvironment)
    {
        return new SystemConfiguration
        {
            Section = Section,
            Key = Key,
            Value = Value,
            DataType = DataType,
            Description = Description,
            IsSystem = IsSystem,
            IsEncrypted = IsEncrypted,
            DisplayOrder = DisplayOrder,
            Criticality = Criticality,
            RequiresApproval = RequiresApproval,
            EnableVersioning = EnableVersioning,
            MaxVersions = MaxVersions,
            Schema = Schema,
            DefaultValue = DefaultValue,
            ValidationRules = ValidationRules,
            Tags = Tags,
            Environment = targetEnvironment,
            IsReadOnly = IsReadOnly
        };
    }

    /// <summary>
    /// 创建配置
    /// </summary>
    /// <param name="section">分组</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="description">描述</param>
    /// <param name="environment">环境</param>
    /// <returns>系统配置</returns>
    public static SystemConfiguration Create(string section, string key, string? value, 
        string dataType = "string", string? description = null, string environment = "Production")
    {
        var config = new SystemConfiguration
        {
            Section = section,
            Key = key,
            Value = value,
            DataType = dataType,
            Description = description,
            Environment = environment
        };

        // 创建初始版本
        config.CreateVersion(ConfigurationChangeType.Create, "Initial configuration");
        
        return config;
    }
}