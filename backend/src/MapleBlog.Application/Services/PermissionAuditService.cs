using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using System.Text.Json;

namespace MapleBlog.Application.Services;

/// <summary>
/// 权限审计服务
/// 负责记录权限相关的操作和事件
/// </summary>
public class PermissionAuditService
{
    private readonly ILogger<PermissionAuditService> _logger;
    private readonly IAuditLogRepository _auditLogRepository;

    public PermissionAuditService(
        ILogger<PermissionAuditService> logger,
        IAuditLogRepository auditLogRepository)
    {
        _logger = logger;
        _auditLogRepository = auditLogRepository;
    }

    /// <summary>
    /// 记录权限检查事件
    /// </summary>
    public async Task LogPermissionCheckAsync(
        Guid? userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        bool isAllowed,
        string reason,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = await GetUserNameAsync(userId),
                Action = "PermissionCheck",
                ResourceType = resourceType,
                ResourceId = resourceId.ToString(),
                Description = $"Permission check for {resourceType}:{resourceId} - {operation}",
                NewValues = JsonSerializer.Serialize(new { IsAllowed = isAllowed, Reason = reason }),
                Result = isAllowed ? "Success" : "Denied",
                Category = "Security",
                RiskLevel = isAllowed ? "Low" : "Medium",
                IsSensitive = !isAllowed,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    PermissionCheck = new
                    {
                        ResourceType = resourceType,
                        ResourceId = resourceId,
                        Operation = operation.ToString(),
                        IsAllowed = isAllowed,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogDebug("Permission check logged for user {UserId}, resource {ResourceType}:{ResourceId}, operation {Operation}, result {Result}",
                userId, resourceType, resourceId, operation, isAllowed ? "Allowed" : "Denied");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission check for user {UserId}", userId);
        }
    }

