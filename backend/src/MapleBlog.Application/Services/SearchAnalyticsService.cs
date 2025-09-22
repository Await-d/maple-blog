using MapleBlog.Application.DTOs.Search;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services;

/// <summary>
/// 搜索分析服务实现
/// 提供搜索行为追踪、数据分析和优化建议
/// </summary>
public class SearchAnalyticsService : ISearchAnalyticsService
{
    private readonly ILogger<SearchAnalyticsService> _logger;
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    // 缓存键前缀
    private const string CACHE_PREFIX_POPULAR_TERMS = "search:popular_terms";
    private const string CACHE_PREFIX_SEARCH_HISTORY = "search:user_history";
    private const string CACHE_PREFIX_SUGGESTIONS = "search:suggestions";
    private const string CACHE_PREFIX_ANALYTICS = "search:analytics";

    public SearchAnalyticsService(
        ILogger<SearchAnalyticsService> logger,
        IApplicationDbContext context,
        IDistributedCache cache)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// 记录搜索行为
    /// </summary>
    public async Task RecordSearchAsync(string searchTerm, string? userId = null, string? ipAddress = null)
    {
        try
        {
            var normalizedTerm = NormalizeSearchTerm(searchTerm);

            // 创建搜索查询记录
            var searchQuery = SearchQuery.Create(searchTerm,
                userId != null ? Guid.Parse(userId) : null);

            searchQuery.IpAddress = ipAddress;

            _context.SearchQueries.Add(searchQuery);

            // 更新或创建热门搜索记录
            var popularSearch = await _context.PopularSearches
                .FirstOrDefaultAsync(ps => ps.Query.ToLower() == normalizedTerm.ToLower());

            if (popularSearch != null)
            {
                popularSearch.SearchCount++;
                popularSearch.LastSearchedAt = DateTime.UtcNow;
            }
            else
            {
                popularSearch = new PopularSearch
                {
                    Query = normalizedTerm,
                    SearchCount = 1,
                    FirstSearchedAt = DateTime.UtcNow,
                    LastSearchedAt = DateTime.UtcNow
                };
                _context.PopularSearches.Add(popularSearch);
            }

            await _context.SaveChangesAsync();

            // 清除相关缓存
            await InvalidateRelatedCaches(normalizedTerm);

            _logger.LogDebug("Search recorded: {SearchTerm} by user {UserId}", searchTerm, userId ?? "anonymous");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search: {SearchTerm}", searchTerm);
        }
    }

    /// <summary>
    /// 获取热门搜索词
    /// </summary>
    public async Task<IEnumerable<SearchTermStatsDto>> GetPopularSearchTermsAsync(int count = 20)
    {
        var cacheKey = $"{CACHE_PREFIX_POPULAR_TERMS}:{count}";

        try
        {
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<IEnumerable<SearchTermStatsDto>>(cachedResult, _jsonOptions)
                       ?? Enumerable.Empty<SearchTermStatsDto>();
            }

            var popularSearches = await _context.PopularSearches
                .OrderByDescending(ps => ps.SearchCount)
                .Take(count)
                .Select(ps => new SearchTermStatsDto
                {
                    Term = ps.Query,
                    Count = ps.SearchCount,
                    FirstSearchedAt = ps.FirstSearchedAt,
                    LastSearchedAt = ps.LastSearchedAt,
                    Percentage = 0 // 计算百分比
                })
                .ToListAsync();

            // 计算百分比
            var totalSearches = popularSearches.Sum(ps => ps.Count);
            foreach (var search in popularSearches)
            {
                search.Percentage = totalSearches > 0 ? (double)search.Count / totalSearches * 100 : 0;
            }

            // 缓存结果（10分钟）
            var serializedResult = JsonSerializer.Serialize(popularSearches, _jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

            return popularSearches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular search terms");
            return Enumerable.Empty<SearchTermStatsDto>();
        }
    }

