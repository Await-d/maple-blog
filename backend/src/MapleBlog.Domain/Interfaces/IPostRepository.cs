using System.Linq.Expressions;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Post entity with blog-specific operations
    /// </summary>
    public interface IPostRepository : IRepository<Post>
    {
        /// <summary>
        /// Gets posts with their related data (author, category, tags)
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Posts with related data</returns>
        Task<IReadOnlyList<Post>> GetPostsWithDetailsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a post by slug with all related data
        /// </summary>
        /// <param name="slug">Post slug</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Post with related data if found</returns>
        Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts by author with related data
        /// </summary>
        /// <param name="authorId">Author ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="status">Optional status filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Author's posts</returns>
        Task<IReadOnlyList<Post>> GetByAuthorAsync(
            Guid authorId,
            int pageNumber = 1,
            int pageSize = 10,
            PostStatus? status = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts by category with related data
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category posts</returns>
        Task<IReadOnlyList<Post>> GetByCategoryAsync(
            Guid categoryId,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts by tag with related data
        /// </summary>
        /// <param name="tagId">Tag ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tagged posts</returns>
        Task<IReadOnlyList<Post>> GetByTagAsync(
            Guid tagId,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts by multiple tags (posts that have ALL specified tags)
        /// </summary>
        /// <param name="tagIds">Tag IDs</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Posts with all specified tags</returns>
        Task<IReadOnlyList<Post>> GetByTagsAsync(
            IEnumerable<Guid> tagIds,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches posts by title and content
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching posts</returns>
        Task<IReadOnlyList<Post>> SearchAsync(
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets published posts ordered by publication date
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="orderDescending">Whether to order by newest first</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Published posts</returns>
        Task<IReadOnlyList<Post>> GetPublishedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool orderDescending = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets featured posts
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Featured posts</returns>
        Task<IReadOnlyList<Post>> GetFeaturedAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets most popular posts by view count
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="daysBack">Number of days to look back (null for all time)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Popular posts</returns>
        Task<IReadOnlyList<Post>> GetMostPopularAsync(
            int pageNumber = 1,
            int pageSize = 10,
            int? daysBack = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent posts by creation date
        /// </summary>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Number of posts per page</param>
        /// <param name="publishedOnly">Whether to include only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Recent posts</returns>
        Task<IReadOnlyList<Post>> GetRecentAsync(
            int pageNumber = 1,
            int pageSize = 10,
            bool publishedOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets related posts based on category and tags
        /// </summary>
        /// <param name="postId">Current post ID</param>
        /// <param name="count">Number of related posts to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Related posts</returns>
        Task<IReadOnlyList<Post>> GetRelatedAsync(
            Guid postId,
            int count = 5,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts scheduled for publication that are ready to be published
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Posts ready for publication</returns>
        Task<IReadOnlyList<Post>> GetScheduledForPublicationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts statistics
        /// </summary>
        /// <param name="authorId">Optional author filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Posts statistics</returns>
        Task<PostStatistics> GetStatisticsAsync(Guid? authorId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets posts grouped by date for archive display
        /// </summary>
        /// <param name="year">Optional year filter</param>
        /// <param name="month">Optional month filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Posts grouped by publication date</returns>
        Task<IReadOnlyDictionary<DateTime, IReadOnlyList<Post>>> GetArchiveAsync(
            int? year = null,
            int? month = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a slug is available (not used by any existing post)
        /// </summary>
        /// <param name="slug">Slug to check</param>
        /// <param name="excludePostId">Post ID to exclude from check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if slug is available</returns>
        Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? excludePostId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk updates post status
        /// </summary>
        /// <param name="postIds">Post IDs to update</param>
        /// <param name="status">New status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of updated posts</returns>
        Task<int> BulkUpdateStatusAsync(
            IEnumerable<Guid> postIds,
            PostStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the first published post
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>First published post if any</returns>
        Task<Post?> GetFirstPublishedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total reading time for all posts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total reading time in minutes</returns>
        Task<int> GetTotalReadingTimeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total views for an author's posts
        /// </summary>
        /// <param name="authorId">Author ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total views for author</returns>
        Task<int> GetAuthorTotalViewsAsync(Guid authorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the date of an author's last post
        /// </summary>
        /// <param name="authorId">Author ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Date of last post</returns>
        Task<DateTime?> GetAuthorLastPostDateAsync(Guid authorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all published posts
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All published posts</returns>
        Task<IEnumerable<Post>> GetAllPublishedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most viewed posts
        /// </summary>
        /// <param name="count">Number of posts to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most viewed posts</returns>
        Task<IEnumerable<Post>> GetMostViewedPostsAsync(int count = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total views for posts matching a predicate
        /// </summary>
        /// <param name="predicate">Filter predicate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total views</returns>
        Task<long> GetTotalViewsAsync(Expression<Func<Post, bool>>? predicate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets published posts within a date range
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="categoryId">Optional category filter</param>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Published posts in date range</returns>
        Task<IEnumerable<Post>> GetPublishedPostsAsync(
            int pageNumber,
            int pageSize,
            Guid? categoryId,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Posts statistics data
    /// </summary>
    public class PostStatistics
    {
        public int TotalPosts { get; init; }
        public int PublishedPosts { get; init; }
        public int DraftPosts { get; init; }
        public int PrivatePosts { get; init; }
        public int ScheduledPosts { get; init; }
        public int ArchivedPosts { get; init; }
        public int TotalViews { get; init; }
        public int TotalLikes { get; init; }
        public int TotalComments { get; init; }
        public int FeaturedPosts { get; init; }
        public DateTime? LastPostDate { get; init; }
        public Post? MostViewedPost { get; init; }
        public Post? MostLikedPost { get; init; }
    }
}