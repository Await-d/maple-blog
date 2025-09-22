namespace MapleBlog.Application.DTOs.Search;

/// <summary>
/// 搜索请求模型
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// 搜索查询关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 搜索类型
    /// </summary>
    public string? SearchType { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// 分类筛选
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 标签筛选
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 搜索过滤器
    /// </summary>
    public SearchFilters? Filters { get; set; }

    /// <summary>
    /// 排序选项
    /// </summary>
    public SearchSort? Sort { get; set; }
}

/// <summary>
/// 搜索响应模型
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// 搜索ID
    /// </summary>
    public Guid SearchId { get; set; }

    /// <summary>
    /// 搜索结果
    /// </summary>
    public IEnumerable<SearchResultItem> Results { get; set; } = new List<SearchResultItem>();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 查询关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 搜索耗时（毫秒）
    /// </summary>
    public int ExecutionTimeMs { get; set; }

    /// <summary>
    /// 搜索建议
    /// </summary>
    public IEnumerable<string> Suggestions { get; set; } = new List<string>();

    /// <summary>
    /// 是否有更多结果
    /// </summary>
    public bool HasMore { get; set; }

    /// <summary>
    /// 请求是否成功
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public int ExecutionTime { get; set; }
}

/// <summary>
/// 搜索结果项
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// 结果ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Entity ID (alias for compatibility)
    /// </summary>
    public Guid EntityId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// 结果类型
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Entity type (alias for compatibility)
    /// </summary>
    public string EntityType
    {
        get => Type;
        set => Type = value;
    }

    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容摘要
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Content (alias for compatibility)
    /// </summary>
    public string Content
    {
        get => Summary;
        set => Summary = value;
    }

    /// <summary>
    /// URL路径
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// URL slug
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 相关性评分
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 高亮片段
    /// </summary>
    public IEnumerable<string> Highlights { get; set; } = new List<string>();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Publication date
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// 缩略图
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// View count
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Like count
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// Comment count
    /// </summary>
    public int CommentCount { get; set; }
}

/// <summary>
/// 热门搜索项
/// </summary>
public class PopularSearchItem
{
    /// <summary>
    /// 查询关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 搜索次数
    /// </summary>
    public int SearchCount { get; set; }

    /// <summary>
    /// 最后搜索时间
    /// </summary>
    public DateTime LastSearched { get; set; }

    /// <summary>
    /// 趋势方向（1: 上升, 0: 持平, -1: 下降）
    /// </summary>
    public int Trend { get; set; }

    /// <summary>
    /// 相对于上一周期的变化百分比
    /// </summary>
    public double ChangePercentage { get; set; }

    /// <summary>
    /// 是否推广
    /// </summary>
    public bool IsPromoted { get; set; }
}

/// <summary>
/// 搜索历史项
/// </summary>
public class SearchHistoryItem
{
    /// <summary>
    /// 历史记录ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 查询关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 搜索时间
    /// </summary>
    public DateTime SearchedAt { get; set; }

    /// <summary>
    /// 搜索类型
    /// </summary>
    public string SearchType { get; set; } = string.Empty;

    /// <summary>
    /// 结果数量
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// 搜索耗时（毫秒）
    /// </summary>
    public int? ExecutionTime { get; set; }

    /// <summary>
    /// 是否找到结果
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// 点击的结果数量
    /// </summary>
    public int ClickedResults { get; set; }
}

/// <summary>
/// 搜索过滤器
/// </summary>
public class SearchFilters
{
    /// <summary>
    /// 内容类型过滤
    /// </summary>
    public IEnumerable<string> ContentTypes { get; set; } = new List<string>();

    /// <summary>
    /// 分类过滤
    /// </summary>
    public IEnumerable<string> Categories { get; set; } = new List<string>();

    /// <summary>
    /// 标签过滤
    /// </summary>
    public IEnumerable<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// 日期范围过滤
    /// </summary>
    public DateRange? DateRange { get; set; }

    /// <summary>
    /// 作者过滤
    /// </summary>
    public IEnumerable<string> Authors { get; set; } = new List<string>();
}

/// <summary>
/// 搜索排序选项
/// </summary>
public class SearchSort
{
    /// <summary>
    /// 排序字段
    /// </summary>
    public string Field { get; set; } = "Relevance";

    /// <summary>
    /// 排序方向
    /// </summary>
    public string Direction { get; set; } = "Desc";

    /// <summary>
    /// 二级排序字段
    /// </summary>
    public string? SecondaryField { get; set; }

    /// <summary>
    /// 二级排序方向
    /// </summary>
    public string? SecondaryDirection { get; set; }
}

/// <summary>
/// 日期范围
/// </summary>
public class DateRange
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? To { get; set; }
}