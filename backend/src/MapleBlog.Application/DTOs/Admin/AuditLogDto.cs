using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 审计日志过滤器
    /// </summary>
    /// <summary>
    /// 审计日志DTO
    /// </summary>
    public class AuditLogDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 用户邮箱
        /// </summary>
        public string? UserEmail { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 资源名称
        /// </summary>
        public string? ResourceName { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        public string? RequestPath { get; set; }

        /// <summary>
        /// 请求方法
        /// </summary>
        public string? RequestMethod { get; set; }

        /// <summary>
        /// 响应状态码
        /// </summary>
        public int? ResponseStatusCode { get; set; }

        /// <summary>
        /// 处理时长（毫秒）
        /// </summary>
        public long? Duration { get; set; }

        /// <summary>
        /// 变更前数据
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// 变更后数据
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// 变更信息
        /// </summary>
        public string? Changes { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 额外信息
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// 严重程度
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 标签
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 环境
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// 应用名称
        /// </summary>
        public string ApplicationName { get; set; } = string.Empty;

        /// <summary>
        /// 应用版本
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;

        /// <summary>
        /// 关联ID
        /// </summary>
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        public string? ModuleName { get; set; }

        /// <summary>
        /// 功能名称
        /// </summary>
        public string? FeatureName { get; set; }

        /// <summary>
        /// 业务流程
        /// </summary>
        public string? BusinessProcess { get; set; }

        /// <summary>
        /// 风险级别
        /// </summary>
        public string RiskLevel { get; set; } = string.Empty;

        /// <summary>
        /// 合规标记
        /// </summary>
        public string[] ComplianceFlags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 数据分类
        /// </summary>
        public string DataClassification { get; set; } = string.Empty;

        /// <summary>
        /// 保留期限
        /// </summary>
        public int? RetentionPeriod { get; set; }

        /// <summary>
        /// 是否已归档
        /// </summary>
        public bool IsArchived { get; set; }

        /// <summary>
        /// 归档时间
        /// </summary>
        public DateTime? ArchivedAt { get; set; }
    }

    public class AuditLogFilter
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public string? ResourceType { get; set; }

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 风险级别
        /// </summary>
        public string? RiskLevel { get; set; }

        /// <summary>
        /// 操作分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public string? Result { get; set; }

        /// <summary>
        /// 是否敏感操作
        /// </summary>
        public bool? IsSensitive { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// 页面大小
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// 排序方向
        /// </summary>
        public string SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// 审计日志统计信息
    /// </summary>
    public class AuditLogStatistics
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public long TotalLogs { get; set; }

        /// <summary>
        /// 总数量（别名，用于兼容性）
        /// </summary>
        public long TotalCount => TotalLogs;

        /// <summary>
        /// 成功操作数
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// 失败操作数
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// 失败数量（别名，用于兼容性）
        /// </summary>
        public long FailedCount => FailureCount;

        /// <summary>
        /// 敏感操作数
        /// </summary>
        public long SensitiveOperationCount { get; set; }

        /// <summary>
        /// 敏感数量（别名，用于兼容性）
        /// </summary>
        public long SensitiveCount => SensitiveOperationCount;

        /// <summary>
        /// 高风险操作数
        /// </summary>
        public long HighRiskOperationCount { get; set; }

        /// <summary>
        /// 高风险数量（别名，用于兼容性）
        /// </summary>
        public long HighRiskCount => HighRiskOperationCount;

        /// <summary>
        /// 独立用户数
        /// </summary>
        public int UniqueUserCount { get; set; }

        /// <summary>
        /// 独立用户数量（别名，用于兼容性）
        /// </summary>
        public int UniqueUsersCount => UniqueUserCount;

        /// <summary>
        /// 独立IP数
        /// </summary>
        public int UniqueIpCount { get; set; }

        /// <summary>
        /// 按操作类型统计
        /// </summary>
        public Dictionary<string, long> ActionStatistics { get; set; } = new();

        /// <summary>
        /// 按资源类型统计
        /// </summary>
        public Dictionary<string, long> ResourceTypeStatistics { get; set; } = new();

        /// <summary>
        /// 资源统计列表（用于兼容性）
        /// </summary>
        public List<ResourceStatisticDto> ResourceStatistics { get; set; } = new();

        /// <summary>
        /// 按分类统计
        /// </summary>
        public Dictionary<string, long> CategoryStatistics { get; set; } = new();

        /// <summary>
        /// 按风险级别统计
        /// </summary>
        public Dictionary<string, long> RiskLevelStatistics { get; set; } = new();

        /// <summary>
        /// 按小时统计
        /// </summary>
        public Dictionary<int, long> HourlyStatistics { get; set; } = new();

        /// <summary>
        /// 按日期统计
        /// </summary>
        public Dictionary<DateTime, long> DailyStatistics { get; set; } = new();

        /// <summary>
        /// 最活跃用户统计
        /// </summary>
        public List<UserActivityStats> TopActiveUsers { get; set; } = new();

        /// <summary>
        /// 最频繁IP统计
        /// </summary>
        public List<IpActivityStats> TopActiveIps { get; set; } = new();
    }

    /// <summary>
    /// 用户活动统计
    /// </summary>
    public class UserActivityStats
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 操作数量
        /// </summary>
        public long OperationCount { get; set; }

        /// <summary>
        /// 成功操作数
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// 失败操作数
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// IP活动统计
    /// </summary>
    public class IpActivityStats
    {
        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 操作数量
        /// </summary>
        public long OperationCount { get; set; }

        /// <summary>
        /// 独立用户数
        /// </summary>
        public int UniqueUserCount { get; set; }

        /// <summary>
        /// 成功操作数
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// 失败操作数
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// 是否可疑
        /// </summary>
        public bool IsSuspicious { get; set; }
    }

    /// <summary>
    /// 操作统计DTO
    /// </summary>
    public class ActionStatisticDto
    {
        /// <summary>
        /// 操作类型
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 操作数量
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// 资源统计DTO
    /// </summary>
    public class ResourceStatisticDto
    {
        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 操作数量
        /// </summary>
        public int Count { get; set; }
    }
}