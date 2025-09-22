using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 搜索查询记录实体
/// </summary>
public class SearchQuery
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 查询关键词
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 标准化查询关键词
    /// </summary>
    [Required]
    [StringLength(500)]
    public string NormalizedQuery { get; set; } = string.Empty;

    /// <summary>
    /// 结果数量
    /// </summary>
    public int ResultCount { get; set; } = 0;

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public int? ExecutionTime { get; set; }

    /// <summary>
    /// 搜索类型
    /// </summary>
    [StringLength(20)]
    public string SearchType { get; set; } = "general";

    /// <summary>
    /// 搜索过滤条件（JSON格式）
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }

    // 用户信息

    /// <summary>
    /// IP地址
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    // 统计信息

    /// <summary>
    /// 点击的搜索结果（JSON格式）
    /// </summary>
    public List<object>? ClickedResults { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 导航属性

    /// <summary>
    /// 用户
    /// </summary>
    public virtual User? User { get; set; }

    // 业务方法

    /// <summary>
    /// 设置查询关键词
    /// </summary>
    /// <param name="query">查询关键词</param>
    public void SetQuery(string query)
    {
        Query = query?.Trim() ?? string.Empty;
        NormalizedQuery = Query.ToLowerInvariant();
    }

    /// <summary>
    /// 设置搜索结果
    /// </summary>
    /// <param name="resultCount">结果数量</param>
    /// <param name="executionTime">执行时间</param>
    public void SetResults(int resultCount, int? executionTime = null)
    {
        ResultCount = resultCount;
        ExecutionTime = executionTime;
    }

    /// <summary>
    /// 记录点击结果
    /// </summary>
    /// <param name="resultId">结果ID</param>
    /// <param name="resultType">结果类型</param>
    /// <param name="position">位置</param>
    public void RecordClick(Guid resultId, string resultType, int position)
    {
        ClickedResults ??= new List<object>();
        ClickedResults.Add(new
        {
            ResultId = resultId,
            ResultType = resultType,
            Position = position,
            ClickedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 创建搜索查询记录
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="userId">用户ID</param>
    /// <param name="searchType">搜索类型</param>
    /// <returns>搜索查询记录</returns>
    public static SearchQuery Create(string query, Guid? userId = null, string searchType = "general")
    {
        var searchQuery = new SearchQuery
        {
            UserId = userId,
            SearchType = searchType
        };
        searchQuery.SetQuery(query);
        return searchQuery;
    }
}