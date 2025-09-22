using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 热门搜索实体
/// </summary>
public class PopularSearch
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 查询关键词
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 标准化查询（用于匹配）
    /// </summary>
    [Required]
    [StringLength(500)]
    public string NormalizedQuery { get; set; } = string.Empty;

    /// <summary>
    /// 搜索次数
    /// </summary>
    public int SearchCount { get; set; } = 1;

    /// <summary>
    /// 最后搜索时间
    /// </summary>
    public DateTime LastSearched { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后搜索时间（别名，向下兼容）
    /// </summary>
    public DateTime LastSearchedAt
    {
        get => LastSearched;
        set => LastSearched = value;
    }

    /// <summary>
    /// 是否推广
    /// </summary>
    public bool IsPromoted { get; set; } = false;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// 首次搜索时间
    /// </summary>
    public DateTime FirstSearchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 业务方法

    /// <summary>
    /// 增加搜索次数
    /// </summary>
    /// <param name="count">增加数量</param>
    public void IncreaseSearchCount(int count = 1)
    {
        SearchCount += count;
        LastSearched = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 设置为推广
    /// </summary>
    /// <param name="displayOrder">显示顺序</param>
    public void Promote(int displayOrder = 0)
    {
        IsPromoted = true;
        DisplayOrder = displayOrder;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消推广
    /// </summary>
    public void Unpromote()
    {
        IsPromoted = false;
        DisplayOrder = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 创建热门搜索
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="initialCount">初始搜索次数</param>
    /// <returns>热门搜索</returns>
    public static PopularSearch Create(string query, int initialCount = 1)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        return new PopularSearch
        {
            Query = trimmedQuery,
            NormalizedQuery = trimmedQuery.ToLowerInvariant(),
            SearchCount = initialCount
        };
    }
}