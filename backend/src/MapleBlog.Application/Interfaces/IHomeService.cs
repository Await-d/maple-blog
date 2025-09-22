using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Home service interface for aggregating home page data
    /// </summary>
    public interface IHomeService
    {
        /// <summary>
        /// Gets comprehensive home page data for anonymous users
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Home page data aggregation</returns>
        Task<HomePageDto> GetHomePageDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets personalized home page data for authenticated users
        /// </summary>
        /// <param name="userId">User ID for personalization</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Personalized home page data aggregation</returns>
        Task<HomePageDto> GetPersonalizedHomePageDataAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets featured posts for the hero section
        /// </summary>
        /// <param name="count">Number of featured posts to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Featured posts</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetFeaturedPostsAsync(int count = 5, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets latest published posts
        /// </summary>
        /// <param name="count">Number of latest posts to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Latest posts</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetLatestPostsAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets popular posts by views
        /// </summary>
        /// <param name="count">Number of popular posts to return</param>
        /// <param name="daysBack">Number of days to look back for popularity calculation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Popular posts</returns>
        Task<IReadOnlyList<PostSummaryDto>> GetPopularPostsAsync(int count = 10, int daysBack = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes all home page caches (background job)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task RefreshHomePageCacheAsync(CancellationToken cancellationToken = default);
    }
}