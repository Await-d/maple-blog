using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for UserInteraction entity
    /// </summary>
    public interface IUserInteractionRepository : IRepository<UserInteraction>
    {
        /// <summary>
        /// Records a user interaction
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="postId">Post ID (optional)</param>
        /// <param name="interactionType">Type of interaction</param>
        /// <param name="duration">Duration of interaction</param>
        /// <param name="ipAddress">User's IP address</param>
        /// <param name="userAgent">User's browser/device info</param>
        /// <param name="referrer">Referrer URL</param>
        /// <param name="sessionId">Session ID</param>
        /// <param name="metadata">Additional metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created interaction</returns>
        Task<UserInteraction> RecordInteractionAsync(
            Guid userId,
            Guid? postId = null,
            string interactionType = "view",
            TimeSpan? duration = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? referrer = null,
            string? sessionId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user interactions for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="interactionType">Optional interaction type filter</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User interactions</returns>
        Task<IReadOnlyList<UserInteraction>> GetUserInteractionsAsync(
            Guid userId,
            string? interactionType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets post IDs that a user has interacted with
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="interactionTypes">Optional interaction types to include</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Post IDs</returns>
        Task<IReadOnlyList<Guid>> GetUserInteractedPostIdsAsync(
            Guid userId,
            IEnumerable<string>? interactionTypes = null,
            DateTime? fromDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets interactions for a specific post
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="interactionType">Optional interaction type filter</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Post interactions</returns>
        Task<IReadOnlyList<UserInteraction>> GetPostInteractionsAsync(
            Guid postId,
            string? interactionType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int pageNumber = 1,
            int pageSize = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user interaction statistics
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Interaction statistics</returns>
        Task<UserInteractionStats> GetUserInteractionStatsAsync(
            Guid userId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets post interaction statistics
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="toDate">Optional end date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Interaction statistics</returns>
        Task<PostInteractionStats> GetPostInteractionStatsAsync(
            Guid postId,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets most engaged users (by interaction count and quality)
        /// </summary>
        /// <param name="count">Number of users to return</param>
        /// <param name="fromDate">Optional start date filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most engaged users with their interaction stats</returns>
        Task<IReadOnlyList<UserEngagementSummary>> GetMostEngagedUsersAsync(
            int count = 10,
            DateTime? fromDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets interaction trends over time
        /// </summary>
        /// <param name="granularity">Time granularity (day, week, month)</param>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <param name="interactionType">Optional interaction type filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Interaction trends</returns>
        Task<IReadOnlyList<InteractionTrend>> GetInteractionTrendsAsync(
            string granularity = "day",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? interactionType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cleanup old interactions (for privacy and performance)
        /// </summary>
        /// <param name="olderThan">Delete interactions older than this date</param>
        /// <param name="keepInteractionTypes">Interaction types to keep regardless of age</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of deleted interactions</returns>
        Task<int> CleanupOldInteractionsAsync(
            DateTime olderThan,
            IEnumerable<string>? keepInteractionTypes = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// User interaction statistics
    /// </summary>
    public class UserInteractionStats
    {
        public Guid UserId { get; init; }
        public int TotalInteractions { get; init; }
        public int ViewCount { get; init; }
        public int LikeCount { get; init; }
        public int CommentCount { get; init; }
        public int ShareCount { get; init; }
        public TimeSpan TotalReadingTime { get; init; }
        public DateTime? FirstInteraction { get; init; }
        public DateTime? LastInteraction { get; init; }
        public int UniquePostsInteracted { get; init; }
        public double EngagementScore { get; init; }
    }

    /// <summary>
    /// Post interaction statistics
    /// </summary>
    public class PostInteractionStats
    {
        public Guid PostId { get; init; }
        public int TotalInteractions { get; init; }
        public int UniqueUsers { get; init; }
        public int ViewCount { get; init; }
        public int LikeCount { get; init; }
        public int CommentCount { get; init; }
        public int ShareCount { get; init; }
        public TimeSpan AverageReadingTime { get; init; }
        public DateTime? FirstInteraction { get; init; }
        public DateTime? LastInteraction { get; init; }
        public double EngagementRate { get; init; }
    }

    /// <summary>
    /// User engagement summary
    /// </summary>
    public class UserEngagementSummary
    {
        public Guid UserId { get; init; }
        public string UserName { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public int InteractionCount { get; init; }
        public double EngagementScore { get; init; }
        public DateTime LastActive { get; init; }
        public TimeSpan TotalReadingTime { get; init; }
        public int UniquePostsRead { get; init; }
    }

    /// <summary>
    /// Interaction trend data point
    /// </summary>
    public class InteractionTrend
    {
        public DateTime Date { get; init; }
        public int InteractionCount { get; init; }
        public int UniqueUsers { get; init; }
        public string InteractionType { get; init; } = string.Empty;
    }
}