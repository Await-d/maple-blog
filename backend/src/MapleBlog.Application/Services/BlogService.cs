using AutoMapper;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using AppMarkdownService = MapleBlog.Application.Interfaces.IApplicationMarkdownService;

namespace MapleBlog.Application.Services;

/// <summary>
/// Blog service implementation for article management operations
/// </summary>
public class BlogService : IBlogService
{
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUserRepository _userRepository;
    private readonly AppMarkdownService _markdownService;
    private readonly IMapper _mapper;
    private readonly ILogger<BlogService> _logger;

    public BlogService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IUserRepository userRepository,
        AppMarkdownService markdownService,
        IMapper mapper,
        ILogger<BlogService> logger)
    {
        _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _markdownService = markdownService ?? throw new ArgumentNullException(nameof(markdownService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PostDto?> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            return post != null ? _mapper.Map<PostDto>(post) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving post {PostId}", id);
            throw;
        }
    }

    public async Task<PostDto?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var post = await _postRepository.GetBySlugAsync(slug, cancellationToken);
            return post != null ? _mapper.Map<PostDto>(post) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving post by slug {Slug}", slug);
            throw;
        }
    }

    public async Task<PostListResponse> GetPostsAsync(PostQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var posts = await GetFilteredPostsAsync(query, cancellationToken);
            var totalCount = await GetFilteredPostsCountAsync(query, cancellationToken);

            var postDtos = _mapper.Map<List<PostListDto>>(posts);

            return CreatePostListResponse(postDtos, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts with query {@Query}", query);
            throw;
        }
    }

    public async Task<PostListResponse> GetPostsByAuthorAsync(Guid authorId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            query.AuthorId = authorId;
            return await GetPostsAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts by author {AuthorId}", authorId);
            throw;
        }
    }

    public async Task<PostListResponse> GetPostsByCategoryAsync(Guid categoryId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            query.CategoryId = categoryId;
            return await GetPostsAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts by category {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<PostListResponse> GetPostsByTagAsync(Guid tagId, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            query.TagIds = new List<Guid> { tagId };
            return await GetPostsAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving posts by tag {TagId}", tagId);
            throw;
        }
    }

    public async Task<PostListResponse> SearchPostsAsync(string searchQuery, PostQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            query.Search = searchQuery;
            return await GetPostsAsync(query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts with query {SearchQuery}", searchQuery);
            throw;
        }
    }

    public async Task<PostDto> CreatePostAsync(CreatePostRequest request, Guid authorId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating post {Title} by author {AuthorId}", request.Title, authorId);

            // Validate author exists
            var author = await _userRepository.GetByIdAsync(authorId, cancellationToken);
            if (author == null)
                throw new ArgumentException($"Author with ID {authorId} not found.", nameof(authorId));

            // Validate category exists (if provided)
            if (request.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken);
                if (category == null)
                    throw new ArgumentException($"Category with ID {request.CategoryId.Value} not found.", nameof(request.CategoryId));
            }

            // Validate tags exist
            if (request.TagIds.Any())
            {
                var existingTags = await _tagRepository.GetByIdsAsync(request.TagIds, cancellationToken);
                var missingTagIds = request.TagIds.Except(existingTags.Select(t => t.Id)).ToList();
                if (missingTagIds.Any())
                    throw new ArgumentException($"Tags with IDs {string.Join(", ", missingTagIds)} not found.", nameof(request.TagIds));
            }

            // Create post entity
            var post = _mapper.Map<Post>(request);
            post.AuthorId = authorId;

            // Generate unique slug if not provided
            if (string.IsNullOrWhiteSpace(post.Slug))
            {
                post.Slug = await GenerateUniqueSlugAsync(request.Title, cancellationToken: cancellationToken);
            }
            else
            {
                // Validate slug uniqueness
                if (!await IsSlugUniqueAsync(post.Slug, cancellationToken: cancellationToken))
                {
                    post.Slug = await GenerateUniqueSlugAsync(post.Slug, cancellationToken: cancellationToken);
                }
            }

            // Process content and extract metadata
            await ProcessContentMetadataAsync(post, cancellationToken);

            // Save post
            await _postRepository.AddAsync(post, cancellationToken);
            await _postRepository.SaveChangesAsync(cancellationToken);

            // Handle tags association
            if (request.TagIds.Any())
            {
                await UpdatePostTagsInternalAsync(post.Id, request.TagIds, cancellationToken);
            }

            // Reload with related data
            var savedPost = await _postRepository.GetByIdAsync(post.Id, cancellationToken);
            var result = _mapper.Map<PostDto>(savedPost);

            _logger.LogInformation("Successfully created post {PostId} ({Title})", post.Id, post.Title);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post {Title}", request.Title);
            throw;
        }
    }

    public async Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating post {PostId} by user {UserId}", id, userId);

            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return null;

            // Check permissions (author or admin)
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                throw new UnauthorizedAccessException("You don't have permission to update this post.");

            // Validate category exists (if provided)
            if (request.CategoryId.HasValue)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId.Value, cancellationToken);
                if (category == null)
                    throw new ArgumentException($"Category with ID {request.CategoryId.Value} not found.", nameof(request.CategoryId));
            }

            // Validate tags exist
            if (request.TagIds.Any())
            {
                var existingTags = await _tagRepository.GetByIdsAsync(request.TagIds, cancellationToken);
                var missingTagIds = request.TagIds.Except(existingTags.Select(t => t.Id)).ToList();
                if (missingTagIds.Any())
                    throw new ArgumentException($"Tags with IDs {string.Join(", ", missingTagIds)} not found.", nameof(request.TagIds));
            }

            // Update post properties
            _mapper.Map(request, post);

            // Handle slug changes
            if (!string.Equals(post.Slug, request.Slug, StringComparison.OrdinalIgnoreCase))
            {
                if (!await IsSlugUniqueAsync(request.Slug!, id, cancellationToken))
                {
                    post.Slug = await GenerateUniqueSlugAsync(request.Slug!, id, cancellationToken);
                }
            }

            // Process content and extract metadata
            await ProcessContentMetadataAsync(post, cancellationToken);

            // Update post
            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            // Update tags association
            await UpdatePostTagsInternalAsync(id, request.TagIds, cancellationToken);

            // Reload with related data
            var updatedPost = await _postRepository.GetByIdAsync(id, cancellationToken);
            var result = _mapper.Map<PostDto>(updatedPost);

            _logger.LogInformation("Successfully updated post {PostId}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post {PostId}", id);
            throw;
        }
    }

    public async Task<OperationResult> PublishPostAsync(Guid id, PublishPostRequest? request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to publish this post.");

            if (request?.ScheduledAt.HasValue == true)
            {
                // Schedule for later publication
                post.Status = PostStatus.Scheduled;
                post.PublishedAt = request.ScheduledAt.Value;
            }
            else
            {
                // Publish immediately
                post.Publish();
            }

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} published by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post published successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post {PostId}", id);
            return OperationResult.Failure("An error occurred while publishing the post.");
        }
    }

    public async Task<OperationResult> UnpublishPostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to unpublish this post.");

            post.Unpublish();

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} unpublished by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post unpublished successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing post {PostId}", id);
            return OperationResult.Failure("An error occurred while unpublishing the post.");
        }
    }

    public async Task<OperationResult> ArchivePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to archive this post.");

            post.Archive();

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} archived by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post archived successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving post {PostId}", id);
            return OperationResult.Failure("An error occurred while archiving the post.");
        }
    }

    public async Task<OperationResult> DeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to delete this post.");

            // Soft delete
            post.IsDeleted = true;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedBy = userId;

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} soft deleted by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            return OperationResult.Failure("An error occurred while deleting the post.");
        }
    }

    public async Task<OperationResult> PermanentlyDeletePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Only admins can permanently delete posts
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.HasRole("Admin"))
                return OperationResult.Failure("Only administrators can permanently delete posts.");

            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            _postRepository.Remove(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} permanently deleted by admin {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post permanently deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting post {PostId}", id);
            return OperationResult.Failure("An error occurred while permanently deleting the post.");
        }
    }

    public async Task<OperationResult> RestorePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to restore this post.");

            if (!post.IsDeleted)
                return OperationResult.Failure("Post is not deleted.");

            post.IsDeleted = false;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedBy = userId;

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} restored by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post restored successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring post {PostId}", id);
            return OperationResult.Failure("An error occurred while restoring the post.");
        }
    }

    public async Task<PostDto?> DuplicatePostAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var originalPost = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (originalPost == null)
                return null;

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return null;

            // Create duplicate
            var duplicate = new Post
            {
                Id = Guid.NewGuid(),
                Title = $"Copy of {originalPost.Title}",
                Content = originalPost.Content,
                ContentType = originalPost.ContentType,
                Summary = originalPost.Summary,
                CategoryId = originalPost.CategoryId,
                AuthorId = userId, // Set current user as author
                Status = PostStatus.Draft, // Always start as draft
                AllowComments = originalPost.AllowComments,
                IsFeatured = false, // Don't copy featured status
                IsSticky = false, // Don't copy sticky status
                MetaTitle = originalPost.MetaTitle,
                MetaDescription = originalPost.MetaDescription,
                MetaKeywords = originalPost.MetaKeywords,
                CanonicalUrl = null, // Reset canonical URL
                OgTitle = originalPost.OgTitle,
                OgDescription = originalPost.OgDescription,
                OgImageUrl = originalPost.OgImageUrl,
                Language = originalPost.Language,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            // Generate unique slug
            duplicate.Slug = await GenerateUniqueSlugAsync(duplicate.Title, cancellationToken: cancellationToken);

            // Process content metadata
            await ProcessContentMetadataAsync(duplicate, cancellationToken);

            await _postRepository.AddAsync(duplicate, cancellationToken);
            await _postRepository.SaveChangesAsync(cancellationToken);

            // Copy tags if any
            var originalTags = originalPost.PostTags.Select(pt => pt.TagId).ToList();
            if (originalTags.Any())
            {
                await UpdatePostTagsInternalAsync(duplicate.Id, originalTags, cancellationToken);
            }

            // Reload with related data
            var savedPost = await _postRepository.GetByIdAsync(duplicate.Id, cancellationToken);
            var result = _mapper.Map<PostDto>(savedPost);

            _logger.LogInformation("Post {PostId} duplicated as {DuplicateId} by user {UserId}", id, duplicate.Id, userId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating post {PostId}", id);
            throw;
        }
    }

    public async Task<OperationResult> IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            post.IncreaseViewCount();

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            return OperationResult.CreateSuccess("View count incremented.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for post {PostId}", id);
            return OperationResult.Failure("An error occurred while updating view count.");
        }
    }

    public async Task<OperationResult> ToggleFeaturedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions (admin only)
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.HasRole("Admin"))
                return OperationResult.Failure("Only administrators can manage featured posts.");

            post.IsFeatured = !post.IsFeatured;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedBy = userId;

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} featured status toggled to {IsFeatured} by admin {UserId}",
                id, post.IsFeatured, userId);

            return OperationResult.CreateSuccess($"Post {(post.IsFeatured ? "featured" : "unfeatured")} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling featured status for post {PostId}", id);
            return OperationResult.Failure("An error occurred while updating featured status.");
        }
    }

    public async Task<OperationResult> ToggleStickyAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions (admin only)
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || !user.HasRole("Admin"))
                return OperationResult.Failure("Only administrators can manage sticky posts.");

            post.IsSticky = !post.IsSticky;
            post.UpdatedAt = DateTime.UtcNow;
            post.UpdatedBy = userId;

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post {PostId} sticky status toggled to {IsSticky} by admin {UserId}",
                id, post.IsSticky, userId);

            return OperationResult.CreateSuccess($"Post {(post.IsSticky ? "stickied" : "unstickied")} successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling sticky status for post {PostId}", id);
            return OperationResult.Failure("An error occurred while updating sticky status.");
        }
    }

    public async Task<OperationResult> UpdatePostTagsAsync(Guid id, IEnumerable<Guid> tagIds, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null)
                return OperationResult.Failure("Post not found.");

            // Check permissions
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null || (post.AuthorId != userId && !user.HasRole("Admin")))
                return OperationResult.Failure("You don't have permission to update this post's tags.");

            await UpdatePostTagsInternalAsync(id, tagIds, cancellationToken);

            _logger.LogInformation("Post {PostId} tags updated by user {UserId}", id, userId);
            return OperationResult.CreateSuccess("Post tags updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tags for post {PostId}", id);
            return OperationResult.Failure("An error occurred while updating post tags.");
        }
    }

    public async Task<IEnumerable<PostDto>> GetScheduledPostsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var posts = await _postRepository.GetScheduledForPublicationAsync(cancellationToken);
            return _mapper.Map<IEnumerable<PostDto>>(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled posts");
            throw;
        }
    }

    public async Task<int> ProcessScheduledPostsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduledPosts = await _postRepository.GetScheduledForPublicationAsync(cancellationToken);
            var publishedCount = 0;

            foreach (var post in scheduledPosts)
            {
                if (post.PublishedAt <= DateTime.UtcNow)
                {
                    post.Publish();
                    _postRepository.Update(post);
                    publishedCount++;
                }
            }

            if (publishedCount > 0)
            {
                await _postRepository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Published {Count} scheduled posts", publishedCount);
            }

            return publishedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled posts");
            throw;
        }
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludePostId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _postRepository.IsSlugAvailableAsync(slug, excludePostId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking slug uniqueness for {Slug}", slug);
            throw;
        }
    }

    public async Task<string> GenerateUniqueSlugAsync(string title, Guid? excludePostId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseSlug = GenerateSlugFromTitle(title);
            var slug = baseSlug;
            var counter = 1;

            while (!await _postRepository.IsSlugAvailableAsync(slug, excludePostId, cancellationToken))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unique slug for title {Title}", title);
            throw;
        }
    }

    public async Task<BulkOperationResult> BulkOperationAsync(IEnumerable<Guid> postIds, string operation, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return BulkOperationResult.Failure(new[] { "User not found." });

            var postIdList = postIds.ToList();
            var posts = await _postRepository.FindAsync(p => postIdList.Contains(p.Id), cancellationToken);

            var errors = new List<string>();
            var successCount = 0;

            foreach (var post in posts)
            {
                try
                {
                    // Check permissions for each post
                    if (post.AuthorId != userId && !user.HasRole("Admin"))
                    {
                        errors.Add($"No permission to modify post '{post.Title}'");
                        continue;
                    }

                    switch (operation.ToLowerInvariant())
                    {
                        case "publish":
                            if (post.Status != PostStatus.Published)
                            {
                                post.Publish();
                                successCount++;
                            }
                            break;

                        case "unpublish":
                            if (post.Status == PostStatus.Published)
                            {
                                post.Unpublish();
                                successCount++;
                            }
                            break;

                        case "archive":
                            if (post.Status != PostStatus.Archived)
                            {
                                post.Archive();
                                successCount++;
                            }
                            break;

                        case "delete":
                            if (!post.IsDeleted)
                            {
                                post.IsDeleted = true;
                                post.UpdatedAt = DateTime.UtcNow;
                                post.UpdatedBy = userId;
                                successCount++;
                            }
                            break;

                        default:
                            errors.Add($"Unknown operation: {operation}");
                            continue;
                    }

                    _postRepository.Update(post);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing post {PostId} in bulk operation {Operation}", post.Id, operation);
                    errors.Add($"Error processing post '{post.Title}': {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                await _postRepository.SaveChangesAsync(cancellationToken);
            }

            var failureCount = postIdList.Count - successCount;

            if (successCount > 0 && failureCount == 0)
                return BulkOperationResult.CreateSuccess(successCount, $"Successfully {operation}ed {successCount} posts.");

            if (successCount == 0)
                return BulkOperationResult.Failure(errors, 0, failureCount);

            return BulkOperationResult.Mixed(successCount, failureCount, errors,
                $"Processed {successCount} posts successfully, {failureCount} failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk operation {Operation}", operation);
            return BulkOperationResult.Failure(new[] { "An unexpected error occurred during bulk operation." });
        }
    }

    // Private helper methods

    private async Task<IReadOnlyList<Post>> GetFilteredPostsAsync(PostQueryDto query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            return await _postRepository.SearchAsync(query.Search, query.Page, query.PageSize,
                query.Status != PostStatus.Draft, cancellationToken);
        }

        if (query.CategoryId.HasValue)
        {
            return await _postRepository.GetByCategoryAsync(query.CategoryId.Value, query.Page, query.PageSize,
                query.Status != PostStatus.Draft, cancellationToken);
        }

        if (query.TagIds != null && query.TagIds.Any())
        {
            return await _postRepository.GetByTagsAsync(query.TagIds, query.Page, query.PageSize,
                query.Status != PostStatus.Draft, cancellationToken);
        }

        if (query.AuthorId.HasValue)
        {
            return await _postRepository.GetByAuthorAsync(query.AuthorId.Value, query.Page, query.PageSize,
                query.Status, cancellationToken);
        }

        if (query.IsFeatured == true)
        {
            return await _postRepository.GetFeaturedAsync(query.Page, query.PageSize,
                query.Status != PostStatus.Draft, cancellationToken);
        }

        return await _postRepository.GetPostsWithDetailsAsync(query.Page, query.PageSize, cancellationToken);
    }

    private async Task<int> GetFilteredPostsCountAsync(PostQueryDto query, CancellationToken cancellationToken)
    {
        // This is a simplified count - in a real implementation you'd want more precise counting
        // that matches the filtering logic above
        return await _postRepository.CountAsync(p => !p.IsDeleted, cancellationToken);
    }

    private static PostListResponse CreatePostListResponse(List<PostListDto> posts, int totalCount, int currentPage, int pageSize)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PostListResponse
        {
            Items = posts,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            PageSize = pageSize,
            HasNext = currentPage < totalPages,
            HasPrevious = currentPage > 1
        };
    }

    private async Task ProcessContentMetadataAsync(Post post, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(post.Content))
        {
            // Get markdown stats
            var markdownStats = await _markdownService.GetMarkdownStatsAsync(post.Content);
            post.WordCount = markdownStats.WordCount;
            post.ReadingTime = CalculateReadingTime(markdownStats.WordCount);

            // Generate summary if not provided
            if (string.IsNullOrEmpty(post.Summary))
            {
                var plainText = await _markdownService.ExtractPlainTextAsync(post.Content);
                post.Summary = ExtractSummary(plainText, 200);
            }
        }
    }

    private static string ExtractSummary(string content, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Remove extra whitespace and newlines
        var cleanContent = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();

        if (cleanContent.Length <= maxLength)
            return cleanContent;

        // Find the last complete sentence within the limit
        var truncated = cleanContent.Substring(0, maxLength);
        var lastSentenceEnd = Math.Max(
            truncated.LastIndexOf('.'),
            Math.Max(truncated.LastIndexOf('!'), truncated.LastIndexOf('?'))
        );

        if (lastSentenceEnd > maxLength / 2) // If we found a sentence end in the latter half
        {
            return truncated.Substring(0, lastSentenceEnd + 1).Trim();
        }

        // Otherwise, find the last space and truncate there
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 0)
        {
            return truncated.Substring(0, lastSpace).Trim() + "...";
        }

        return truncated + "...";
    }

    private static int CalculateReadingTime(int wordCount)
    {
        // Average reading speed is about 200-250 words per minute
        // We'll use 250 words per minute for calculation
        const int wordsPerMinute = 250;
        var minutes = Math.Ceiling((double)wordCount / wordsPerMinute);
        return Math.Max(1, (int)minutes); // Minimum 1 minute
    }

    private async Task UpdatePostTagsInternalAsync(Guid postId, IEnumerable<Guid> tagIds, CancellationToken cancellationToken)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null) return;

        // Remove existing tags
        var existingTags = post.PostTags.ToList();
        foreach (var postTag in existingTags)
        {
            post.PostTags.Remove(postTag);
        }

        // Add new tags
        var tags = await _tagRepository.GetByIdsAsync(tagIds, cancellationToken);
        foreach (var tag in tags)
        {
            post.PostTags.Add(new PostTag { PostId = postId, TagId = tag.Id });
            tag.IncreaseUsageCount();
        }

        // Update usage counts for removed tags
        var removedTagIds = existingTags.Select(pt => pt.TagId).Except(tagIds).ToList();
        if (removedTagIds.Any())
        {
            var removedTags = await _tagRepository.GetByIdsAsync(removedTagIds, cancellationToken);
            foreach (var tag in removedTags)
            {
                tag.DecreaseUsageCount();
                _tagRepository.Update(tag);
            }
        }

        _postRepository.Update(post);
        await _postRepository.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSlugFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return title.ToLowerInvariant()
                   .Replace(" ", "-")
                   .Replace("_", "-")
                   .Trim('-');
    }

    // Controller compatibility methods

    public async Task<PagedResultDto<PostDto>> GetPublishedPostsAsync(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new PostQueryDto
            {
                Page = pageNumber,
                PageSize = pageSize,
                Status = PostStatus.Published,
                CategoryId = categoryId,
                TagIds = tagId.HasValue ? new List<Guid> { tagId.Value } : null,
                Search = searchTerm
            };

            var response = await GetPostsAsync(query, cancellationToken);

            return PagedResultDto<PostDto>.Create(
                response.Items.Select(_mapper.Map<PostDto>).ToList(),
                response.TotalCount,
                response.CurrentPage,
                response.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published posts");
            throw;
        }
    }

    public async Task<PagedResultDto<PostDto>> GetAllPostsAsync(int pageNumber, int pageSize, PostStatus? status = null, Guid? authorId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new PostQueryDto
            {
                Page = pageNumber,
                PageSize = pageSize,
                Status = status,
                AuthorId = authorId
            };

            var response = await GetPostsAsync(query, cancellationToken);

            return PagedResultDto<PostDto>.Create(
                response.Items.Select(_mapper.Map<PostDto>).ToList(),
                response.TotalCount,
                response.CurrentPage,
                response.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all posts");
            throw;
        }
    }

    public async Task<PagedResultDto<PostDto>> GetPostsByCategoryAsync(Guid categoryId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new PostQueryDto
            {
                Page = pageNumber,
                PageSize = pageSize,
                CategoryId = categoryId,
                Status = PostStatus.Published
            };

            var response = await GetPostsByCategoryAsync(categoryId, query, cancellationToken);

            return PagedResultDto<PostDto>.Create(
                response.Items.Select(_mapper.Map<PostDto>).ToList(),
                response.TotalCount,
                response.CurrentPage,
                response.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by category {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<PagedResultDto<PostDto>> GetPostsByTagAsync(Guid tagId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new PostQueryDto
            {
                Page = pageNumber,
                PageSize = pageSize,
                TagIds = new List<Guid> { tagId },
                Status = PostStatus.Published
            };

            var response = await GetPostsByTagAsync(tagId, query, cancellationToken);

            return PagedResultDto<PostDto>.Create(
                response.Items.Select(_mapper.Map<PostDto>).ToList(),
                response.TotalCount,
                response.CurrentPage,
                response.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts by tag {TagId}", tagId);
            throw;
        }
    }

    public async Task<PostDto> CreatePostAsync(Guid authorId, CreatePostDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // Map CreatePostDto to CreatePostRequest
            var request = _mapper.Map<CreatePostRequest>(createDto);
            return await CreatePostAsync(request, authorId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post from DTO");
            throw;
        }
    }

    public async Task<PostDto?> UpdatePostAsync(Guid id, UpdatePostDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            // For this simplified implementation, we'll need the current user ID
            // In a real implementation, this should be passed as a parameter
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null) return null;

            // Map UpdatePostDto to UpdatePostRequest
            var request = _mapper.Map<UpdatePostRequest>(updateDto);
            return await UpdatePostAsync(id, request, post.AuthorId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post from DTO");
            throw;
        }
    }

    public async Task<PostDto?> PublishPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null) return null;

            var result = await PublishPostAsync(id, null, post.AuthorId, cancellationToken);
            if (!result.Success) return null;

            return await GetPostByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing post {PostId}", id);
            throw;
        }
    }

    public async Task<PostDto?> UnpublishPostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null) return null;

            var result = await UnpublishPostAsync(id, post.AuthorId, cancellationToken);
            if (!result.Success) return null;

            return await GetPostByIdAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing post {PostId}", id);
            throw;
        }
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var post = await _postRepository.GetByIdAsync(id, cancellationToken);
            if (post == null) return;

            await DeletePostAsync(id, post.AuthorId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting post {PostId}", id);
            throw;
        }
    }
}