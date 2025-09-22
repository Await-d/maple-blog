namespace MapleBlog.Application.DTOs.Search;

public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class SearchResultDto<T>
{
    public IEnumerable<T> Results { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string Query { get; set; } = string.Empty;
}

public class SearchStatsDto
{
    public int TotalSearches { get; set; }
    public int TotalResults { get; set; }
    public DateTime LastSearchDate { get; set; }

    /// <summary>
    /// Number of indexed posts
    /// </summary>
    public int IndexedPostsCount { get; set; }

    /// <summary>
    /// Number of published posts
    /// </summary>
    public int PublishedPostsCount { get; set; }

    /// <summary>
    /// Average results per search
    /// </summary>
    public double AverageResultsPerSearch { get; set; }
}

public class SearchTermStatsDto
{
    public string Term { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime FirstSearchedAt { get; set; }
    public DateTime LastSearchedAt { get; set; }
    public double Percentage { get; set; }
    public double? AverageResultCount { get; set; }
    public double? AverageExecutionTime { get; set; }
}

public class SearchAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSearches { get; set; }
    public int UniqueTerms { get; set; }
    public int UniqueSearchTerms { get; set; }
    public double AverageResultsPerSearch { get; set; }
    public double AverageExecutionTime { get; set; }
    public int SearchesWithResults { get; set; }
    public int SearchesWithoutResults { get; set; }
    public double SuccessRate { get; set; }
    public IEnumerable<SearchTermStatsDto> TopTerms { get; set; } = new List<SearchTermStatsDto>();
    public IEnumerable<SearchTermStatsDto> TopSearchTerms { get; set; } = new List<SearchTermStatsDto>();
}

public class SearchTrendDto
{
    public DateTime Date { get; set; }
    public int SearchCount { get; set; }
    public int UniqueTermsCount { get; set; }
    public double AverageExecutionTime { get; set; }
    public IEnumerable<SearchTrendDto>? TrendData { get; set; }
}

