using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Tag service interface for tag management operations
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Gets a tag by its ID
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag DTO if found, null otherwise</returns>
    Task<TagDto?> GetTagByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tag by its slug
    /// </summary>
    /// <param name="slug">Tag slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag DTO if found, null otherwise</returns>
    Task<TagDto?> GetTagBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tag by its name
    /// </summary>
    /// <param name="name">Tag name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag DTO if found, null otherwise</returns>
    Task<TagDto?> GetTagByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tags with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated tag list response</returns>
    Task<TagListResponse> GetTagsAsync(TagQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets popular tags for tag cloud display
    /// </summary>
    /// <param name="count">Number of tags to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular tags with weights</returns>
    Task<List<TagCloudDto>> GetTagCloudAsync(int count = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tag suggestions based on partial name match
    /// </summary>
    /// <param name="partialName">Partial tag name</param>
    /// <param name="limit">Maximum number of suggestions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tag suggestions</returns>
    Task<List<TagAutoCompleteDto>> GetTagSuggestionsAsync(string partialName, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets intelligent tag suggestions based on content analysis
    /// </summary>
    /// <param name="content">Content to analyze for tag suggestions</param>
    /// <param name="title">Optional title for additional context</param>
    /// <param name="existingTagIds">Already selected tag IDs to exclude</param>
    /// <param name="limit">Maximum number of suggestions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of intelligent tag suggestions</returns>
    Task<List<TagSuggestionDto>> GetIntelligentTagSuggestionsAsync(string content, string? title = null, IEnumerable<Guid>? existingTagIds = null, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets related tags based on co-occurrence
    /// </summary>
    /// <param name="tagId">Reference tag ID</param>
    /// <param name="count">Number of related tags to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of related tags</returns>
    Task<List<TagDto>> GetRelatedTagsAsync(Guid tagId, int count = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets trending tags with recent activity
    /// </summary>
    /// <param name="count">Number of trending tags to return</param>
    /// <param name="daysBack">Number of days to look back for trending calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of trending tags</returns>
    Task<List<TagDto>> GetTrendingTagsAsync(int count = 20, int daysBack = 7, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unused tags that can be cleaned up
    /// </summary>
    /// <param name="olderThanDays">Optional filter for tags older than specified days</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unused tags</returns>
    Task<List<TagDto>> GetUnusedTagsAsync(int? olderThanDays = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new tag
    /// </summary>
    /// <param name="request">Create tag request</param>
    /// <param name="userId">User ID creating the tag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tag DTO</returns>
    Task<TagDto> CreateTagAsync(CreateTagRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="request">Update tag request</param>
    /// <param name="userId">User ID performing the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated tag DTO</returns>
    Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="isActive">New active status</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> SetTagStatusAsync(Guid id, bool isActive, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="userId">User ID performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> DeleteTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a tag (admin only)
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="userId">User ID performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> PermanentlyDeleteTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted tag
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="userId">User ID performing the restoration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> RestoreTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges multiple tags into one target tag
    /// </summary>
    /// <param name="request">Merge tags request</param>
    /// <param name="userId">User ID performing the merge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> MergeTagsAsync(MergeTagsRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or finds tags from a list of names
    /// </summary>
    /// <param name="tagNames">Tag names to create or find</param>
    /// <param name="userId">User ID creating new tags</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tag DTOs</returns>
    Task<List<TagDto>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates tag slug uniqueness
    /// </summary>
    /// <param name="slug">Slug to validate</param>
    /// <param name="excludeTagId">Tag ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if slug is unique, false otherwise</returns>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeTagId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique slug from name
    /// </summary>
    /// <param name="name">Tag name</param>
    /// <param name="excludeTagId">Tag ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique slug</returns>
    Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeTagId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates usage counts for all tags (maintenance operation)
    /// </summary>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with number of tags updated</returns>
    Task<OperationResult> RecalculateUsageCountsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations on multiple tags
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResult> BulkOperationAsync(BulkTagOperationRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches tags by name and description
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <param name="includeInactive">Whether to include inactive tags</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching tags</returns>
    Task<List<TagDto>> SearchTagsAsync(string searchQuery, bool includeInactive = false, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tag statistics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag statistics</returns>
    Task<TagStatsDto> GetTagStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets popular tags (most used)
    /// </summary>
    /// <param name="limit">Number of tags to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of popular tags</returns>
    Task<List<TagDto>> GetPopularTagsAsync(int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tag statistics for a specific tag
    /// </summary>
    /// <param name="tagId">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag statistics</returns>
    Task<TagStatsDto?> GetTagStatsAsync(Guid tagId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tag analytics data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tag analytics</returns>
    Task<TagAnalyticsDto> GetTagAnalyticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tag (returns boolean for backward compatibility)
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges tags (for backward compatibility)
    /// </summary>
    /// <param name="mergeDto">Merge data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged tag</returns>
    Task<TagDto> MergeTagsAsync(MergeTagsDto mergeDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a tag using DTO (for backward compatibility)
    /// </summary>
    /// <param name="createDto">Create tag DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created tag</returns>
    Task<TagDto> CreateTagAsync(CreateTagDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tag using DTO (for backward compatibility)
    /// </summary>
    /// <param name="id">Tag ID</param>
    /// <param name="updateDto">Update tag DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated tag</returns>
    Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated tags for backward compatibility
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="includeHidden">Include hidden tags</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result</returns>
    Task<PagedResultDto<TagDto>> GetTagsAsync(int pageNumber, int pageSize, string? searchTerm, string sortBy, bool includeHidden, CancellationToken cancellationToken = default);
}