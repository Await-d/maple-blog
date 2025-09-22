using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 双因素认证审计服务接口
/// </summary>
public interface ITwoFactorAuditService
{
    #region Audit Logging

    /// <summary>
    /// 记录成功的审计事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="description">事件描述</param>
    /// <param name="method">2FA方法</param>
    /// <param name="auditContext">审计上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计记录ID</returns>
    Task<Guid> LogSuccessAsync(
        Guid userId,
        string eventType,
        string description,
        TwoFactorMethod? method = null,
        AuditContextDto? auditContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录失败的审计事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="description">事件描述</param>
    /// <param name="failureReason">失败原因</param>
    /// <param name="method">2FA方法</param>
    /// <param name="auditContext">审计上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计记录ID</returns>
    Task<Guid> LogFailureAsync(
        Guid userId,
        string eventType,
        string description,
        string failureReason,
        TwoFactorMethod? method = null,
        AuditContextDto? auditContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录安全事件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="description">事件描述</param>
    /// <param name="severity">严重程度</param>
    /// <param name="auditContext">审计上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计记录ID</returns>
    Task<Guid> LogSecurityEventAsync(
        Guid userId,
        string eventType,
        string description,
        SecurityEventSeverity severity,
        AuditContextDto? auditContext = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Audit Querying

    /// <summary>
    /// 获取用户的审计日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="filter">过滤条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<OperationResult<PagedResultDto<TwoFactorAuditLogDto>>> GetUserAuditLogsAsync(
        Guid userId,
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有审计日志（管理员功能）
    /// </summary>
    /// <param name="filter">过滤条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审计日志列表</returns>
    Task<OperationResult<PagedResultDto<TwoFactorAuditLogDto>>> GetAllAuditLogsAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可疑活动列表
    /// </summary>
    /// <param name="filter">过滤条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可疑活动列表</returns>
    Task<OperationResult<PagedResultDto<TwoFactorAuditLogDto>>> GetSuspiciousActivitiesAsync(
        AuditLogFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取安全统计信息
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>安全统计</returns>
    Task<OperationResult<SecurityStatisticsDto>> GetSecurityStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Risk Analysis

    /// <summary>
    /// 分析用户的风险级别
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>风险分析结果</returns>
    Task<OperationResult<UserRiskAnalysisDto>> AnalyzeUserRiskAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检测异常活动
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="auditContext">当前审计上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常检测结果</returns>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        Guid userId,
        AuditContextDto auditContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成风险报告
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>风险报告</returns>
    Task<OperationResult<RiskReportDto>> GenerateRiskReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    #endregion

    #region Compliance and Reporting

    /// <summary>
    /// 生成合规性报告
    /// </summary>
    /// <param name="reportType">报告类型</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>合规性报告</returns>
    Task<OperationResult<ComplianceReportDto>> GenerateComplianceReportAsync(
        ComplianceReportType reportType,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 导出审计日志
    /// </summary>
    /// <param name="filter">过滤条件</param>
    /// <param name="exportFormat">导出格式</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>导出结果</returns>
    Task<OperationResult<AuditExportDto>> ExportAuditLogsAsync(
        AuditLogFilterDto filter,
        AuditExportFormat exportFormat,
        CancellationToken cancellationToken = default);

    #endregion

    #region Maintenance

    /// <summary>
    /// 清理过期的审计日志
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理结果</returns>
    Task<OperationResult<AuditCleanupResult>> CleanupExpiredLogsAsync(
        int retentionDays,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 归档旧的审计日志
    /// </summary>
    /// <param name="archiveThresholdDays">归档阈值天数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>归档结果</returns>
    Task<OperationResult<AuditArchiveResult>> ArchiveOldLogsAsync(
        int archiveThresholdDays,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// 审计上下文DTO
/// </summary>
public class AuditContextDto
{
    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 设备指纹
    /// </summary>
    public string? DeviceFingerprint { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 额外的元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 审计日志DTO
/// </summary>
public class TwoFactorAuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TwoFactorMethod? Method { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
    public string? DeviceFingerprint { get; set; }
    public int RiskScore { get; set; }
    public bool IsSuspicious { get; set; }
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// 审计日志过滤条件DTO
/// </summary>
public class AuditLogFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? UserId { get; set; }
    public string? EventType { get; set; }
    public TwoFactorMethod? Method { get; set; }
    public bool? IsSuccess { get; set; }
    public bool? IsSuspicious { get; set; }
    public string? IpAddress { get; set; }
    public int? MinRiskScore { get; set; }
    public int? MaxRiskScore { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}


/// <summary>
/// 用户风险分析DTO
/// </summary>
public class UserRiskAnalysisDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int OverallRiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<RiskFactorDto> RiskFactors { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// 风险因素DTO
/// </summary>
public class RiskFactorDto
{
    public string Factor { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// 异常检测结果
/// </summary>
public class AnomalyDetectionResult
{
    public bool HasAnomalies { get; set; }
    public List<AnomalyDto> Anomalies { get; set; } = new();
    public int ConfidenceScore { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// 异常DTO
/// </summary>
public class AnomalyDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; }
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object>? Evidence { get; set; }
}

/// <summary>
/// 安全统计DTO
/// </summary>
public class SecurityStatisticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public double SuccessRate { get; set; }
    public int SuspiciousEvents { get; set; }
    public int HighRiskEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<TwoFactorMethod, int> EventsByMethod { get; set; } = new();
    public Dictionary<string, int> EventsByHour { get; set; } = new();
    public List<string> TopRiskFactors { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 风险报告DTO
/// </summary>
public class RiskReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int HighRiskUsers { get; set; }
    public int MediumRiskUsers { get; set; }
    public int LowRiskUsers { get; set; }
    public List<UserRiskSummaryDto> TopRiskUsers { get; set; } = new();
    public List<RiskTrendDto> RiskTrends { get; set; } = new();
    public List<string> SecurityRecommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 用户风险摘要DTO
/// </summary>
public class UserRiskSummaryDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public int SuspiciousEvents { get; set; }
    public DateTime LastActivity { get; set; }
}

/// <summary>
/// 风险趋势DTO
/// </summary>
public class RiskTrendDto
{
    public DateTime Date { get; set; }
    public double AverageRiskScore { get; set; }
    public int HighRiskEvents { get; set; }
    public int TotalEvents { get; set; }
}

/// <summary>
/// 合规性报告DTO
/// </summary>
public class ComplianceReportDto
{
    public ComplianceReportType ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, object> ComplianceMetrics { get; set; } = new();
    public List<ComplianceViolationDto> Violations { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// 合规性违规DTO
/// </summary>
public class ComplianceViolationDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime OccurredAt { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// 审计导出DTO
/// </summary>
public class AuditExportDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public int RecordCount { get; set; }
    public DateTime ExportedAt { get; set; }
}

/// <summary>
/// 审计清理结果
/// </summary>
public class AuditCleanupResult
{
    public int DeletedRecords { get; set; }
    public DateTime OldestRetainedDate { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// 审计归档结果
/// </summary>
public class AuditArchiveResult
{
    public int ArchivedRecords { get; set; }
    public string ArchiveLocation { get; set; } = string.Empty;
    public DateTime ArchiveDate { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// 安全事件严重程度
/// </summary>
public enum SecurityEventSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// 合规性报告类型
/// </summary>
public enum ComplianceReportType
{
    GDPR = 1,
    SOC2 = 2,
    ISO27001 = 3,
    HIPAA = 4,
    PCI_DSS = 5
}

/// <summary>
/// 审计导出格式
/// </summary>
public enum AuditExportFormat
{
    CSV = 1,
    JSON = 2,
    XML = 3,
    Excel = 4
}