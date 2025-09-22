using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Blog service interface for article management operations
/// </summary>
public interface IBlogService
{
    /// <summary>
    /// Gets a post by its ID
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post DTO if found, null otherwise</returns>
    Task<PostDto?> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a post by its slug
    /// </summary>
    /// <param name="slug">Post slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post DTO if found, null otherwise</returns>
    Task<PostDto?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated posts with filtering and sorting options
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated post list response</returns>
    Task<PostListResponse> GetPostsAsync(PostQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts by specific author
    /// </summary>
    /// <param name="authorId">Author ID</param>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated post list response</returns>
    Task<PostListResponse> GetPostsByAuthorAsync(Guid authorId, PostQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts by category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated post list response</returns>
    Task<PostListResponse> GetPostsByCategoryAsync(Guid categoryId, PostQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts by tag
    /// </summary>
    /// <param name="tagId">Tag ID</param>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated post list response</returns>
    Task<PostListResponse> GetPostsByTagAsync(Guid tagId, PostQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches posts by content and metadata
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated post list response</returns>
    Task<PostListResponse> SearchPostsAsync(string searchQuery, PostQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new post
    /// </summary>
    /// <param name="request">Create post request</param>
    /// <param name="authorId">Author ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created post DTO</returns>
    Task<PostDto> CreatePostAsync(CreatePostRequest request, Guid authorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="request">Update post request</param>
    /// <param name="userId">User ID performing the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated post DTO</returns>
    Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="request">Publish request</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> PublishPostAsync(Guid id, PublishPostRequest? request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a post (sets to draft)
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> UnpublishPostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> ArchivePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> DeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a post (admin only)
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> PermanentlyDeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted post
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> RestorePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Duplicates a post
    /// </summary>
    /// <param name="id">Source post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Duplicated post DTO</returns>
    Task<PostDto?> DuplicatePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments post view count
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles post featured status
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> ToggleFeaturedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles post sticky status
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> ToggleStickyAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates post tags
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="tagIds">New tag IDs</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> UpdatePostTagsAsync(Guid id, IEnumerable<Guid> tagIds, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts scheduled for publication
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of posts ready for publication</returns>
    Task<IEnumerable<PostDto>> GetScheduledPostsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes scheduled posts for publication
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of posts published</returns>
    Task<int> ProcessScheduledPostsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates post slug uniqueness
    /// </summary>
    /// <param name="slug">Slug to validate</param>
    /// <param name="excludePostId">Post ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if slug is unique, false otherwise</returns>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludePostId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique slug from title
    /// </summary>
    /// <param name="title">Post title</param>
    /// <param name="excludePostId">Post ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique slug</returns>
    Task<string> GenerateUniqueSlugAsync(string title, Guid? excludePostId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations on multiple posts
    /// </summary>
    /// <param name="postIds">Post IDs to operate on</param>
    /// <param name="operation">Operation to perform (publish, unpublish, archive, delete)</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with details</returns>
    Task<BulkOperationResult> BulkOperationAsync(IEnumerable<Guid> postIds, string operation, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets published posts with pagination (used by public API)
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="tagId">Optional tag filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of published posts</returns>
    Task<PagedResultDto<PostDto>> GetPublishedPostsAsync(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all posts with pagination (used by admin/author API)
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="authorId">Optional author filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result of all posts</returns>
    Task<PagedResultDto<PostDto>> GetAllPostsAsync(int pageNumber, int pageSize, PostStatus? status = null, Guid? authorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts by category with pagination (controller compatibility)
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of posts in category</returns>
    Task<PagedResultDto<PostDto>> GetPostsByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets posts by tag with pagination (controller compatibility)
    /// </summary>
    /// <param name="tagId">Tag ID</param>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of posts with tag</returns>
    Task<PagedResultDto<PostDto>> GetPostsByTagAsync(Guid tagId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a post using DTO for controller compatibility
    /// </summary>
    /// <param name="authorId">Author ID</param>
    /// <param name="createDto">Create post DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created post</returns>
    Task<PostDto> CreatePostAsync(Guid authorId, CreatePostDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a post using DTO for controller compatibility
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="updateDto">Update post DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated post</returns>
    Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a post for controller compatibility
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published post</returns>
    Task<PostDto?> PublishPostAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unpublishes a post for controller compatibility
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unpublished post</returns>
    Task<PostDto?> UnpublishPostAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a post for controller compatibility
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation success</returns>
    Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResult
{
    /// <summary>
    /// Whether the operation was successful overall
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of items successfully processed
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of items that failed processing
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Error messages for failed items
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Overall operation message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public static BulkOperationResult CreateSuccess(int successCount, string message = "")
        => new() { Success = true, SuccessCount = successCount, Message = message };

    public static BulkOperationResult Failure(IEnumerable<string> errors, int successCount = 0, int failureCount = 0)
        => new() { Success = false, SuccessCount = successCount, FailureCount = failureCount, Errors = errors.ToList() };

    public static BulkOperationResult Mixed(int successCount, int failureCount, IEnumerable<string> errors, string message = "")
        => new() { Success = successCount > 0, SuccessCount = successCount, FailureCount = failureCount, Errors = errors.ToList(), Message = message };
}