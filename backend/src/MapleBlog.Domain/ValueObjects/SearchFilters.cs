using MapleBlog.Domain.Constants;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.ValueObjects;

/// <summary>
/// 搜索过滤器值对象
/// </summary>
public record SearchFilters
{
    /// <summary>
    /// 内容类型过滤
    /// </summary>
    public List<ContentType>? ContentTypes { get; init; }

    /// <summary>
    /// 分类ID过滤
    /// </summary>
    public List<Guid>? CategoryIds { get; init; }

    /// <summary>
    /// 标签ID过滤
    /// </summary>
    public List<Guid>? TagIds { get; init; }

    /// <summary>
    /// 作者ID过滤
    /// </summary>
    public List<Guid>? AuthorIds { get; init; }

    /// <summary>
    /// 日期范围过滤
    /// </summary>
    public DateRange? DateRange { get; init; }

    /// <summary>
    /// 最小分数过滤
    /// </summary>
    public float? MinScore { get; init; }

    /// <summary>
    /// 最大分数过滤
    /// </summary>
    public float? MaxScore { get; init; }

    /// <summary>
    /// 语言过滤
    /// </summary>
    public List<string>? Languages { get; init; }

    /// <summary>
    /// 状态过滤
    /// </summary>
    public List<string>? Statuses { get; init; }

    /// <summary>
    /// 自定义字段过滤
    /// </summary>
    public Dictionary<string, object>? CustomFields { get; init; }

    /// <summary>
    /// 创建空过滤器
    /// </summary>
    public static SearchFilters Empty => new();

    /// <summary>
    /// 是否有过滤条件
    /// </summary>
    public bool HasFilters =>
        ContentTypes?.Any() == true ||
        CategoryIds?.Any() == true ||
        TagIds?.Any() == true ||
        AuthorIds?.Any() == true ||
        DateRange != null ||
        MinScore.HasValue ||
        MaxScore.HasValue ||
        Languages?.Any() == true ||
        Statuses?.Any() == true ||
        CustomFields?.Any() == true;

    /// <summary>
    /// 应用内容类型过滤
    /// </summary>
    public SearchFilters WithContentTypes(params ContentType[] contentTypes)
    {
        return this with { ContentTypes = contentTypes.ToList() };
    }

    /// <summary>
    /// 应用分类过滤
    /// </summary>
    public SearchFilters WithCategories(params Guid[] categoryIds)
    {
        return this with { CategoryIds = categoryIds.ToList() };
    }

    /// <summary>
    /// 应用标签过滤
    /// </summary>
    public SearchFilters WithTags(params Guid[] tagIds)
    {
        return this with { TagIds = tagIds.ToList() };
    }

    /// <summary>
    /// 应用作者过滤
    /// </summary>
    public SearchFilters WithAuthors(params Guid[] authorIds)
    {
        return this with { AuthorIds = authorIds.ToList() };
    }

    /// <summary>
    /// 应用日期范围过滤
    /// </summary>
    public SearchFilters WithDateRange(DateTime? startDate, DateTime? endDate)
    {
        var dateRange = DateRange.Create(startDate, endDate);
        return this with { DateRange = dateRange };
    }

    /// <summary>
    /// 应用分数范围过滤
    /// </summary>
    public SearchFilters WithScoreRange(float? minScore, float? maxScore = null)
    {
        return this with { MinScore = minScore, MaxScore = maxScore };
    }

    /// <summary>
    /// 应用语言过滤
    /// </summary>
    public SearchFilters WithLanguages(params string[] languages)
    {
        return this with { Languages = languages.ToList() };
    }

    /// <summary>
    /// 应用自定义字段过滤
    /// </summary>
    public SearchFilters WithCustomField(string field, object value)
    {
        var customFields = CustomFields?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();
        customFields[field] = value;
        return this with { CustomFields = customFields };
    }

    /// <summary>
    /// 合并过滤器
    /// </summary>
    public SearchFilters Merge(SearchFilters other)
    {
        return new SearchFilters
        {
            ContentTypes = MergeLists(ContentTypes, other.ContentTypes),
            CategoryIds = MergeLists(CategoryIds, other.CategoryIds),
            TagIds = MergeLists(TagIds, other.TagIds),
            AuthorIds = MergeLists(AuthorIds, other.AuthorIds),
            DateRange = other.DateRange ?? DateRange,
            MinScore = other.MinScore ?? MinScore,
            MaxScore = other.MaxScore ?? MaxScore,
            Languages = MergeLists(Languages, other.Languages),
            Statuses = MergeLists(Statuses, other.Statuses),
            CustomFields = MergeDictionaries(CustomFields, other.CustomFields)
        };
    }

    private static List<T>? MergeLists<T>(List<T>? list1, List<T>? list2)
    {
        if (list1 == null && list2 == null) return null;
        if (list1 == null) return list2;
        if (list2 == null) return list1;
        return list1.Union(list2).ToList();
    }

