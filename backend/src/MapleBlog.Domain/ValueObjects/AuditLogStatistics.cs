namespace MapleBlog.Domain.ValueObjects;

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
    /// 成功操作数
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// 失败操作数
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// 高风险操作数
    /// </summary>
    public long HighRiskOperations { get; set; }

    /// <summary>
    /// 敏感操作数
    /// </summary>
    public long SensitiveOperations { get; set; }

    /// <summary>
    /// 唯一用户数
    /// </summary>
    public long UniqueUsers { get; set; }

    /// <summary>
    /// 唯一IP数
    /// </summary>
    public long UniqueIpAddresses { get; set; }

    /// <summary>
    /// 最常见的操作类型
    /// </summary>
    public Dictionary<string, long> TopActions { get; set; } = new();

    /// <summary>
    /// 最常见的资源类型
    /// </summary>
    public Dictionary<string, long> TopResourceTypes { get; set; } = new();

    /// <summary>
    /// 按小时统计的活动
    /// </summary>
    public Dictionary<int, long> ActivityByHour { get; set; } = new();

    /// <summary>
    /// 按日期统计的活动
    /// </summary>
    public Dictionary<DateTime, long> ActivityByDate { get; set; } = new();

    /// <summary>
    /// 统计时间范围
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 统计时间范围
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取成功率
    /// </summary>
    /// <returns>成功率百分比</returns>
    public double GetSuccessRate()
    {
        if (TotalLogs == 0) return 0;
        return (double)SuccessfulOperations / TotalLogs * 100;
    }

    /// <summary>
    /// 获取失败率
    /// </summary>
    /// <returns>失败率百分比</returns>
    public double GetFailureRate()
    {
        if (TotalLogs == 0) return 0;
        return (double)FailedOperations / TotalLogs * 100;
    }

    /// <summary>
    /// 获取风险操作率
    /// </summary>
    /// <returns>风险操作率百分比</returns>
    public double GetRiskOperationRate()
    {
        if (TotalLogs == 0) return 0;
        return (double)HighRiskOperations / TotalLogs * 100;
    }
}