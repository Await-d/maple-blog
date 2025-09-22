using MapleBlog.Admin.DTOs;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO.Compression;
using System.Text;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 配置服务扩展实现 - 包含剩余的复杂功能
/// </summary>
public partial class ConfigurationService
{
    #region 审批流程实现

    public async Task<List<ConfigurationVersionDto>> GetPendingApprovalsAsync()
    {
        try
        {
            var pendingVersions = await _versionRepository.GetQueryable()
                .Where(v => v.ApprovalStatus == ConfigurationApprovalStatus.Pending)
                .Include(v => v.Configuration)
                .Include(v => v.ApprovedByUser)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync();

            return pendingVersions.Select(MapVersionToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            throw;
        }
    }

    public async Task<bool> ApproveConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId)
    {
        try
        {
            var version = await _versionRepository.GetByIdAsync(approvalDto.VersionId);
            if (version == null) return false;

            if (version.ApprovalStatus != ConfigurationApprovalStatus.Pending)
            {
                throw new InvalidOperationException("Configuration is not in pending approval status");
            }

            // 检查审批权限
            var config = await _configRepository.GetByIdAsync(version.ConfigurationId);
            if (config == null) return false;

            if (!await IsUserAuthorizedForApprovalAsync(userId, config.Criticality))
            {
                throw new UnauthorizedAccessException("User not authorized for configuration approval");
            }

            // 审批配置变更
            version.Approve(userId, approvalDto.Notes);

            // 如果是删除操作，执行删除
            if (version.ChangeType == ConfigurationChangeType.Delete)
            {
                await _configRepository.DeleteAsync(config);
            }
            else
            {
                // 应用变更到配置
                config.Value = version.Value;
                config.DataType = version.DataType;
                config.Description = version.Description;
                config.DisplayOrder = version.DisplayOrder;
                config.CurrentVersion = version.Version;
                
                // 设置当前版本
                version.SetAsCurrent();
                
                await _configRepository.UpdateAsync(config);
            }

            await _versionRepository.UpdateAsync(version);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Approve", approvalDto.Notes, userId);

            // 清除缓存
            await InvalidateCacheAsync(config.Section, config.Key);

            _logger.LogInformation("Configuration approved: {Section}.{Key} version {Version} by user {UserId}", 
                config.Section, config.Key, version.Version, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving configuration: {VersionId}", approvalDto.VersionId);
            throw;
        }
    }

    public async Task<bool> RejectConfigurationAsync(ConfigurationApprovalDto approvalDto, Guid userId)
    {
        try
        {
            var version = await _versionRepository.GetByIdAsync(approvalDto.VersionId);
            if (version == null) return false;

            if (version.ApprovalStatus != ConfigurationApprovalStatus.Pending)
            {
                throw new InvalidOperationException("Configuration is not in pending approval status");
            }

            var config = await _configRepository.GetByIdAsync(version.ConfigurationId);
            if (config == null) return false;

            if (!await IsUserAuthorizedForApprovalAsync(userId, config.Criticality))
            {
                throw new UnauthorizedAccessException("User not authorized for configuration approval");
            }

            // 拒绝配置变更
            version.Reject(userId, approvalDto.Notes);
            await _versionRepository.UpdateAsync(version);

            // 记录审计日志
            await LogConfigurationChangeAsync(config.Id, "Reject", approvalDto.Notes, userId);

            _logger.LogInformation("Configuration rejected: {Section}.{Key} version {Version} by user {UserId}", 
                config.Section, config.Key, version.Version, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting configuration: {VersionId}", approvalDto.VersionId);
            throw;
        }
    }

    private async Task<bool> IsUserAuthorizedForApprovalAsync(Guid userId, ConfigurationCriticality criticality)
    {
        // 实现审批权限检查逻辑
        // 这里应该根据用户角色和配置重要性级别进行权限检查
        return true; // 简化实现
    }

    #endregion

    #region 验证和校验实现

    public async Task<ConfigurationValidationResultDto> ValidateConfigurationAsync(CreateConfigurationDto configDto)
    {
        var result = new ConfigurationValidationResultDto { IsValid = true };

        try
        {
            // 1. 基础字段验证
            if (string.IsNullOrWhiteSpace(configDto.Section))
                result.Errors.Add("Section is required");

            if (string.IsNullOrWhiteSpace(configDto.Key))
                result.Errors.Add("Key is required");

            // 2. 数据类型验证
            var typeValidation = ValidateDataTypeValue(configDto.Value, configDto.DataType);
            if (!typeValidation.IsValid)
                result.Errors.Add(typeValidation.ErrorMessage!);

            // 3. 验证规则检查
            if (!string.IsNullOrEmpty(configDto.ValidationRules))
            {
                var ruleValidation = ValidateAgainstRules(configDto.Value, configDto.ValidationRules);
                if (!ruleValidation.IsValid)
                    result.Errors.Add(ruleValidation.ErrorMessage!);
            }

            // 4. Schema 验证
            if (!string.IsNullOrEmpty(configDto.Schema))
            {
                var schemaValidation = ValidateAgainstSchema(configDto.Value, configDto.Schema);
                if (!schemaValidation.IsValid)
                    result.Errors.Add(schemaValidation.ErrorMessage!);
            }

            // 5. 业务规则验证
            await ValidateBusinessRulesAsync(configDto, result);

            // 6. 安全性检查
            ValidateSecurityRules(configDto, result);

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public async Task<ConfigurationValidationResultDto> ValidateValueAsync(Guid configurationId, string? value)
    {
        var result = new ConfigurationValidationResultDto { IsValid = true };

        try
        {
            var config = await _configRepository.GetByIdAsync(configurationId);
            if (config == null)
            {
                result.Errors.Add("Configuration not found");
                result.IsValid = false;
                return result;
            }

            var validation = config.ValidateValue(value);
            if (!validation.IsValid)
            {
                result.Errors.Add(validation.ErrorMessage!);
                result.IsValid = false;
            }

            // 额外的业务验证
            await ValidateValueBusinessRulesAsync(config, value, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration value");
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public async Task<List<string>> ValidateConfigurationIntegrityAsync()
    {
        var issues = new List<string>();

        try
        {
            // 1. 检查版本一致性
            var configurations = await _configRepository.GetQueryable()
                .Include(c => c.Versions)
                .ToListAsync();

            foreach (var config in configurations)
            {
                // 检查当前版本是否存在对应的版本记录
                var currentVersion = config.Versions.FirstOrDefault(v => v.IsCurrent);
                if (currentVersion == null)
                {
                    issues.Add($"Configuration {config.Section}.{config.Key} has no current version");
                }

                // 检查版本号连续性
                var versions = config.Versions.OrderBy(v => v.Version).ToList();
                for (int i = 1; i < versions.Count; i++)
                {
                    if (versions[i].Version != versions[i - 1].Version + 1)
                    {
                        issues.Add($"Configuration {config.Section}.{config.Key} has gap in version numbers");
                        break;
                    }
                }

                // 检查校验和
                foreach (var version in versions)
                {
                    if (!version.ValidateChecksum())
                    {
                        issues.Add($"Configuration {config.Section}.{config.Key} version {version.Version} has invalid checksum");
                    }
                }
            }

            // 2. 检查重复配置
            var duplicates = configurations
                .GroupBy(c => new { c.Section, c.Key, c.Environment })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                issues.Add($"Duplicate configuration found: {duplicate.Key.Section}.{duplicate.Key.Key} in {duplicate.Key.Environment}");
            }

            // 3. 检查过期配置
            var expiredConfigs = configurations.Where(c => c.ExpirationDate.HasValue && c.ExpirationDate.Value < DateTime.UtcNow);
            foreach (var expired in expiredConfigs)
            {
                issues.Add($"Configuration {expired.Section}.{expired.Key} has expired on {expired.ExpirationDate:yyyy-MM-dd}");
            }

            // 4. 检查配置依赖关系
            await ValidateConfigurationDependenciesAsync(configurations, issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration integrity");
            issues.Add($"Integrity validation error: {ex.Message}");
        }

        return issues;
    }

    private (bool IsValid, string? ErrorMessage) ValidateDataTypeValue(string? value, string dataType)
    {
        if (string.IsNullOrEmpty(value)) return (true, null);

        return dataType.ToLower() switch
        {
            "bool" or "boolean" => bool.TryParse(value, out _) ? (true, null) : (false, "Invalid boolean value"),
            "int" or "integer" => int.TryParse(value, out _) ? (true, null) : (false, "Invalid integer value"),
            "decimal" or "double" => decimal.TryParse(value, out _) ? (true, null) : (false, "Invalid decimal value"),
            "datetime" => DateTime.TryParse(value, out _) ? (true, null) : (false, "Invalid datetime value"),
            "email" => IsValidEmailAddress(value) ? (true, null) : (false, "Invalid email format"),
            "url" => Uri.TryCreate(value, UriKind.Absolute, out _) ? (true, null) : (false, "Invalid URL format"),
            "json" => IsValidJsonString(value) ? (true, null) : (false, "Invalid JSON format"),
            _ => (true, null)
        };
    }

    private (bool IsValid, string? ErrorMessage) ValidateAgainstRules(string? value, string rules)
    {
        try
        {
            if (rules.StartsWith("{"))
            {
                // JSON 格式的复杂规则
                var ruleObj = JsonSerializer.Deserialize<Dictionary<string, object>>(rules);
                return ValidateJsonRules(value, ruleObj!);
            }
            else
            {
                // 正则表达式规则
                var regex = new System.Text.RegularExpressions.Regex(rules);
                return regex.IsMatch(value ?? "") ? (true, null) : (false, "Value does not match validation pattern");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Validation rule error: {ex.Message}");
        }
    }

    private (bool IsValid, string? ErrorMessage) ValidateAgainstSchema(string? value, string schema)
    {
        try
        {
            // 这里可以集成 JSON Schema 验证库
            // 例如 NJsonSchema 或其他 JSON Schema 验证器
            return (true, null); // 简化实现
        }
        catch (Exception ex)
        {
            return (false, $"Schema validation error: {ex.Message}");
        }
    }

    private (bool IsValid, string? ErrorMessage) ValidateJsonRules(string? value, Dictionary<string, object> rules)
    {
        // 实现 JSON 规则验证
        // 例如：长度限制、值范围、格式要求等
        if (rules.ContainsKey("minLength") && value != null)
        {
            var minLength = Convert.ToInt32(rules["minLength"]);
            if (value.Length < minLength)
                return (false, $"Value must be at least {minLength} characters long");
        }

        if (rules.ContainsKey("maxLength") && value != null)
        {
            var maxLength = Convert.ToInt32(rules["maxLength"]);
            if (value.Length > maxLength)
                return (false, $"Value must be no more than {maxLength} characters long");
        }

        return (true, null);
    }

    private async Task ValidateBusinessRulesAsync(CreateConfigurationDto configDto, ConfigurationValidationResultDto result)
    {
        // 实现业务规则验证
        // 例如：配置命名约定、环境一致性、安全要求等

        // 检查配置命名约定
        if (!IsValidConfigurationNaming(configDto.Section, configDto.Key))
        {
            result.Warnings.Add("Configuration naming does not follow recommended conventions");
        }

        // 检查环境一致性
        if (configDto.Environment != "Production" && configDto.Criticality == ConfigurationCriticality.Critical)
        {
            result.Warnings.Add("Critical configurations should typically be in Production environment");
        }

        // 检查加密配置的安全性
        if (configDto.IsEncrypted && string.IsNullOrEmpty(configDto.Value))
        {
            result.Warnings.Add("Encrypted configuration has empty value");
        }
    }

    private async Task ValidateValueBusinessRulesAsync(SystemConfiguration config, string? value, ConfigurationValidationResultDto result)
    {
        // 实现值的业务规则验证
        // 例如：检查配置值的合理性、影响范围等

        if (config.IsSystem && string.IsNullOrEmpty(value))
        {
            result.Warnings.Add("System configuration should not have empty value");
        }
    }

    private void ValidateSecurityRules(CreateConfigurationDto configDto, ConfigurationValidationResultDto result)
    {
        // 实现安全规则验证
        
        // 检查敏感配置
        if (IsSensitiveConfiguration(configDto.Section, configDto.Key) && !configDto.IsEncrypted)
        {
            result.Warnings.Add("Sensitive configuration should be encrypted");
        }

        // 检查密码强度
        if (configDto.DataType.ToLower() == "password" && !IsStrongPassword(configDto.Value))
        {
            result.Errors.Add("Password does not meet security requirements");
        }
    }

    private async Task ValidateConfigurationDependenciesAsync(List<SystemConfiguration> configurations, List<string> issues)
    {
        // 实现配置依赖关系验证
        // 例如：检查数据库连接字符串是否与数据库配置一致等
    }

    private bool IsValidConfigurationNaming(string section, string key)
    {
        // 实现配置命名约定检查
        return !string.IsNullOrWhiteSpace(section) && !string.IsNullOrWhiteSpace(key);
    }

    private bool IsSensitiveConfiguration(string section, string key)
    {
        var sensitiveKeywords = new[] { "password", "secret", "key", "token", "connectionstring" };
        var fullKey = $"{section}.{key}".ToLower();
        return sensitiveKeywords.Any(keyword => fullKey.Contains(keyword));
    }

    private bool IsStrongPassword(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private bool IsValidEmailAddress(string email)
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

    private bool IsValidJsonString(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 运行时配置值获取

    public async Task<T?> GetValueAsync<T>(string section, string key, T? defaultValue = default, string environment = "Production")
    {
        try
        {
            var config = await GetConfigurationAsync(section, key, environment);
            if (config == null || !IsConfigurationEffective(config))
                return defaultValue;

            if (typeof(T) == typeof(string))
                return (T?)(object?)config.Value;

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                return (T?)(object?)bool.Parse(config.Value ?? "false");

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                return (T?)(object?)int.Parse(config.Value ?? "0");

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                return (T?)(object?)decimal.Parse(config.Value ?? "0");

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                return (T?)(object?)DateTime.Parse(config.Value ?? DateTime.MinValue.ToString());

            // 对于复杂类型，尝试JSON反序列化
            if (!string.IsNullOrEmpty(config.Value))
                return JsonSerializer.Deserialize<T>(config.Value);

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value: {Section}.{Key}", section, key);
            return defaultValue;
        }
    }

    public async Task<string?> GetStringValueAsync(string section, string key, string? defaultValue = null, string environment = "Production")
    {
        return await GetValueAsync(section, key, defaultValue, environment);
    }

    public async Task<bool> GetBoolValueAsync(string section, string key, bool defaultValue = false, string environment = "Production")
    {
        return await GetValueAsync(section, key, defaultValue, environment);
    }

    public async Task<int> GetIntValueAsync(string section, string key, int defaultValue = 0, string environment = "Production")
    {
        return await GetValueAsync(section, key, defaultValue, environment);
    }

    private bool IsConfigurationEffective(ConfigurationDto config)
    {
        var now = DateTime.UtcNow;
        
        if (config.EffectiveDate.HasValue && now < config.EffectiveDate.Value)
            return false;
            
        if (config.ExpirationDate.HasValue && now > config.ExpirationDate.Value)
            return false;
            
        return true;
    }

    #endregion

    #region 缓存管理

    public async Task RefreshCacheAsync()
    {
        try
        {
            // 清除所有配置缓存
            if (_cache is MemoryCache memoryCache)
            {
                // 获取所有以配置前缀开头的缓存键并清除
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
                    {
                        var keysToRemove = new List<object>();
                        foreach (DictionaryEntry entry in entries)
                        {
                            if (entry.Key.ToString()?.StartsWith(CACHE_PREFIX) == true)
                            {
                                keysToRemove.Add(entry.Key);
                            }
                        }

                        foreach (var key in keysToRemove)
                        {
                            _cache.Remove(key);
                        }
                    }
                }
            }

            _logger.LogInformation("Configuration cache refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing configuration cache");
            throw;
        }
    }

    #endregion
}