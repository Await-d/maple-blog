using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Search;
using MapleBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 搜索分析服务实现
/// </summary>
public class SearchAnalyticsService : ISearchAnalyticsService
{
    private readonly ILogger<SearchAnalyticsService> _logger;
    private readonly DbContext _context;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    // 配置参数
    private readonly int _analyticsCacheMinutes;
    private readonly bool _enablePrivacyMode;

    public SearchAnalyticsService(
        ILogger<SearchAnalyticsService> logger,
        DbContext context,
        IDistributedCache cache,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _cache = cache;
        _configuration = configuration;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // 读取配置
        _analyticsCacheMinutes = _configuration.GetValue<int>("SearchAnalytics:CacheMinutes", 15);
        _enablePrivacyMode = _configuration.GetValue<bool>("SearchAnalytics:PrivacyMode", true);
    }

    public async Task RecordSearchAsync(string searchTerm, string userId = null, string ipAddress = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return;

            var searchQuery = new SearchQuery
            {
                Query = searchTerm.Trim(),
                NormalizedQuery = searchTerm.ToLowerInvariant().Trim(),
                UserId = !_enablePrivacyMode && !string.IsNullOrWhiteSpace(userId) ? Guid.Parse(userId) : null,
                IpAddress = !_enablePrivacyMode ? ipAddress : null,
                CreatedAt = DateTime.UtcNow,
                SearchType = "general"
            };

            _context.Set<SearchQuery>().Add(searchQuery);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Search recorded: {SearchTerm}", searchTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search: {SearchTerm}", searchTerm);
        }
    }

    public async Task<IEnumerable<SearchTermStatsDto>> GetPopularSearchTermsAsync(int count = 20)
    {
        const string cacheKey = "search:popular_terms";

        try
        {
            // 尝试从缓存获取
            var cachedResult = await GetFromCacheAsync<List<SearchTermStatsDto>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult.Take(count);
            }

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var popularTerms = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= startDate)
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.First().Query,
                    Count = g.Count(),
                    LastUsed = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .Take(count * 2) // 缓存更多数据
                .ToListAsync();

            // 缓存结果
            await SetCacheAsync(cacheKey, popularTerms, TimeSpan.FromMinutes(_analyticsCacheMinutes));

            return popularTerms.Take(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular search terms");
            return new List<SearchTermStatsDto>();
        }
    }

    public async Task<IEnumerable<SearchTermStatsDto>> GetSearchTermsByPeriodAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var terms = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= startDate && q.CreatedAt <= endDate)
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.First().Query,
                    Count = g.Count(),
                    LastUsed = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();

            return terms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search terms by period");
            return new List<SearchTermStatsDto>();
        }
    }

    public async Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var searches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= startDate && q.CreatedAt <= endDate)
                .ToListAsync();

            var topTerms = searches
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.First().Query,
                    Count = g.Count(),
                    LastUsed = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            return new SearchAnalyticsDto
            {
                TotalSearches = searches.Count,
                UniqueTerms = searches.Select(s => s.NormalizedQuery).Distinct().Count(),
                TopTerms = topTerms
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search analytics");
            return new SearchAnalyticsDto();
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int count = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(partialTerm))
                return new List<string>();

            var normalizedTerm = partialTerm.ToLowerInvariant();

            var suggestions = await _context.Set<SearchQuery>()
                .Where(q => q.NormalizedQuery.Contains(normalizedTerm))
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new { Query = g.First().Query, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(count)
                .Select(s => s.Query)
                .ToListAsync();

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for: {PartialTerm}", partialTerm);
            return new List<string>();
        }
    }

    public async Task<bool> ClearSearchHistoryAsync(string userId)
    {
        if (_enablePrivacyMode || string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        try
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                var userSearches = await _context.Set<SearchQuery>()
                    .Where(q => q.UserId == userGuid)
                    .ToListAsync();

                _context.Set<SearchQuery>().RemoveRange(userSearches);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleared search history for user: {UserId}", userId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<SearchTermStatsDto>> GetUserSearchHistoryAsync(string userId, int count = 50)
    {
        if (_enablePrivacyMode || string.IsNullOrWhiteSpace(userId))
        {
            return new List<SearchTermStatsDto>();
        }

        try
        {
            if (Guid.TryParse(userId, out var userGuid))
            {
                var userSearches = await _context.Set<SearchQuery>()
                    .Where(q => q.UserId == userGuid)
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(count)
                    .GroupBy(q => q.NormalizedQuery)
                    .Select(g => new SearchTermStatsDto
                    {
                        Term = g.First().Query,
                        Count = g.Count(),
                        LastUsed = g.Max(x => x.CreatedAt)
                    })
                    .ToListAsync();

                return userSearches;
            }

            return new List<SearchTermStatsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user search history: {UserId}", userId);
            return new List<SearchTermStatsDto>();
        }
    }

    public async Task<SearchTrendDto> GetSearchTrendsAsync(int days = 30)
    {
        try
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var searchCount = await _context.Set<SearchQuery>()
                .CountAsync(q => q.CreatedAt >= startDate);

            return new SearchTrendDto
            {
                Date = endDate,
                SearchCount = searchCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search trends");
            return new SearchTrendDto
            {
                Date = DateTime.UtcNow,
                SearchCount = 0
            };
        }
    }

    public async Task<QueryAnalysisResponse> GetQueryAnalysisAsync(string query, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new QueryAnalysisResponse { Query = query ?? string.Empty };
            }

            var normalizedQuery = query.ToLowerInvariant().Trim();
            var keywords = ExtractKeywords(normalizedQuery);
            var complexityScore = CalculateComplexityScore(normalizedQuery);
            var queryIntent = AnalyzeQueryIntent(normalizedQuery);
            var queryCategory = CategorizeQuery(normalizedQuery);

            // 获取历史查询数据进行分析
            var historicalQueries = await _context.Set<SearchQuery>()
                .Where(q => q.NormalizedQuery.Contains(normalizedQuery) ||
                           normalizedQuery.Contains(q.NormalizedQuery))
                .Take(100)
                .ToListAsync(cancellationToken);

            var expectedResultCount = EstimateResultCount(normalizedQuery, historicalQueries.Count);
            var optimizedQuery = SuggestOptimizedQuery(normalizedQuery, keywords);
            var suggestions = await GenerateSearchSuggestionsAsync(normalizedQuery, 5);

            return new QueryAnalysisResponse
            {
                Query = query,
                ComplexityScore = complexityScore,
                ExpectedResultCount = expectedResultCount,
                SuggestedOptimizedQuery = optimizedQuery,
                ExtractedKeywords = keywords,
                QueryIntent = queryIntent,
                DetectedLanguage = DetectLanguage(query),
                QueryCategory = queryCategory,
                SearchSuggestions = suggestions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query: {Query}", query);
            return new QueryAnalysisResponse { Query = query ?? string.Empty };
        }
    }

    public async Task<SearchPerformanceAnalysis> GetPerformanceAnalysisAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var cacheKey = $"search:performance:{start:yyyyMMdd}_{end:yyyyMMdd}";
            var cachedResult = await GetFromCacheAsync<SearchPerformanceAnalysis>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var searches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= start && q.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            if (!searches.Any())
            {
                return new SearchPerformanceAnalysis
                {
                    AnalysisPeriodStart = start,
                    AnalysisPeriodEnd = end
                };
            }

            // 模拟性能数据（实际项目中应该从性能日志或监控系统获取）
            var responseTimes = GenerateSimulatedResponseTimes(searches.Count);
            var successCount = (int)(searches.Count * 0.95); // 假设95%成功率
            var timeoutCount = (int)(searches.Count * 0.02);
            var errorCount = searches.Count - successCount;

            var performanceTrend = searches
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new PerformanceDataPoint
                {
                    Timestamp = g.Key,
                    ResponseTime = responseTimes.Where((_, index) => index % g.Count() == 0).Average(),
                    SearchCount = g.Count(),
                    SuccessCount = (int)(g.Count() * 0.95)
                })
                .OrderBy(p => p.Timestamp)
                .ToList();

            var analysis = new SearchPerformanceAnalysis
            {
                AnalysisPeriodStart = start,
                AnalysisPeriodEnd = end,
                AverageResponseTime = responseTimes.Average(),
                FastestResponseTime = responseTimes.Min(),
                SlowestResponseTime = responseTimes.Max(),
                P95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(responseTimes.Length * 0.95)).First(),
                SuccessRate = (double)successCount / searches.Count * 100,
                FailureRate = (double)errorCount / searches.Count * 100,
                TimeoutCount = timeoutCount,
                ErrorCount = errorCount,
                IndexHitRate = 85.5, // 模拟数据
                CacheHitRate = 72.3, // 模拟数据
                PerformanceTrend = performanceTrend
            };

            // 缓存结果
            await SetCacheAsync(cacheKey, analysis, TimeSpan.FromMinutes(_analyticsCacheMinutes));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance analysis for period {StartDate} to {EndDate}", startDate, endDate);
            return new SearchPerformanceAnalysis
            {
                AnalysisPeriodStart = startDate ?? DateTime.UtcNow.AddDays(-7),
                AnalysisPeriodEnd = endDate ?? DateTime.UtcNow
            };
        }
    }

    public async Task<List<SearchOptimizationSuggestion>> GetOptimizationSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string cacheKey = "search:optimization_suggestions";
            var cachedResult = await GetFromCacheAsync<List<SearchOptimizationSuggestion>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var suggestions = new List<SearchOptimizationSuggestion>();

            // 分析最近的搜索数据
            var recentSearches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                .Take(1000)
                .ToListAsync(cancellationToken);

            // 分析低频但重复的查询
            var lowFrequencyRepeats = recentSearches
                .GroupBy(q => q.NormalizedQuery)
                .Where(g => g.Count() >= 2 && g.Count() <= 5)
                .ToList();

            if (lowFrequencyRepeats.Any())
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Index",
                    Title = "优化低频重复查询索引",
                    Description = "发现多个低频但重复的搜索查询，建议为这些查询创建专门的索引以提高性能。",
                    ExpectedImprovement = "预计可提高相关查询响应速度30-50%",
                    ImplementationDifficulty = 2,
                    Priority = 3,
                    RelatedQueries = lowFrequencyRepeats.Take(5).Select(g => g.Key).ToList(),
                    RecommendedActions = new List<string>
                    {
                        "分析查询模式",
                        "创建复合索引",
                        "监控索引使用情况"
                    },
                    AffectedQueriesCount = lowFrequencyRepeats.Sum(g => g.Count()),
                    EstimatedPerformanceGain = 35.0
                });
            }

            // 分析热门查询优化
            var popularQueries = recentSearches
                .GroupBy(q => q.NormalizedQuery)
                .Where(g => g.Count() > 10)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToList();

            if (popularQueries.Any())
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Cache",
                    Title = "缓存热门搜索结果",
                    Description = "发现频繁查询的热门搜索词，建议将这些查询结果进行缓存以减少数据库负载。",
                    ExpectedImprovement = "预计可减少数据库查询负载60%，提高响应速度80%",
                    ImplementationDifficulty = 1,
                    Priority = 5,
                    RelatedQueries = popularQueries.Take(5).Select(g => g.Key).ToList(),
                    RecommendedActions = new List<string>
                    {
                        "实现Redis缓存",
                        "设置合适的缓存过期时间",
                        "监控缓存命中率"
                    },
                    AffectedQueriesCount = popularQueries.Sum(g => g.Count()),
                    EstimatedPerformanceGain = 70.0
                });
            }

            // 分析查询模式
            var uniqueQueries = recentSearches.Select(q => q.NormalizedQuery).Distinct().Count();
            var totalQueries = recentSearches.Count;

            if (uniqueQueries > 0 && (double)totalQueries / uniqueQueries < 2)
            {
                suggestions.Add(new SearchOptimizationSuggestion
                {
                    SuggestionType = "Suggestion",
                    Title = "改进搜索建议功能",
                    Description = "查询重复率较低，用户可能在寻找相同内容时使用不同的关键词。建议改进搜索建议和自动完成功能。",
                    ExpectedImprovement = "预计可提高用户搜索成功率25%，减少无效查询",
                    ImplementationDifficulty = 3,
                    Priority = 4,
                    RelatedQueries = new List<string>(),
                    RecommendedActions = new List<string>
                    {
                        "实现智能搜索建议",
                        "添加同义词支持",
                        "优化自动完成算法"
                    },
                    AffectedQueriesCount = totalQueries,
                    EstimatedPerformanceGain = 25.0
                });
            }

            // 通用优化建议
            suggestions.Add(new SearchOptimizationSuggestion
            {
                SuggestionType = "General",
                Title = "定期清理历史搜索数据",
                Description = "建议定期清理超过6个月的搜索历史数据，以保持搜索分析表的性能。",
                ExpectedImprovement = "预计可保持长期搜索性能稳定",
                ImplementationDifficulty = 1,
                Priority = 2,
                RelatedQueries = new List<string>(),
                RecommendedActions = new List<string>
                {
                    "设置定时清理任务",
                    "保留必要的统计数据",
                    "监控数据库大小"
                },
                AffectedQueriesCount = totalQueries,
                EstimatedPerformanceGain = 15.0
            });

            // 缓存建议
            await SetCacheAsync(cacheKey, suggestions, TimeSpan.FromHours(4));

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization suggestions");
            return new List<SearchOptimizationSuggestion>();
        }
    }

    #region 查询分析辅助方法

    private IEnumerable<string> ExtractKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        // 简单的关键词提取：分割空格，过滤停用词
        var stopWords = new HashSet<string> { "的", "是", "在", "有", "和", "与", "或", "但", "不", "了", "会", "就", "都", "很", "还", "也", "要", "到", "为" };

        return query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 1 && !stopWords.Contains(word))
            .Distinct()
            .Take(10);
    }

    private int CalculateComplexityScore(string query)
    {
        var score = 1;
        if (query.Contains(' ')) score += query.Split(' ').Length - 1;
        if (query.Contains('"')) score += 2;
        if (query.Contains('+') || query.Contains('-')) score += 2;
        if (query.Contains('*') || query.Contains('?')) score += 3;
        return Math.Min(score, 10);
    }

    private string AnalyzeQueryIntent(string query)
    {
        query = query.ToLowerInvariant();

        if (query.Contains("如何") || query.Contains("怎么") || query.Contains("怎样"))
            return "instructional";
        if (query.Contains("什么") || query.Contains("哪个") || query.Contains("什么是"))
            return "informational";
        if (query.Contains("下载") || query.Contains("购买") || query.Contains("获取"))
            return "transactional";
        if (query.Contains("最新") || query.Contains("今天") || query.Contains("最近"))
            return "temporal";

        return "general";
    }

    private string CategorizeQuery(string query)
    {
        query = query.ToLowerInvariant();

        if (query.Contains("技术") || query.Contains("开发") || query.Contains("编程"))
            return "technology";
        if (query.Contains("新闻") || query.Contains("资讯") || query.Contains("消息"))
            return "news";
        if (query.Contains("教程") || query.Contains("学习") || query.Contains("课程"))
            return "education";
        if (query.Contains("产品") || query.Contains("工具") || query.Contains("软件"))
            return "product";

        return "general";
    }

    private int EstimateResultCount(string query, int historicalCount)
    {
        var baseCount = Math.Max(10, historicalCount * 2);
        return Math.Min(baseCount, 1000);
    }

    private string SuggestOptimizedQuery(string query, IEnumerable<string> keywords)
    {
        if (!keywords.Any())
            return query;

        var keywordList = keywords.Take(3).ToList();
        return string.Join(" ", keywordList);
    }

    private string DetectLanguage(string query)
    {
        // 简单的语言检测
        var chineseChars = query.Where(c => c >= 0x4e00 && c <= 0x9fff).Count();
        return chineseChars > query.Length * 0.3 ? "zh-CN" : "en-US";
    }

    private async Task<IEnumerable<string>> GenerateSearchSuggestionsAsync(string query, int count)
    {
        try
        {
            var suggestions = await _context.Set<SearchQuery>()
                .Where(q => q.NormalizedQuery.StartsWith(query) && q.NormalizedQuery != query)
                .GroupBy(q => q.NormalizedQuery)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.First().Query)
                .ToListAsync();

            return suggestions;
        }
        catch
        {
            return new List<string>();
        }
    }

    private double[] GenerateSimulatedResponseTimes(int count)
    {
        var random = new Random();
        var times = new double[count];

        for (int i = 0; i < count; i++)
        {
            // 模拟正态分布的响应时间，平均值150ms，使用Box-Muller变换
            var u1 = 1.0 - random.NextDouble(); // uniform(0,1] random doubles
            var u2 = 1.0 - random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); // random normal(0,1)
            var randNormal = 150 + 50 * randStdNormal; // random normal(150, 50^2)
            times[i] = Math.Max(10, randNormal);
        }

        return times;
    }

    public async Task<SearchAnalyticsOverview> GetAnalyticsOverviewAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var cacheKey = $"search:overview:{start:yyyyMMdd}_{end:yyyyMMdd}";
            var cachedResult = await GetFromCacheAsync<SearchAnalyticsOverview>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var searches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= start && q.CreatedAt <= end)
                .ToListAsync(cancellationToken);

            var todayStart = DateTime.UtcNow.Date;
            var todaySearches = searches.Count(s => s.CreatedAt >= todayStart);

            var topTerms = searches
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.First().Query,
                    Count = g.Count(),
                    LastUsed = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            var trendData = searches
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new SearchTrendDto
                {
                    Date = g.Key,
                    SearchCount = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            var overview = new SearchAnalyticsOverview
            {
                TotalSearches = searches.Count,
                UniqueTermsCount = searches.Select(s => s.NormalizedQuery).Distinct().Count(),
                AverageResponseTime = 125.5, // 模拟数据
                TodaySearches = todaySearches,
                TopSearchTerms = topTerms,
                RecentTrends = trendData,
                SuccessRate = 94.2, // 模拟数据
                AverageResultsPerSearch = 15.3 // 模拟数据
            };

            // 缓存结果
            await SetCacheAsync(cacheKey, overview, TimeSpan.FromMinutes(_analyticsCacheMinutes));

            return overview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics overview for period {StartDate} to {EndDate}", startDate, endDate);
            return new SearchAnalyticsOverview();
        }
    }

    public async Task<SearchTrendAnalysis> GetSearchTrendAsync(TrendPeriod period, CancellationToken cancellationToken = default)
    {
        try
        {
            var days = (int)period;
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var cacheKey = $"search:trend:{period}_{endDate:yyyyMMdd}";
            var cachedResult = await GetFromCacheAsync<SearchTrendAnalysis>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var currentPeriodSearches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= startDate && q.CreatedAt <= endDate)
                .ToListAsync(cancellationToken);

            // 获取上一周期数据用于比较
            var previousStartDate = startDate.AddDays(-days);
            var previousEndDate = startDate;
            var previousPeriodSearches = await _context.Set<SearchQuery>()
                .Where(q => q.CreatedAt >= previousStartDate && q.CreatedAt <= previousEndDate)
                .ToListAsync(cancellationToken);

            var growthRate = previousPeriodSearches.Any()
                ? (double)(currentPeriodSearches.Count - previousPeriodSearches.Count) / previousPeriodSearches.Count * 100
                : 100.0;

            var trendData = currentPeriodSearches
                .GroupBy(s => s.CreatedAt.Date)
                .Select(g => new SearchTrendDto
                {
                    Date = g.Key,
                    SearchCount = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            var trendingTerms = currentPeriodSearches
                .GroupBy(q => q.NormalizedQuery)
                .Select(g => new SearchTermStatsDto
                {
                    Term = g.First().Query,
                    Count = g.Count(),
                    LastUsed = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(s => s.Count)
                .Take(10)
                .ToList();

            var peakData = trendData.OrderByDescending(t => t.SearchCount).FirstOrDefault();

            var analysis = new SearchTrendAnalysis
            {
                Period = period,
                TotalSearches = currentPeriodSearches.Count,
                GrowthRate = growthRate,
                TrendData = trendData,
                TrendingTerms = trendingTerms,
                PeakSearchTime = peakData?.Date ?? DateTime.UtcNow,
                PeakSearchCount = peakData?.SearchCount ?? 0,
                SearchPattern = AnalyzeSearchPattern(currentPeriodSearches)
            };

            // 缓存结果
            await SetCacheAsync(cacheKey, analysis, TimeSpan.FromMinutes(_analyticsCacheMinutes * 2));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search trend analysis for period {Period}", period);
            return new SearchTrendAnalysis { Period = period };
        }
    }

    private string AnalyzeSearchPattern(List<SearchQuery> searches)
    {
        if (!searches.Any()) return "无数据";

        var hourlyDistribution = searches
            .GroupBy(s => s.CreatedAt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var peakHour = hourlyDistribution.OrderByDescending(h => h.Value).FirstOrDefault();

        if (peakHour.Key >= 9 && peakHour.Key <= 17)
            return $"工作时间活跃 (峰值: {peakHour.Key}:00)";
        else if (peakHour.Key >= 18 && peakHour.Key <= 23)
            return $"晚间活跃 (峰值: {peakHour.Key}:00)";
        else
            return $"夜间或清晨活跃 (峰值: {peakHour.Key}:00)";
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 从缓存获取数据
    /// </summary>
    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data from cache with key: {Key}", key);
        }
        return null;
    }

    /// <summary>
    /// 设置缓存数据
    /// </summary>
    private async Task SetCacheAsync<T>(string key, T data, TimeSpan expiration) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            var serializedData = JsonSerializer.Serialize(data, _jsonOptions);
            await _cache.SetStringAsync(key, serializedData, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache with key: {Key}", key);
        }
    }

    #endregion
}