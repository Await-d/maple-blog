using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Infrastructure.Caching;

/// <summary>
/// Cached decorator for BlogService to improve performance
/// Implements caching strategies for blog-related operations
/// </summary>
public class CachedBlogService : IBlogService
{
    private readonly IBlogService _blogService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedBlogService> _logger;
    private readonly CacheOptions _options;

    public CachedBlogService(
        IBlogService blogService,
        ICacheService cacheService,
        ILogger<CachedBlogService> logger,
        IOptions<CacheOptions> options)
    {
        _blogService = blogService;
        _cacheService = cacheService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<PostDto?> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Post(id);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPostByIdAsync(id, cancellationToken),
            _options.PostExpiration,
            cancellationToken);
    }

    public async Task<PostDto?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.PostBySlug(slug);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPostBySlugAsync(slug, cancellationToken),
            _options.PostExpiration,
            cancellationToken);
    }

    public async Task<PagedResultDto<PostDto>> GetPublishedPostsAsync(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.PostList(pageNumber, pageSize, categoryId?.ToString(), searchTerm);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPublishedPostsAsync(pageNumber, pageSize, categoryId, tagId, searchTerm, cancellationToken),
            _options.PostExpiration,
            cancellationToken) ?? new PagedResultDto<PostDto> { Items = new List<PostDto>() };
    }

    public async Task<PagedResultDto<PostDto>> GetAllPostsAsync(int pageNumber, int pageSize, PostStatus? status = null, Guid? authorId = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"posts:all:{pageNumber}:{pageSize}:{status}:{authorId}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetAllPostsAsync(pageNumber, pageSize, status, authorId, cancellationToken),
            _options.ShortExpiration, // Shorter cache for admin views
            cancellationToken) ?? new PagedResultDto<PostDto> { Items = new List<PostDto>() };
    }

    public async Task<PostListResponse> GetPostsAsync(PostQueryDto query, CancellationToken cancellationToken = default)
    {
        // Cache post lists with shorter expiration due to frequent updates
        var cacheKey = CacheKeys.PostList(
            query.Page,
            query.PageSize,
            query.CategoryId?.ToString(),
            query.Search);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPostsAsync(query, cancellationToken),
            _options.ShortExpiration, // Shorter cache for lists
            cancellationToken) ?? new PostListResponse { Items = new List<PostListDto>() };
    }

    public async Task<PostListResponse> GetPostsByAuthorAsync(Guid authorId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        // For author-specific queries, include author ID in cache key
        var modifiedQuery = new PostQueryDto
        {
            Search = query.Search,
            Status = query.Status,
            CategoryId = query.CategoryId,
            TagIds = query.TagIds,
            AuthorId = authorId, // Override with specific author ID
            IsFeatured = query.IsFeatured,
            IsSticky = query.IsSticky,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            SortOrder = query.SortOrder,
            Page = query.Page,
            PageSize = query.PageSize
        };
        return await GetPostsAsync(modifiedQuery, cancellationToken);
    }

    public async Task<PostListResponse> GetPostsByCategoryAsync(Guid categoryId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        var modifiedQuery = new PostQueryDto
        {
            Search = query.Search,
            Status = query.Status,
            CategoryId = categoryId, // Override with specific category ID
            TagIds = query.TagIds,
            AuthorId = query.AuthorId,
            IsFeatured = query.IsFeatured,
            IsSticky = query.IsSticky,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            SortOrder = query.SortOrder,
            Page = query.Page,
            PageSize = query.PageSize
        };
        return await GetPostsAsync(modifiedQuery, cancellationToken);
    }

    public async Task<PostListResponse> GetPostsByTagAsync(Guid tagId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        var modifiedQuery = new PostQueryDto
        {
            Search = query.Search,
            Status = query.Status,
            CategoryId = query.CategoryId,
            TagIds = new List<Guid> { tagId }, // Override with specific tag ID
            AuthorId = query.AuthorId,
            IsFeatured = query.IsFeatured,
            IsSticky = query.IsSticky,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            SortOrder = query.SortOrder,
            Page = query.Page,
            PageSize = query.PageSize
        };
        return await GetPostsAsync(modifiedQuery, cancellationToken);
    }

    public async Task<PagedResultDto<PostDto>> GetPostsByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"posts:category:{categoryId}:{pageNumber}:{pageSize}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPostsByCategoryAsync(categoryId, pageNumber, pageSize, cancellationToken),
            _options.PostExpiration,
            cancellationToken) ?? new PagedResultDto<PostDto> { Items = new List<PostDto>() };
    }

    public async Task<PagedResultDto<PostDto>> GetPostsByTagAsync(Guid tagId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"posts:tag:{tagId}:{pageNumber}:{pageSize}";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetPostsByTagAsync(tagId, pageNumber, pageSize, cancellationToken),
            _options.PostExpiration,
            cancellationToken) ?? new PagedResultDto<PostDto> { Items = new List<PostDto>() };
    }

    public async Task<PostListResponse> SearchPostsAsync(string searchQuery, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        var modifiedQuery = new PostQueryDto
        {
            Search = searchQuery, // Override with specific search query
            Status = query.Status,
            CategoryId = query.CategoryId,
            TagIds = query.TagIds,
            AuthorId = query.AuthorId,
            IsFeatured = query.IsFeatured,
            IsSticky = query.IsSticky,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            SortBy = query.SortBy,
            SortOrder = query.SortOrder,
            Page = query.Page,
            PageSize = query.PageSize
        };
        return await GetPostsAsync(modifiedQuery, cancellationToken);
    }

    // Write operations - these invalidate cache
    public async Task<PostDto> CreatePostAsync(Guid authorId, CreatePostDto createDto, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreatePostAsync(authorId, createDto, cancellationToken);

        // Invalidate related caches
        await InvalidatePostCaches(result.Id, result.Slug, result.Category?.Id, authorId);

        return result;
    }

    public async Task<PostDto> CreatePostAsync(CreatePostRequest request, Guid authorId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.CreatePostAsync(request, authorId, cancellationToken);

        // Invalidate related caches
        await InvalidatePostCaches(result.Id, result.Slug, result.Category?.Id, authorId);

        return result;
    }

    public async Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostDto updateDto, CancellationToken cancellationToken = default)
    {
        // Get the old post to invalidate old slug cache if slug changed
        var oldPost = await _blogService.GetPostByIdAsync(id, cancellationToken);

        var result = await _blogService.UpdatePostAsync(id, updateDto, cancellationToken);

        if (result != null)
        {
            // Invalidate caches for both old and new data
            await InvalidatePostCaches(id, result.Slug, result.Category?.Id, result.AuthorId);

            // If slug changed, also invalidate old slug cache
            if (oldPost != null && oldPost.Slug != result.Slug)
            {
                await _cacheService.RemoveAsync(CacheKeys.PostBySlug(oldPost.Slug), cancellationToken);
            }
        }

        return result;
    }

    public async Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        // Get the old post to invalidate old slug cache if slug changed
        var oldPost = await _blogService.GetPostByIdAsync(id, cancellationToken);

        var result = await _blogService.UpdatePostAsync(id, request, userId, cancellationToken);

        if (result != null)
        {
            // Invalidate caches for both old and new data
            await InvalidatePostCaches(id, result.Slug, result.Category?.Id, userId);

            // If slug changed, also invalidate old slug cache
            if (oldPost != null && oldPost.Slug != result.Slug)
            {
                await _cacheService.RemoveAsync(CacheKeys.PostBySlug(oldPost.Slug), cancellationToken);
            }
        }

        return result;
    }

    public async Task<OperationResult> PublishPostAsync(Guid id, PublishPostRequest? request, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PublishPostAsync(id, request, userId, cancellationToken);

        if (result.Success)
        {
            // Publishing changes post visibility, invalidate all post-related caches
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<PostDto?> PublishPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PublishPostAsync(id, cancellationToken);

        if (result != null)
        {
            // Publishing changes post visibility, invalidate all post-related caches
            await InvalidatePostCaches(id, null, null, result.AuthorId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<PostDto?> UnpublishPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UnpublishPostAsync(id, cancellationToken);

        if (result != null)
        {
            await InvalidatePostCaches(id, null, null, result.AuthorId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _blogService.DeletePostAsync(id, cancellationToken);

        // Invalidate all post-related caches since we don't have post details after deletion
        await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
    }

    public async Task<OperationResult> UnpublishPostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UnpublishPostAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> ArchivePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ArchivePostAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
        }

        return result;
    }

    public async Task<OperationResult> DeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.DeletePostAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> PermanentlyDeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.PermanentlyDeletePostAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> RestorePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.RestorePostAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
        }

        return result;
    }

    public async Task<PostDto?> DuplicatePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.DuplicatePostAsync(id, userId, cancellationToken);

        if (result != null)
        {
            // New post created, invalidate list caches
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.IncrementViewCountAsync(id, cancellationToken);

        if (result.Success)
        {
            // View count changed, remove cached post
            await _cacheService.RemoveAsync(CacheKeys.Post(id), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> ToggleFeaturedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ToggleFeaturedAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> ToggleStickyAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ToggleStickyAsync(id, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    public async Task<OperationResult> UpdatePostTagsAsync(Guid id, IEnumerable<Guid> tagIds, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.UpdatePostTagsAsync(id, tagIds, userId, cancellationToken);

        if (result.Success)
        {
            await InvalidatePostCaches(id, null, null, userId);
            // Tag changes affect tag-related caches too
            await _cacheService.RemoveByPatternAsync(CacheKeys.TagPattern(), cancellationToken);
        }

        return result;
    }

    // Read-only operations that can be cached
    public async Task<IEnumerable<PostDto>> GetScheduledPostsAsync(CancellationToken cancellationToken = default)
    {
        // Scheduled posts change frequently, use short cache
        const string cacheKey = "scheduled:posts";

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            () => _blogService.GetScheduledPostsAsync(cancellationToken),
            _options.ShortExpiration,
            cancellationToken) ?? Enumerable.Empty<PostDto>();
    }

    public async Task<int> ProcessScheduledPostsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _blogService.ProcessScheduledPostsAsync(cancellationToken);

        if (result > 0)
        {
            // Scheduled posts were published, invalidate caches
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
            await _cacheService.RemoveAsync("scheduled:posts", cancellationToken);
        }

        return result;
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludePostId = null, CancellationToken cancellationToken = default)
    {
        // Don't cache slug uniqueness checks as they're typically one-time operations
        return await _blogService.IsSlugUniqueAsync(slug, excludePostId, cancellationToken);
    }

    public async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludePostId = null, CancellationToken cancellationToken = default)
    {
        // Don't cache slug generation
        return await _blogService.GenerateUniqueSlugAsync(title, excludePostId, cancellationToken);
    }

    public async Task<BulkOperationResult> BulkOperationAsync(IEnumerable<Guid> postIds, string operation, Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _blogService.BulkOperationAsync(postIds, operation, userId, cancellationToken);

        if (result.SuccessCount > 0)
        {
            // Bulk operations affect multiple posts, clear all post-related caches
            await _cacheService.RemoveByPatternAsync(CacheKeys.PostPattern(), cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Invalidates caches related to a specific post
    /// </summary>
    private async Task InvalidatePostCaches(Guid postId, string? slug = null, Guid? categoryId = null, Guid? authorId = null)
    {
        var tasks = new List<Task>
        {
            _cacheService.RemoveAsync(CacheKeys.Post(postId))
        };

        if (!string.IsNullOrEmpty(slug))
        {
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.PostBySlug(slug)));
        }

        // Clear list caches - these are more expensive but necessary for consistency
        tasks.Add(_cacheService.RemoveByPatternAsync(CacheKeys.PostPattern()));

        if (categoryId.HasValue)
        {
            tasks.Add(_cacheService.RemoveByPatternAsync(CacheKeys.CategoryPattern()));
        }

        if (authorId.HasValue)
        {
            tasks.Add(_cacheService.RemoveAsync(CacheKeys.AuthorStats(authorId.Value)));
        }

        await Task.WhenAll(tasks);

        _logger.LogDebug("Invalidated caches for post {PostId}", postId);
    }
}