namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 敏感词检测结果
/// </summary>
public record SensitiveWordResult
{
    /// <summary>
    /// 是否包含敏感词
    /// </summary>
    public bool ContainsSensitiveWords { get; init; }

    /// <summary>
    /// 检测到的敏感词总数
    /// </summary>
    public int TotalDetectedWords { get; init; }

    /// <summary>
    /// 检测到的所有敏感词
    /// </summary>
    public IEnumerable<string> DetectedWords { get; init; } = [];

    /// <summary>
    /// 高风险敏感词
    /// </summary>
    public IEnumerable<string> HighRiskWords { get; init; } = [];

    /// <summary>
    /// 中风险敏感词
    /// </summary>
    public IEnumerable<string> MediumRiskWords { get; init; } = [];

    /// <summary>
    /// 低风险敏感词
    /// </summary>
    public IEnumerable<string> LowRiskWords { get; init; } = [];

    /// <summary>
    /// 过滤后的内容（如果启用了掩码替换）
    /// </summary>
    public string? FilteredContent { get; init; }

    /// <summary>
    /// 是否建议人工审核
    /// </summary>
    public bool RequiresManualReview { get; init; }
}

/// <summary>
/// 敏感词风险等级
/// </summary>
public enum SensitiveWordRiskLevel
{
    /// <summary>
    /// 低风险
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中风险
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高风险
    /// </summary>
    High = 3
}

/// <summary>
/// 敏感词过滤服务接口
/// </summary>
public interface ISensitiveWordFilter
{
    /// <summary>
    /// 检查内容是否包含敏感词
    /// </summary>
    /// <param name="content">要检查的内容</param>
    /// <param name="replaceWithMask">是否用掩码替换敏感词</param>
    /// <returns>检测结果</returns>
    Task<SensitiveWordResult> CheckContentAsync(string content, bool replaceWithMask = false);

    /// <summary>
    /// 批量检查内容
    /// </summary>
    /// <param name="contents">内容列表</param>
    /// <param name="replaceWithMask">是否用掩码替换敏感词</param>
    /// <returns>检测结果列表</returns>
    Task<IEnumerable<SensitiveWordResult>> CheckBatchAsync(IEnumerable<string> contents, bool replaceWithMask = false);

    /// <summary>
    /// 添加敏感词
    /// </summary>
    /// <param name="words">敏感词列表</param>
    /// <param name="riskLevel">风险等级</param>
    Task AddSensitiveWordsAsync(IEnumerable<string> words, SensitiveWordRiskLevel riskLevel);

    /// <summary>
    /// 移除敏感词
    /// </summary>
    /// <param name="words">要移除的敏感词</param>
    Task RemoveSensitiveWordsAsync(IEnumerable<string> words);

    /// <summary>
    /// 重新加载敏感词库
    /// </summary>
    Task ReloadSensitiveWordsAsync();

    /// <summary>
    /// 获取敏感词统计信息
    /// </summary>
    Task<(int HighRisk, int MediumRisk, int LowRisk, int Total)> GetSensitiveWordStatsAsync();
}