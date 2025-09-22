namespace MapleBlog.Domain.ValueObjects;

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
    /// 总操作数
    /// </summary>
    public long TotalOperations { get; set; }

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
    /// 涉及的用户数
    /// </summary>
    public long UniqueUsers { get; set; }

    /// <summary>
    /// 涉及的会话数
    /// </summary>
    public long UniqueSessions { get; set; }

    /// <summary>
    /// 首次活动时间
    /// </summary>
    public DateTime? FirstActivity { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime? LastActivity { get; set; }

    /// <summary>
    /// 地理位置信息
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 操作类型分布
    /// </summary>
    public Dictionary<string, long> ActionDistribution { get; set; } = new();

    /// <summary>
    /// 资源类型分布
    /// </summary>
    public Dictionary<string, long> ResourceDistribution { get; set; } = new();

    /// <summary>
    /// 活动时间分布（按小时）
    /// </summary>
    public Dictionary<int, long> HourlyActivity { get; set; } = new();

    /// <summary>
    /// 用户代理分布
    /// </summary>
    public Dictionary<string, long> UserAgentDistribution { get; set; } = new();

    /// <summary>
    /// 获取失败率
    /// </summary>
    /// <returns>失败率百分比</returns>
    public double GetFailureRate()
    {
        if (TotalOperations == 0) return 0;
        return (double)FailedOperations / TotalOperations * 100;
    }

    /// <summary>
    /// 获取风险评分
    /// </summary>
    /// <returns>风险评分（0-100）</returns>
    public double GetRiskScore()
    {
        if (TotalOperations == 0) return 0;

        var failureRate = (double)FailedOperations / TotalOperations;
        var riskRate = (double)HighRiskOperations / TotalOperations;

        // 考虑操作密度（单位时间内的操作数）
        var densityScore = 0.0;
        if (FirstActivity.HasValue && LastActivity.HasValue)
        {
            var timeSpan = LastActivity.Value - FirstActivity.Value;
            if (timeSpan.TotalHours > 0)
            {
                var operationsPerHour = TotalOperations / timeSpan.TotalHours;
                densityScore = Math.Min(1.0, operationsPerHour / 100); // 超过100操作/小时认为是高密度
            }
        }

        return Math.Min(100, (failureRate * 40 + riskRate * 40 + densityScore * 20) * 100);
    }

    /// <summary>
    /// 检查是否为可疑IP
    /// </summary>
    /// <param name="failureThreshold">失败率阈值</param>
    /// <param name="operationThreshold">操作数阈值</param>
    /// <returns>是否可疑</returns>
    public bool IsSuspicious(double failureThreshold = 20.0, long operationThreshold = 100)
    {
        return GetFailureRate() > failureThreshold ||
               TotalOperations > operationThreshold ||
               GetRiskScore() > 60;
    }

    /// <summary>
    /// 检查是否为机器人行为
    /// </summary>
    /// <returns>是否为机器人</returns>
    public bool IsBotLike()
    {
        // 检查用户代理分布，如果只有很少的用户代理但操作很多，可能是机器人
        if (UserAgentDistribution.Count <= 2 && TotalOperations > 50)
            return true;

        // 检查活动密度，如果活动过于集中可能是机器人
        if (FirstActivity.HasValue && LastActivity.HasValue)
        {
            var timeSpan = LastActivity.Value - FirstActivity.Value;
            if (timeSpan.TotalHours > 0 && timeSpan.TotalHours < 1 && TotalOperations > 100)
                return true;
        }

        return false;
    }
}