    /// <summary>
    /// 获取指定时间段的搜索词统计
    /// </summary>
    public async Task<IEnumerable<SearchTermStatsDto>> GetSearchTermsByPeriodAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var searchStats = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= startDate && sq.CreatedAt <= endDate)
                .GroupBy(sq => sq.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.Key,
                    Count = g.Count(),
                    FirstSearchedAt = g.Min(sq => sq.CreatedAt),
                    LastSearchedAt = g.Max(sq => sq.CreatedAt),
                    AverageResultCount = g.Average(sq => sq.ResultCount),
                    AverageExecutionTime = g.Where(sq => sq.ExecutionTime.HasValue).Average(sq => sq.ExecutionTime!.Value)
                })
                .OrderByDescending(stat => stat.Count)
                .Take(50)
                .ToListAsync();

            // 计算百分比
            var totalSearches = searchStats.Sum(s => s.Count);
            foreach (var stat in searchStats)
            {
                stat.Percentage = totalSearches > 0 ? (double)stat.Count / totalSearches * 100 : 0;
            }

            return searchStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search terms by period: {StartDate} - {EndDate}", startDate, endDate);
            return Enumerable.Empty<SearchTermStatsDto>();
        }
    }

    /// <summary>
    /// 获取搜索分析数据
    /// </summary>
    public async Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var cacheKey = $"{CACHE_PREFIX_ANALYTICS}:{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<SearchAnalyticsDto>(cachedResult, _jsonOptions)
                       ?? new SearchAnalyticsDto();
            }

            var searches = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= startDate && sq.CreatedAt <= endDate)
                .ToListAsync();

            var analytics = new SearchAnalyticsDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalSearches = searches.Count,
                UniqueSearchTerms = searches.Select(s => s.NormalizedQuery).Distinct().Count(),
                AverageResultsPerSearch = searches.Average(s => s.ResultCount),
                AverageExecutionTime = searches.Where(s => s.ExecutionTime.HasValue).Average(s => s.ExecutionTime!.Value),
                SearchesWithResults = searches.Count(s => s.ResultCount > 0),
                SearchesWithoutResults = searches.Count(s => s.ResultCount == 0),
                TopSearchTerms = await GetPopularSearchTermsAsync(10)
            };

            analytics.SuccessRate = analytics.TotalSearches > 0
                ? (double)analytics.SearchesWithResults / analytics.TotalSearches * 100
                : 0;

            // 缓存结果（30分钟）
            var serializedResult = JsonSerializer.Serialize(analytics, _jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search analytics: {StartDate} - {EndDate}", startDate, endDate);
            return new SearchAnalyticsDto
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int count = 10)
    {
        if (string.IsNullOrWhiteSpace(partialTerm))
            return Enumerable.Empty<string>();

        var cacheKey = $"{CACHE_PREFIX_SUGGESTIONS}:{partialTerm.ToLower()}:{count}";

        try
        {
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<IEnumerable<string>>(cachedResult, _jsonOptions)
                       ?? Enumerable.Empty<string>();
            }

            var normalizedPartial = partialTerm.ToLower().Trim();

            // 从热门搜索中获取建议
            var suggestions = await _context.PopularSearches
                .Where(ps => ps.Query.ToLower().Contains(normalizedPartial))
                .OrderByDescending(ps => ps.SearchCount)
                .Take(count)
                .Select(ps => ps.Query)
                .ToListAsync();

            // 如果建议不够，从搜索索引中获取更多建议
            if (suggestions.Count < count)
            {
                var remaining = count - suggestions.Count;
                var additionalSuggestions = await _context.SearchIndexes
                    .Where(si => si.IsActive && si.Title != null && si.Title.ToLower().Contains(normalizedPartial))
                    .Select(si => si.Title!)
                    .Distinct()
                    .Take(remaining * 2)
                    .ToListAsync();

                // 过滤掉已存在的建议
                var newSuggestions = additionalSuggestions
                    .Where(s => !suggestions.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .Take(remaining);

                suggestions.AddRange(newSuggestions);
            }

            // 缓存结果（5分钟）
            var serializedResult = JsonSerializer.Serialize(suggestions.Take(count), _jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return suggestions.Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for: {PartialTerm}", partialTerm);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 清除用户搜索历史
    /// </summary>
    public async Task<bool> ClearSearchHistoryAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return false;

            var userSearches = await _context.SearchQueries
                .Where(sq => sq.UserId == userGuid)
                .ToListAsync();

            _context.SearchQueries.RemoveRange(userSearches);
            await _context.SaveChangesAsync();

            // 清除相关缓存
            await _cache.RemoveAsync($"{CACHE_PREFIX_SEARCH_HISTORY}:{userId}");

            _logger.LogInformation("Cleared search history for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取用户搜索历史
    /// </summary>
    public async Task<IEnumerable<SearchTermStatsDto>> GetUserSearchHistoryAsync(string userId, int count = 50)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
                return Enumerable.Empty<SearchTermStatsDto>();

            var cacheKey = $"{CACHE_PREFIX_SEARCH_HISTORY}:{userId}:{count}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedResult))
            {
                return JsonSerializer.Deserialize<IEnumerable<SearchTermStatsDto>>(cachedResult, _jsonOptions)
                       ?? Enumerable.Empty<SearchTermStatsDto>();
            }

            var searchHistory = await _context.SearchQueries
                .Where(sq => sq.UserId == userGuid)
                .GroupBy(sq => sq.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.Key,
                    Count = g.Count(),
                    FirstSearchedAt = g.Min(sq => sq.CreatedAt),
                    LastSearchedAt = g.Max(sq => sq.CreatedAt),
                    AverageResultCount = g.Average(sq => sq.ResultCount),
                    AverageExecutionTime = g.Where(sq => sq.ExecutionTime.HasValue).Average(sq => sq.ExecutionTime!.Value)
                })
                .OrderByDescending(stat => stat.LastSearchedAt)
                .Take(count)
                .ToListAsync();

            // 缓存结果（30分钟）
            var serializedResult = JsonSerializer.Serialize(searchHistory, _jsonOptions);
            await _cache.SetStringAsync(cacheKey, serializedResult, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return searchHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user search history: {UserId}", userId);
            return Enumerable.Empty<SearchTermStatsDto>();
        }
    }

    /// <summary>
    /// 获取搜索趋势
    /// </summary>
    public async Task<SearchTrendDto> GetSearchTrendsAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var endDate = DateTime.UtcNow;

            var searchesByDay = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= startDate)
                .GroupBy(sq => sq.CreatedAt.Date)
                .Select(g => new SearchTrendDto
                {
                    Date = g.Key,
                    SearchCount = g.Count(),
                    UniqueTermsCount = g.Select(sq => sq.NormalizedQuery).Distinct().Count(),
                    AverageExecutionTime = g.Where(sq => sq.ExecutionTime.HasValue).Average(sq => sq.ExecutionTime!.Value)
                })
                .OrderBy(t => t.Date)
                .ToListAsync();

            // 填补缺失的日期
            var allDays = new List<SearchTrendDto>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var existingTrend = searchesByDay.FirstOrDefault(t => t.Date == date);
                allDays.Add(existingTrend ?? new SearchTrendDto
                {
                    Date = date,
                    SearchCount = 0,
                    UniqueTermsCount = 0,
                    AverageExecutionTime = 0
                });
            }

            return new SearchTrendDto
            {
                Date = DateTime.UtcNow.Date,
                SearchCount = allDays.Sum(d => d.SearchCount),
                UniqueTermsCount = allDays.Where(d => d.UniqueTermsCount > 0).Sum(d => d.UniqueTermsCount),
                AverageExecutionTime = allDays.Where(d => d.AverageExecutionTime > 0).Average(d => d.AverageExecutionTime),
                TrendData = allDays
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search trends for {Days} days", days);
            return new SearchTrendDto
            {
                Date = DateTime.UtcNow.Date,
                TrendData = Enumerable.Empty<SearchTrendDto>()
            };
        }
    }

    /// <summary>
    /// 获取查询分析
    /// </summary>
    public async Task<QueryAnalysisResponse> GetQueryAnalysisAsync(string query, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var analysis = new QueryAnalysisResponse
            {
                Query = query,
                DetectedLanguage = DetectLanguage(query),
                ExtractedKeywords = ExtractKeywords(query),
                QueryIntent = AnalyzeQueryIntent(query),
                QueryCategory = CategorizeQuery(query)
            };

            // 计算复杂度评分
            analysis.ComplexityScore = CalculateComplexityScore(query);

            // 获取预期结果数量（基于历史数据）
            var historicalData = await _context.SearchQueries
                .Where(sq => sq.NormalizedQuery.ToLower() == query.ToLower().Trim())
                .Where(sq => !startDate.HasValue || sq.CreatedAt >= startDate)
                .Where(sq => !endDate.HasValue || sq.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            analysis.ExpectedResultCount = historicalData.Any()
                ? (int)historicalData.Average(h => h.ResultCount)
                : EstimateResultCount(query);

            // 生成优化建议
            analysis.SuggestedOptimizedQuery = GenerateOptimizedQuery(query);

            // 获取搜索建议
            analysis.SearchSuggestions = await GetSearchSuggestionsAsync(query, 5);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query: {Query}", query);
            return new QueryAnalysisResponse { Query = query };
        }
    }

    /// <summary>
    /// 获取性能分析
    /// </summary>
    public async Task<SearchPerformanceAnalysis> GetPerformanceAnalysisAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var searches = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= start && sq.CreatedAt <= end)
                .Where(sq => sq.ExecutionTime.HasValue)
                .ToListAsync(cancellationToken);

            if (!searches.Any())
            {
                return new SearchPerformanceAnalysis
                {
                    AnalysisPeriodStart = start,
                    AnalysisPeriodEnd = end
                };
            }

            var executionTimes = searches.Select(s => (double)s.ExecutionTime!.Value).OrderBy(t => t).ToList();

            var analysis = new SearchPerformanceAnalysis
            {
                AnalysisPeriodStart = start,
                AnalysisPeriodEnd = end,
                AverageResponseTime = executionTimes.Average(),
                FastestResponseTime = executionTimes.Min(),
                SlowestResponseTime = executionTimes.Max(),
                P95ResponseTime = GetPercentile(executionTimes, 95),
                SuccessRate = (double)searches.Count(s => s.ResultCount > 0) / searches.Count * 100,
                FailureRate = (double)searches.Count(s => s.ResultCount == 0) / searches.Count * 100,
                TimeoutCount = searches.Count(s => s.ExecutionTime > 10000), // 超过10秒视为超时
                ErrorCount = 0, // 需要额外的错误记录机制
                IndexHitRate = 95.0, // 模拟值，实际需要从搜索引擎获取
                CacheHitRate = 85.0  // 模拟值，实际需要从缓存系统获取
            };

            // 生成趋势数据
            analysis.PerformanceTrend = GeneratePerformanceTrendData(searches, start, end);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance analysis: {StartDate} - {EndDate}", startDate, endDate);
            return new SearchPerformanceAnalysis
            {
                AnalysisPeriodStart = startDate ?? DateTime.UtcNow.AddDays(-30),
                AnalysisPeriodEnd = endDate ?? DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 获取优化建议
    /// </summary>
    public async Task<List<SearchOptimizationSuggestion>> GetOptimizationSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = new List<SearchOptimizationSuggestion>();

            // 分析慢查询
            var slowQueries = await _context.SearchQueries
                .Where(sq => sq.ExecutionTime > 3000) // 超过3秒的查询
                .GroupBy(sq => sq.NormalizedQuery)
                .Where(g => g.Count() > 5) // 出现超过5次
                .Select(g => new { Query = g.Key, Count = g.Count(), AvgTime = g.Average(sq => sq.ExecutionTime) })
                .OrderByDescending(q => q.AvgTime)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var slowQuery in slowQueries)
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Performance",
                    Title = $"优化慢查询: {slowQuery.Query}",
                    Description = $"查询 '{slowQuery.Query}' 平均响应时间为 {slowQuery.AvgTime:F0}ms，建议优化索引或查询条件",
                    ExpectedImprovement = "响应时间可提升50-80%",
                    ImplementationDifficulty = 3,
                    Priority = 4,
                    RelatedQueries = new[] { slowQuery.Query },
                    RecommendedActions = new[]
                    {
                        "检查搜索索引是否正确配置",
                        "考虑添加缓存机制",
                        "优化查询条件复杂度"
                    },
                    AffectedQueriesCount = slowQuery.Count,
                    EstimatedPerformanceGain = 65.0
                });
            }

            // 分析无结果查询
            var noResultQueries = await _context.SearchQueries
                .Where(sq => sq.ResultCount == 0)
                .GroupBy(sq => sq.NormalizedQuery)
                .Where(g => g.Count() > 3)
                .Select(g => new { Query = g.Key, Count = g.Count() })
                .OrderByDescending(q => q.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var noResultQuery in noResultQueries)
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Content",
                    Title = $"改进无结果查询: {noResultQuery.Query}",
                    Description = $"查询 '{noResultQuery.Query}' 经常返回空结果，建议添加相关内容或同义词处理",
                    ExpectedImprovement = "用户体验显著提升",
                    ImplementationDifficulty = 2,
                    Priority = 3,
                    RelatedQueries = new[] { noResultQuery.Query },
                    RecommendedActions = new[]
                    {
                        "添加相关内容或文档",
                        "配置同义词词典",
                        "提供搜索建议"
                    },
                    AffectedQueriesCount = noResultQuery.Count,
                    EstimatedPerformanceGain = 40.0
                });
            }

            // 分析热门查询的性能
            var popularQueries = await _context.PopularSearches
                .OrderByDescending(ps => ps.SearchCount)
                .Take(20)
                .ToListAsync(cancellationToken);

            var popularQueryPerformance = await _context.SearchQueries
                .Where(sq => popularQueries.Select(pq => pq.Query.ToLower()).Contains(sq.NormalizedQuery.ToLower()))
                .GroupBy(sq => sq.NormalizedQuery)
                .Select(g => new { Query = g.Key, AvgTime = g.Average(sq => sq.ExecutionTime), Count = g.Count() })
                .Where(q => q.AvgTime > 1000) // 超过1秒
                .ToListAsync(cancellationToken);

            foreach (var query in popularQueryPerformance)
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Cache",
                    Title = $"缓存热门查询: {query.Query}",
                    Description = $"热门查询 '{query.Query}' 响应时间为 {query.AvgTime:F0}ms，建议添加缓存",
                    ExpectedImprovement = "响应时间可提升90%以上",
                    ImplementationDifficulty = 1,
                    Priority = 5,
                    RelatedQueries = new[] { query.Query },
                    RecommendedActions = new[]
                    {
                        "为热门查询添加Redis缓存",
                        "设置合适的缓存过期时间",
                        "监控缓存命中率"
                    },
                    AffectedQueriesCount = query.Count,
                    EstimatedPerformanceGain = 92.0
                });
            }

            return suggestions.OrderByDescending(s => s.Priority).ThenByDescending(s => s.EstimatedPerformanceGain).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization suggestions");
            return new List<SearchOptimizationSuggestion>();
        }
    }

    /// <summary>
    /// 获取分析概览
    /// </summary>
    public async Task<SearchAnalyticsOverview> GetAnalyticsOverviewAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var searches = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= start && sq.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            var todaySearches = await _context.SearchQueries
                .CountAsync(sq => sq.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken);

            var topTerms = await GetPopularSearchTermsAsync(10);
            var trends = await GetSearchTrendsAsync(7);

            return new SearchAnalyticsOverview
            {
                TotalSearches = searches.Count,
                UniqueTermsCount = searches.Select(s => s.NormalizedQuery).Distinct().Count(),
                AverageResponseTime = searches.Where(s => s.ExecutionTime.HasValue).Average(s => s.ExecutionTime!.Value),
                TodaySearches = todaySearches,
                TopSearchTerms = topTerms,
                RecentTrends = trends.TrendData ?? Enumerable.Empty<SearchTrendDto>(),
                SuccessRate = searches.Any() ? (double)searches.Count(s => s.ResultCount > 0) / searches.Count * 100 : 0,
                AverageResultsPerSearch = searches.Any() ? searches.Average(s => s.ResultCount) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics overview");
            return new SearchAnalyticsOverview();
        }
    }

    /// <summary>
    /// 获取搜索趋势分析
    /// </summary>
    public async Task<SearchTrendAnalysis> GetSearchTrendAsync(TrendPeriod period, CancellationToken cancellationToken = default)
    {
        try
        {
            var days = (int)period;
            var startDate = DateTime.UtcNow.AddDays(-days);
            var previousStartDate = startDate.AddDays(-days); // 前一个周期用于计算增长率

            var currentPeriodSearches = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= startDate)
                .ToListAsync(cancellationToken);

            var previousPeriodSearches = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= previousStartDate && sq.CreatedAt < startDate)
                .ToListAsync(cancellationToken);

            var trendData = new List<SearchTrendDto>();
            var groupByPeriod = GetGroupByPeriod(period);

            // 生成趋势数据
            var groupedCurrentData = GroupSearchesByPeriod(currentPeriodSearches, startDate, DateTime.UtcNow, groupByPeriod);
            trendData.AddRange(groupedCurrentData);

            // 计算增长率
            var currentTotal = currentPeriodSearches.Count;
            var previousTotal = previousPeriodSearches.Count;
            var growthRate = previousTotal > 0 ? (double)(currentTotal - previousTotal) / previousTotal * 100 : 0;

            // 找出趋势词汇
            var trendingTerms = await _context.SearchQueries
                .Where(sq => sq.CreatedAt >= startDate)
                .GroupBy(sq => sq.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.Key,
                    Count = g.Count(),
                    FirstSearchedAt = g.Min(sq => sq.CreatedAt),
                    LastSearchedAt = g.Max(sq => sq.CreatedAt)
                })
                .OrderByDescending(t => t.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            // 找出峰值时间
            var peakData = trendData.OrderByDescending(t => t.SearchCount).FirstOrDefault();

            return new SearchTrendAnalysis
            {
                Period = period,
                TotalSearches = currentTotal,
                GrowthRate = growthRate,
                TrendData = trendData,
                TrendingTerms = trendingTerms,
                PeakSearchTime = peakData?.Date ?? DateTime.UtcNow,
                PeakSearchCount = peakData?.SearchCount ?? 0,
                SearchPattern = AnalyzeSearchPattern(trendData, period)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search trend analysis for period: {Period}", period);
            return new SearchTrendAnalysis
            {
                Period = period,
                TrendData = Enumerable.Empty<SearchTrendDto>()
            };
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 标准化搜索词
    /// </summary>
    private string NormalizeSearchTerm(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return string.Empty;

        return searchTerm.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// 清除相关缓存
    /// </summary>
    private async Task InvalidateRelatedCaches(string searchTerm)
    {
        var cacheKeysToRemove = new[]
        {
            $"{CACHE_PREFIX_POPULAR_TERMS}:*",
            $"{CACHE_PREFIX_SUGGESTIONS}:{searchTerm.ToLower()}:*",
            $"{CACHE_PREFIX_ANALYTICS}:*"
        };

        foreach (var keyPattern in cacheKeysToRemove)
        {
            try
            {
                // Redis通配符删除需要特殊处理，这里简化处理
                if (!keyPattern.Contains("*"))
                {
                    await _cache.RemoveAsync(keyPattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache key: {Key}", keyPattern);
            }
        }
    }

    /// <summary>
    /// 检测语言
    /// </summary>
    private string DetectLanguage(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "zh-CN";

        // 简单的语言检测：包含中文字符则为中文
        return Regex.IsMatch(query, @"[\u4e00-\u9faf]") ? "zh-CN" : "en-US";
    }

    /// <summary>
    /// 提取关键词
    /// </summary>
    private IEnumerable<string> ExtractKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Enumerable.Empty<string>();

        // 简单的关键词提取：分割单词并过滤停用词
        var words = query.Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}' },
            StringSplitOptions.RemoveEmptyEntries);

        var stopWords = new HashSet<string> { "的", "了", "在", "是", "我", "有", "和", "就", "不", "人", "都", "一", "个", "上", "也", "为", "要", "他", "时", "来", "自", "会", "那", "得", "于", "着", "下", "的", "去", "你", "对", "说", "们", "而", "把", "还", "与", "及", "给", "从", "被", "她", "但", "更", "很", "又", "已", "这", "以", "将", "用", "如", "所", "到", "能", "可", "之", "只", "后", "和", "又", "等", "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it", "its", "of", "on", "that", "the", "to", "was", "will", "with" };

        return words
            .Where(w => w.Length > 1 && !stopWords.Contains(w.ToLowerInvariant()))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 分析查询意图
    /// </summary>
    private string AnalyzeQueryIntent(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Unknown";

        var lowerQuery = query.ToLowerInvariant();

        if (lowerQuery.Contains("如何") || lowerQuery.Contains("怎么") || lowerQuery.StartsWith("how"))
            return "Instructional";

        if (lowerQuery.Contains("什么是") || lowerQuery.Contains("是什么") || lowerQuery.StartsWith("what"))
            return "Informational";

        if (lowerQuery.Contains("购买") || lowerQuery.Contains("价格") || lowerQuery.Contains("buy"))
            return "Transactional";

        if (lowerQuery.Contains("在哪") || lowerQuery.Contains("地址") || lowerQuery.Contains("where"))
            return "Navigational";

        return "General";
    }

    /// <summary>
    /// 分类查询
    /// </summary>
    private string CategorizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "General";

        var lowerQuery = query.ToLowerInvariant();

        var categories = new Dictionary<string, string[]>
        {
            ["Technology"] = new[] { "技术", "开发", "编程", "代码", "软件", "程序", "tech", "code", "programming", "software", "development" },
            ["Business"] = new[] { "商业", "企业", "管理", "营销", "销售", "business", "management", "marketing", "sales", "enterprise" },
            ["Education"] = new[] { "教育", "学习", "培训", "课程", "教程", "education", "learning", "training", "course", "tutorial" },
            ["Health"] = new[] { "健康", "医疗", "疾病", "治疗", "医生", "health", "medical", "disease", "treatment", "doctor" },
            ["Entertainment"] = new[] { "娱乐", "电影", "音乐", "游戏", "小说", "entertainment", "movie", "music", "game", "novel" }
        };

        foreach (var category in categories)
        {
            if (category.Value.Any(keyword => lowerQuery.Contains(keyword)))
            {
                return category.Key;
            }
        }

        return "General";
    }

    /// <summary>
    /// 计算复杂度评分
    /// </summary>
    private int CalculateComplexityScore(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 1;

        var score = 1;
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 基于单词数量
        score += Math.Min(words.Length, 10);

        // 基于特殊字符
        if (query.Contains("\"") || query.Contains("'")) score += 2;
        if (query.Contains("*") || query.Contains("?")) score += 3;
        if (query.Contains("(") || query.Contains(")")) score += 2;

        // 基于逻辑操作符
        var logicalOperators = new[] { "AND", "OR", "NOT", "和", "或", "非" };
        score += logicalOperators.Count(op => query.ToUpperInvariant().Contains(op)) * 2;

        return Math.Min(score, 10); // 限制在1-10之间
    }

    /// <summary>
    /// 估计结果数量
    /// </summary>
    private int EstimateResultCount(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return 0;

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // 简单估算：单词越多，结果可能越少
        if (words.Length == 1) return 100;
        if (words.Length == 2) return 50;
        if (words.Length >= 3) return 20;

        return 10;
    }

    /// <summary>
    /// 生成优化查询
    /// </summary>
    private string GenerateOptimizedQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        var optimized = query.Trim();

        // 移除多余空格
        optimized = Regex.Replace(optimized, @"\s+", " ");

        // 转换为小写（除非包含专有名词）
        if (!Regex.IsMatch(optimized, @"[A-Z]{2,}"))
        {
            optimized = optimized.ToLowerInvariant();
        }

        return optimized;
    }

    /// <summary>
    /// 获取百分位数
    /// </summary>
    private double GetPercentile(List<double> values, int percentile)
    {
        if (!values.Any()) return 0;

        var index = (int)Math.Ceiling(values.Count * percentile / 100.0) - 1;
        return values[Math.Max(0, Math.Min(index, values.Count - 1))];
    }

    /// <summary>
    /// 生成性能趋势数据
    /// </summary>
    private IEnumerable<PerformanceDataPoint> GeneratePerformanceTrendData(List<SearchQuery> searches, DateTime startDate, DateTime endDate)
    {
        var dataPoints = new List<PerformanceDataPoint>();
        var timeSpan = endDate - startDate;
        var interval = timeSpan.TotalDays > 7 ? TimeSpan.FromDays(1) : TimeSpan.FromHours(1);

        for (var time = startDate; time <= endDate; time = time.Add(interval))
        {
            var periodSearches = searches.Where(s => s.CreatedAt >= time && s.CreatedAt < time.Add(interval)).ToList();

            if (periodSearches.Any())
            {
                dataPoints.Add(new PerformanceDataPoint
                {
                    Timestamp = time,
                    ResponseTime = periodSearches.Where(s => s.ExecutionTime.HasValue).Average(s => s.ExecutionTime!.Value),
                    SearchCount = periodSearches.Count,
                    SuccessCount = periodSearches.Count(s => s.ResultCount > 0)
                });
            }
        }

        return dataPoints;
    }

    /// <summary>
    /// 获取分组周期
    /// </summary>
    private TimeSpan GetGroupByPeriod(TrendPeriod period)
    {
        return period switch
        {
            TrendPeriod.Daily => TimeSpan.FromHours(1),
            TrendPeriod.Weekly => TimeSpan.FromDays(1),
            TrendPeriod.Monthly => TimeSpan.FromDays(1),
            TrendPeriod.Quarterly => TimeSpan.FromDays(7),
            TrendPeriod.Yearly => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(1)
        };
    }

    /// <summary>
    /// 按周期分组搜索数据
    /// </summary>
    private IEnumerable<SearchTrendDto> GroupSearchesByPeriod(List<SearchQuery> searches, DateTime startDate, DateTime endDate, TimeSpan groupBy)
    {
        var trendData = new List<SearchTrendDto>();

        for (var time = startDate; time <= endDate; time = time.Add(groupBy))
        {
            var periodSearches = searches.Where(s => s.CreatedAt >= time && s.CreatedAt < time.Add(groupBy)).ToList();

            trendData.Add(new SearchTrendDto
            {
                Date = time,
                SearchCount = periodSearches.Count,
                UniqueTermsCount = periodSearches.Select(s => s.NormalizedQuery).Distinct().Count(),
                AverageExecutionTime = periodSearches.Where(s => s.ExecutionTime.HasValue).Average(s => s.ExecutionTime!.Value)
            });
        }

        return trendData;
    }

    /// <summary>
    /// 分析搜索模式
    /// </summary>
    private string AnalyzeSearchPattern(IEnumerable<SearchTrendDto> trendData, TrendPeriod period)
    {
        var data = trendData.ToList();
        if (!data.Any()) return "No Pattern";

        var searchCounts = data.Select(d => d.SearchCount).ToList();
        var average = searchCounts.Average();
        var variance = searchCounts.Select(x => Math.Pow(x - average, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        // 判断模式
        if (stdDev < average * 0.1) return "Stable";
        if (searchCounts.Last() > searchCounts.First() * 1.2) return "Growing";
        if (searchCounts.Last() < searchCounts.First() * 0.8) return "Declining";
        if (stdDev > average * 0.5) return "Volatile";

        return "Normal";
    }

    #endregion
}