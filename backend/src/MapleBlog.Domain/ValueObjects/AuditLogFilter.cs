namespace MapleBlog.Domain.ValueObjects;

/// <summary>
/// 审计日志过滤条件
/// </summary>
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
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 风险级别
    /// </summary>
    public string? RiskLevel { get; set; }

    /// <summary>
    /// 操作结果
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// 是否敏感操作
    /// </summary>
    public bool? IsSensitive { get; set; }

    /// <summary>
    /// 操作分类
    /// </summary>
    public string? Category { get; set; }

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
    public string? SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// 排序方向
    /// </summary>
    public bool IsDescending { get; set; } = true;
}