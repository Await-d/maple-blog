using MapleBlog.Domain.Constants;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MapleBlog.Application.Services;

/// <summary>
/// 搜索应用服务
/// 提供统一的搜索业务逻辑，包括个性化搜索、搜索历史管理和推荐
/// </summary>
public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly ILogger<AdvancedSearchService> _logger;
    private readonly ISearchEngine _searchEngine;
    private readonly Application.Interfaces.ISearchIndexManager _searchIndexManager;
    private readonly DbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    // 搜索配置
    private readonly bool _enablePersonalization;
    private readonly bool _enableRecommendations;
    private readonly int _maxSearchHistoryCount;
    private readonly int _suggestionCacheMinutes;

    public AdvancedSearchService(
        ILogger<AdvancedSearchService> logger,
        ISearchEngine searchEngine,
        Application.Interfaces.ISearchIndexManager searchIndexManager,
        DbContext context,
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _searchEngine = searchEngine;
        _searchIndexManager = searchIndexManager;
        _context = context;
        _cache = cache;
        _configuration = configuration;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // 读取配置
        _enablePersonalization = _configuration.GetValue<bool>("Search:EnablePersonalization", true);
        _enableRecommendations = _configuration.GetValue<bool>("Search:EnableRecommendations", true);
        _maxSearchHistoryCount = _configuration.GetValue<int>("Search:MaxSearchHistoryCount", 100);
        _suggestionCacheMinutes = _configuration.GetValue<int>("Search:SuggestionCacheMinutes", 10);
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 验证搜索请求
            var validation = ValidateSearchRequest(request);
            if (!validation.IsValid)
            {
                return new SearchResponse
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage,
                    ExecutionTime = (int)stopwatch.ElapsedMilliseconds
                };
            }

            // 构建搜索条件
            var criteria = await BuildSearchCriteria(request, cancellationToken);

            // 应用个性化设置
            if (_enablePersonalization && request.UserId.HasValue)
            {
                criteria = await ApplyPersonalization(criteria, request.UserId.Value, cancellationToken);
            }

            // 执行搜索
            var searchResult = await _searchEngine.SearchAsync(criteria, cancellationToken);

            // 记录搜索历史
            _ = Task.Run(async () => await RecordSearchHistoryAsync(request, searchResult, cancellationToken), cancellationToken);

            // 增强搜索结果
            var enhancedResults = await EnhanceSearchResults(searchResult, request, cancellationToken);

            // 获取搜索建议
            var suggestions = await GetSearchSuggestionsAsync(request.Query, cancellationToken);

            // 获取推荐内容
            var recommendations = _enableRecommendations && request.UserId.HasValue
                ? (await GetRecommendationsAsync(request.UserId.Value, request.Query, cancellationToken)).ToList()
                : new List<SearchResultItem>();

            stopwatch.Stop();

            return new SearchResponse
            {
                Success = true,
                Results = enhancedResults.Items,
                TotalCount = enhancedResults.TotalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)enhancedResults.TotalCount / request.PageSize),
                HasMore = enhancedResults.HasMore,
                Suggestions = suggestions,
                Recommendations = recommendations,
                Facets = await GetSearchFacetsAsync(request.Query, cancellationToken),
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                SearchId = Guid.NewGuid()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search for query: {Query}", request.Query);
            return new SearchResponse
            {
                Success = false,
                ErrorMessage = "搜索服务暂时不可用，请稍后再试"
            };
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string query, int count = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Enumerable.Empty<string>();
        }

        var cacheKey = $"{SearchConstants.Cache.SuggestionPrefix}{query.ToLowerInvariant()}:{count}";

        try
        {
            // 尝试从缓存获取
            var cachedSuggestions = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedSuggestions))
            {
                var suggestions = JsonSerializer.Deserialize<List<string>>(cachedSuggestions, _jsonOptions);
                if (suggestions != null)
                {
                    return suggestions;
                }
            }

            // 从搜索引擎获取建议
            var searchSuggestions = await _searchEngine.GetSuggestionsAsync(query, count, cancellationToken);
            var suggestionList = searchSuggestions.ToList();

            // 补充热门搜索建议
            var popularSuggestions = await GetPopularSearchSuggestions(query, count - suggestionList.Count, cancellationToken);
            suggestionList.AddRange(popularSuggestions);

            // 缓存结果
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_suggestionCacheMinutes)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(suggestionList, _jsonOptions), cacheOptions, cancellationToken);

            return suggestionList.Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 获取用户搜索历史
    /// </summary>
    public async Task<IEnumerable<SearchHistoryItem>> GetSearchHistoryAsync(Guid userId, int count = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchHistory = await _context.Set<SearchQuery>()
                .Where(q => q.UserId == userId)
                .OrderByDescending(q => q.CreatedAt)
                .Take(count)
                .Select(q => new SearchHistoryItem
                {
                    Query = q.Query,
                    ResultCount = q.ResultCount,
                    SearchedAt = q.CreatedAt,
                    SearchType = q.SearchType
                })
                .ToListAsync(cancellationToken);

            return searchHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user: {UserId}", userId);
            return Enumerable.Empty<SearchHistoryItem>();
        }
    }

    /// <summary>
    /// 清除用户搜索历史
    /// </summary>
    public async Task<bool> ClearSearchHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchQueries = await _context.Set<SearchQuery>()
                .Where(q => q.UserId == userId)
                .ToListAsync(cancellationToken);

            if (searchQueries.Any())
            {
                _context.Set<SearchQuery>().RemoveRange(searchQueries);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取热门搜索
    /// </summary>
    public async Task<IEnumerable<PopularSearchItem>> GetPopularSearchesAsync(int count = 10, TimeSpan? timeRange = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var since = timeRange.HasValue ? DateTime.UtcNow.Subtract(timeRange.Value) : DateTime.UtcNow.AddDays(-7);

            var popularSearches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= since)
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new PopularSearchItem
                {
                    Query = g.First().Query,
                    SearchCount = g.Count(),
                    LastSearched = g.Max(q => q.CreatedAt),
                    AverageResultCount = (int)g.Average(q => q.ResultCount)
                })
                .OrderByDescending(p => p.SearchCount)
                .Take(count)
                .ToListAsync(cancellationToken);

            return popularSearches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular searches");
            return Enumerable.Empty<PopularSearchItem>();
        }
    }

    /// <summary>
    /// 获取个性化推荐
    /// </summary>
    public async Task<IEnumerable<SearchResultItem>> GetRecommendationsAsync(Guid userId, string? baseQuery = null, CancellationToken cancellationToken = default)
    {
        if (!_enableRecommendations)
        {
            return Enumerable.Empty<SearchResultItem>();
        }

        try
        {
            // 获取用户搜索偏好
            var userPreferences = await GetUserSearchPreferences(userId, cancellationToken);

            // 基于偏好生成推荐搜索
            var recommendationCriteria = BuildRecommendationCriteria(userPreferences, baseQuery);

            var searchResult = await _searchEngine.SearchAsync(recommendationCriteria, cancellationToken);

            return searchResult.Items.Take(5); // 限制推荐数量
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user: {UserId}", userId);
            return Enumerable.Empty<SearchResultItem>();
        }
    }

    /// <summary>
    /// 记录搜索点击
    /// </summary>
    public async Task RecordSearchClickAsync(Guid searchId, Guid resultId, string resultType, int position, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里可以记录点击行为用于改进搜索算法
            _logger.LogDebug("Search click recorded - SearchId: {SearchId}, ResultId: {ResultId}, Position: {Position}",
                searchId, resultId, position);

            // 更新搜索查询记录
            if (userId.HasValue)
            {
                var searchQuery = await _context.Set<SearchQuery>()
                    .Where(q => q.UserId == userId)
                    .OrderByDescending(q => q.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                searchQuery?.RecordClick(resultId, resultType, position);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search click");
        }
    }

    #region 私有方法

    /// <summary>
    /// 验证搜索请求
    /// </summary>
    private SearchValidationResult ValidateSearchRequest(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new SearchValidationResult { IsValid = false, ErrorMessage = "搜索关键词不能为空" };
        }

        if (request.Query.Length > SearchConstants.Limits.MaxQueryLength)
        {
            return new SearchValidationResult { IsValid = false, ErrorMessage = $"搜索关键词不能超过{SearchConstants.Limits.MaxQueryLength}个字符" };
        }

        if (request.PageSize > SearchConstants.Index.MaxPageSize)
        {
            return new SearchValidationResult { IsValid = false, ErrorMessage = $"每页大小不能超过{SearchConstants.Index.MaxPageSize}" };
        }

        return new SearchValidationResult { IsValid = true };
    }

    /// <summary>
    /// 构建搜索条件
    /// </summary>
    private async Task<SearchCriteria> BuildSearchCriteria(SearchRequest request, CancellationToken cancellationToken)
    {
        var criteria = SearchCriteria.Create(request.Query, request.Page, request.PageSize);

        // 应用过滤器
        if (request.Filters != null)
        {
            if (request.Filters.ContentTypes?.Any() == true)
            {
                criteria.ContentType = request.Filters.ContentTypes.First().ToString().ToLowerInvariant();
            }

            if (request.Filters.CategoryIds?.Any() == true)
            {
                criteria.CategoryId = request.Filters.CategoryIds.First();
            }

            if (request.Filters.TagIds?.Any() == true)
            {
                criteria.TagIds = request.Filters.TagIds;
            }

            if (request.Filters.AuthorIds?.Any() == true)
            {
                criteria.AuthorId = request.Filters.AuthorIds.First();
            }

            if (request.Filters.DateRange != null)
            {
                criteria.SetDateRange(request.Filters.DateRange.StartDate, request.Filters.DateRange.EndDate);
            }

            if (request.Filters.MinScore.HasValue)
            {
                criteria.MinScore = request.Filters.MinScore.Value;
            }
        }

        // 应用排序
        if (request.Sort != null)
        {
            criteria.SetSorting(request.Sort.Field, request.Sort.Direction);
        }

        // 设置搜索类型
        criteria.SearchType = request.SearchType ?? SearchConstants.Defaults.SearchType;

        // 应用字段权重
        if (criteria.FieldWeights == null)
        {
            criteria.ApplyDefaultWeights();
        }

        return criteria;
    }

    /// <summary>
    /// 应用个性化设置
    /// </summary>
    private async Task<SearchCriteria> ApplyPersonalization(SearchCriteria criteria, Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var userPreferences = await GetUserSearchPreferences(userId, cancellationToken);

            // 基于用户偏好调整字段权重
            if (userPreferences.PreferredCategories?.Any() == true)
            {
                criteria.SetFieldWeight(SearchConstants.Fields.Category, 1.5f);
            }

            // 可以根据用户历史搜索行为进一步个性化
            return criteria;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying personalization for user: {UserId}", userId);
            return criteria;
        }
    }

    /// <summary>
    /// 增强搜索结果
    /// </summary>
    private async Task<SearchResult> EnhanceSearchResults(SearchResult searchResult, SearchRequest request, CancellationToken cancellationToken)
    {
        // 这里可以添加结果增强逻辑，比如：
        // 1. 添加相关推荐
        // 2. 过滤用户无权访问的内容
        // 3. 添加额外的元数据

        foreach (var item in searchResult.Items)
        {
            // 增强结果项
            await EnhanceResultItem(item, request, cancellationToken);
        }

        return searchResult;
    }

    /// <summary>
    /// 增强单个结果项
    /// </summary>
    private async Task EnhanceResultItem(SearchResultItem item, SearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 根据实体类型添加额外信息
            switch (item.EntityType.ToLowerInvariant())
            {
                case SearchConstants.EntityTypes.Post:
                    await EnhancePostResult(item, cancellationToken);
                    break;
                case SearchConstants.EntityTypes.User:
                    await EnhanceUserResult(item, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing result item: {EntityId}", item.EntityId);
        }
    }

    /// <summary>
    /// 增强文章搜索结果
    /// </summary>
    private async Task EnhancePostResult(SearchResultItem item, CancellationToken cancellationToken)
    {
        var post = await _context.Set<Post>()
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == item.EntityId, cancellationToken);

        if (post != null)
        {
            item.ExtraData["authorName"] = post.Author?.UserName ?? "匿名";
            item.ExtraData["categoryName"] = post.Category?.Name ?? "未分类";
            item.ExtraData["tags"] = post.Tags?.Select(t => t.Name).ToList() ?? new List<string>();
            item.ExtraData["publishedAt"] = post.PublishedAt;
            item.ExtraData["viewCount"] = post.ViewCount;
            item.ExtraData["commentCount"] = post.CommentCount;
        }
    }

    /// <summary>
    /// 增强用户搜索结果
    /// </summary>
    private async Task EnhanceUserResult(SearchResultItem item, CancellationToken cancellationToken)
    {
        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == item.EntityId, cancellationToken);

        if (user != null)
        {
            item.ExtraData["displayName"] = user.DisplayName ?? user.UserName;
            item.ExtraData["bio"] = user.Bio;
            item.ExtraData["postCount"] = await _context.Set<Post>().CountAsync(p => p.AuthorId == user.Id && p.IsPublished, cancellationToken);
        }
    }

    /// <summary>
    /// 记录搜索历史
    /// </summary>
    private async Task RecordSearchHistoryAsync(SearchRequest request, SearchResult searchResult, CancellationToken cancellationToken)
    {
        try
        {
            var searchQuery = SearchQuery.Create(request.Query, request.UserId, request.SearchType ?? "general");
            searchQuery.SetResults((int)searchResult.TotalCount, searchResult.ExecutionTime);

            // 设置请求信息
            if (!string.IsNullOrEmpty(request.IpAddress))
            {
                searchQuery.IpAddress = request.IpAddress;
            }

            if (!string.IsNullOrEmpty(request.UserAgent))
            {
                searchQuery.UserAgent = request.UserAgent;
            }

            if (request.Filters?.HasFilters == true)
            {
                var filtersDict = new Dictionary<string, object>();
                if (request.Filters.ContentTypes?.Any() == true)
                    filtersDict["contentTypes"] = request.Filters.ContentTypes.Select(ct => ct.ToString()).ToList();
                if (request.Filters.CategoryIds?.Any() == true)
                    filtersDict["categoryIds"] = request.Filters.CategoryIds;
                if (request.Filters.TagIds?.Any() == true)
                    filtersDict["tagIds"] = request.Filters.TagIds;
                if (request.Filters.DateRange != null)
                    filtersDict["dateRange"] = request.Filters.DateRange;

                searchQuery.Filters = filtersDict;
            }

            _context.Set<SearchQuery>().Add(searchQuery);

            // 维护搜索历史数量限制
            if (request.UserId.HasValue)
            {
                await MaintainSearchHistoryLimit(request.UserId.Value, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 更新热门搜索统计
            _ = Task.Run(async () => await UpdatePopularSearchAsync(request.Query, cancellationToken), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search history");
        }
    }

    /// <summary>
    /// 维护搜索历史数量限制
    /// </summary>
    private async Task MaintainSearchHistoryLimit(Guid userId, CancellationToken cancellationToken)
    {
        var historyCount = await _context.Set<SearchQuery>()
            .CountAsync(q => q.UserId == userId, cancellationToken);

        if (historyCount >= _maxSearchHistoryCount)
        {
            var excessCount = historyCount - _maxSearchHistoryCount + 1;
            var oldestQueries = await _context.Set<SearchQuery>()
                .Where(q => q.UserId == userId)
                .OrderBy(q => q.CreatedAt)
                .Take(excessCount)
                .ToListAsync(cancellationToken);

            _context.Set<SearchQuery>().RemoveRange(oldestQueries);
        }
    }

    /// <summary>
    /// 更新热门搜索统计
    /// </summary>
    private async Task UpdatePopularSearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var normalizedQuery = query.ToLowerInvariant().Trim();

            var popularSearch = await _context.Set<PopularSearch>()
                .FirstOrDefaultAsync(p => p.NormalizedQuery == normalizedQuery, cancellationToken);

            if (popularSearch != null)
            {
                popularSearch.SearchCount++;
                popularSearch.LastSearched = DateTime.UtcNow;
            }
            else
            {
                popularSearch = new PopularSearch
                {
                    Query = query,
                    NormalizedQuery = normalizedQuery,
                    SearchCount = 1,
                    LastSearched = DateTime.UtcNow
                };
                _context.Set<PopularSearch>().Add(popularSearch);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating popular search statistics");
        }
    }

    /// <summary>
    /// 获取用户搜索偏好
    /// </summary>
    private async Task<UserSearchPreferences> GetUserSearchPreferences(Guid userId, CancellationToken cancellationToken)
    {
        // 分析用户历史搜索行为来构建偏好
        var recentSearches = await _context.Set<SearchQuery>()
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var preferences = new UserSearchPreferences
        {
            UserId = userId,
            PreferredCategories = new List<Guid>(),
            PreferredTags = new List<Guid>(),
            PreferredSearchTypes = new List<string>()
        };

        // 分析搜索模式
        if (recentSearches.Any())
        {
            // 统计偏好的搜索类型
            var searchTypeStats = recentSearches
                .GroupBy(s => s.SearchType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            preferences.PreferredSearchTypes = searchTypeStats;
        }

        return preferences;
    }

    /// <summary>
    /// 构建推荐搜索条件
    /// </summary>
    private SearchCriteria BuildRecommendationCriteria(UserSearchPreferences preferences, string? baseQuery)
    {
        var criteria = SearchCriteria.Create(baseQuery ?? "", 1, 10);

        // 基于用户偏好调整搜索条件
        if (preferences.PreferredCategories?.Any() == true)
        {
            criteria.CategoryId = preferences.PreferredCategories.First();
        }

        if (preferences.PreferredTags?.Any() == true)
        {
            criteria.TagIds = preferences.PreferredTags.Take(3).ToList();
        }

        // 设置为热门内容优先
        criteria.SetSorting(SearchConstants.SortFields.Popularity, SearchConstants.SortDirections.Descending);

        return criteria;
    }

    /// <summary>
    /// 获取热门搜索建议
    /// </summary>
    private async Task<List<string>> GetPopularSearchSuggestions(string query, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return new List<string>();
        }

        try
        {
            var suggestions = await _context.Set<PopularSearch>()
                .Where(p => p.Query.Contains(query) && p.SearchCount > 1)
                .OrderByDescending(p => p.SearchCount)
                .Take(count)
                .Select(p => p.Query)
                .ToListAsync(cancellationToken);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular search suggestions");
            return new List<string>();
        }
    }

    /// <summary>
    /// 获取搜索分面
    /// </summary>
    private async Task<Dictionary<string, List<SearchFacetItem>>> GetSearchFacetsAsync(string query, CancellationToken cancellationToken)
    {
        var facets = new Dictionary<string, List<SearchFacetItem>>();

        try
        {
            // 内容类型分面
            facets["contentTypes"] = new List<SearchFacetItem>
            {
                new() { Key = "post", Label = "文章", Count = 0 },
                new() { Key = "comment", Label = "评论", Count = 0 },
                new() { Key = "user", Label = "用户", Count = 0 }
            };

            // 可以添加更多分面，如分类、标签等

            return facets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search facets");
            return facets;
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    private async Task<List<string>> GetSearchSuggestionsAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<string>();

            // 基于历史搜索获取建议
            var suggestions = await _context.Set<SearchHistory>()
                .Where(h => h.Query.Contains(query) && h.ResultCount > 0)
                .GroupBy(h => h.Query)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(5)
                .ToListAsync(cancellationToken);

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return new List<string>();
        }
    }

    #endregion
}

/// <summary>
/// 高级搜索服务接口
/// </summary>
public interface IAdvancedSearchService
{
    /// <summary>
    /// 执行搜索
    /// </summary>
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    Task<IEnumerable<string>> GetSuggestionsAsync(string query, int count = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户搜索历史
    /// </summary>
    Task<IEnumerable<SearchHistoryItem>> GetSearchHistoryAsync(Guid userId, int count = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除用户搜索历史
    /// </summary>
    Task<bool> ClearSearchHistoryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取热门搜索
    /// </summary>
    Task<IEnumerable<PopularSearchItem>> GetPopularSearchesAsync(int count = 10, TimeSpan? timeRange = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取个性化推荐
    /// </summary>
    Task<IEnumerable<SearchResultItem>> GetRecommendationsAsync(Guid userId, string? baseQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录搜索点击
    /// </summary>
    Task RecordSearchClickAsync(Guid searchId, Guid resultId, string resultType, int position, Guid? userId = null, CancellationToken cancellationToken = default);
}

#region DTO类

/// <summary>
/// 搜索请求
/// </summary>
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchType { get; set; }
    public SearchFilters? Filters { get; set; }
    public SearchSort? Sort { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// 搜索响应
/// </summary>
public class SearchResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<SearchResultItem> Results { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasMore { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<SearchResultItem> Recommendations { get; set; } = new();
    public Dictionary<string, List<SearchFacetItem>> Facets { get; set; } = new();
    public int ExecutionTime { get; set; }
    public Guid SearchId { get; set; }
}

/// <summary>
/// 搜索历史项
/// </summary>
public class SearchHistoryItem
{
    public string Query { get; set; } = string.Empty;
    public int ResultCount { get; set; }
    public DateTime SearchedAt { get; set; }
    public string SearchType { get; set; } = string.Empty;
}

/// <summary>
/// 热门搜索项
/// </summary>
public class PopularSearchItem
{
    public string Query { get; set; } = string.Empty;
    public string NormalizedQuery { get; set; } = string.Empty;
    public int SearchCount { get; set; }
    public DateTime LastSearched { get; set; }
    public int AverageResultCount { get; set; }
}

/// <summary>
/// 搜索分面项
/// </summary>
public class SearchFacetItem
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public long Count { get; set; }
}

/// <summary>
/// 用户搜索偏好
/// </summary>
public class UserSearchPreferences
{
    public Guid UserId { get; set; }
    public List<Guid>? PreferredCategories { get; set; }
    public List<Guid>? PreferredTags { get; set; }
    public List<string>? PreferredSearchTypes { get; set; }
}

/// <summary>
/// 搜索验证结果
/// </summary>
public class SearchValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion