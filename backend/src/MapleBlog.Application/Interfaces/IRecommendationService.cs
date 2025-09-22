using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Recommendation service interface for personalized content suggestions
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Gets personalized post recommendations for a user
        /// </summary>
        /// <param name="userId">User ID for personalization</param>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Personalized post recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetPersonalizedRecommendationsAsync(
            Guid userId,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets content-based recommendations for anonymous users
        /// </summary>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Content-based recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetAnonymousRecommendationsAsync(
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommendations based on current post (for "You might also like" sections)
        /// </summary>
        /// <param name="postId">Current post ID</param>
        /// <param name="userId">Optional user ID for personalization</param>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Related post recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetRelatedPostRecommendationsAsync(
            Guid postId,
            Guid? userId = null,
            int count = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommendations based on category preferences
        /// </summary>
        /// <param name="categoryIds">Preferred category IDs</param>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category-based recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetCategoryBasedRecommendationsAsync(
            IEnumerable<Guid> categoryIds,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommendations based on tag preferences
        /// </summary>
        /// <param name="tagIds">Preferred tag IDs</param>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag-based recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetTagBasedRecommendationsAsync(
            IEnumerable<Guid> tagIds,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recommendations based on author following
        /// </summary>
        /// <param name="authorIds">Followed author IDs</param>
        /// <param name="count">Number of recommendations to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Author-based recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetAuthorBasedRecommendationsAsync(
            IEnumerable<Guid> authorIds,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records user interaction for improving recommendations
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="postId">Post ID</param>
        /// <param name="interactionType">Type of interaction (view, like, share, comment)</param>
        /// <param name="duration">Time spent on content (for views)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task RecordUserInteractionAsync(
            Guid userId,
            Guid postId,
            string interactionType,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user preferences based on behavior analysis
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated user preferences</returns>
        Task<PersonalizationDto> UpdateUserPreferencesAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets trending posts for general recommendations
        /// </summary>
        /// <param name="timeframe">Timeframe for trending analysis (days)</param>
        /// <param name="count">Number of trending posts to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trending post recommendations</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetTrendingRecommendationsAsync(
            int timeframe = 7,
            int count = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh recommendation model (background job)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task RefreshRecommendationModelAsync(CancellationToken cancellationToken = default);
    }
}