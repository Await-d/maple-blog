using MapleBlog.Admin.DTOs;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 配置管理服务 - 提供企业级配置管理功能
/// </summary>
public interface IConfigurationService
{
    // 基础CRUD操作
    Task<ConfigurationDto?> GetConfigurationAsync(Guid id);
    Task<ConfigurationDto?> GetConfigurationAsync(string section, string key, string environment = "Production");
    Task<List<ConfigurationDto>> GetConfigurationsAsync(ConfigurationSearchDto searchDto);
    Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, Guid userId);
    Task<ConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateConfigurationDto updateDto, Guid userId);
    Task<bool> DeleteConfigurationAsync(Guid id, string reason, Guid userId);
    
    // 版本控制
    Task<List<ConfigurationVersionDto>> GetVersionHistoryAsync(Guid configurationId);
    Task<ConfigurationVersionDto?> GetVersionAsync(Guid configurationId, int version);
    Task<bool> RollbackToVersionAsync(ConfigurationRollbackDto rollbackDto, Guid userId);
    
    // 审批流程
    Task<List<ConfigurationVersionDto>> GetPendingApprovalsAsync();
    Task<bool> ApproveConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId);
    Task<bool> RejectConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId);
    
    // 验证和校验
    Task<ConfigurationValidationResultDto> ValidateConfigurationAsync(CreateConfigurationDto configDto);
    Task<ConfigurationValidationResultDto> ValidateValueAsync(Guid configurationId, string? value);
    Task<List<string>> ValidateConfigurationIntegrityAsync();
    
    // 批量操作
    Task<bool> BatchUpdateConfigurationsAsync(ConfigurationBatchOperationDto batchDto, Guid userId);
    Task<List<ConfigurationDto>> BatchCreateConfigurationsAsync(List<CreateConfigurationDto> createDtos, Guid userId);
    
    // 模板管理
    Task<List<ConfigurationTemplateDto>> GetTemplatesAsync();
    Task<ConfigurationTemplateDto> CreateTemplateAsync(ConfigurationTemplateDto templateDto, Guid userId);
    Task<List<ConfigurationDto>> ApplyTemplateAsync(Guid templateId, string targetEnvironment, Guid userId);
    
    // 备份和恢复
    Task<ConfigurationBackupDto> CreateBackupAsync(string name, string? description, List<string>? sections, Guid userId);
    Task<List<ConfigurationBackupDto>> GetBackupsAsync();
    Task<bool> RestoreFromBackupAsync(ConfigurationRestoreDto restoreDto, Guid userId);
    
    // 导入导出
    Task<string> ExportConfigurationsAsync(ConfigurationExportDto exportDto);
    Task<ConfigurationValidationResultDto> ImportConfigurationsAsync(ConfigurationImportDto importDto, Guid userId);
    
    // 统计和监控
    Task<ConfigurationStatisticsDto> GetStatisticsAsync();
    Task<List<ConfigurationChangeActivity>> GetRecentActivityAsync(int days = 7);
    
    // 缓存管理
    Task RefreshCacheAsync();
    Task InvalidateCacheAsync(string section, string key);
    
    // 配置值获取 (运行时使用)
    Task<T?> GetValueAsync<T>(string section, string key, T? defaultValue = default, string environment = "Production");
    Task<string?> GetStringValueAsync(string section, string key, string? defaultValue = null, string environment = "Production");
    Task<bool> GetBoolValueAsync(string section, string key, bool defaultValue = false, string environment = "Production");
    Task<int> GetIntValueAsync(string section, string key, int defaultValue = 0, string environment = "Production");
}

