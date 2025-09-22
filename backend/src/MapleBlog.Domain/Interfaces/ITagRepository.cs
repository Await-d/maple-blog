using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Tag entity with tagging-specific operations
    /// </summary>
    public interface ITagRepository : IRepository<Tag>
    {
        /// <summary>
        /// Gets a tag by slug
        /// </summary>
        /// <param name="slug">Tag slug</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag if found</returns>
        Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a tag by name (case-insensitive)
        /// </summary>
        /// <param name="name">Tag name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag if found</returns>
        Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tags by names (case-insensitive)
        /// </summary>
        /// <param name="names">Tag names</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching tags</returns>
        Task<IReadOnlyList<Tag>> GetByNamesAsync(
            IEnumerable<string> names,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets popular tags ordered by usage count
        /// </summary>
        /// <param name="count">Number of tags to return</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="minUseCount">Minimum usage count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Popular tags</returns>
        Task<IReadOnlyList<Tag>> GetPopularAsync(
            int count = 50,
            bool activeOnly = true,
            int minUseCount = 1,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets trending tags (tags with recent high activity)
        /// </summary>
        /// <param name="count">Number of tags to return</param>
        /// <param name="daysBack">Number of days to look back for trending calculation</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trending tags</returns>
        Task<IReadOnlyList<TagTrendingInfo>> GetTrendingAsync(
            int count = 20,
            int daysBack = 7,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches tags by name and description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="limit">Maximum number of results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching tags</returns>
        Task<IReadOnlyList<Tag>> SearchAsync(
            string searchTerm,
            bool activeOnly = true,
            int limit = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tag suggestions based on partial name match
        /// </summary>
        /// <param name="partialName">Partial tag name</param>
        /// <param name="limit">Maximum number of suggestions</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag suggestions</returns>
        Task<IReadOnlyList<Tag>> GetSuggestionsAsync(
            string partialName,
            int limit = 10,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets related tags based on co-occurrence in posts
        /// </summary>
        /// <param name="tagId">Reference tag ID</param>
        /// <param name="count">Number of related tags to return</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Related tags with correlation scores</returns>
        Task<IReadOnlyList<RelatedTagInfo>> GetRelatedTagsAsync(
            Guid tagId,
            int count = 10,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets unused tags (tags with zero usage count)
        /// </summary>
        /// <param name="olderThanDays">Optional filter for tags older than specified days</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unused tags</returns>
        Task<IReadOnlyList<Tag>> GetUnusedTagsAsync(
            int? olderThanDays = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tags with their post counts
        /// </summary>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="publishedPostsOnly">Whether to count only published posts</param>
        /// <param name="orderByUseCount">Whether to order by use count (descending)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tags with post counts</returns>
        Task<IReadOnlyList<TagWithPostCount>> GetTagsWithPostCountsAsync(
            bool activeOnly = true,
            bool publishedPostsOnly = true,
            bool orderByUseCount = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tags by popularity level
        /// </summary>
        /// <param name="popularityLevel">Popularity level filter</param>
        /// <param name="activeOnly">Whether to include only active tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tags at the specified popularity level</returns>
        Task<IReadOnlyList<Tag>> GetByPopularityLevelAsync(
            TagPopularityLevel popularityLevel,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates tags from a list of names
        /// </summary>
        /// <param name="tagNames">Tag names to create or find</param>
        /// <param name="createdByUserId">User ID creating the tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of tag entities</returns>
        Task<IReadOnlyList<Tag>> GetOrCreateTagsAsync(
            IEnumerable<string> tagNames,
            Guid? createdByUserId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates use counts for multiple tags
        /// </summary>
        /// <param name="tagUsageCounts">Dictionary of tag ID to new usage count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of updated tags</returns>
        Task<int> UpdateUseCountsAsync(
            IDictionary<Guid, int> tagUsageCounts,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk updates tag status
        /// </summary>
        /// <param name="tagIds">Tag IDs to update</param>
        /// <param name="isActive">New active status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of updated tags</returns>
        Task<int> BulkUpdateStatusAsync(
            IEnumerable<Guid> tagIds,
            bool isActive,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Merges multiple tags into one target tag
        /// </summary>
        /// <param name="sourceTagIds">Source tag IDs to merge</param>
        /// <param name="targetTagId">Target tag ID to merge into</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected post-tag relationships</returns>
        Task<int> MergeTagsAsync(
            IEnumerable<Guid> sourceTagIds,
            Guid targetTagId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a slug is available
        /// </summary>
        /// <param name="slug">Slug to check</param>
        /// <param name="excludeTagId">Tag ID to exclude from check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if slug is available</returns>
        Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? excludeTagId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tag statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tag statistics</returns>
        Task<TagStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most used tags
        /// </summary>
        /// <param name="count">Number of tags to return</param>
        /// <param name="minUsage">Minimum usage count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most used tags</returns>
        Task<IReadOnlyList<Tag>> GetMostUsedAsync(int count = 10, int minUsage = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets tags by IDs
        /// </summary>
        /// <param name="tagIds">Tag IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Tags matching the IDs</returns>
        Task<IReadOnlyList<Tag>> GetByIdsAsync(IEnumerable<Guid> tagIds, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Tag trending information
    /// </summary>
    public class TagTrendingInfo
    {
        public Tag Tag { get; init; } = null!;
        public int RecentUseCount { get; init; }
        public double TrendingScore { get; init; }
        public int PercentageChange { get; init; }
    }

    /// <summary>
    /// Related tag information with correlation score
    /// </summary>
    public class RelatedTagInfo
    {
        public Tag Tag { get; init; } = null!;
        public int CoOccurrenceCount { get; init; }
        public double CorrelationScore { get; init; }
    }

    /// <summary>
    /// Tag with post count information
    /// </summary>
    public class TagWithPostCount
    {
        public Tag Tag { get; init; } = null!;
        public int PostCount { get; init; }
        public int PublishedPostCount { get; init; }
        public DateTime? LastUsedDate { get; init; }
    }

    /// <summary>
    /// Tag statistics data
    /// </summary>
    public class TagStatistics
    {
        public int TotalTags { get; init; }
        public int ActiveTags { get; init; }
        public int InactiveTags { get; init; }
        public int UsedTags { get; init; }
        public int UnusedTags { get; init; }
        public int TotalTagUsages { get; init; }
        public double AverageUsagePerTag { get; init; }
        public Tag? MostUsedTag { get; init; }
        public int MostUsedTagCount { get; init; }
        public Tag? MostRecentTag { get; init; }
        public IReadOnlyDictionary<TagPopularityLevel, int> TagsByPopularity { get; init; } = new Dictionary<TagPopularityLevel, int>();
    }
}