    /// <summary>
    /// 记录权限授予事件
    /// </summary>
    public async Task LogPermissionGrantedAsync(
        Guid targetUserId,
        Guid grantedByUserId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        bool isTemporary,
        DateTime? expiresAt = null,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = grantedByUserId,
                UserName = await GetUserNameAsync(grantedByUserId),
                Action = isTemporary ? "GrantTemporaryPermission" : "GrantPermission",
                Description = $"Permission {(isTemporary ? "temporary" : "permanent")} operation for {resourceType}:{resourceId}",
                ResourceType = resourceType,
                ResourceId = resourceId.ToString(),
                // KeyValues property replaced with AdditionalData
                NewValues = JsonSerializer.Serialize(new
                {
                    TargetUserId = targetUserId,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Operation = operation.ToString(),
                    IsTemporary = isTemporary,
                    ExpiresAt = expiresAt,
                    Reason = reason
                }),
                Result = "Success",
                Category = "Security",
                RiskLevel = "High",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    PermissionGrant = new
                    {
                        TargetUserId = targetUserId,
                        GrantedByUserId = grantedByUserId,
                        ResourceType = resourceType,
                        ResourceId = resourceId,
                        Operation = operation.ToString(),
                        IsTemporary = isTemporary,
                        ExpiresAt = expiresAt,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogInformation("Permission granted: User {GrantedByUserId} granted {PermissionType} permission to user {TargetUserId} for {ResourceType}:{ResourceId}",
                grantedByUserId, isTemporary ? "temporary" : "permanent", targetUserId, resourceType, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission grant");
        }
    }

    /// <summary>
    /// 记录权限撤销事件
    /// </summary>
    public async Task LogPermissionRevokedAsync(
        Guid targetUserId,
        Guid revokedByUserId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        bool isTemporary,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = revokedByUserId,
                UserName = await GetUserNameAsync(revokedByUserId),
                Action = isTemporary ? "RevokeTemporaryPermission" : "RevokePermission",
                Description = $"Permission {(isTemporary ? "temporary" : "permanent")} operation for {resourceType}:{resourceId}",
                ResourceType = resourceType,
                ResourceId = resourceId.ToString(),
                // KeyValues property replaced with AdditionalData
                OldValues = JsonSerializer.Serialize(new
                {
                    TargetUserId = targetUserId,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Operation = operation.ToString(),
                    IsActive = true
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    IsActive = false,
                    RevokedAt = DateTime.UtcNow,
                    RevokeReason = reason
                }),
                Result = "Success",
                Category = "Security",
                RiskLevel = "Medium",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    PermissionRevoke = new
                    {
                        TargetUserId = targetUserId,
                        RevokedByUserId = revokedByUserId,
                        ResourceType = resourceType,
                        ResourceId = resourceId,
                        Operation = operation.ToString(),
                        IsTemporary = isTemporary,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogInformation("Permission revoked: User {RevokedByUserId} revoked {PermissionType} permission from user {TargetUserId} for {ResourceType}:{ResourceId}",
                revokedByUserId, isTemporary ? "temporary" : "permanent", targetUserId, resourceType, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission revoke");
        }
    }

    /// <summary>
    /// 记录权限委派事件
    /// </summary>
    public async Task LogPermissionDelegatedAsync(
        Guid fromUserId,
        Guid toUserId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        DateTime expiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = fromUserId,
                UserName = await GetUserNameAsync(fromUserId),
                Action = "DelegatePermission",
                // TableName property not available in AuditLog
                ResourceType = resourceType,
                ResourceId = resourceId.ToString(),
                // KeyValues property replaced with AdditionalData
                NewValues = JsonSerializer.Serialize(new
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    Operation = operation.ToString(),
                    ExpiresAt = expiresAt,
                    Type = TemporaryPermissionType.Delegated.ToString()
                }),
                Result = "Success",
                Category = "Security",
                RiskLevel = "High",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    PermissionDelegation = new
                    {
                        FromUserId = fromUserId,
                        ToUserId = toUserId,
                        ResourceType = resourceType,
                        ResourceId = resourceId,
                        Operation = operation.ToString(),
                        ExpiresAt = expiresAt,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogInformation("Permission delegated: User {FromUserId} delegated permission to user {ToUserId} for {ResourceType}:{ResourceId} until {ExpiresAt}",
                fromUserId, toUserId, resourceType, resourceId, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission delegation");
        }
    }

    /// <summary>
    /// 记录权限违规尝试
    /// </summary>
    public async Task LogUnauthorizedAccessAttemptAsync(
        Guid? userId,
        string resourceType,
        Guid resourceId,
        DataOperation operation,
        string reason,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = await GetUserNameAsync(userId),
                Action = "UnauthorizedAccess",
                // TableName property not available in AuditLog
                ResourceType = resourceType,
                ResourceId = resourceId.ToString(),
                // KeyValues property replaced with AdditionalData
                NewValues = JsonSerializer.Serialize(new
                {
                    AttemptedOperation = operation.ToString(),
                    Reason = reason,
                    Blocked = true
                }),
                Result = "Blocked",
                Category = "Security",
                RiskLevel = "High",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    SecurityIncident = new
                    {
                        UserId = userId,
                        ResourceType = resourceType,
                        ResourceId = resourceId,
                        Operation = operation.ToString(),
                        Reason = reason,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogWarning("Unauthorized access attempt: User {UserId} attempted {Operation} on {ResourceType}:{ResourceId} - {Reason}",
                userId, operation, resourceType, resourceId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log unauthorized access attempt");
        }
    }

    /// <summary>
    /// 记录权限配置变更
    /// </summary>
    public async Task LogPermissionConfigurationChangeAsync(
        Guid changedByUserId,
        string changeType,
        string configurationItem,
        object? oldValue,
        object? newValue,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = changedByUserId,
                UserName = await GetUserNameAsync(changedByUserId),
                Action = "ConfigurationChange",
                // TableName property not available in AuditLog
                ResourceType = "SystemConfiguration",
                ResourceId = configurationItem,
                // KeyValues property replaced with AdditionalData
                OldValues = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValues = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                Result = "Success",
                Category = "Configuration",
                RiskLevel = "High",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    ConfigurationChange = new
                    {
                        ChangedByUserId = changedByUserId,
                        ChangeType = changeType,
                        ConfigurationItem = configurationItem,
                        OldValue = oldValue,
                        NewValue = newValue,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogInformation("Permission configuration changed: User {UserId} performed {ChangeType} on {ConfigurationItem}",
                changedByUserId, changeType, configurationItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission configuration change");
        }
    }

    /// <summary>
    /// 记录大量权限操作（批量操作）
    /// </summary>
    public async Task LogBulkPermissionOperationAsync(
        Guid operatorUserId,
        string operationType,
        int affectedCount,
        string targetDescription,
        string? reason = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = operatorUserId,
                UserName = await GetUserNameAsync(operatorUserId),
                Action = "BulkOperation",
                // TableName property not available in AuditLog
                ResourceType = "BulkPermissions",
                ResourceId = Guid.NewGuid().ToString(),
                // KeyValues property replaced with AdditionalData
                NewValues = JsonSerializer.Serialize(new
                {
                    OperationType = operationType,
                    AffectedCount = affectedCount,
                    TargetDescription = targetDescription,
                    Reason = reason
                }),
                Result = "Success",
                Category = "Security",
                RiskLevel = "Critical",
                IsSensitive = true,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new
                {
                    BulkOperation = new
                    {
                        OperatorUserId = operatorUserId,
                        OperationType = operationType,
                        AffectedCount = affectedCount,
                        TargetDescription = targetDescription,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    }
                })
            };

            await _auditLogRepository.AddAsync(auditLog);
            _logger.LogWarning("Bulk permission operation: User {UserId} performed {OperationType} affecting {Count} items - {Description}",
                operatorUserId, operationType, affectedCount, targetDescription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log bulk permission operation");
        }
    }

    /// <summary>
    /// 获取用户名（如果用户ID存在）
    /// </summary>
    private async Task<string?> GetUserNameAsync(Guid? userId)
    {
        if (!userId.HasValue)
            return null;

        try
        {
            // 这里应该从用户服务或仓储获取用户名
            // 暂时返回用户ID作为用户名
            return userId.Value.ToString();
        }
        catch
        {
            return userId?.ToString();
        }
    }

    /// <summary>
    /// 获取权限审计报告
    /// </summary>
    public async Task<PermissionAuditReport> GenerateAuditReportAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? userId = null,
        string? resourceType = null)
    {
        try
        {
            var logs = await _auditLogRepository.GetByFilterAsync(new Domain.ValueObjects.AuditLogFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId,
                ResourceType = resourceType,
                Category = "Security"
            });

            var report = new PermissionAuditReport
            {
                StartDate = startDate,
                EndDate = endDate,
                GeneratedAt = DateTime.UtcNow,
                UserId = userId,
                ResourceType = resourceType
            };

            var logList = logs.ToList();
            report.TotalEvents = logList.Count;
            report.PermissionChecks = logList.Count(l => l.Action == "PermissionCheck");
            report.PermissionGrants = logList.Count(l => l.Action == "GrantPermission" || l.Action == "GrantTemporaryPermission");
            report.PermissionRevokes = logList.Count(l => l.Action == "RevokePermission" || l.Action == "RevokeTemporaryPermission");
            report.UnauthorizedAttempts = logList.Count(l => l.Action == "UnauthorizedAccess");
            report.HighRiskEvents = logList.Count(l => l.RiskLevel == "High" || l.RiskLevel == "Critical");

            report.EventsByType = logList
                .GroupBy(l => l.Action)
                .ToDictionary(g => g.Key, g => g.Count());

            report.EventsByUser = logList
                .Where(l => l.UserId.HasValue)
                .GroupBy(l => l.UserId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            report.EventsByResource = logList
                .Where(l => !string.IsNullOrEmpty(l.ResourceType))
                .GroupBy(l => l.ResourceType!)
                .ToDictionary(g => g.Key, g => g.Count());

            report.SuspiciousActivities = logList
                .Where(l => l.RiskLevel == "High" || l.RiskLevel == "Critical" || l.Result == "Blocked")
                .Select(l => new SuspiciousActivity
                {
                    Timestamp = l.CreatedAt,
                    UserId = l.UserId,
                    Action = l.Action,
                    ResourceType = l.ResourceType,
                    ResourceId = l.ResourceId,
                    RiskLevel = l.RiskLevel,
                    IpAddress = l.IpAddress,
                    Result = l.Result
                })
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToList();

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate permission audit report");
            throw;
        }
    }
}

/// <summary>
/// 权限审计报告
/// </summary>
public class PermissionAuditReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? ResourceType { get; set; }

    public int TotalEvents { get; set; }
    public int PermissionChecks { get; set; }
    public int PermissionGrants { get; set; }
    public int PermissionRevokes { get; set; }
    public int UnauthorizedAttempts { get; set; }
    public int HighRiskEvents { get; set; }

    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<Guid, int> EventsByUser { get; set; } = new();
    public Dictionary<string, int> EventsByResource { get; set; } = new();

    public List<SuspiciousActivity> SuspiciousActivities { get; set; } = new();
}

/// <summary>
/// 可疑活动
/// </summary>
public class SuspiciousActivity
{
    public DateTime Timestamp { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? RiskLevel { get; set; }
    public string? IpAddress { get; set; }
    public string? Result { get; set; }
}