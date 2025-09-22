using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 数据分析服务实现
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IPostRepository _postRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<AnalyticsService> _logger;

        // Cache keys
        private const string WebsiteAnalyticsCacheKeyPrefix = "analytics:website";
        private const string UserBehaviorAnalyticsCacheKeyPrefix = "analytics:user-behavior";
        private const string ContentPerformanceCacheKeyPrefix = "analytics:content-performance";
        private const string SearchKeywordsCacheKeyPrefix = "analytics:search-keywords";
        private const string GeographicAnalyticsCacheKeyPrefix = "analytics:geographic";
        private const string DeviceBrowserCacheKeyPrefix = "analytics:device-browser";
        private const string TrafficSourceCacheKeyPrefix = "analytics:traffic-source";
        private const string UserRetentionCacheKeyPrefix = "analytics:user-retention";
        private const string ConversionFunnelCacheKeyPrefix = "analytics:conversion-funnel";
        private const string ABTestResultCacheKeyPrefix = "analytics:ab-test";
        private const string CustomReportCacheKeyPrefix = "analytics:custom-report";
        private const string RealTimeVisitorsCacheKey = "analytics:realtime-visitors";
        private const string PerformanceMetricsCacheKeyPrefix = "analytics:performance-metrics";
        private const string GoalCompletionCacheKeyPrefix = "analytics:goal-completion";

        // Cache expiry times
        private static readonly TimeSpan WebsiteAnalyticsCacheTime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan UserBehaviorAnalyticsCacheTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ContentPerformanceCacheTime = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan SearchKeywordsCacheTime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan GeographicAnalyticsCacheTime = TimeSpan.FromHours(1);
        private static readonly TimeSpan DeviceBrowserCacheTime = TimeSpan.FromHours(2);
        private static readonly TimeSpan TrafficSourceCacheTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan UserRetentionCacheTime = TimeSpan.FromHours(4);
        private static readonly TimeSpan ConversionFunnelCacheTime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan ABTestResultCacheTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan CustomReportCacheTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan RealTimeVisitorsCacheTime = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan PerformanceMetricsCacheTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan GoalCompletionCacheTime = TimeSpan.FromMinutes(15);

        public AnalyticsService(
            IPostRepository postRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IUserRepository userRepository,
            ICommentRepository commentRepository,
            IDistributedCache cache,
            IMapper mapper,
            ILogger<AnalyticsService> logger)
        {
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<WebsiteAnalyticsDto> GetWebsiteAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{WebsiteAnalyticsCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<WebsiteAnalyticsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Website analytics retrieved from cache for {StartDate} to {EndDate}", startDate, endDate);
                    return cachedData;
                }

                _logger.LogDebug("Calculating website analytics from database for {StartDate} to {EndDate}", startDate, endDate);

                // Get total page views (using post views as proxy)
                var totalPageViews = await _postRepository.GetTotalViewsAsync(
                    p => p.Status == PostStatus.Published && p.PublishedAt >= startDate && p.PublishedAt <= endDate);

                // Get unique visitors (simulate based on post engagement)
                var uniqueVisitors = (int)(totalPageViews * 0.6); // Simulate 60% unique visitor rate
                var newVisitors = (int)(uniqueVisitors * 0.4); // Simulate 40% new visitors
                var returningVisitors = uniqueVisitors - newVisitors;

                // Calculate average session duration (simulate based on content length)
                var averageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(2, 8));

                // Calculate bounce rate (simulate)
                var bounceRate = 0.3 + (Random.Shared.NextDouble() * 0.3); // 30-60%

                // Calculate average page load time (simulate)
                var averagePageLoadTime = 1.2 + (Random.Shared.NextDouble() * 0.8); // 1.2-2.0 seconds

                // Calculate conversion rate (simulate)
                var conversionRate = 0.02 + (Random.Shared.NextDouble() * 0.03); // 2-5%

                // Get daily trends
                var dailyTrends = await GetDailyVisitTrendsAsync(startDate, endDate);

                // Get top pages (using posts as proxy)
                var topPosts = await _postRepository.GetMostPopularAsync(1, 10,
                    (int)(endDate - startDate).TotalDays, CancellationToken.None);
                var topPages = topPosts.Select(p => new PageStatsDto
                {
                    PagePath = $"/posts/{p.Slug}",
                    PageTitle = p.Title,
                    PageViews = p.ViewCount,
                    UniquePageViews = (int)(p.ViewCount * 0.8),
                    AverageTimeOnPage = TimeSpan.FromMinutes(Random.Shared.Next(1, 5)),
                    ExitRate = Random.Shared.NextDouble() * 0.4
                }).ToList();

                // Simulate landing and exit pages based on top pages
                var topLandingPages = topPages.Take(5).ToList();
                var topExitPages = topPages.Skip(5).Take(5).ToList();

                var analytics = new WebsiteAnalyticsDto
                {
                    TotalPageViews = totalPageViews,
                    UniqueVisitors = uniqueVisitors,
                    NewVisitors = newVisitors,
                    ReturningVisitors = returningVisitors,
                    AverageSessionDuration = averageSessionDuration,
                    BounceRate = Math.Round(bounceRate, 3),
                    AveragePageLoadTime = Math.Round(averagePageLoadTime, 2),
                    ConversionRate = Math.Round(conversionRate, 4),
                    DailyTrends = dailyTrends,
                    TopPages = topPages,
                    TopLandingPages = topLandingPages,
                    TopExitPages = topExitPages
                };

                // Cache the result
                await SetCacheAsync(cacheKey, analytics, WebsiteAnalyticsCacheTime);

                _logger.LogInformation("Website analytics calculated for {StartDate} to {EndDate}", startDate, endDate);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating website analytics for {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<UserBehaviorAnalyticsDto> GetUserBehaviorAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{UserBehaviorAnalyticsCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<UserBehaviorAnalyticsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("User behavior analytics retrieved from cache for {StartDate} to {EndDate}", startDate, endDate);
                    return cachedData;
                }

                _logger.LogDebug("Calculating user behavior analytics from database for {StartDate} to {EndDate}", startDate, endDate);

                // Get active users count
                var activeUsers = await _userRepository.CountAsync(
                    u => u.LastLoginAt >= startDate && u.LastLoginAt <= endDate);

                // Simulate session duration distribution
                var sessionDurations = new List<SessionDurationStatsDto>
                {
                    new() { DurationRange = "0-30 seconds", SessionCount = Random.Shared.Next(100, 300), Percentage = 0.15 },
                    new() { DurationRange = "31-60 seconds", SessionCount = Random.Shared.Next(200, 400), Percentage = 0.25 },
                    new() { DurationRange = "1-3 minutes", SessionCount = Random.Shared.Next(300, 500), Percentage = 0.35 },
                    new() { DurationRange = "3-10 minutes", SessionCount = Random.Shared.Next(150, 250), Percentage = 0.20 },
                    new() { DurationRange = "10+ minutes", SessionCount = Random.Shared.Next(50, 100), Percentage = 0.05 }
                };

                // Simulate page depth analysis
                var pageDepths = new List<PageDepthStatsDto>
                {
                    new() { PageDepth = 1, SessionCount = Random.Shared.Next(400, 600), Percentage = 0.45 },
                    new() { PageDepth = 2, SessionCount = Random.Shared.Next(200, 350), Percentage = 0.25 },
                    new() { PageDepth = 3, SessionCount = Random.Shared.Next(100, 200), Percentage = 0.15 },
                    new() { PageDepth = 4, SessionCount = Random.Shared.Next(50, 100), Percentage = 0.10 },
                    new() { PageDepth = 5, SessionCount = Random.Shared.Next(20, 50), Percentage = 0.05 }
                };

                // Simulate user paths
                var userPaths = new List<UserPathStatsDto>
                {
                    new() { PathSequence = "Home -> Blog -> Post", UserCount = Random.Shared.Next(200, 400), ConversionRate = 0.75 },
                    new() { PathSequence = "Home -> About -> Contact", UserCount = Random.Shared.Next(50, 150), ConversionRate = 0.30 },
                    new() { PathSequence = "Search -> Post -> Comment", UserCount = Random.Shared.Next(100, 200), ConversionRate = 0.45 },
                    new() { PathSequence = "Home -> Archive -> Category", UserCount = Random.Shared.Next(80, 120), ConversionRate = 0.60 }
                };

                // Simulate action frequencies
                var actionFrequencies = new List<ActionFrequencyStatsDto>
                {
                    new() { ActionType = "Page View", Count = Random.Shared.Next(5000, 8000), Percentage = 0.70 },
                    new() { ActionType = "Comment", Count = Random.Shared.Next(200, 500), Percentage = 0.10 },
                    new() { ActionType = "Like", Count = Random.Shared.Next(500, 1000), Percentage = 0.15 },
                    new() { ActionType = "Share", Count = Random.Shared.Next(100, 300), Percentage = 0.05 }
                };

                // Simulate hourly activity
                var hourlyActivity = Enumerable.Range(0, 24).Select(hour => new HourlyActivityStatsDto
                {
                    Hour = hour,
                    ActivityCount = hour switch
                    {
                        >= 9 and <= 11 => Random.Shared.Next(800, 1200), // Morning peak
                        >= 14 and <= 16 => Random.Shared.Next(600, 1000), // Afternoon peak
                        >= 19 and <= 21 => Random.Shared.Next(1000, 1500), // Evening peak
                        >= 22 or <= 6 => Random.Shared.Next(50, 200), // Night
                        _ => Random.Shared.Next(300, 600) // Regular hours
                    },
                    ActivityPercentage = 0.0 // Will be calculated
                }).ToList();

                // Calculate activity percentages
                var totalActivity = hourlyActivity.Sum(h => h.ActivityCount);
                hourlyActivity.ForEach(h => h.ActivityPercentage = Math.Round((double)h.ActivityCount / totalActivity, 3));

                // Simulate device usage
                var deviceUsage = new DeviceUsageStatsDto
                {
                    DeviceTypes = new List<DeviceTypeStatsDto>
                    {
                        new() { DeviceType = "Desktop", Count = Random.Shared.Next(400, 600), Percentage = 0.50 },
                        new() { DeviceType = "Mobile", Count = Random.Shared.Next(300, 500), Percentage = 0.40 },
                        new() { DeviceType = "Tablet", Count = Random.Shared.Next(50, 150), Percentage = 0.10 }
                    },
                    Browsers = new List<BrowserStatsDto>
                    {
                        new() { Browser = "Chrome", Version = "120+", Count = Random.Shared.Next(500, 700), Percentage = 0.60 },
                        new() { Browser = "Firefox", Version = "119+", Count = Random.Shared.Next(150, 250), Percentage = 0.20 },
                        new() { Browser = "Safari", Version = "17+", Count = Random.Shared.Next(100, 200), Percentage = 0.15 },
                        new() { Browser = "Edge", Version = "119+", Count = Random.Shared.Next(30, 80), Percentage = 0.05 }
                    },
                    OperatingSystems = new List<OperatingSystemStatsDto>
                    {
                        new() { OperatingSystem = "Windows", Version = "10/11", Count = Random.Shared.Next(400, 600), Percentage = 0.55 },
                        new() { OperatingSystem = "macOS", Version = "14+", Count = Random.Shared.Next(200, 300), Percentage = 0.25 },
                        new() { OperatingSystem = "iOS", Version = "17+", Count = Random.Shared.Next(100, 200), Percentage = 0.15 },
                        new() { OperatingSystem = "Android", Version = "13+", Count = Random.Shared.Next(30, 80), Percentage = 0.05 }
                    }
                };

                // Simulate loyalty stats
                var loyaltyStats = new UserLoyaltyStatsDto
                {
                    NewUsers = Random.Shared.Next(100, 200),
                    ReturningUsers = Random.Shared.Next(300, 500),
                    LoyaltyScore = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.6, 2), // 0.6-0.9
                    LoyaltySegments = new List<LoyaltySegmentDto>
                    {
                        new() { SegmentName = "New", UserCount = Random.Shared.Next(100, 200), Percentage = 0.25 },
                        new() { SegmentName = "Casual", UserCount = Random.Shared.Next(200, 300), Percentage = 0.40 },
                        new() { SegmentName = "Regular", UserCount = Random.Shared.Next(100, 200), Percentage = 0.25 },
                        new() { SegmentName = "Loyal", UserCount = Random.Shared.Next(50, 100), Percentage = 0.10 }
                    }
                };

                var analytics = new UserBehaviorAnalyticsDto
                {
                    ActiveUsers = activeUsers,
                    SessionDurations = sessionDurations,
                    PageDepths = pageDepths,
                    UserPaths = userPaths,
                    ActionFrequencies = actionFrequencies,
                    HourlyActivity = hourlyActivity,
                    DeviceUsage = deviceUsage,
                    LoyaltyStats = loyaltyStats
                };

                // Cache the result
                await SetCacheAsync(cacheKey, analytics, UserBehaviorAnalyticsCacheTime);

                _logger.LogInformation("User behavior analytics calculated for {StartDate} to {EndDate}", startDate, endDate);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating user behavior analytics for {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<ContentPerformanceAnalyticsDto> GetContentPerformanceAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{ContentPerformanceCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<ContentPerformanceAnalyticsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Content performance analytics retrieved from cache for {StartDate} to {EndDate}", startDate, endDate);
                    return cachedData;
                }

                _logger.LogDebug("Calculating content performance analytics from database for {StartDate} to {EndDate}", startDate, endDate);

                // Get published posts in date range
                var posts = await _postRepository.GetPublishedPostsAsync(
                    pageNumber: 1,
                    pageSize: int.MaxValue,
                    categoryId: null,
                    startDate: startDate,
                    endDate: endDate,
                    cancellationToken: CancellationToken.None);

                var totalContent = posts.Count();
                var averageViews = totalContent > 0 ? Math.Round((double)posts.Sum(p => p.ViewCount) / totalContent, 2) : 0;
                var averageComments = totalContent > 0 ? Math.Round((double)posts.Sum(p => p.CommentCount) / totalContent, 2) : 0;
                var averageShares = totalContent > 0 ? Math.Round((double)posts.Sum(p => p.LikeCount) / totalContent, 2) : 0; // Using likes as shares proxy

                // Calculate engagement rate
                var totalEngagements = posts.Sum(p => p.ViewCount + p.CommentCount + p.LikeCount);
                var totalViews = posts.Sum(p => p.ViewCount);
                var engagementRate = totalViews > 0 ? Math.Round((double)totalEngagements / totalViews, 4) : 0;

                // Get top performing content
                var topPerformingContent = posts
                    .OrderByDescending(p => p.ViewCount + p.CommentCount * 10 + p.LikeCount * 5) // Weighted scoring
                    .Take(10)
                    .Select(p => new ContentPerformanceDto
                    {
                        ContentId = p.Id,
                        Title = p.Title,
                        ContentType = "Post",
                        ViewCount = p.ViewCount,
                        CommentCount = p.CommentCount,
                        ShareCount = p.LikeCount, // Using likes as shares proxy
                        EngagementRate = p.ViewCount > 0 ? Math.Round((double)(p.CommentCount + p.LikeCount) / p.ViewCount, 4) : 0,
                        PublishedAt = p.PublishedAt ?? p.CreatedAt
                    })
                    .ToList();

                // Calculate performance trends
                var performanceTrends = new List<DailyPerformanceStatsDto>();
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var dayPosts = posts.Where(p => (p.PublishedAt ?? p.CreatedAt).Date == currentDate);
                    var dayEngagement = dayPosts.Any() ?
                        Math.Round((double)dayPosts.Sum(p => p.CommentCount + p.LikeCount) / Math.Max(1, dayPosts.Sum(p => p.ViewCount)), 4) : 0;

                    performanceTrends.Add(new DailyPerformanceStatsDto
                    {
                        Date = currentDate,
                        AverageEngagement = dayEngagement,
                        TotalViews = dayPosts.Sum(p => p.ViewCount),
                        TotalComments = dayPosts.Sum(p => p.CommentCount),
                        TotalShares = dayPosts.Sum(p => p.LikeCount)
                    });

                    currentDate = currentDate.AddDays(1);
                }

                // Get category performance
                var categoryPerformance = await GetCategoryPerformanceAsync(posts);

                // Get tag performance
                var tagPerformance = await GetTagPerformanceAsync(posts);

                // Get author performance
                var authorPerformance = await GetAuthorPerformanceAsync(posts);

                var analytics = new ContentPerformanceAnalyticsDto
                {
                    TotalContent = totalContent,
                    AverageViews = averageViews,
                    AverageComments = averageComments,
                    AverageShares = averageShares,
                    EngagementRate = engagementRate,
                    TopPerformingContent = topPerformingContent,
                    PerformanceTrends = performanceTrends,
                    CategoryPerformance = categoryPerformance,
                    TagPerformance = tagPerformance,
                    AuthorPerformance = authorPerformance
                };

                // Cache the result
                await SetCacheAsync(cacheKey, analytics, ContentPerformanceCacheTime);

                _logger.LogInformation("Content performance analytics calculated for {StartDate} to {EndDate}", startDate, endDate);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating content performance analytics for {StartDate} to {EndDate}", startDate, endDate);
                throw;
            }
        }

        public async Task<IEnumerable<SearchKeywordAnalyticsDto>> GetSearchKeywordAnalyticsAsync(DateTime startDate, DateTime endDate, int limit = 50)
        {
            try
            {
                var cacheKey = $"{SearchKeywordsCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}:{limit}";
                var cachedData = await GetFromCacheAsync<List<SearchKeywordAnalyticsDto>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Search keyword analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating search keyword analytics");

                // Simulate search keywords based on post titles and content
                var posts = await _postRepository.GetPublishedPostsAsync(1, int.MaxValue, null, startDate, endDate);
                var tags = await _tagRepository.GetMostUsedAsync(limit, 1);

                var keywords = new List<SearchKeywordAnalyticsDto>();

                // Generate keywords from tags
                foreach (var tag in tags.Take(limit / 2))
                {
                    keywords.Add(new SearchKeywordAnalyticsDto
                    {
                        Keyword = tag.Name,
                        SearchCount = Random.Shared.Next(50, 500),
                        ResultClicks = Random.Shared.Next(20, 200),
                        ClickThroughRate = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.1, 3),
                        AveragePosition = Math.Round(Random.Shared.NextDouble() * 5 + 1, 1),
                        NoResultSearches = Random.Shared.Next(0, 10),
                        TrendChange = Math.Round((Random.Shared.NextDouble() - 0.5) * 0.4, 3),
                        RelatedKeywords = new[] { $"{tag.Name} tutorial", $"{tag.Name} guide", $"{tag.Name} tips" }
                    });
                }

                // Generate additional common search terms
                var commonTerms = new[] { "blog", "tutorial", "guide", "tips", "how to", "best practices", "introduction", "advanced" };
                foreach (var term in commonTerms.Take(limit - keywords.Count))
                {
                    keywords.Add(new SearchKeywordAnalyticsDto
                    {
                        Keyword = term,
                        SearchCount = Random.Shared.Next(100, 800),
                        ResultClicks = Random.Shared.Next(40, 300),
                        ClickThroughRate = Math.Round(Random.Shared.NextDouble() * 0.4 + 0.2, 3),
                        AveragePosition = Math.Round(Random.Shared.NextDouble() * 3 + 1, 1),
                        NoResultSearches = Random.Shared.Next(0, 5),
                        TrendChange = Math.Round((Random.Shared.NextDouble() - 0.5) * 0.3, 3),
                        RelatedKeywords = new[] { $"{term} examples", $"{term} best", $"{term} latest" }
                    });
                }

                var result = keywords.OrderByDescending(k => k.SearchCount).Take(limit).ToList();

                // Cache the result
                await SetCacheAsync(cacheKey, result, SearchKeywordsCacheTime);

                _logger.LogInformation("Search keyword analytics calculated with {Count} keywords", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating search keyword analytics");
                throw;
            }
        }

        public async Task<IEnumerable<GeographicAnalyticsDto>> GetGeographicAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{GeographicAnalyticsCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<List<GeographicAnalyticsDto>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Geographic analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating geographic analytics");

                // Simulate geographic data based on common countries/regions
                var locations = new List<GeographicAnalyticsDto>
                {
                    new() {
                        CountryCode = "CN", CountryName = "China", Region = "Beijing", City = "Beijing",
                        Visitors = Random.Shared.Next(1000, 3000), PageViews = Random.Shared.Next(5000, 15000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 8)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.2, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.05 + 0.02, 4),
                        Coordinates = new CoordinatesDto { Latitude = 39.9042, Longitude = 116.4074 }
                    },
                    new() {
                        CountryCode = "US", CountryName = "United States", Region = "California", City = "San Francisco",
                        Visitors = Random.Shared.Next(800, 2000), PageViews = Random.Shared.Next(4000, 10000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(4, 9)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.25, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.06 + 0.03, 4),
                        Coordinates = new CoordinatesDto { Latitude = 37.7749, Longitude = -122.4194 }
                    },
                    new() {
                        CountryCode = "JP", CountryName = "Japan", Region = "Tokyo", City = "Tokyo",
                        Visitors = Random.Shared.Next(600, 1500), PageViews = Random.Shared.Next(3000, 8000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(2, 6)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.4 + 0.3, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.04 + 0.015, 4),
                        Coordinates = new CoordinatesDto { Latitude = 35.6762, Longitude = 139.6503 }
                    },
                    new() {
                        CountryCode = "DE", CountryName = "Germany", Region = "Bavaria", City = "Munich",
                        Visitors = Random.Shared.Next(400, 1000), PageViews = Random.Shared.Next(2000, 5000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 7)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.35 + 0.25, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.05 + 0.025, 4),
                        Coordinates = new CoordinatesDto { Latitude = 48.1351, Longitude = 11.5820 }
                    },
                    new() {
                        CountryCode = "GB", CountryName = "United Kingdom", Region = "England", City = "London",
                        Visitors = Random.Shared.Next(500, 1200), PageViews = Random.Shared.Next(2500, 6000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 8)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.3, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.045 + 0.02, 4),
                        Coordinates = new CoordinatesDto { Latitude = 51.5074, Longitude = -0.1278 }
                    }
                };

                var result = locations.OrderByDescending(l => l.Visitors).ToList();

                // Cache the result
                await SetCacheAsync(cacheKey, result, GeographicAnalyticsCacheTime);

                _logger.LogInformation("Geographic analytics calculated for {Count} locations", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating geographic analytics");
                throw;
            }
        }

        public async Task<DeviceBrowserAnalyticsDto> GetDeviceBrowserAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{DeviceBrowserCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<DeviceBrowserAnalyticsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Device browser analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating device browser analytics");

                var analytics = new DeviceBrowserAnalyticsDto
                {
                    DeviceTypes = new List<DeviceTypeStatsDto>
                    {
                        new() { DeviceType = "Desktop", Count = Random.Shared.Next(3000, 5000), Percentage = 0.55 },
                        new() { DeviceType = "Mobile", Count = Random.Shared.Next(2000, 4000), Percentage = 0.35 },
                        new() { DeviceType = "Tablet", Count = Random.Shared.Next(500, 1000), Percentage = 0.10 }
                    },
                    OperatingSystems = new List<OperatingSystemStatsDto>
                    {
                        new() { OperatingSystem = "Windows", Version = "10/11", Count = Random.Shared.Next(2500, 4000), Percentage = 0.45 },
                        new() { OperatingSystem = "macOS", Version = "Sonoma", Count = Random.Shared.Next(1500, 2500), Percentage = 0.25 },
                        new() { OperatingSystem = "iOS", Version = "17+", Count = Random.Shared.Next(1000, 2000), Percentage = 0.20 },
                        new() { OperatingSystem = "Android", Version = "13+", Count = Random.Shared.Next(500, 1000), Percentage = 0.10 }
                    },
                    Browsers = new List<BrowserStatsDto>
                    {
                        new() { Browser = "Chrome", Version = "120+", Count = Random.Shared.Next(3500, 5500), Percentage = 0.60 },
                        new() { Browser = "Safari", Version = "17+", Count = Random.Shared.Next(1200, 2000), Percentage = 0.20 },
                        new() { Browser = "Firefox", Version = "121+", Count = Random.Shared.Next(800, 1500), Percentage = 0.12 },
                        new() { Browser = "Edge", Version = "120+", Count = Random.Shared.Next(400, 800), Percentage = 0.08 }
                    },
                    ScreenResolutions = new List<ScreenResolutionStatsDto>
                    {
                        new() { Resolution = "1920x1080", Count = Random.Shared.Next(2000, 3000), Percentage = 0.35 },
                        new() { Resolution = "1366x768", Count = Random.Shared.Next(1200, 2000), Percentage = 0.25 },
                        new() { Resolution = "1536x864", Count = Random.Shared.Next(800, 1500), Percentage = 0.15 },
                        new() { Resolution = "1440x900", Count = Random.Shared.Next(600, 1200), Percentage = 0.12 },
                        new() { Resolution = "Other", Count = Random.Shared.Next(500, 1000), Percentage = 0.13 }
                    },
                    MobileDevices = new List<MobileDeviceStatsDto>
                    {
                        new() { Brand = "Apple", Model = "iPhone", Count = Random.Shared.Next(1000, 2000), Percentage = 0.40 },
                        new() { Brand = "Samsung", Model = "Galaxy", Count = Random.Shared.Next(800, 1500), Percentage = 0.30 },
                        new() { Brand = "Google", Model = "Pixel", Count = Random.Shared.Next(300, 600), Percentage = 0.15 },
                        new() { Brand = "Xiaomi", Model = "Mi/Redmi", Count = Random.Shared.Next(200, 500), Percentage = 0.10 },
                        new() { Brand = "Other", Model = "Various", Count = Random.Shared.Next(100, 300), Percentage = 0.05 }
                    },
                    NetworkTypes = new List<NetworkTypeStatsDto>
                    {
                        new() { NetworkType = "WiFi", Count = Random.Shared.Next(4000, 6000), Percentage = 0.70 },
                        new() { NetworkType = "4G/LTE", Count = Random.Shared.Next(1500, 2500), Percentage = 0.25 },
                        new() { NetworkType = "5G", Count = Random.Shared.Next(200, 500), Percentage = 0.04 },
                        new() { NetworkType = "3G", Count = Random.Shared.Next(50, 150), Percentage = 0.01 }
                    }
                };

                // Cache the result
                await SetCacheAsync(cacheKey, analytics, DeviceBrowserCacheTime);

                _logger.LogInformation("Device browser analytics calculated");
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating device browser analytics");
                throw;
            }
        }

        public async Task<IEnumerable<TrafficSourceAnalyticsDto>> GetTrafficSourceAnalyticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{TrafficSourceCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<List<TrafficSourceAnalyticsDto>>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Traffic source analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating traffic source analytics");

                var sources = new List<TrafficSourceAnalyticsDto>
                {
                    new() {
                        SourceType = "Direct", SourceName = "Direct Traffic", Medium = "(none)",
                        Visitors = Random.Shared.Next(2000, 4000), Sessions = Random.Shared.Next(2500, 5000),
                        PageViews = Random.Shared.Next(10000, 20000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 8)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.2, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.05 + 0.02, 4),
                        RevenueContribution = Random.Shared.Next(1000, 5000)
                    },
                    new() {
                        SourceType = "Search", SourceName = "Google", Medium = "organic",
                        Visitors = Random.Shared.Next(1500, 3000), Sessions = Random.Shared.Next(2000, 4000),
                        PageViews = Random.Shared.Next(8000, 15000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(4, 9)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.25 + 0.15, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.06 + 0.03, 4),
                        RevenueContribution = Random.Shared.Next(800, 4000)
                    },
                    new() {
                        SourceType = "Social", SourceName = "Social Media", Medium = "social",
                        Visitors = Random.Shared.Next(800, 2000), Sessions = Random.Shared.Next(1000, 2500),
                        PageViews = Random.Shared.Next(4000, 10000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(2, 6)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.4 + 0.3, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.03 + 0.01, 4),
                        RevenueContribution = Random.Shared.Next(300, 2000)
                    },
                    new() {
                        SourceType = "Referral", SourceName = "Referral Sites", Medium = "referral",
                        Visitors = Random.Shared.Next(500, 1500), Sessions = Random.Shared.Next(600, 1800),
                        PageViews = Random.Shared.Next(2000, 6000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 7)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.35 + 0.25, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.04 + 0.02, 4),
                        RevenueContribution = Random.Shared.Next(400, 2500)
                    },
                    new() {
                        SourceType = "Email", SourceName = "Email Marketing", Medium = "email",
                        Visitors = Random.Shared.Next(300, 800), Sessions = Random.Shared.Next(400, 1000),
                        PageViews = Random.Shared.Next(1500, 4000),
                        AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(5, 10)),
                        BounceRate = Math.Round(Random.Shared.NextDouble() * 0.2 + 0.1, 3),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.08 + 0.04, 4),
                        RevenueContribution = Random.Shared.Next(500, 3000)
                    }
                };

                var result = sources.OrderByDescending(s => s.Visitors).ToList();

                // Cache the result
                await SetCacheAsync(cacheKey, result, TrafficSourceCacheTime);

                _logger.LogInformation("Traffic source analytics calculated for {Count} sources", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating traffic source analytics");
                throw;
            }
        }

        public async Task<UserRetentionAnalyticsDto> GetUserRetentionAnalyticsAsync(DateTime cohortDate, string periodType = "weekly")
        {
            try
            {
                var cacheKey = $"{UserRetentionCacheKeyPrefix}:{cohortDate:yyyyMMdd}:{periodType}";
                var cachedData = await GetFromCacheAsync<UserRetentionAnalyticsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("User retention analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating user retention analytics for cohort {CohortDate}", cohortDate);

                // Get users registered in the cohort period
                var cohortSize = await _userRepository.CountAsync(
                    u => u.CreatedAt >= cohortDate && u.CreatedAt < cohortDate.AddDays(1));

                if (cohortSize == 0)
                {
                    cohortSize = 100; // Default for simulation
                }

                // Generate retention periods based on periodType
                var retentionPeriods = new List<RetentionPeriodDto>();
                var periodDays = periodType.ToLower() switch
                {
                    "daily" => 1,
                    "weekly" => 7,
                    "monthly" => 30,
                    _ => 7
                };

                for (int i = 1; i <= 12; i++) // 12 periods
                {
                    var retainedUsers = Math.Max(1, (int)(cohortSize * Math.Pow(0.85, i))); // Simulate decay
                    var retentionRate = Math.Round((double)retainedUsers / cohortSize, 3);

                    retentionPeriods.Add(new RetentionPeriodDto
                    {
                        Period = i,
                        RetainedUsers = retainedUsers,
                        RetentionRate = retentionRate
                    });
                }

                // Calculate average retention rate
                var averageRetentionRate = Math.Round(retentionPeriods.Average(r => r.RetentionRate), 3);

                // Generate retention trends
                var retentionTrends = new List<RetentionTrendDto>();
                var trendStartDate = cohortDate.AddDays(-30);
                for (int i = 0; i < 30; i++)
                {
                    var date = trendStartDate.AddDays(i);
                    var trendRate = 0.4 + (Random.Shared.NextDouble() * 0.3); // 40-70%
                    retentionTrends.Add(new RetentionTrendDto
                    {
                        Date = date,
                        RetentionRate = Math.Round(trendRate, 3)
                    });
                }

                // Generate churn analysis
                var churnAnalysis = new ChurnAnalysisDto
                {
                    ChurnRate = Math.Round(1 - averageRetentionRate, 3),
                    PredictedChurnRate = Math.Round(1 - averageRetentionRate + 0.05, 3),
                    ChurnReasons = new List<ChurnReasonDto>
                    {
                        new() { Reason = "Lack of engagement", Percentage = 0.35 },
                        new() { Reason = "Content not relevant", Percentage = 0.25 },
                        new() { Reason = "Technical issues", Percentage = 0.15 },
                        new() { Reason = "Found alternative", Percentage = 0.15 },
                        new() { Reason = "Other", Percentage = 0.10 }
                    }
                };

                var analytics = new UserRetentionAnalyticsDto
                {
                    CohortDate = cohortDate,
                    CohortSize = cohortSize,
                    RetentionPeriods = retentionPeriods,
                    AverageRetentionRate = averageRetentionRate,
                    RetentionTrends = retentionTrends,
                    ChurnAnalysis = churnAnalysis
                };

                // Cache the result
                await SetCacheAsync(cacheKey, analytics, UserRetentionCacheTime);

                _logger.LogInformation("User retention analytics calculated for cohort {CohortDate} with {CohortSize} users", cohortDate, cohortSize);
                return analytics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating user retention analytics for cohort {CohortDate}", cohortDate);
                throw;
            }
        }

        public async Task<ConversionFunnelDto> GetConversionFunnelAsync(IEnumerable<string> funnelSteps, DateTime startDate, DateTime endDate)
        {
            try
            {
                var stepsArray = funnelSteps.ToArray();
                var cacheKey = $"{ConversionFunnelCacheKeyPrefix}:{string.Join("-", stepsArray)}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<ConversionFunnelDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Conversion funnel analytics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating conversion funnel analytics for {StepCount} steps", stepsArray.Length);

                var funnelName = $"Conversion Funnel - {DateTime.Now:yyyy-MM-dd}";
                var steps = new List<FunnelStepDto>();
                var dropOffPoints = new List<DropOffAnalysisDto>();

                var initialUsers = Random.Shared.Next(5000, 15000);
                var currentUsers = initialUsers;

                for (int i = 0; i < stepsArray.Length; i++)
                {
                    var stepName = stepsArray[i];
                    var dropOffRate = i == 0 ? 0 : Math.Round(Random.Shared.NextDouble() * 0.4 + 0.1, 3); // 10-50% drop-off

                    if (i > 0)
                    {
                        currentUsers = (int)(currentUsers * (1 - dropOffRate));
                    }

                    var conversionRate = Math.Round((double)currentUsers / initialUsers, 3);

                    steps.Add(new FunnelStepDto
                    {
                        StepOrder = i + 1,
                        StepName = stepName,
                        UserCount = currentUsers,
                        ConversionRate = conversionRate,
                        DropOffRate = dropOffRate
                    });

                    // Add drop-off analysis for steps with significant drop-off
                    if (dropOffRate > 0.2)
                    {
                        dropOffPoints.Add(new DropOffAnalysisDto
                        {
                            StepName = stepName,
                            DropOffCount = (int)(currentUsers / (1 - dropOffRate) * dropOffRate),
                            DropOffRate = dropOffRate,
                            PossibleReasons = new[] { "Complex process", "Technical issues", "Lack of motivation", "Alternative found" }
                        });
                    }
                }

                var overallConversionRate = steps.Any() ? steps.Last().ConversionRate : 0;
                var averageCompletionTime = TimeSpan.FromMinutes(Random.Shared.Next(5, 30));

                var funnel = new ConversionFunnelDto
                {
                    FunnelName = funnelName,
                    Steps = steps,
                    OverallConversionRate = overallConversionRate,
                    AverageCompletionTime = averageCompletionTime,
                    DropOffPoints = dropOffPoints
                };

                // Cache the result
                await SetCacheAsync(cacheKey, funnel, ConversionFunnelCacheTime);

                _logger.LogInformation("Conversion funnel analytics calculated with {StepCount} steps, overall conversion: {Rate:P}", stepsArray.Length, overallConversionRate);
                return funnel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating conversion funnel analytics");
                throw;
            }
        }

        public async Task<ABTestResultDto> GetABTestResultAsync(Guid testId)
        {
            try
            {
                var cacheKey = $"{ABTestResultCacheKeyPrefix}:{testId}";
                var cachedData = await GetFromCacheAsync<ABTestResultDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("A/B test result retrieved from cache for test {TestId}", testId);
                    return cachedData;
                }

                _logger.LogDebug("Calculating A/B test result for test {TestId}", testId);

                var testName = $"A/B Test {testId.ToString()[..8]}";
                var status = Random.Shared.Next(1, 4) switch
                {
                    1 => "Running",
                    2 => "Completed",
                    3 => "Paused",
                    _ => "Draft"
                };

                var startDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(7, 30));
                var endDate = status == "Completed" ? startDate.AddDays(Random.Shared.Next(7, 21)) : (DateTime?)null;

                // Generate variant results
                var variants = new List<TestVariantResultDto>
                {
                    new() {
                        VariantName = "Control (A)",
                        ParticipantCount = Random.Shared.Next(1000, 3000),
                        ConversionCount = Random.Shared.Next(50, 200),
                        ConversionRate = 0,
                        ConfidenceInterval = 0.95
                    },
                    new() {
                        VariantName = "Variant (B)",
                        ParticipantCount = Random.Shared.Next(1000, 3000),
                        ConversionCount = Random.Shared.Next(60, 250),
                        ConversionRate = 0,
                        ConfidenceInterval = 0.95
                    }
                };

                // Calculate conversion rates
                foreach (var variant in variants)
                {
                    variant.ConversionRate = Math.Round((double)variant.ConversionCount / variant.ParticipantCount, 4);
                }

                // Determine winner
                var winnerVariant = variants.OrderByDescending(v => v.ConversionRate).First();
                var statisticalSignificance = Math.Round(Random.Shared.NextDouble() * 0.3 + 0.7, 3); // 70-100%
                var confidenceInterval = 0.95;

                var testResult = new ABTestResultDto
                {
                    TestId = testId,
                    TestName = testName,
                    Status = status,
                    StartDate = startDate,
                    EndDate = endDate,
                    Variants = variants,
                    WinnerVariant = winnerVariant.VariantName,
                    StatisticalSignificance = statisticalSignificance,
                    ConfidenceInterval = confidenceInterval
                };

                // Cache the result
                await SetCacheAsync(cacheKey, testResult, ABTestResultCacheTime);

                _logger.LogInformation("A/B test result calculated for test {TestId}: Winner is {Winner} with {Significance:P} confidence",
                    testId, winnerVariant.VariantName, statisticalSignificance);
                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating A/B test result for test {TestId}", testId);
                throw;
            }
        }

        public async Task<CustomReportDto> GenerateCustomReportAsync(CustomReportRequestDto reportRequest)
        {
            try
            {
                var cacheKey = $"{CustomReportCacheKeyPrefix}:{reportRequest.ReportType}:{reportRequest.DateRange.StartDate:yyyyMMdd}:{reportRequest.DateRange.EndDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<CustomReportDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Custom report retrieved from cache for {ReportType}", reportRequest.ReportType);
                    return cachedData;
                }

                _logger.LogDebug("Generating custom report for {ReportType}", reportRequest.ReportType);

                var reportId = Guid.NewGuid();
                var generatedAt = DateTime.UtcNow;

                // Generate report data based on request
                var reportData = new
                {
                    TotalRecords = Random.Shared.Next(1000, 10000),
                    ProcessedAt = generatedAt,
                    DataSources = reportRequest.DataSources,
                    AppliedFilters = reportRequest.Filters,
                    Summary = new
                    {
                        TotalViews = Random.Shared.Next(50000, 200000),
                        UniqueUsers = Random.Shared.Next(5000, 25000),
                        ConversionRate = Math.Round(Random.Shared.NextDouble() * 0.05 + 0.02, 4),
                        Revenue = Random.Shared.Next(10000, 50000)
                    }
                };

                // Generate charts
                var charts = new List<ChartDataDto>
                {
                    new() {
                        ChartType = "line",
                        Title = "Traffic Trends",
                        Data = Enumerable.Range(1, 30).Select(i => new { Day = i, Views = Random.Shared.Next(100, 1000) }),
                        Options = new { responsive = true, maintainAspectRatio = false }
                    },
                    new() {
                        ChartType = "pie",
                        Title = "Traffic Sources",
                        Data = new[] {
                            new { Source = "Direct", Percentage = 40 },
                            new { Source = "Search", Percentage = 35 },
                            new { Source = "Social", Percentage = 15 },
                            new { Source = "Other", Percentage = 10 }
                        },
                        Options = new { responsive = true }
                    }
                };

                // Generate key metrics
                var keyMetrics = new List<KeyMetricDto>
                {
                    new() { Name = "Total Page Views", Value = Random.Shared.Next(100000, 500000), Unit = "views", ChangePercent = Random.Shared.NextDouble() * 20 - 10, Trend = "up" },
                    new() { Name = "Unique Visitors", Value = Random.Shared.Next(10000, 50000), Unit = "visitors", ChangePercent = Random.Shared.NextDouble() * 15 - 7.5, Trend = "stable" },
                    new() { Name = "Conversion Rate", Value = Math.Round(Random.Shared.NextDouble() * 0.05 + 0.02, 4), Unit = "%", ChangePercent = Random.Shared.NextDouble() * 10 - 5, Trend = "up" },
                    new() { Name = "Average Session Duration", Value = $"{Random.Shared.Next(2, 8)}m {Random.Shared.Next(10, 59)}s", Unit = "time", ChangePercent = Random.Shared.NextDouble() * 8 - 4, Trend = "down" }
                };

                // Generate insights
                var insights = new List<InsightDto>
                {
                    new() {
                        Type = "Performance",
                        Title = "Traffic Growth Opportunity",
                        Description = "Mobile traffic has increased by 25% this month.",
                        Recommendation = "Consider optimizing mobile experience to capture more conversions.",
                        Priority = 1
                    },
                    new() {
                        Type = "Content",
                        Title = "Top Performing Content",
                        Description = "Blog posts about technology trends are generating 40% more engagement.",
                        Recommendation = "Create more content in this category to maintain momentum.",
                        Priority = 2
                    },
                    new() {
                        Type = "User Behavior",
                        Title = "Session Duration Decline",
                        Description = "Average session duration has decreased by 8% compared to last month.",
                        Recommendation = "Review page load times and content quality to improve user engagement.",
                        Priority = 3
                    }
                };

                var report = new CustomReportDto
                {
                    ReportId = reportId,
                    ReportName = reportRequest.ReportName,
                    GeneratedAt = generatedAt,
                    DateRange = reportRequest.DateRange,
                    ReportData = reportData,
                    Charts = charts,
                    KeyMetrics = keyMetrics,
                    Insights = insights
                };

                // Cache the result
                await SetCacheAsync(cacheKey, report, CustomReportCacheTime);

                _logger.LogInformation("Custom report {ReportName} generated with ID {ReportId}", reportRequest.ReportName, reportId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report {ReportName}", reportRequest.ReportName);
                throw;
            }
        }

        public async Task<ExportResultDto> ExportAnalyticsDataAsync(ExportRequestDto exportRequest)
        {
            try
            {
                _logger.LogDebug("Starting analytics data export in {Format} format", exportRequest.ExportFormat);

                var exportId = Guid.NewGuid();
                var fileName = $"analytics_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{exportRequest.ExportFormat.ToLower()}";
                var generatedAt = DateTime.UtcNow;
                var expiresAt = generatedAt.AddHours(24); // Export files expire after 24 hours

                // Simulate data processing
                await Task.Delay(100); // Simulate processing time

                var recordCount = Random.Shared.Next(1000, 10000);
                var fileSize = recordCount * Random.Shared.Next(100, 500); // Simulate file size

                // Generate download URL (in real implementation, this would be a cloud storage URL)
                var downloadUrl = $"/api/analytics/exports/{exportId}/download";

                var exportResult = new ExportResultDto
                {
                    ExportId = exportId,
                    FileName = fileName,
                    FileSize = fileSize,
                    DownloadUrl = downloadUrl,
                    ExpiresAt = expiresAt,
                    Status = "Completed",
                    RecordCount = recordCount,
                    GeneratedAt = generatedAt
                };

                _logger.LogInformation("Analytics data export completed: {FileName} with {RecordCount} records ({FileSize} bytes)",
                    fileName, recordCount, fileSize);
                return exportResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting analytics data in {Format} format", exportRequest.ExportFormat);
                throw;
            }
        }

        public async Task<RealTimeVisitorDto> GetRealTimeVisitorsAsync()
        {
            try
            {
                var cachedData = await GetFromCacheAsync<RealTimeVisitorDto>(RealTimeVisitorsCacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Real-time visitors data retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating real-time visitors data");

                var currentVisitors = Random.Shared.Next(50, 200);
                var recentPosts = await _postRepository.GetMostPopularAsync(1, 5, 1); // Today's popular posts

                var realTimeData = new RealTimeVisitorDto
                {
                    CurrentVisitors = currentVisitors,
                    ActivePages = recentPosts.Select(p => new ActivePageDto
                    {
                        PagePath = $"/posts/{p.Slug}",
                        PageTitle = p.Title,
                        ActiveVisitors = Random.Shared.Next(1, 20)
                    }).ToList(),
                    VisitorLocations = new List<VisitorLocationDto>
                    {
                        new() { Country = "China", City = "Beijing", VisitorCount = Random.Shared.Next(10, 50) },
                        new() { Country = "United States", City = "New York", VisitorCount = Random.Shared.Next(8, 30) },
                        new() { Country = "Japan", City = "Tokyo", VisitorCount = Random.Shared.Next(5, 25) },
                        new() { Country = "Germany", City = "Berlin", VisitorCount = Random.Shared.Next(3, 15) },
                        new() { Country = "United Kingdom", City = "London", VisitorCount = Random.Shared.Next(4, 20) }
                    },
                    TrafficSources = new List<RealTimeTrafficSourceDto>
                    {
                        new() { Source = "Direct", VisitorCount = Random.Shared.Next(15, 60) },
                        new() { Source = "Google", VisitorCount = Random.Shared.Next(10, 40) },
                        new() { Source = "Social Media", VisitorCount = Random.Shared.Next(5, 30) },
                        new() { Source = "Referral", VisitorCount = Random.Shared.Next(3, 20) }
                    },
                    RecentVisits = Enumerable.Range(1, 10).Select(i => new RecentVisitDto
                    {
                        PagePath = $"/posts/example-post-{i}",
                        VisitorLocation = $"City {i}",
                        Timestamp = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 30))
                    }).ToList(),
                    LastUpdated = DateTime.UtcNow
                };

                // Cache for a very short time since it's real-time data
                await SetCacheAsync(RealTimeVisitorsCacheKey, realTimeData, RealTimeVisitorsCacheTime);

                _logger.LogInformation("Real-time visitors data calculated: {CurrentVisitors} active", currentVisitors);
                return realTimeData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating real-time visitors data");
                throw;
            }
        }

        public async Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{PerformanceMetricsCacheKeyPrefix}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<PerformanceMetricsDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Performance metrics retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Calculating performance metrics");

                // Simulate performance metrics
                var metrics = new PerformanceMetricsDto
                {
                    AveragePageLoadTime = Math.Round(1.2 + Random.Shared.NextDouble() * 0.8, 2), // 1.2-2.0s
                    FirstContentfulPaint = Math.Round(0.8 + Random.Shared.NextDouble() * 0.6, 2), // 0.8-1.4s
                    LargestContentfulPaint = Math.Round(1.5 + Random.Shared.NextDouble() * 1.0, 2), // 1.5-2.5s
                    CumulativeLayoutShift = Math.Round(Random.Shared.NextDouble() * 0.1, 3), // 0-0.1
                    FirstInputDelay = Math.Round(Random.Shared.NextDouble() * 100, 1), // 0-100ms
                    TimeToInteractive = Math.Round(2.0 + Random.Shared.NextDouble() * 1.5, 2), // 2.0-3.5s
                    PerformanceScore = Random.Shared.Next(75, 100), // 75-100
                    SpeedIndex = Math.Round(1.8 + Random.Shared.NextDouble() * 1.0, 2), // 1.8-2.8s
                    PagePerformance = new List<PagePerformanceDto>
                    {
                        new() { PagePath = "/", LoadTime = 1.1, PerformanceScore = 95 },
                        new() { PagePath = "/blog", LoadTime = 1.3, PerformanceScore = 92 },
                        new() { PagePath = "/posts/*", LoadTime = 1.5, PerformanceScore = 88 },
                        new() { PagePath = "/archive", LoadTime = 1.7, PerformanceScore = 85 },
                        new() { PagePath = "/search", LoadTime = 2.1, PerformanceScore = 80 }
                    },
                    DevicePerformance = new List<DevicePerformanceDto>
                    {
                        new() { DeviceType = "Desktop", AverageLoadTime = 1.2, PerformanceScore = 92 },
                        new() { DeviceType = "Mobile", AverageLoadTime = 1.8, PerformanceScore = 85 },
                        new() { DeviceType = "Tablet", AverageLoadTime = 1.5, PerformanceScore = 88 }
                    }
                };

                // Cache the result
                await SetCacheAsync(cacheKey, metrics, PerformanceMetricsCacheTime);

                _logger.LogInformation("Performance metrics calculated with average load time: {LoadTime}s", metrics.AveragePageLoadTime);
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance metrics");
                throw;
            }
        }

        public async Task<GoalCompletionDto> GetGoalCompletionAsync(Guid goalId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"{GoalCompletionCacheKeyPrefix}:{goalId}:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                var cachedData = await GetFromCacheAsync<GoalCompletionDto>(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogDebug("Goal completion analytics retrieved from cache for goal {GoalId}", goalId);
                    return cachedData;
                }

                _logger.LogDebug("Calculating goal completion analytics for goal {GoalId}", goalId);

                // Simulate goal completion data
                var goalName = $"Goal {goalId.ToString()[..8]}";
                var goalType = Random.Shared.Next(1, 4) switch
                {
                    1 => "Registration",
                    2 => "Engagement",
                    3 => "Conversion",
                    _ => "Retention"
                };

                var totalVisitors = Random.Shared.Next(5000, 15000);
                var completions = Random.Shared.Next(100, 1000);
                var completionRate = Math.Round((double)completions / totalVisitors, 4);
                var goalValue = Random.Shared.Next(10, 100);

                // Generate completion trends
                var completionTrends = new List<GoalCompletionTrendDto>();
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var dailyCompletions = Random.Shared.Next(5, 50);
                    var dailyRate = Math.Round(Random.Shared.NextDouble() * 0.1 + 0.02, 4);

                    completionTrends.Add(new GoalCompletionTrendDto
                    {
                        Date = currentDate,
                        Completions = dailyCompletions,
                        CompletionRate = dailyRate
                    });

                    currentDate = currentDate.AddDays(1);
                }

                // Generate completion paths
                var completionPaths = new List<GoalPathDto>
                {
                    new() { PathSequence = "Home -> Product -> Signup", CompletionCount = Random.Shared.Next(100, 300), ConversionRate = 0.85 },
                    new() { PathSequence = "Search -> Product -> Signup", CompletionCount = Random.Shared.Next(50, 200), ConversionRate = 0.70 },
                    new() { PathSequence = "Blog -> Product -> Signup", CompletionCount = Random.Shared.Next(30, 150), ConversionRate = 0.60 },
                    new() { PathSequence = "Social -> Product -> Signup", CompletionCount = Random.Shared.Next(20, 100), ConversionRate = 0.45 }
                };

                var goalCompletion = new GoalCompletionDto
                {
                    GoalId = goalId,
                    GoalName = goalName,
                    GoalType = goalType,
                    Completions = completions,
                    CompletionRate = completionRate,
                    GoalValue = goalValue,
                    CompletionTrends = completionTrends,
                    CompletionPaths = completionPaths
                };

                // Cache the result
                await SetCacheAsync(cacheKey, goalCompletion, GoalCompletionCacheTime);

                _logger.LogInformation("Goal completion analytics calculated for goal {GoalId}: {Completions} completions", goalId, completions);
                return goalCompletion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating goal completion analytics for goal {GoalId}", goalId);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(cachedValue))
                {
                    return JsonSerializer.Deserialize<T>(cachedValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving from cache: {Key}", key);
            }

            return null;
        }

        private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiry)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                };

                var serializedValue = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedValue, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting cache: {Key}", key);
            }
        }

        private async Task<IEnumerable<DailyVisitStatsDto>> GetDailyVisitTrendsAsync(DateTime startDate, DateTime endDate)
        {
            var trends = new List<DailyVisitStatsDto>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                // Get posts published on this day
                var dayPostViews = await _postRepository.GetTotalViewsAsync(
                    p => p.Status == PostStatus.Published &&
                         (p.PublishedAt ?? p.CreatedAt).Date == currentDate);

                // Simulate unique visitors based on views
                var uniqueVisitors = (int)(dayPostViews * 0.6);
                var newVisitors = (int)(uniqueVisitors * 0.4);
                var returningVisitors = uniqueVisitors - newVisitors;

                trends.Add(new DailyVisitStatsDto
                {
                    Date = currentDate,
                    TotalViews = dayPostViews,
                    UniqueVisitors = uniqueVisitors,
                    PageViews = dayPostViews + Random.Shared.Next(100, 500), // Add some non-post page views
                    BounceRate = 0.3 + (Random.Shared.NextDouble() * 0.3),
                    AverageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(2, 8))
                });

                currentDate = currentDate.AddDays(1);
            }

            return trends;
        }

        private async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync(IEnumerable<dynamic> posts)
        {
            var categories = await _categoryRepository.GetAllAsync();
            var categoryPerformance = new List<CategoryPerformanceDto>();

            foreach (var category in categories)
            {
                var categoryPosts = await _postRepository.GetByCategoryAsync(category.Id, 1, int.MaxValue);
                var totalViews = categoryPosts.Sum(p => p.ViewCount);
                var avgEngagement = categoryPosts.Any() ?
                    Math.Round((double)categoryPosts.Sum(p => p.CommentCount + p.LikeCount) / Math.Max(1, categoryPosts.Sum(p => p.ViewCount)), 4) : 0;

                if (categoryPosts.Any())
                {
                    categoryPerformance.Add(new CategoryPerformanceDto
                    {
                        CategoryId = category.Id,
                        CategoryName = category.Name,
                        ContentCount = categoryPosts.Count(),
                        TotalViews = totalViews,
                        AverageEngagement = avgEngagement
                    });
                }
            }

            return categoryPerformance.OrderByDescending(c => c.TotalViews).Take(10);
        }

        private async Task<IEnumerable<TagPerformanceDto>> GetTagPerformanceAsync(IEnumerable<dynamic> posts)
        {
            var tags = await _tagRepository.GetMostUsedAsync(20, 1);
            var tagPerformance = new List<TagPerformanceDto>();

            foreach (var tag in tags)
            {
                var tagPosts = await _postRepository.GetByTagAsync(tag.Id, 1, int.MaxValue);
                var totalViews = tagPosts.Sum(p => p.ViewCount);
                var popularityScore = tagPosts.Any() ?
                    Math.Round((double)totalViews / tagPosts.Count() * tag.PostTags.Count, 2) : 0;

                tagPerformance.Add(new TagPerformanceDto
                {
                    TagId = tag.Id,
                    TagName = tag.Name,
                    UsageCount = tag.PostTags.Count,
                    TotalViews = totalViews,
                    PopularityScore = popularityScore
                });
            }

            return tagPerformance.OrderByDescending(t => t.PopularityScore).Take(10);
        }

        private async Task<IEnumerable<AuthorPerformanceDto>> GetAuthorPerformanceAsync(IEnumerable<dynamic> posts)
        {
            var authors = await _userRepository.GetActiveAuthorsAsync(20);
            var authorPerformance = new List<AuthorPerformanceDto>();

            foreach (var author in authors)
            {
                var authorPosts = await _postRepository.GetByAuthorAsync(author.Id, 1, int.MaxValue);
                var totalViews = authorPosts.Sum(p => p.ViewCount);
                var totalComments = authorPosts.Sum(p => p.CommentCount);
                var avgEngagement = authorPosts.Any() ?
                    Math.Round((double)totalComments / Math.Max(1, totalViews), 4) : 0;

                if (authorPosts.Any())
                {
                    authorPerformance.Add(new AuthorPerformanceDto
                    {
                        AuthorId = author.Id,
                        AuthorName = author.DisplayName ?? author.UserName,
                        ContentCount = authorPosts.Count(),
                        TotalViews = totalViews,
                        AverageEngagement = avgEngagement,
                        TotalComments = totalComments
                    });
                }
            }

            return authorPerformance.OrderByDescending(a => a.TotalViews).Take(10);
        }

        #endregion
    }
}