    private static Dictionary<string, object>? MergeDictionaries(Dictionary<string, object>? dict1, Dictionary<string, object>? dict2)
    {
        if (dict1 == null && dict2 == null) return null;
        if (dict1 == null) return dict2;
        if (dict2 == null) return dict1;

        var merged = new Dictionary<string, object>(dict1);
        foreach (var kvp in dict2)
        {
            merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }
}

/// <summary>
/// 日期范围值对象
/// </summary>
public record DateRange
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 创建日期范围
    /// </summary>
    public static DateRange? Create(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue && !endDate.HasValue)
            return null;

        // 确保开始日期不大于结束日期
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        return new DateRange
        {
            StartDate = startDate,
            EndDate = endDate
        };
    }

    /// <summary>
    /// 今天
    /// </summary>
    public static DateRange Today => new()
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(1).AddTicks(-1)
    };

    /// <summary>
    /// 本周
    /// </summary>
    public static DateRange ThisWeek
    {
        get
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);
            return new DateRange { StartDate = startOfWeek, EndDate = endOfWeek };
        }
    }

    /// <summary>
    /// 本月
    /// </summary>
    public static DateRange ThisMonth
    {
        get
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
            return new DateRange { StartDate = startOfMonth, EndDate = endOfMonth };
        }
    }

    /// <summary>
    /// 本年
    /// </summary>
    public static DateRange ThisYear
    {
        get
        {
            var today = DateTime.Today;
            var startOfYear = new DateTime(today.Year, 1, 1);
            var endOfYear = startOfYear.AddYears(1).AddTicks(-1);
            return new DateRange { StartDate = startOfYear, EndDate = endOfYear };
        }
    }

    /// <summary>
    /// 最近N天
    /// </summary>
    public static DateRange LastDays(int days)
    {
        var today = DateTime.Today;
        var startDate = today.AddDays(-days);
        return new DateRange { StartDate = startDate, EndDate = today.AddDays(1).AddTicks(-1) };
    }

    /// <summary>
    /// 是否在范围内
    /// </summary>
    public bool IsInRange(DateTime date)
    {
        return (!StartDate.HasValue || date >= StartDate) &&
               (!EndDate.HasValue || date <= EndDate);
    }

    /// <summary>
    /// 获取时间跨度
    /// </summary>
    public TimeSpan? Duration
    {
        get
        {
            if (StartDate.HasValue && EndDate.HasValue)
                return EndDate.Value - StartDate.Value;
            return null;
        }
    }
}

/// <summary>
/// 搜索排序值对象
/// </summary>
public record SearchSort
{
    /// <summary>
    /// 排序字段
    /// </summary>
    public string Field { get; init; } = SearchConstants.SortFields.Relevance;

    /// <summary>
    /// 排序方向
    /// </summary>
    public string Direction { get; init; } = SearchConstants.SortDirections.Descending;

    /// <summary>
    /// 缺失值处理
    /// </summary>
    public string? Missing { get; init; }

    /// <summary>
    /// 排序模式
    /// </summary>
    public string? Mode { get; init; }

    /// <summary>
    /// 创建排序
    /// </summary>
    public static SearchSort Create(string field, string direction = SearchConstants.SortDirections.Descending)
    {
        return new SearchSort
        {
            Field = field?.ToLowerInvariant() ?? SearchConstants.SortFields.Relevance,
            Direction = direction?.ToLowerInvariant() == SearchConstants.SortDirections.Ascending
                ? SearchConstants.SortDirections.Ascending
                : SearchConstants.SortDirections.Descending
        };
    }

    /// <summary>
    /// 按相关性排序
    /// </summary>
    public static SearchSort ByRelevance => Create(SearchConstants.SortFields.Relevance, SearchConstants.SortDirections.Descending);

    /// <summary>
    /// 按日期排序
    /// </summary>
    public static SearchSort ByDate(bool ascending = false) =>
        Create(SearchConstants.SortFields.Date, ascending ? SearchConstants.SortDirections.Ascending : SearchConstants.SortDirections.Descending);

    /// <summary>
    /// 按标题排序
    /// </summary>
    public static SearchSort ByTitle(bool ascending = true) =>
        Create(SearchConstants.SortFields.Title, ascending ? SearchConstants.SortDirections.Ascending : SearchConstants.SortDirections.Descending);

    /// <summary>
    /// 按热度排序
    /// </summary>
    public static SearchSort ByPopularity => Create(SearchConstants.SortFields.Popularity, SearchConstants.SortDirections.Descending);

    /// <summary>
    /// 是否为相关性排序
    /// </summary>
    public bool IsRelevanceSort => Field.Equals(SearchConstants.SortFields.Relevance, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 是否为升序
    /// </summary>
    public bool IsAscending => Direction.Equals(SearchConstants.SortDirections.Ascending, StringComparison.OrdinalIgnoreCase);
}