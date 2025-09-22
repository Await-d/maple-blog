namespace MapleBlog.Domain.ValueObjects;

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

    /// <summary>
    /// 首次活动时间
    /// </summary>
    public DateTime? FirstActivity { get; set; }

    /// <summary>
    /// 获取成功率
    /// </summary>
    /// <returns>成功率百分比</returns>
    public double GetSuccessRate()
    {
        if (OperationCount == 0) return 0;
        return (double)SuccessCount / OperationCount * 100;
    }

    /// <summary>
    /// 获取失败率
    /// </summary>
    /// <returns>失败率百分比</returns>
    public double GetFailureRate()
    {
        if (OperationCount == 0) return 0;
        return (double)FailureCount / OperationCount * 100;
    }
}