using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Home service implementation for data aggregation with caching
    /// </summary>
    public class HomeService : IHomeService
    {
        private readonly IStatsService _statsService;
        private readonly IRecommendationService _recommendationService;
        private readonly Domain.Interfaces.IPostRepository _postRepository;
        private readonly Domain.Interfaces.ICategoryRepository _categoryRepository;
        private readonly Domain.Interfaces.ITagRepository _tagRepository;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<HomeService> _logger;

        // Cache keys
        private const string AnonymousHomePageCacheKey = "home:anonymous";
        private const string PersonalizedHomePageCacheKey = "home:personalized";
        private const string FeaturedPostsCacheKey = "home:featured-posts";
        private const string LatestPostsCacheKey = "home:latest-posts";
        private const string PopularPostsCacheKey = "home:popular-posts";

        // Cache expiry times
        private static readonly TimeSpan AnonymousHomePageCacheTime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan PersonalizedHomePageCacheTime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan FeaturedPostsCacheTime = TimeSpan.FromHours(1);
        private static readonly TimeSpan LatestPostsCacheTime = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan PopularPostsCacheTime = TimeSpan.FromMinutes(30);

        public HomeService(
            IStatsService statsService,
            IRecommendationService recommendationService,
            IPostRepository postRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IDistributedCache cache,
            IMapper mapper,
            ILogger<HomeService> logger)
        {
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _recommendationService = recommendationService ?? throw new ArgumentNullException(nameof(recommendationService));
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HomePageDto> GetHomePageDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get from cache first
                var cachedData = await GetFromCacheAsync<HomePageDto>(AnonymousHomePageCacheKey, cancellationToken);
                if (cachedData != null)
                {
                    _logger.LogDebug("Anonymous home page data retrieved from cache");
                    return cachedData;
                }

                _logger.LogDebug("Aggregating anonymous home page data");

                // Execute all data fetching tasks in parallel for optimal performance
                var tasks = new List<Task>
                {
                    GetFeaturedPostsAsync(5, cancellationToken),
                    GetLatestPostsAsync(10, cancellationToken),
                    GetPopularPostsAsync(10, 30, cancellationToken),
                    _statsService.GetCategoryStatsAsync(false, cancellationToken),
                    _statsService.GetTagStatsAsync(20, 1, cancellationToken),
                    _statsService.GetSiteStatsAsync(cancellationToken),
                    _statsService.GetActiveAuthorsAsync(8, cancellationToken),
                    _recommendationService.GetAnonymousRecommendationsAsync(8, cancellationToken)
                };

                await Task.WhenAll(tasks);

                // Extract results from completed tasks
                var featuredPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[0];
                var latestPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[1];
                var popularPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[2];
                var categories = await (Task<IReadOnlyList<CategorySummaryDto>>)tasks[3];
                var tags = await (Task<IReadOnlyList<TagSummaryDto>>)tasks[4];
                var siteStats = await (Task<SiteStatsDto>)tasks[5];
                var activeAuthors = await (Task<IReadOnlyList<AuthorSummaryDto>>)tasks[6];
                var recommendations = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[7];

                var homePageData = new HomePageDto
                {
                    FeaturedPosts = featuredPosts,
                    LatestPosts = latestPosts,
                    PopularPosts = popularPosts,
                    Categories = categories,
                    PopularTags = tags,
                    SiteStats = siteStats,
                    ActiveAuthors = activeAuthors,
                    RecommendedPosts = null, // No personalization for anonymous users
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(AnonymousHomePageCacheTime)
                };

                // Cache the result
                await SetCacheAsync(AnonymousHomePageCacheKey, homePageData, AnonymousHomePageCacheTime, cancellationToken);

                _logger.LogInformation("Anonymous home page data aggregated and cached");
                return homePageData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating anonymous home page data");
                throw;
            }
        }

        public async Task<HomePageDto> GetPersonalizedHomePageDataAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{PersonalizedHomePageCacheKey}:{userId}";
                var cachedData = await GetFromCacheAsync<HomePageDto>(cacheKey, cancellationToken);
                if (cachedData != null)
                {
                    _logger.LogDebug("Personalized home page data retrieved from cache for user {UserId}", userId);
                    return cachedData;
                }

                _logger.LogDebug("Aggregating personalized home page data for user {UserId}", userId);

                // Execute all data fetching tasks in parallel
                var tasks = new List<Task>
                {
                    GetFeaturedPostsAsync(5, cancellationToken),
                    GetLatestPostsAsync(10, cancellationToken),
                    GetPopularPostsAsync(10, 30, cancellationToken),
                    _statsService.GetCategoryStatsAsync(false, cancellationToken),
                    _statsService.GetTagStatsAsync(20, 1, cancellationToken),
                    _statsService.GetSiteStatsAsync(cancellationToken),
                    _statsService.GetActiveAuthorsAsync(8, cancellationToken),
                    _recommendationService.GetPersonalizedRecommendationsAsync(userId, 10, cancellationToken)
                };

                await Task.WhenAll(tasks);

                // Extract results from completed tasks
                var featuredPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[0];
                var latestPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[1];
                var popularPosts = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[2];
                var categories = await (Task<IReadOnlyList<CategorySummaryDto>>)tasks[3];
                var tags = await (Task<IReadOnlyList<TagSummaryDto>>)tasks[4];
                var siteStats = await (Task<SiteStatsDto>)tasks[5];
                var activeAuthors = await (Task<IReadOnlyList<AuthorSummaryDto>>)tasks[6];
                var personalizedRecommendations = await (Task<IReadOnlyList<PostSummaryDto>>)tasks[7];

                var homePageData = new HomePageDto
                {
                    FeaturedPosts = featuredPosts,
                    LatestPosts = latestPosts,
                    PopularPosts = popularPosts,
                    Categories = categories,
                    PopularTags = tags,
                    SiteStats = siteStats,
                    ActiveAuthors = activeAuthors,
                    RecommendedPosts = personalizedRecommendations,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(PersonalizedHomePageCacheTime)
                };

                // Cache the result with shorter expiry for personalized data
                await SetCacheAsync(cacheKey, homePageData, PersonalizedHomePageCacheTime, cancellationToken);

                _logger.LogInformation("Personalized home page data aggregated and cached for user {UserId}", userId);
                return homePageData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating personalized home page data for user {UserId}", userId);
                // Fallback to anonymous data
                return await GetHomePageDataAsync(cancellationToken);
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetFeaturedPostsAsync(int count = 5, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{FeaturedPostsCacheKey}:{count}";
                var cachedPosts = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedPosts != null)
                {
                    _logger.LogDebug("Featured posts retrieved from cache");
                    return cachedPosts;
                }

                var featuredPosts = await _postRepository.GetFeaturedAsync(1, count, true, cancellationToken);
                var postDtos = _mapper.Map<List<PostSummaryDto>>(featuredPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, postDtos, FeaturedPostsCacheTime, cancellationToken);

                _logger.LogInformation("Featured posts retrieved and cached: {Count}", postDtos.Count);
                return postDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured posts");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetLatestPostsAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{LatestPostsCacheKey}:{count}";
                var cachedPosts = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedPosts != null)
                {
                    _logger.LogDebug("Latest posts retrieved from cache");
                    return cachedPosts;
                }

                var latestPosts = await _postRepository.GetPublishedAsync(1, count, true, cancellationToken);
                var postDtos = _mapper.Map<List<PostSummaryDto>>(latestPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, postDtos, LatestPostsCacheTime, cancellationToken);

                _logger.LogInformation("Latest posts retrieved and cached: {Count}", postDtos.Count);
                return postDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest posts");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetPopularPostsAsync(int count = 10, int daysBack = 30, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{PopularPostsCacheKey}:{count}:{daysBack}";
                var cachedPosts = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedPosts != null)
                {
                    _logger.LogDebug("Popular posts retrieved from cache");
                    return cachedPosts;
                }

                var popularPosts = await _postRepository.GetMostPopularAsync(1, count, daysBack, cancellationToken);
                var postDtos = _mapper.Map<List<PostSummaryDto>>(popularPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, postDtos, PopularPostsCacheTime, cancellationToken);

                _logger.LogInformation("Popular posts retrieved and cached: {Count}", postDtos.Count);
                return postDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving popular posts");
                return new List<PostSummaryDto>();
            }
        }

        public async Task RefreshHomePageCacheAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting home page cache refresh");

                // Remove all home page caches
                await _cache.RemoveAsync(AnonymousHomePageCacheKey, cancellationToken);

                // Remove personalized caches (in production, use pattern-based deletion)
                // await InvalidateCachePatternAsync($"{PersonalizedHomePageCacheKey}:*");

                // Remove component caches
                await _cache.RemoveAsync($"{FeaturedPostsCacheKey}:5", cancellationToken);
                await _cache.RemoveAsync($"{LatestPostsCacheKey}:10", cancellationToken);
                await _cache.RemoveAsync($"{PopularPostsCacheKey}:10:30", cancellationToken);

                // Refresh underlying service caches
                await _statsService.RefreshCachedStatsAsync(cancellationToken);
                await _recommendationService.RefreshRecommendationModelAsync(cancellationToken);

                // Preload fresh anonymous data
                await GetHomePageDataAsync(cancellationToken);

                _logger.LogInformation("Home page cache refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing home page cache");
                throw;
            }
        }

        #region Private Methods

        private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken) where T : class
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync(key, cancellationToken);
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

        private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                };

                var serializedValue = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting cache: {Key}", key);
            }
        }

        #endregion
    }
}