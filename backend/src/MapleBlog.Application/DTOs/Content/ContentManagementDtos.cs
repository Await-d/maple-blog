namespace MapleBlog.Application.DTOs.Content;

/// <summary>
/// SEO优化结果
/// </summary>
public class SeoOptimizationResultDto
{
    public bool Success { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string> Keywords { get; set; } = new();
    public Dictionary<string, string> MetaTags { get; set; } = new();
    public int Score { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 批量SEO结果
/// </summary>
public class BatchSeoResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<SeoOptimizationResultDto> Results { get; set; } = new();

    /// <summary>
    /// 优化数量
    /// </summary>
    public int OptimizedCount => SuccessCount;

    /// <summary>
    /// 跳过数量
    /// </summary>
    public int SkippedCount { get; set; }
}



/// <summary>
/// 导入结果
/// </summary>
public class ImportResultDto
{
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<Guid> ImportedIds { get; set; } = new();
}



/// <summary>
/// 计划内容
/// </summary>
public class ScheduledContentDto
{
    public Guid Id { get; set; }
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}