/// <summary>
/// 配置管理服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IRepository<SystemConfiguration> _configRepository;
    private readonly IRepository<ConfigurationVersion> _versionRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationService> _logger;
    
    private const string CACHE_PREFIX = "config:";
    private const int CACHE_DURATION_MINUTES = 30;

    public ConfigurationService(
        IRepository<SystemConfiguration> configRepository,
        IRepository<ConfigurationVersion> versionRepository,
        IRepository<User> userRepository,
        IAuditLogRepository auditLogRepository,
        IMemoryCache cache,
        ILogger<ConfigurationService> logger)
    {
        _configRepository = configRepository;
        _versionRepository = versionRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _cache = cache;
        _logger = logger;
    }

    #region 基础CRUD操作

    public async Task<ConfigurationDto?> GetConfigurationAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{CACHE_PREFIX}id:{id}";
            if (_cache.TryGetValue(cacheKey, out ConfigurationDto? cachedConfig))
            {
                return cachedConfig;
            }

            var config = await _configRepository.GetByIdAsync(id);
            if (config == null) return null;

            var dto = MapToDto(config);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration by ID: {Id}", id);
            throw;
        }
    }

    public async Task<ConfigurationDto?> GetConfigurationAsync(string section, string key, string environment = "Production")
    {
        try
        {
            var cacheKey = $"{CACHE_PREFIX}{section}:{key}:{environment}";
            if (_cache.TryGetValue(cacheKey, out ConfigurationDto? cachedConfig))
            {
                return cachedConfig;
            }

            var config = await _configRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.Section == section && c.Key == key && c.Environment == environment);

            if (config == null) return null;

            var dto = MapToDto(config);
            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration: {Section}.{Key} in {Environment}", section, key, environment);
            throw;
        }
    }

    public async Task<List<ConfigurationDto>> GetConfigurationsAsync(ConfigurationSearchDto searchDto)
    {
        try
        {
            var query = _configRepository.GetQueryable();

            // 应用搜索条件
            if (!string.IsNullOrEmpty(searchDto.Section))
                query = query.Where(c => c.Section.Contains(searchDto.Section));

            if (!string.IsNullOrEmpty(searchDto.Key))
                query = query.Where(c => c.Key.Contains(searchDto.Key));

            if (!string.IsNullOrEmpty(searchDto.Value))
                query = query.Where(c => c.Value != null && c.Value.Contains(searchDto.Value));

            if (!string.IsNullOrEmpty(searchDto.Environment))
                query = query.Where(c => c.Environment == searchDto.Environment);

            if (searchDto.Criticality.HasValue)
                query = query.Where(c => c.Criticality == searchDto.Criticality.Value);

            if (searchDto.IsSystem.HasValue)
                query = query.Where(c => c.IsSystem == searchDto.IsSystem.Value);

            if (searchDto.IsEncrypted.HasValue)
                query = query.Where(c => c.IsEncrypted == searchDto.IsEncrypted.Value);

            if (searchDto.RequiresApproval.HasValue)
                query = query.Where(c => c.RequiresApproval == searchDto.RequiresApproval.Value);

            if (searchDto.IsReadOnly.HasValue)
                query = query.Where(c => c.IsReadOnly == searchDto.IsReadOnly.Value);

            if (!string.IsNullOrEmpty(searchDto.Tags))
                query = query.Where(c => c.Tags != null && c.Tags.Contains(searchDto.Tags));

            if (searchDto.CreatedAfter.HasValue)
                query = query.Where(c => c.CreatedAt >= searchDto.CreatedAfter.Value);

            if (searchDto.CreatedBefore.HasValue)
                query = query.Where(c => c.CreatedAt <= searchDto.CreatedBefore.Value);

            if (searchDto.UpdatedAfter.HasValue)
                query = query.Where(c => c.UpdatedAt >= searchDto.UpdatedAfter.Value);

            if (searchDto.UpdatedBefore.HasValue)
                query = query.Where(c => c.UpdatedAt <= searchDto.UpdatedBefore.Value);

            // 排序
            query = searchDto.SortBy?.ToLower() switch
            {
                "section" => searchDto.SortDescending ? query.OrderByDescending(c => c.Section) : query.OrderBy(c => c.Section),
                "key" => searchDto.SortDescending ? query.OrderByDescending(c => c.Key) : query.OrderBy(c => c.Key),
                "environment" => searchDto.SortDescending ? query.OrderByDescending(c => c.Environment) : query.OrderBy(c => c.Environment),
                "criticality" => searchDto.SortDescending ? query.OrderByDescending(c => c.Criticality) : query.OrderBy(c => c.Criticality),
                "createdat" => searchDto.SortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                "updatedat" => searchDto.SortDescending ? query.OrderByDescending(c => c.UpdatedAt) : query.OrderBy(c => c.UpdatedAt),
                _ => query.OrderBy(c => c.Section).ThenBy(c => c.Key)
            };

            // 分页
            var totalCount = await query.CountAsync();
            var configs = await query
                .Skip((searchDto.Page - 1) * searchDto.PageSize)
                .Take(searchDto.PageSize)
                .ToListAsync();

            return configs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching configurations");
            throw;
        }
    }

    public async Task<ConfigurationDto> CreateConfigurationAsync(CreateConfigurationDto createDto, Guid userId)
    {
        try
        {
            // 验证配置是否已存在
            var existing = await _configRepository.GetQueryable()
                .FirstOrDefaultAsync(c => c.Section == createDto.Section && 
                                        c.Key == createDto.Key && 
                                        c.Environment == createDto.Environment);

            if (existing != null)
            {
                throw new InvalidOperationException($"Configuration {createDto.Section}.{createDto.Key} already exists in {createDto.Environment} environment");
            }

            // 验证配置值
            var validationResult = await ValidateConfigurationAsync(createDto);
            if (!validationResult.IsValid)
            {
                throw new ArgumentException($"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // 创建配置
            var config = SystemConfiguration.Create(
                createDto.Section,
                createDto.Key,
                createDto.IsEncrypted ? await EncryptValueAsync(createDto.Value) : createDto.Value,
                createDto.DataType,
                createDto.Description,
                createDto.Environment);

            // 设置其他属性
            config.IsSystem = createDto.IsSystem;
            config.IsEncrypted = createDto.IsEncrypted;
            config.DisplayOrder = createDto.DisplayOrder;
            config.Criticality = createDto.Criticality;
            config.RequiresApproval = createDto.RequiresApproval;
            config.EnableVersioning = createDto.EnableVersioning;
            config.MaxVersions = createDto.MaxVersions;
            config.Schema = createDto.Schema;
            config.DefaultValue = createDto.DefaultValue;
            config.ValidationRules = createDto.ValidationRules;
            config.Tags = createDto.Tags;
            config.ImpactAssessment = createDto.ImpactAssessment;
            config.EffectiveDate = createDto.EffectiveDate;
            config.ExpirationDate = createDto.ExpirationDate;
            config.LastChangeReason = createDto.ChangeReason;

            await _configRepository.AddAsync(config);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Create", createDto.ChangeReason, userId);

            // 清除缓存
            await InvalidateCacheAsync(config.Section, config.Key);

            _logger.LogInformation("Configuration created: {Section}.{Key} by user {UserId}", 
                config.Section, config.Key, userId);

            return MapToDto(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration: {Section}.{Key}", createDto.Section, createDto.Key);
            throw;
        }
    }

    public async Task<ConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateConfigurationDto updateDto, Guid userId)
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(id);
            if (config == null)
            {
                throw new ArgumentException($"Configuration with ID {id} not found");
            }

            if (config.IsReadOnly)
            {
                throw new InvalidOperationException("Cannot update read-only configuration");
            }

            // 验证新值
            if (updateDto.Value != null)
            {
                var validationResult = await ValidateValueAsync(id, updateDto.Value);
                if (!validationResult.IsValid)
                {
                    throw new ArgumentException($"Value validation failed: {string.Join(", ", validationResult.Errors)}");
                }
            }

            // 记录旧值用于审计
            var oldValue = config.Value;
            var changeDetails = new
            {
                OldValue = oldValue,
                NewValue = updateDto.Value,
                Changes = new List<string>()
            };

            // 更新配置
            if (updateDto.Value != null)
            {
                var newValue = config.IsEncrypted ? await EncryptValueAsync(updateDto.Value) : updateDto.Value;
                var needsApproval = config.SetValue(newValue, updateDto.ChangeReason, updateDto.DataType, userId);
                
                if (needsApproval && !await IsUserAuthorizedForDirectUpdateAsync(userId, config.Criticality))
                {
                    // 如果需要审批，创建待审批版本
                    await CreatePendingVersionAsync(config, userId, updateDto.ChangeReason);
                    _logger.LogInformation("Configuration update requires approval: {Section}.{Key}", 
                        config.Section, config.Key);
                }
            }

            if (updateDto.Description != null) config.Description = updateDto.Description;
            if (updateDto.DisplayOrder.HasValue) config.DisplayOrder = updateDto.DisplayOrder.Value;
            if (updateDto.Criticality.HasValue) config.SetCriticality(updateDto.Criticality.Value);
            if (updateDto.RequiresApproval.HasValue) config.RequiresApproval = updateDto.RequiresApproval.Value;
            if (updateDto.EnableVersioning.HasValue) config.EnableVersioning = updateDto.EnableVersioning.Value;
            if (updateDto.MaxVersions.HasValue) config.MaxVersions = updateDto.MaxVersions.Value;
            if (updateDto.Schema != null) config.Schema = updateDto.Schema;
            if (updateDto.DefaultValue != null) config.DefaultValue = updateDto.DefaultValue;
            if (updateDto.ValidationRules != null) config.ValidationRules = updateDto.ValidationRules;
            if (updateDto.Tags != null) config.Tags = updateDto.Tags;
            if (updateDto.ImpactAssessment != null) config.ImpactAssessment = updateDto.ImpactAssessment;
            if (updateDto.IsReadOnly.HasValue) config.IsReadOnly = updateDto.IsReadOnly.Value;
            if (updateDto.EffectiveDate.HasValue) config.EffectiveDate = updateDto.EffectiveDate;
            if (updateDto.ExpirationDate.HasValue) config.ExpirationDate = updateDto.ExpirationDate;

            await _configRepository.UpdateAsync(config);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Update", updateDto.ChangeReason, userId, 
                JsonSerializer.Serialize(changeDetails));

            // 清除缓存
            await InvalidateCacheAsync(config.Section, config.Key);

            _logger.LogInformation("Configuration updated: {Section}.{Key} by user {UserId}", 
                config.Section, config.Key, userId);

            return MapToDto(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteConfigurationAsync(Guid id, string reason, Guid userId)
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(id);
            if (config == null) return false;

            if (config.IsSystem)
            {
                throw new InvalidOperationException("Cannot delete system configuration");
            }

            if (config.IsReadOnly)
            {
                throw new InvalidOperationException("Cannot delete read-only configuration");
            }

            // 检查是否需要审批
            if (config.RequiresApproval || config.Criticality >= ConfigurationCriticality.Medium)
            {
                if (!await IsUserAuthorizedForDirectUpdateAsync(userId, config.Criticality))
                {
                    // 创建删除审批请求
                    await CreatePendingDeletionAsync(config, userId, reason);
                    return true;
                }
            }

            // 创建删除版本记录
            if (config.EnableVersioning)
            {
                config.CreateVersion(ConfigurationChangeType.Delete, reason);
            }

            await _configRepository.DeleteAsync(config);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Delete", reason, userId);

            // 清除缓存
            await InvalidateCacheAsync(config.Section, config.Key);

            _logger.LogInformation("Configuration deleted: {Section}.{Key} by user {UserId}", 
                config.Section, config.Key, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration: {Id}", id);
            throw;
        }
    }

    #endregion

    #region 版本控制

    public async Task<List<ConfigurationVersionDto>> GetVersionHistoryAsync(Guid configurationId)
    {
        try
        {
            var versions = await _versionRepository.GetQueryable()
                .Where(v => v.ConfigurationId == configurationId)
                .Include(v => v.ApprovedByUser)
                .OrderByDescending(v => v.Version)
                .ToListAsync();

            return versions.Select(MapVersionToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history for configuration: {ConfigurationId}", configurationId);
            throw;
        }
    }

    public async Task<ConfigurationVersionDto?> GetVersionAsync(Guid configurationId, int version)
    {
        try
        {
            var versionEntity = await _versionRepository.GetQueryable()
                .Include(v => v.ApprovedByUser)
                .FirstOrDefaultAsync(v => v.ConfigurationId == configurationId && v.Version == version);

            return versionEntity != null ? MapVersionToDto(versionEntity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version {Version} for configuration: {ConfigurationId}", 
                version, configurationId);
            throw;
        }
    }

    public async Task<bool> RollbackToVersionAsync(ConfigurationRollbackDto rollbackDto, Guid userId)
    {
        try
        {
            var config = await _configRepository.GetByIdAsync(rollbackDto.ConfigurationId);
            if (config == null) return false;

            if (config.IsReadOnly && !rollbackDto.ForceRollback)
            {
                throw new InvalidOperationException("Cannot rollback read-only configuration without force flag");
            }

            // 检查用户权限
            if (!await IsUserAuthorizedForRollbackAsync(userId, config.Criticality))
            {
                throw new UnauthorizedAccessException("User not authorized for configuration rollback");
            }

            var success = config.RollbackToVersion(rollbackDto.TargetVersion, rollbackDto.Reason);
            if (!success) return false;

            await _configRepository.UpdateAsync(config);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Rollback", rollbackDto.Reason, userId);

            // 清除缓存
            await InvalidateCacheAsync(config.Section, config.Key);

            _logger.LogInformation("Configuration rolled back: {Section}.{Key} to version {Version} by user {UserId}", 
                config.Section, config.Key, rollbackDto.TargetVersion, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back configuration: {ConfigurationId}", rollbackDto.ConfigurationId);
            throw;
        }
    }

    #endregion

    #region 辅助方法

    private ConfigurationDto MapToDto(SystemConfiguration config)
    {
        return new ConfigurationDto
        {
            Id = config.Id,
            Section = config.Section,
            Key = config.Key,
            Value = config.IsEncrypted ? "[ENCRYPTED]" : config.Value,
            DataType = config.DataType,
            Description = config.Description,
            IsSystem = config.IsSystem,
            IsEncrypted = config.IsEncrypted,
            DisplayOrder = config.DisplayOrder,
            CurrentVersion = config.CurrentVersion,
            Criticality = config.Criticality,
            RequiresApproval = config.RequiresApproval,
            EnableVersioning = config.EnableVersioning,
            MaxVersions = config.MaxVersions,
            Schema = config.Schema,
            DefaultValue = config.DefaultValue,
            ValidationRules = config.ValidationRules,
            Tags = config.Tags,
            Environment = config.Environment,
            LastChangeReason = config.LastChangeReason,
            ImpactAssessment = config.ImpactAssessment,
            IsReadOnly = config.IsReadOnly,
            EffectiveDate = config.EffectiveDate,
            ExpirationDate = config.ExpirationDate,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
            CreatedBy = config.CreatedBy,
            UpdatedBy = config.UpdatedBy
        };
    }

    private ConfigurationVersionDto MapVersionToDto(ConfigurationVersion version)
    {
        return new ConfigurationVersionDto
        {
            Id = version.Id,
            ConfigurationId = version.ConfigurationId,
            Version = version.Version,
            Section = version.Section,
            Key = version.Key,
            Value = version.IsEncrypted ? "[ENCRYPTED]" : version.Value,
            DataType = version.DataType,
            Description = version.Description,
            IsSystem = version.IsSystem,
            IsEncrypted = version.IsEncrypted,
            DisplayOrder = version.DisplayOrder,
            ChangeType = version.ChangeType,
            ChangeReason = version.ChangeReason,
            ChangeDetails = version.ChangeDetails,
            ApprovalStatus = version.ApprovalStatus,
            ApprovedByUserId = version.ApprovedByUserId,
            ApprovedByUserName = version.ApprovedByUser?.UserName,
            ApprovedAt = version.ApprovedAt,
            ApprovalNotes = version.ApprovalNotes,
            IsCurrent = version.IsCurrent,
            CanRollback = version.CanRollback,
            Checksum = version.Checksum,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy
        };
    }

    private async Task<string?> EncryptValueAsync(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        // 这里应该使用实际的加密服务
        // 示例使用简单的Base64编码，实际应用中应使用AES等强加密算法
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes);
    }

    private async Task<string?> DecryptValueAsync(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue)) return encryptedValue;
        
        try
        {
            var bytes = Convert.FromBase64String(encryptedValue);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return encryptedValue; // 如果解密失败，返回原值
        }
    }

    private async Task<bool> IsUserAuthorizedForDirectUpdateAsync(Guid userId, ConfigurationCriticality criticality)
    {
        // 实现用户权限检查逻辑
        // 这里应该检查用户角色和权限
        return true; // 简化实现
    }

    private async Task<bool> IsUserAuthorizedForRollbackAsync(Guid userId, ConfigurationCriticality criticality)
    {
        // 实现回滚权限检查逻辑
        return true; // 简化实现
    }

    private async Task CreatePendingVersionAsync(SystemConfiguration config, Guid userId, string? reason)
    {
        // 创建待审批的版本
        var version = ConfigurationVersion.Create(config, config.CurrentVersion + 1, 
            ConfigurationChangeType.Update, reason);
        version.ApprovalStatus = ConfigurationApprovalStatus.Pending;
        
        await _versionRepository.AddAsync(version);
    }

    private async Task CreatePendingDeletionAsync(SystemConfiguration config, Guid userId, string reason)
    {
        // 创建待审批的删除请求
        var version = ConfigurationVersion.Create(config, config.CurrentVersion + 1, 
            ConfigurationChangeType.Delete, reason);
        version.ApprovalStatus = ConfigurationApprovalStatus.Pending;
        
        await _versionRepository.AddAsync(version);
    }

    private async Task LogConfigurationChangeAsync(Guid configurationId, string action, string? reason, 
        Guid userId, string? details = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = nameof(SystemConfiguration),
            EntityId = configurationId.ToString(),
            Details = details ?? reason ?? string.Empty,
            IpAddress = "127.0.0.1", // 应该从HTTP上下文获取
            UserAgent = "System", // 应该从HTTP上下文获取
            Timestamp = DateTime.UtcNow
        };

        await _auditLogRepository.AddAsync(auditLog);
    }

    #endregion

    // 由于代码过长，我将在下一部分继续实现其余方法
    
    #region 未实现的接口方法 (暂时抛出NotImplementedException)
    
    public async Task<List<ConfigurationVersionDto>> GetPendingApprovalsAsync()
    {
        var pendingVersions = await _context.ConfigurationVersions
            .Where(v => v.Status == ConfigurationStatus.Pending)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();
        
        return _mapper.Map<List<ConfigurationVersionDto>>(pendingVersions);
    }

    public async Task<bool> ApproveConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId)
    {
        var version = await _context.ConfigurationVersions.FindAsync(approvalDto.VersionId);
        if (version == null || version.Status != ConfigurationStatus.Pending)
            return false;

        version.Status = ConfigurationStatus.Active;
        version.ApprovedById = userId;
        version.ApprovedAt = DateTime.UtcNow;
        version.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        await InvalidateCacheAsync(version.Section, version.Key);
        
        return true;
    }

    public async Task<bool> RejectConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId)
    {
        var version = await _context.ConfigurationVersions.FindAsync(approvalDto.VersionId);
        if (version == null || version.Status != ConfigurationStatus.Pending)
            return false;

        version.Status = ConfigurationStatus.Rejected;
        version.RejectedById = userId;
        version.RejectedAt = DateTime.UtcNow;
        version.RejectionReason = approvalDto.Reason;
        version.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ConfigurationValidationResultDto> ValidateConfigurationAsync(CreateConfigurationDto configDto)
    {
        var result = new ConfigurationValidationResultDto
        {
            IsValid = true,
            Errors = new List<string>()
        };

        // Check for duplicate key
        var existing = await _context.Configurations
            .AnyAsync(c => c.Section == configDto.Section && 
                          c.Key == configDto.Key && 
                          c.Environment == configDto.Environment);
        
        if (existing)
        {
            result.IsValid = false;
            result.Errors.Add($"Configuration with key '{configDto.Section}:{configDto.Key}' already exists in {configDto.Environment}");
        }

        // Validate value format
        if (!string.IsNullOrEmpty(configDto.ValidationSchema))
        {
            try
            {
                // Simple validation - can be extended with JSON schema validation
                if (configDto.DataType == "int" && !int.TryParse(configDto.Value, out _))
                {
                    result.IsValid = false;
                    result.Errors.Add("Value must be a valid integer");
                }
                else if (configDto.DataType == "bool" && !bool.TryParse(configDto.Value, out _))
                {
                    result.IsValid = false;
                    result.Errors.Add("Value must be a valid boolean");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation failed: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<ConfigurationValidationResultDto> ValidateValueAsync(Guid configurationId, string? value)
    {
        var result = new ConfigurationValidationResultDto
        {
            IsValid = true,
            Errors = new List<string>()
        };

        var config = await _context.Configurations.FindAsync(configurationId);
        if (config == null)
        {
            result.IsValid = false;
            result.Errors.Add("Configuration not found");
            return result;
        }

        if (config.DataType == "int" && !int.TryParse(value, out _))
        {
            result.IsValid = false;
            result.Errors.Add("Value must be a valid integer");
        }
        else if (config.DataType == "bool" && !bool.TryParse(value, out _))
        {
            result.IsValid = false;
            result.Errors.Add("Value must be a valid boolean");
        }

        return result;
    }

    public async Task<List<string>> ValidateConfigurationIntegrityAsync()
    {
        var issues = new List<string>();
        
        var configs = await _context.Configurations.ToListAsync();
        var groupedConfigs = configs.GroupBy(c => new { c.Section, c.Key, c.Environment });
        
        foreach (var group in groupedConfigs)
        {
            if (group.Count() > 1)
            {
                issues.Add($"Duplicate configuration found: {group.Key.Section}:{group.Key.Key} in {group.Key.Environment}");
            }
        }

        return issues;
    }

    public async Task<bool> BatchUpdateConfigurationsAsync(ConfigurationBatchOperationDto batchDto, Guid userId)
    {
        var configs = await _context.Configurations
            .Where(c => batchDto.ConfigurationIds.Contains(c.Id))
            .ToListAsync();

        foreach (var config in configs)
        {
            if (!string.IsNullOrEmpty(batchDto.NewSection))
                config.Section = batchDto.NewSection;
            
            if (!string.IsNullOrEmpty(batchDto.NewEnvironment))
                config.Environment = batchDto.NewEnvironment;
            
            config.UpdatedAt = DateTime.UtcNow;
            config.UpdatedById = userId;
        }

        await _context.SaveChangesAsync();
        await RefreshCacheAsync();
        
        return true;
    }

    public async Task<List<ConfigurationDto>> BatchCreateConfigurationsAsync(List<CreateConfigurationDto> createDtos, Guid userId)
    {
        var configs = new List<Configuration>();
        
        foreach (var dto in createDtos)
        {
            var config = _mapper.Map<Configuration>(dto);
            config.Id = Guid.NewGuid();
            config.CreatedById = userId;
            config.CreatedAt = DateTime.UtcNow;
            configs.Add(config);
        }

        _context.Configurations.AddRange(configs);
        await _context.SaveChangesAsync();
        
        return _mapper.Map<List<ConfigurationDto>>(configs);
    }

    public async Task<List<ConfigurationTemplateDto>> GetTemplatesAsync()
    {
        var templates = await _context.ConfigurationTemplates
            .OrderBy(t => t.Name)
            .ToListAsync();
        
        return _mapper.Map<List<ConfigurationTemplateDto>>(templates);
    }

    public async Task<ConfigurationTemplateDto> CreateTemplateAsync(ConfigurationTemplateDto templateDto, Guid userId)
    {
        var template = _mapper.Map<ConfigurationTemplate>(templateDto);
        template.Id = Guid.NewGuid();
        template.CreatedById = userId;
        template.CreatedAt = DateTime.UtcNow;
        
        _context.ConfigurationTemplates.Add(template);
        await _context.SaveChangesAsync();
        
        return _mapper.Map<ConfigurationTemplateDto>(template);
    }

    public async Task<List<ConfigurationDto>> ApplyTemplateAsync(Guid templateId, string targetEnvironment, Guid userId)
    {
        var template = await _context.ConfigurationTemplates.FindAsync(templateId);
        if (template == null)
            return new List<ConfigurationDto>();

        var templateConfigs = System.Text.Json.JsonSerializer.Deserialize<List<CreateConfigurationDto>>(template.ConfigurationJson);
        if (templateConfigs == null)
            return new List<ConfigurationDto>();

        foreach (var config in templateConfigs)
        {
            config.Environment = targetEnvironment;
        }

        return await BatchCreateConfigurationsAsync(templateConfigs, userId);
    }

    public async Task<ConfigurationBackupDto> CreateBackupAsync(string name, string? description, List<string>? sections, Guid userId)
    {
        var query = _context.Configurations.AsQueryable();
        
        if (sections != null && sections.Any())
        {
            query = query.Where(c => sections.Contains(c.Section));
        }

        var configs = await query.ToListAsync();
        
        var backup = new ConfigurationBackup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(configs),
            ConfigurationCount = configs.Count,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConfigurationBackups.Add(backup);
        await _context.SaveChangesAsync();
        
        return _mapper.Map<ConfigurationBackupDto>(backup);
    }

    public async Task<List<ConfigurationBackupDto>> GetBackupsAsync()
    {
        var backups = await _context.ConfigurationBackups
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        
        return _mapper.Map<List<ConfigurationBackupDto>>(backups);
    }

    public async Task<bool> RestoreFromBackupAsync(ConfigurationRestoreDto restoreDto, Guid userId)
    {
        var backup = await _context.ConfigurationBackups.FindAsync(restoreDto.BackupId);
        if (backup == null)
            return false;

        var configs = System.Text.Json.JsonSerializer.Deserialize<List<Configuration>>(backup.ConfigurationJson);
        if (configs == null)
            return false;

        if (restoreDto.ClearExisting)
        {
            var existingConfigs = await _context.Configurations.ToListAsync();
            _context.Configurations.RemoveRange(existingConfigs);
        }

        foreach (var config in configs)
        {
            config.Id = Guid.NewGuid();
            config.CreatedById = userId;
            config.CreatedAt = DateTime.UtcNow;
            _context.Configurations.Add(config);
        }

        backup.LastRestoredAt = DateTime.UtcNow;
        backup.RestoredById = userId;
        
        await _context.SaveChangesAsync();
        await RefreshCacheAsync();
        
        return true;
    }

    public async Task<string> ExportConfigurationsAsync(ConfigurationExportDto exportDto)
    {
        var query = _context.Configurations.AsQueryable();
        
        if (exportDto.Sections != null && exportDto.Sections.Any())
        {
            query = query.Where(c => exportDto.Sections.Contains(c.Section));
        }

        if (!string.IsNullOrEmpty(exportDto.Environment))
        {
            query = query.Where(c => c.Environment == exportDto.Environment);
        }

        var configs = await query.ToListAsync();
        
        return exportDto.Format?.ToLower() switch
        {
            "yaml" => ConvertToYaml(configs),
            "xml" => ConvertToXml(configs),
            _ => System.Text.Json.JsonSerializer.Serialize(configs, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            })
        };
    }

    public async Task<ConfigurationValidationResultDto> ImportConfigurationsAsync(ConfigurationImportDto importDto, Guid userId)
    {
        var result = new ConfigurationValidationResultDto
        {
            IsValid = true,
            Errors = new List<string>()
        };

        try
        {
            var configs = System.Text.Json.JsonSerializer.Deserialize<List<Configuration>>(importDto.ConfigurationData);
            if (configs == null || !configs.Any())
            {
                result.IsValid = false;
                result.Errors.Add("No valid configurations found in import data");
                return result;
            }

            if (importDto.ReplaceExisting)
            {
                var existingKeys = configs.Select(c => new { c.Section, c.Key, c.Environment });
                var toRemove = await _context.Configurations
                    .Where(c => existingKeys.Contains(new { c.Section, c.Key, c.Environment }))
                    .ToListAsync();
                _context.Configurations.RemoveRange(toRemove);
            }

            foreach (var config in configs)
            {
                config.Id = Guid.NewGuid();
                config.CreatedById = userId;
                config.CreatedAt = DateTime.UtcNow;
                _context.Configurations.Add(config);
            }

            await _context.SaveChangesAsync();
            await RefreshCacheAsync();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    public async Task<ConfigurationStatisticsDto> GetStatisticsAsync()
    {
        var stats = new ConfigurationStatisticsDto
        {
            TotalConfigurations = await _context.Configurations.CountAsync(),
            TotalSections = await _context.Configurations.Select(c => c.Section).Distinct().CountAsync(),
            ConfigurationsByEnvironment = await _context.Configurations
                .GroupBy(c => c.Environment)
                .Select(g => new EnvironmentStatistics 
                { 
                    Environment = g.Key, 
                    Count = g.Count() 
                })
                .ToListAsync(),
            RecentChanges = await _context.ConfigurationVersions
                .Where(v => v.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync()
        };

        return stats;
    }

    public async Task<List<ConfigurationChangeActivity>> GetRecentActivityAsync(int days = 7)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var activities = await _context.ConfigurationVersions
            .Where(v => v.CreatedAt >= startDate)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new ConfigurationChangeActivity
            {
                Id = v.Id,
                Action = v.Status.ToString(),
                Section = v.Section,
                Key = v.Key,
                Environment = v.Environment,
                OldValue = v.OldValue,
                NewValue = v.Value,
                ChangedById = v.CreatedById,
                ChangedAt = v.CreatedAt,
                Reason = v.ChangeReason
            })
            .ToListAsync();

        return activities;
    }

    public async Task RefreshCacheAsync()
    {
        // Clear all configuration cache entries
        var cacheKeys = _context.Configurations
            .Select(c => $"{CACHE_PREFIX}{c.Section}:{c.Key}:{c.Environment}")
            .Distinct();

        foreach (var key in cacheKeys)
        {
            _cache.Remove(key);
        }

        await Task.CompletedTask;
    }

    public Task InvalidateCacheAsync(string section, string key)
    {
        _cache.Remove($"{CACHE_PREFIX}{section}:{key}:Production");
        _cache.Remove($"{CACHE_PREFIX}{section}:{key}:Development");
        _cache.Remove($"{CACHE_PREFIX}{section}:{key}:Staging");
        return Task.CompletedTask;
    }

    public async Task<T?> GetValueAsync<T>(string section, string key, T? defaultValue = default, string environment = "Production")
    {
        var config = await GetConfigurationAsync(section, key, environment);
        if (config == null || string.IsNullOrEmpty(config.Value))
            return defaultValue;

        try
        {
            var type = typeof(T);
            
            if (type == typeof(string))
                return (T)(object)config.Value;
            
            if (type == typeof(int))
                return (T)(object)int.Parse(config.Value);
            
            if (type == typeof(bool))
                return (T)(object)bool.Parse(config.Value);
            
            if (type == typeof(double))
                return (T)(object)double.Parse(config.Value);
            
            if (type == typeof(decimal))
                return (T)(object)decimal.Parse(config.Value);
            
            // Try JSON deserialization for complex types
            return System.Text.Json.JsonSerializer.Deserialize<T>(config.Value);
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task<string?> GetStringValueAsync(string section, string key, string? defaultValue = null, string environment = "Production")
    {
        return await GetValueAsync<string>(section, key, defaultValue, environment);
    }

    public async Task<bool> GetBoolValueAsync(string section, string key, bool defaultValue = false, string environment = "Production")
    {
        return await GetValueAsync<bool>(section, key, defaultValue, environment);
    }

    public async Task<int> GetIntValueAsync(string section, string key, int defaultValue = 0, string environment = "Production")
    {
        return await GetValueAsync<int>(section, key, defaultValue, environment);
    }

    #endregion

    #region Helper Methods

    private string ConvertToYaml(List<Configuration> configs)
    {
        var sb = new System.Text.StringBuilder();
        var groupedBySection = configs.GroupBy(c => c.Section);
        
        foreach (var section in groupedBySection)
        {
            sb.AppendLine($"{section.Key}:");
            foreach (var config in section)
            {
                sb.AppendLine($"  {config.Key}: {config.Value}");
            }
        }
        
        return sb.ToString();
    }

    private string ConvertToXml(List<Configuration> configs)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<configurations>");
        
        var groupedBySection = configs.GroupBy(c => c.Section);
        foreach (var section in groupedBySection)
        {
            sb.AppendLine($"  <section name=\"{section.Key}\">");
            foreach (var config in section)
            {
                sb.AppendLine($"    <setting key=\"{config.Key}\" value=\"{System.Security.SecurityElement.Escape(config.Value)}\" />");
            }
            sb.AppendLine("  </section>");
        }
        
        sb.AppendLine("</configurations>");
        return sb.ToString();
    }

    #endregion
}