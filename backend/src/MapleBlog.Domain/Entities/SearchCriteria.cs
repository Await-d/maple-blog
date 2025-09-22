using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 搜索条件实体
/// </summary>
public class SearchCriteria
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 内容类型过滤
    /// </summary>
    [StringLength(50)]
    public string? ContentType { get; set; }

    /// <summary>
    /// 分类ID过滤
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// 标签ID过滤
    /// </summary>
    public List<Guid>? TagIds { get; set; }

    /// <summary>
    /// 作者ID过滤
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// 开始日期过滤
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期过滤
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    [StringLength(50)]
    public string SortBy { get; set; } = "relevance";

    /// <summary>
    /// 排序方向
    /// </summary>
    [StringLength(10)]
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 搜索类型
    /// </summary>
    [StringLength(20)]
    public string SearchType { get; set; } = "fulltext";

    /// <summary>
    /// 最小得分阈值
    /// </summary>
    public float? MinScore { get; set; }

    /// <summary>
    /// 高亮配置
    /// </summary>
    public bool EnableHighlight { get; set; } = true;

    /// <summary>
    /// 同义词扩展
    /// </summary>
    public bool ExpandSynonyms { get; set; } = false;

    /// <summary>
    /// 模糊匹配容错度
    /// </summary>
    public int? Fuzziness { get; set; }

    /// <summary>
    /// 权重配置
    /// </summary>
    public Dictionary<string, float>? FieldWeights { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // 业务方法

    /// <summary>
    /// 设置分页参数
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    public void SetPagination(int page, int pageSize)
    {
        Page = Math.Max(1, page);
        PageSize = Math.Clamp(pageSize, 1, 100);
    }

    /// <summary>
    /// 设置排序
    /// </summary>
    /// <param name="sortBy">排序字段</param>
    /// <param name="sortDirection">排序方向</param>
    public void SetSorting(string sortBy, string sortDirection = "desc")
    {
        SortBy = sortBy?.ToLowerInvariant() ?? "relevance";
        SortDirection = sortDirection?.ToLowerInvariant() == "asc" ? "asc" : "desc";
    }

    /// <summary>
    /// 设置日期范围
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    public void SetDateRange(DateTime? startDate, DateTime? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;

        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            (StartDate, EndDate) = (EndDate, StartDate);
        }
    }

    /// <summary>
    /// 添加标签过滤
    /// </summary>
    /// <param name="tagId">标签ID</param>
    public void AddTagFilter(Guid tagId)
    {
        TagIds ??= new List<Guid>();
        if (!TagIds.Contains(tagId))
        {
            TagIds.Add(tagId);
        }
    }

    /// <summary>
    /// 移除标签过滤
    /// </summary>
    /// <param name="tagId">标签ID</param>
    public void RemoveTagFilter(Guid tagId)
    {
        TagIds?.Remove(tagId);
    }

    /// <summary>
    /// 设置字段权重
    /// </summary>
    /// <param name="field">字段名</param>
    /// <param name="weight">权重值</param>
    public void SetFieldWeight(string field, float weight)
    {
        FieldWeights ??= new Dictionary<string, float>();
        FieldWeights[field] = Math.Max(0.1f, weight);
    }

    /// <summary>
    /// 获取跳过记录数
    /// </summary>
    /// <returns>跳过的记录数</returns>
    public int GetSkip() => (Page - 1) * PageSize;

    /// <summary>
    /// 是否有过滤条件
    /// </summary>
    /// <returns>是否有过滤条件</returns>
    public bool HasFilters()
    {
        return !string.IsNullOrWhiteSpace(ContentType)
            || CategoryId.HasValue
            || (TagIds?.Any() ?? false)
            || AuthorId.HasValue
            || StartDate.HasValue
            || EndDate.HasValue;
    }

    /// <summary>
    /// 创建搜索条件
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <returns>搜索条件</returns>
    public static SearchCriteria Create(string query, int page = 1, int pageSize = 10)
    {
        var criteria = new SearchCriteria
        {
            Query = query?.Trim() ?? string.Empty
        };
        criteria.SetPagination(page, pageSize);
        return criteria;
    }

    /// <summary>
    /// 创建默认权重配置
    /// </summary>
    /// <returns>默认字段权重</returns>
    public static Dictionary<string, float> GetDefaultFieldWeights()
    {
        return new Dictionary<string, float>
        {
            ["title"] = 3.0f,
            ["content"] = 1.0f,
            ["keywords"] = 2.0f,
            ["tags"] = 1.5f,
            ["category"] = 1.2f
        };
    }

    /// <summary>
    /// 应用默认权重
    /// </summary>
    public void ApplyDefaultWeights()
    {
        FieldWeights = GetDefaultFieldWeights();
    }
}