using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// Recommendation service with collaborative filtering and content-based algorithms
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly IPostRepository _postRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserInteractionRepository _userInteractionRepository;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<RecommendationService> _logger;

        // Cache keys
        private const string PersonalizedRecommendationsCacheKey = "recommendations:personalized";
        private const string AnonymousRecommendationsCacheKey = "recommendations:anonymous";
        private const string RelatedPostsCacheKey = "recommendations:related";
        private const string TrendingRecommendationsCacheKey = "recommendations:trending";
        private const string UserPreferencesCacheKey = "recommendations:preferences";

        // Cache expiry times
        private static readonly TimeSpan PersonalizedCacheTime = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan AnonymousCacheTime = TimeSpan.FromHours(1);
        private static readonly TimeSpan RelatedPostsCacheTime = TimeSpan.FromHours(2);
        private static readonly TimeSpan TrendingCacheTime = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan UserPreferencesCacheTime = TimeSpan.FromHours(6);

        // Recommendation weights
        private const double CategoryWeight = 0.3;
        private const double TagWeight = 0.2;
        private const double AuthorWeight = 0.2;
        private const double PopularityWeight = 0.15;
        private const double RecencyWeight = 0.15;

        public RecommendationService(
            IPostRepository postRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IUserInteractionRepository userInteractionRepository,
            IDistributedCache cache,
            IMapper mapper,
            ILogger<RecommendationService> logger)
        {
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _userInteractionRepository = userInteractionRepository ?? throw new ArgumentNullException(nameof(userInteractionRepository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetPersonalizedRecommendationsAsync(
            Guid userId,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{PersonalizedRecommendationsCacheKey}:{userId}:{count}";
                var cachedRecommendations = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedRecommendations != null)
                {
                    _logger.LogDebug("Personalized recommendations retrieved from cache for user {UserId}", userId);
                    return cachedRecommendations;
                }

                _logger.LogDebug("Generating personalized recommendations for user {UserId}", userId);

                // Get user preferences
                var userPreferences = await GetOrCreateUserPreferencesAsync(userId, cancellationToken);

                // Get candidate posts
                var candidatePosts = await GetCandidatePostsAsync(userId, cancellationToken);

                // Calculate recommendation scores
                var scoredPosts = await CalculatePersonalizedScoresAsync(candidatePosts, userPreferences, userId, cancellationToken);

                // Sort by score and take top N
                var recommendations = scoredPosts
                    .OrderByDescending(p => p.Score)
                    .Take(count)
                    .Select(p => p.Post)
                    .ToList();

                var recommendationDtos = _mapper.Map<List<PostSummaryDto>>(recommendations);

                // Cache the result
                await SetCacheAsync(cacheKey, recommendationDtos, PersonalizedCacheTime, cancellationToken);

                _logger.LogInformation("Generated {Count} personalized recommendations for user {UserId}", recommendationDtos.Count, userId);
                return recommendationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personalized recommendations for user {UserId}", userId);
                // Fallback to anonymous recommendations
                return await GetAnonymousRecommendationsAsync(count, cancellationToken);
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetAnonymousRecommendationsAsync(
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{AnonymousRecommendationsCacheKey}:{count}";
                var cachedRecommendations = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedRecommendations != null)
                {
                    _logger.LogDebug("Anonymous recommendations retrieved from cache");
                    return cachedRecommendations;
                }

                _logger.LogDebug("Generating anonymous recommendations");

                // Mix of featured, popular, and recent posts
                var featuredPosts = await _postRepository.GetFeaturedAsync(1, count / 3, true, cancellationToken);
                var popularPosts = await _postRepository.GetMostPopularAsync(1, count / 3, 30, cancellationToken);
                var recentPosts = await _postRepository.GetRecentAsync(1, count / 3, true, cancellationToken);

                // Combine and deduplicate
                var allPosts = featuredPosts
                    .Concat(popularPosts)
                    .Concat(recentPosts)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .Take(count)
                    .ToList();

                var recommendationDtos = _mapper.Map<List<PostSummaryDto>>(allPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, recommendationDtos, AnonymousCacheTime, cancellationToken);

                _logger.LogInformation("Generated {Count} anonymous recommendations", recommendationDtos.Count);
                return recommendationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating anonymous recommendations");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetRelatedPostRecommendationsAsync(
            Guid postId,
            Guid? userId = null,
            int count = 5,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{RelatedPostsCacheKey}:{postId}:{userId}:{count}";
                var cachedRecommendations = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedRecommendations != null)
                {
                    _logger.LogDebug("Related post recommendations retrieved from cache for post {PostId}", postId);
                    return cachedRecommendations;
                }

                _logger.LogDebug("Generating related post recommendations for post {PostId}", postId);

                // Use existing related posts functionality from repository
                var relatedPosts = await _postRepository.GetRelatedAsync(postId, count, cancellationToken);
                var recommendationDtos = _mapper.Map<List<PostSummaryDto>>(relatedPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, recommendationDtos, RelatedPostsCacheTime, cancellationToken);

                _logger.LogInformation("Generated {Count} related post recommendations for post {PostId}", recommendationDtos.Count, postId);
                return recommendationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating related post recommendations for post {PostId}", postId);
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetCategoryBasedRecommendationsAsync(
            IEnumerable<Guid> categoryIds,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var categoryList = categoryIds.ToList();
                if (!categoryList.Any())
                {
                    return new List<PostSummaryDto>();
                }

                var recommendations = new List<PostSummaryDto>();

                foreach (var categoryId in categoryList.Take(3)) // Limit to 3 categories
                {
                    var categoryPosts = await _postRepository.GetByCategoryAsync(
                        categoryId, 1, count / categoryList.Count + 1, true, cancellationToken);

                    var categoryDtos = _mapper.Map<List<PostSummaryDto>>(categoryPosts);
                    recommendations.AddRange(categoryDtos);
                }

                // Deduplicate and shuffle
                var uniqueRecommendations = recommendations
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .OrderBy(p => Guid.NewGuid()) // Simple shuffle
                    .Take(count)
                    .ToList();

                _logger.LogInformation("Generated {Count} category-based recommendations", uniqueRecommendations.Count);
                return uniqueRecommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating category-based recommendations");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetTagBasedRecommendationsAsync(
            IEnumerable<Guid> tagIds,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var tagList = tagIds.ToList();
                if (!tagList.Any())
                {
                    return new List<PostSummaryDto>();
                }

                var recommendations = new List<PostSummaryDto>();

                foreach (var tagId in tagList.Take(5)) // Limit to 5 tags
                {
                    var tagPosts = await _postRepository.GetByTagAsync(
                        tagId, 1, count / tagList.Count + 1, true, cancellationToken);

                    var tagDtos = _mapper.Map<List<PostSummaryDto>>(tagPosts);
                    recommendations.AddRange(tagDtos);
                }

                // Deduplicate and shuffle
                var uniqueRecommendations = recommendations
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .OrderBy(p => Guid.NewGuid()) // Simple shuffle
                    .Take(count)
                    .ToList();

                _logger.LogInformation("Generated {Count} tag-based recommendations", uniqueRecommendations.Count);
                return uniqueRecommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tag-based recommendations");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetAuthorBasedRecommendationsAsync(
            IEnumerable<Guid> authorIds,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var authorList = authorIds.ToList();
                if (!authorList.Any())
                {
                    return new List<PostSummaryDto>();
                }

                var recommendations = new List<PostSummaryDto>();

                foreach (var authorId in authorList.Take(5)) // Limit to 5 authors
                {
                    var authorPosts = await _postRepository.GetByAuthorAsync(
                        authorId, 1, count / authorList.Count + 1, PostStatus.Published, cancellationToken);

                    var authorDtos = _mapper.Map<List<PostSummaryDto>>(authorPosts);
                    recommendations.AddRange(authorDtos);
                }

                // Deduplicate and shuffle
                var uniqueRecommendations = recommendations
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .OrderBy(p => Guid.NewGuid()) // Simple shuffle
                    .Take(count)
                    .ToList();

                _logger.LogInformation("Generated {Count} author-based recommendations", uniqueRecommendations.Count);
                return uniqueRecommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating author-based recommendations");
                return new List<PostSummaryDto>();
            }
        }

        public async Task<IReadOnlyList<PostSummaryDto>> GetTrendingRecommendationsAsync(
            int timeframe = 7,
            int count = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{TrendingRecommendationsCacheKey}:{timeframe}:{count}";
                var cachedRecommendations = await GetFromCacheAsync<List<PostSummaryDto>>(cacheKey, cancellationToken);
                if (cachedRecommendations != null)
                {
                    _logger.LogDebug("Trending recommendations retrieved from cache");
                    return cachedRecommendations;
                }

                var trendingPosts = await _postRepository.GetMostPopularAsync(1, count, timeframe, cancellationToken);
                var recommendationDtos = _mapper.Map<List<PostSummaryDto>>(trendingPosts);

                // Cache the result
                await SetCacheAsync(cacheKey, recommendationDtos, TrendingCacheTime, cancellationToken);

                _logger.LogInformation("Generated {Count} trending recommendations", recommendationDtos.Count);
                return recommendationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating trending recommendations");
                return new List<PostSummaryDto>();
            }
        }

        public async Task RecordUserInteractionAsync(
            Guid userId,
            Guid postId,
            string interactionType,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _userInteractionRepository.RecordInteractionAsync(
                    userId, postId, interactionType, duration, cancellationToken: cancellationToken);

                // Invalidate user's personalized recommendations cache
                var cachePattern = $"{PersonalizedRecommendationsCacheKey}:{userId}:*";
                await InvalidateCachePatternAsync(cachePattern);

                _logger.LogDebug("Recorded {InteractionType} interaction for user {UserId} on post {PostId}",
                    interactionType, userId, postId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording user interaction");
            }
        }

        public async Task<PersonalizationDto> UpdateUserPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Analyze user interactions to update preferences
                var interactions = await _userInteractionRepository.GetUserInteractionsAsync(userId, cancellationToken: cancellationToken);

                var preferredCategories = await AnalyzeUserCategoryPreferencesAsync(userId, interactions, cancellationToken);
                var preferredTags = await AnalyzeUserTagPreferencesAsync(userId, interactions, cancellationToken);
                var followedAuthors = await AnalyzeUserAuthorPreferencesAsync(userId, interactions, cancellationToken);

                var preferences = new PersonalizationDto
                {
                    PreferredCategories = preferredCategories,
                    PreferredTags = preferredTags,
                    FollowedAuthors = followedAuthors,
                    UpdatedAt = DateTime.UtcNow
                };

                // Cache the updated preferences
                var cacheKey = $"{UserPreferencesCacheKey}:{userId}";
                await SetCacheAsync(cacheKey, preferences, UserPreferencesCacheTime, cancellationToken);

                _logger.LogInformation("Updated preferences for user {UserId}", userId);
                return preferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user preferences for user {UserId}", userId);
                return new PersonalizationDto();
            }
        }

        public async Task RefreshRecommendationModelAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting recommendation model refresh");

                // Clear all recommendation caches
                await InvalidateCachePatternAsync($"{PersonalizedRecommendationsCacheKey}:*");
                await InvalidateCachePatternAsync($"{AnonymousRecommendationsCacheKey}:*");
                await InvalidateCachePatternAsync($"{TrendingRecommendationsCacheKey}:*");
                await InvalidateCachePatternAsync($"{UserPreferencesCacheKey}:*");

                // Precompute popular recommendations
                await GetAnonymousRecommendationsAsync(10, cancellationToken);
                await GetTrendingRecommendationsAsync(7, 10, cancellationToken);

                _logger.LogInformation("Recommendation model refresh completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing recommendation model");
            }
        }

        #region Private Methods

        private async Task<PersonalizationDto> GetOrCreateUserPreferencesAsync(Guid userId, CancellationToken cancellationToken)
        {
            var cacheKey = $"{UserPreferencesCacheKey}:{userId}";
            var cachedPreferences = await GetFromCacheAsync<PersonalizationDto>(cacheKey, cancellationToken);

            if (cachedPreferences != null)
            {
                return cachedPreferences;
            }

            // Create preferences based on user behavior
            return await UpdateUserPreferencesAsync(userId, cancellationToken);
        }

        private async Task<IReadOnlyList<Domain.Entities.Post>> GetCandidatePostsAsync(Guid userId, CancellationToken cancellationToken)
        {
            // Get recent posts that user hasn't interacted with
            var recentPosts = await _postRepository.GetPublishedAsync(1, 100, true, cancellationToken);
            var userInteractedPosts = await _userInteractionRepository.GetUserInteractedPostIdsAsync(userId, cancellationToken: cancellationToken);

            return recentPosts
                .Where(p => !userInteractedPosts.Contains(p.Id))
                .ToList();
        }

        private async Task<IReadOnlyList<ScoredPost>> CalculatePersonalizedScoresAsync(
            IReadOnlyList<Domain.Entities.Post> posts,
            PersonalizationDto preferences,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var scoredPosts = new List<ScoredPost>();

            foreach (var post in posts)
            {
                var score = 0.0;

                // Category preference score
                if (post.CategoryId.HasValue && preferences.PreferredCategories.Contains(post.CategoryId.Value))
                {
                    score += CategoryWeight;
                }

                // Tag preference score
                var postTagIds = post.PostTags.Select(pt => pt.TagId).ToList();
                var tagMatchCount = postTagIds.Intersect(preferences.PreferredTags).Count();
                var tagScore = (double)tagMatchCount / Math.Max(postTagIds.Count, 1);
                score += tagScore * TagWeight;

                // Author preference score
                if (preferences.FollowedAuthors.Contains(post.AuthorId))
                {
                    score += AuthorWeight;
                }

                // Popularity score (normalized)
                var popularityScore = Math.Log(post.ViewCount + 1) / 10.0; // Log to reduce impact of outliers
                score += Math.Min(popularityScore, 1.0) * PopularityWeight;

                // Recency score
                var daysSincePublished = (DateTime.UtcNow - (post.PublishedAt ?? post.CreatedAt)).TotalDays;
                var recencyScore = Math.Exp(-daysSincePublished / 30.0); // Exponential decay over 30 days
                score += recencyScore * RecencyWeight;

                scoredPosts.Add(new ScoredPost { Post = post, Score = score });
            }

            return scoredPosts;
        }

        private async Task<IReadOnlyList<Guid>> AnalyzeUserCategoryPreferencesAsync(
            Guid userId,
            IReadOnlyList<Domain.Entities.UserInteraction> interactions,
            CancellationToken cancellationToken)
        {
            // Analyze category preferences based on user interactions
            var categoryInteractions = new Dictionary<Guid, int>();

            foreach (var interaction in interactions.Where(i => i.PostId.HasValue))
            {
                var post = await _postRepository.GetByIdAsync(interaction.PostId.Value, cancellationToken);
                if (post?.CategoryId.HasValue == true)
                {
                    categoryInteractions.TryGetValue(post.CategoryId.Value, out var count);
                    categoryInteractions[post.CategoryId.Value] = count + GetInteractionWeight(interaction.InteractionType);
                }
            }

            return categoryInteractions
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private async Task<IReadOnlyList<Guid>> AnalyzeUserTagPreferencesAsync(
            Guid userId,
            IReadOnlyList<Domain.Entities.UserInteraction> interactions,
            CancellationToken cancellationToken)
        {
            var tagInteractions = new Dictionary<Guid, int>();

            foreach (var interaction in interactions.Where(i => i.PostId.HasValue))
            {
                var post = await _postRepository.GetByIdAsync(interaction.PostId.Value, cancellationToken);
                if (post != null)
                {
                    foreach (var postTag in post.PostTags)
                    {
                        tagInteractions.TryGetValue(postTag.TagId, out var count);
                        tagInteractions[postTag.TagId] = count + GetInteractionWeight(interaction.InteractionType);
                    }
                }
            }

            return tagInteractions
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private async Task<IReadOnlyList<Guid>> AnalyzeUserAuthorPreferencesAsync(
            Guid userId,
            IReadOnlyList<Domain.Entities.UserInteraction> interactions,
            CancellationToken cancellationToken)
        {
            var authorInteractions = new Dictionary<Guid, int>();

            foreach (var interaction in interactions.Where(i => i.PostId.HasValue))
            {
                var post = await _postRepository.GetByIdAsync(interaction.PostId.Value, cancellationToken);
                if (post != null)
                {
                    authorInteractions.TryGetValue(post.AuthorId, out var count);
                    authorInteractions[post.AuthorId] = count + GetInteractionWeight(interaction.InteractionType);
                }
            }

            return authorInteractions
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private static int GetInteractionWeight(string interactionType)
        {
            return interactionType.ToLower() switch
            {
                "view" => 1,
                "like" => 3,
                "comment" => 5,
                "share" => 4,
                _ => 1
            };
        }

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

        private async Task InvalidateCachePatternAsync(string pattern)
        {
            try
            {
                // Simple implementation - in production, use Redis pattern deletion
                await _cache.RemoveAsync(pattern.Replace("*", ""));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error invalidating cache pattern: {Pattern}", pattern);
            }
        }

        #endregion

        private class ScoredPost
        {
            public Domain.Entities.Post Post { get; set; } = null!;
            public double Score { get; set; }
        }
    }
}