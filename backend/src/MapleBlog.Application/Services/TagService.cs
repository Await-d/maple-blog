using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services;

/// <summary>
/// Tag service implementation for tag management operations
/// </summary>
public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<TagService> _logger;

    public TagService(
        ITagRepository tagRepository,
        IPostRepository postRepository,
        IUserRepository userRepository,
        IUserContextService userContextService,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a tag by its ID
    /// </summary>
    public async Task<TagDto?> GetTagByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            return tag != null ? MapToDto(tag) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by ID {TagId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets a tag by its slug
    /// </summary>
    public async Task<TagDto?> GetTagBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var tags = await _tagRepository.FindAsync(t => t.Slug == slug.Trim(), cancellationToken);
            var tag = tags.FirstOrDefault();
            return tag != null ? MapToDto(tag) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by slug {Slug}", slug);
            throw;
        }
    }

    /// <summary>
    /// Gets a tag by its name
    /// </summary>
    public async Task<TagDto?> GetTagByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var tags = await _tagRepository.FindAsync(t => t.Name == name.Trim(), cancellationToken);
            var tag = tags.FirstOrDefault();
            return tag != null ? MapToDto(tag) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag by name {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Gets all tags with filtering and pagination
    /// </summary>
    public async Task<TagListResponse> GetTagsAsync(TagQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagRepository.GetAllAsync(cancellationToken);

            // Apply filters
            var filteredTags = tags.AsQueryable();

            if (!query.IncludeInactive)
                filteredTags = filteredTags.Where(t => t.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLowerInvariant();
                filteredTags = filteredTags.Where(t =>
                    t.Name.ToLowerInvariant().Contains(searchTerm) ||
                    (t.Description != null && t.Description.ToLowerInvariant().Contains(searchTerm)));
            }

            if (!query.IncludeUnused)
                filteredTags = filteredTags.Where(t => t.UsageCount > 0);

            if (query.MinUsageCount.HasValue)
                filteredTags = filteredTags.Where(t => t.UsageCount >= query.MinUsageCount.Value);

            if (query.MaxUsageCount.HasValue)
                filteredTags = filteredTags.Where(t => t.UsageCount <= query.MaxUsageCount.Value);

            // Apply sorting
            filteredTags = query.SortBy.ToLowerInvariant() switch
            {
                "name" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredTags.OrderByDescending(t => t.Name) :
                    filteredTags.OrderBy(t => t.Name),
                "usagecount" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredTags.OrderByDescending(t => t.UsageCount) :
                    filteredTags.OrderBy(t => t.UsageCount),
                "createdat" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredTags.OrderByDescending(t => t.CreatedAt) :
                    filteredTags.OrderBy(t => t.CreatedAt),
                _ => filteredTags.OrderBy(t => t.Name)
            };

            var totalCount = filteredTags.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var pagedTags = filteredTags
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return new TagListResponse
            {
                Items = pagedTags.Select(MapToListDto).ToList(),
                TotalCount = totalCount,
                CurrentPage = query.Page,
                TotalPages = totalPages,
                PageSize = query.PageSize,
                HasNext = query.Page < totalPages,
                HasPrevious = query.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tags with query {@Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Gets popular tags for tag cloud display
    /// </summary>
    public async Task<List<TagCloudDto>> GetTagCloudAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagRepository.FindAsync(t => t.IsActive && t.UsageCount > 0, cancellationToken);
            var sortedTags = tags.OrderByDescending(t => t.UsageCount).Take(count).ToList();

            if (!sortedTags.Any())
                return new List<TagCloudDto>();

            var maxUsage = sortedTags.Max(t => t.UsageCount);
            var minUsage = sortedTags.Min(t => t.UsageCount);

            return sortedTags.Select(tag => new TagCloudDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Color = tag.Color,
                UsageCount = tag.UsageCount,
                Weight = CalculateWeight(tag.UsageCount, minUsage, maxUsage)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag cloud");
            throw;
        }
    }

    /// <summary>
    /// Gets tag suggestions based on partial name match
    /// </summary>
    public async Task<List<TagAutoCompleteDto>> GetTagSuggestionsAsync(string partialName, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(partialName))
                return new List<TagAutoCompleteDto>();

            var searchTerm = partialName.Trim().ToLowerInvariant();
            var tags = await _tagRepository.FindAsync(t =>
                t.IsActive && t.Name.ToLowerInvariant().Contains(searchTerm), cancellationToken);

            return tags
                .OrderByDescending(t => t.UsageCount)
                .ThenBy(t => t.Name)
                .Take(limit)
                .Select(t => new TagAutoCompleteDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    Color = t.Color,
                    UsageCount = t.UsageCount
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag suggestions for {PartialName}", partialName);
            throw;
        }
    }

    /// <summary>
    /// Gets intelligent tag suggestions based on content analysis
    /// </summary>
    public async Task<List<TagSuggestionDto>> GetIntelligentTagSuggestionsAsync(string content, string? title = null, IEnumerable<Guid>? existingTagIds = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = new List<TagSuggestionDto>();
            var excludeIds = existingTagIds?.ToHashSet() ?? new HashSet<Guid>();

            // Simple keyword extraction from content and title
            var keywords = ExtractKeywords(content, title);

            // Get existing tags that match keywords
            var existingTags = await _tagRepository.FindAsync(t =>
                t.IsActive && keywords.Any(k => t.Name.ToLowerInvariant().Contains(k.ToLowerInvariant())),
                cancellationToken);

            foreach (var tag in existingTags.Where(t => !excludeIds.Contains(t.Id)))
            {
                var relevanceScore = CalculateRelevanceScore(tag.Name, keywords);
                if (relevanceScore > 0.3) // Minimum relevance threshold
                {
                    suggestions.Add(new TagSuggestionDto
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        Slug = tag.Slug,
                        RelevanceScore = relevanceScore,
                        UsageCount = tag.UsageCount,
                        IsExisting = true
                    });
                }
            }

            // Suggest new tags for keywords that don't match existing tags
            foreach (var keyword in keywords.Take(5)) // Limit new suggestions
            {
                if (!existingTags.Any(t => t.Name.Equals(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestions.Add(new TagSuggestionDto
                    {
                        Id = null,
                        Name = keyword,
                        Slug = GenerateSlug(keyword),
                        RelevanceScore = 0.5,
                        UsageCount = 0,
                        IsExisting = false
                    });
                }
            }

            return suggestions
                .OrderByDescending(s => s.RelevanceScore)
                .ThenByDescending(s => s.UsageCount)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting intelligent tag suggestions");
            throw;
        }
    }

    /// <summary>
    /// Gets related tags based on co-occurrence
    /// </summary>
    public async Task<List<TagDto>> GetRelatedTagsAsync(Guid tagId, int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get posts that use this tag
            var postsWithTag = await _postRepository.FindAsync(p =>
                p.PostTags.Any(pt => pt.TagId == tagId), cancellationToken);

            if (!postsWithTag.Any())
                return new List<TagDto>();

            // Get all tags from these posts except the original tag
            var relatedTagIds = postsWithTag
                .SelectMany(p => p.PostTags)
                .Where(pt => pt.TagId != tagId)
                .GroupBy(pt => pt.TagId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToList();

            var relatedTags = await _tagRepository.FindAsync(t =>
                relatedTagIds.Contains(t.Id) && t.IsActive, cancellationToken);

            return relatedTags.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting related tags for {TagId}", tagId);
            throw;
        }
    }

    /// <summary>
    /// Gets trending tags with recent activity
    /// </summary>
    public async Task<List<TagDto>> GetTrendingTagsAsync(int count = 20, int daysBack = 7, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            // Get posts created in the time period
            var recentPosts = await _postRepository.FindAsync(p =>
                p.CreatedAt >= cutoffDate, cancellationToken);

            if (!recentPosts.Any())
                return new List<TagDto>();

            // Get tags from recent posts and count their usage
            var trendingTagIds = recentPosts
                .SelectMany(p => p.PostTags)
                .GroupBy(pt => pt.TagId)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToList();

            var trendingTags = await _tagRepository.FindAsync(t =>
                trendingTagIds.Contains(t.Id) && t.IsActive, cancellationToken);

            return trendingTags
                .OrderByDescending(t => t.UsageCount)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending tags");
            throw;
        }
    }

    /// <summary>
    /// Gets unused tags that can be cleaned up
    /// </summary>
    public async Task<List<TagDto>> GetUnusedTagsAsync(int? olderThanDays = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagRepository.FindAsync(t => t.UsageCount == 0, cancellationToken);

            if (olderThanDays.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays.Value);
                tags = tags.Where(t => t.CreatedAt < cutoffDate).ToList();
            }

            return tags
                .OrderBy(t => t.CreatedAt)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unused tags");
            throw;
        }
    }

    /// <summary>
    /// Creates a new tag
    /// </summary>
    public async Task<TagDto> CreateTagAsync(CreateTagRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var slug = !string.IsNullOrWhiteSpace(request.Slug) ?
                request.Slug.Trim() :
                await GenerateUniqueSlugAsync(request.Name, cancellationToken: cancellationToken);

            var tag = new Tag
            {
                Name = request.Name.Trim(),
                Slug = slug,
                Description = request.Description?.Trim(),
                Color = request.Color?.Trim(),
                IsActive = request.IsActive,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            await _tagRepository.AddAsync(tag, cancellationToken);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tag created: {TagName} by user {UserId}", tag.Name, userId);
            return MapToDto(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tag {TagName}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing tag
    /// </summary>
    public async Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            if (tag == null)
                return null;

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            var slug = !string.IsNullOrWhiteSpace(request.Slug) ?
                request.Slug.Trim() :
                await GenerateUniqueSlugAsync(request.Name, id, cancellationToken);

            tag.Name = request.Name.Trim();
            tag.Slug = slug;
            tag.Description = request.Description?.Trim();
            tag.Color = request.Color?.Trim();
            tag.IsActive = request.IsActive;
            tag.UpdatedBy = userId;
            tag.UpdateAuditFields();

            _tagRepository.Update(tag);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tag updated: {TagName} by user {UserId}", tag.Name, userId);
            return MapToDto(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tag {TagId}", id);
            throw;
        }
    }

    /// <summary>
    /// Activates or deactivates a tag
    /// </summary>
    public async Task<OperationResult> SetTagStatusAsync(Guid id, bool isActive, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            if (tag == null)
                return OperationResult.Failure("Tag not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            tag.IsActive = isActive;
            tag.UpdatedBy = userId;
            tag.UpdateAuditFields();

            _tagRepository.Update(tag);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            var action = isActive ? "activated" : "deactivated";
            _logger.LogInformation("Tag {Action}: {TagName} by user {UserId}", action, tag.Name, userId);

            return OperationResult.CreateSuccess($"Tag {action} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tag status {TagId}", id);
            return OperationResult.Failure("An error occurred while updating tag status");
        }
    }

    /// <summary>
    /// Soft deletes a tag
    /// </summary>
    public async Task<OperationResult> DeleteTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            if (tag == null)
                return OperationResult.Failure("Tag not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            tag.SoftDelete(userId);
            _tagRepository.Update(tag);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tag soft deleted: {TagName} by user {UserId}", tag.Name, userId);
            return OperationResult.CreateSuccess("Tag deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tag {TagId}", id);
            return OperationResult.Failure("An error occurred while deleting tag");
        }
    }

    /// <summary>
    /// Permanently deletes a tag (admin only)
    /// </summary>
    public async Task<OperationResult> PermanentlyDeleteTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            if (tag == null)
                return OperationResult.Failure("Tag not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            // Check if tag is being used
            if (tag.UsageCount > 0)
                return OperationResult.Failure("Cannot permanently delete a tag that is being used");

            await _tagRepository.RemoveAsync(id, cancellationToken);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tag permanently deleted: {TagName} by user {UserId}", tag.Name, userId);
            return OperationResult.CreateSuccess("Tag permanently deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting tag {TagId}", id);
            return OperationResult.Failure("An error occurred while permanently deleting tag");
        }
    }

    /// <summary>
    /// Restores a soft-deleted tag
    /// </summary>
    public async Task<OperationResult> RestoreTagAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
            if (tag == null)
                return OperationResult.Failure("Tag not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            if (!tag.IsDeleted)
                return OperationResult.Failure("Tag is not deleted");

            tag.Restore(userId);
            _tagRepository.Update(tag);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tag restored: {TagName} by user {UserId}", tag.Name, userId);
            return OperationResult.CreateSuccess("Tag restored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring tag {TagId}", id);
            return OperationResult.Failure("An error occurred while restoring tag");
        }
    }

    /// <summary>
    /// Merges multiple tags into one target tag
    /// </summary>
    public async Task<OperationResult> MergeTagsAsync(MergeTagsRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var targetTag = await _tagRepository.GetByIdAsync(request.TargetTagId, cancellationToken);
            if (targetTag == null)
                return OperationResult.Failure("Target tag not found");

            var sourceTags = await _tagRepository.FindAsync(t =>
                request.SourceTagIds.Contains(t.Id), cancellationToken);
            var sourceTagsList = sourceTags.ToList();

            if (sourceTagsList.Count != request.SourceTagIds.Count)
                return OperationResult.Failure("Some source tags not found");

            // Update all posts that use source tags to use target tag instead
            var postsToUpdate = await _postRepository.FindAsync(p =>
                p.PostTags.Any(pt => request.SourceTagIds.Contains(pt.TagId)), cancellationToken);

            foreach (var post in postsToUpdate)
            {
                var sourcePostTags = post.PostTags.Where(pt => request.SourceTagIds.Contains(pt.TagId)).ToList();

                // Remove source tag associations
                foreach (var postTag in sourcePostTags)
                {
                    post.PostTags.Remove(postTag);
                }

                // Add target tag association if not already present
                if (!post.PostTags.Any(pt => pt.TagId == request.TargetTagId))
                {
                    post.PostTags.Add(new PostTag { PostId = post.Id, TagId = request.TargetTagId });
                }
            }

            // Update usage counts
            var totalUsage = sourceTagsList.Sum(t => t.UsageCount);
            targetTag.UsageCount += totalUsage;
            targetTag.UpdateAuditFields();

            // Delete or deactivate source tags
            if (request.DeleteSourceTags)
            {
                foreach (var sourceTag in sourceTagsList)
                {
                    await _tagRepository.RemoveAsync(sourceTag.Id, cancellationToken);
                }
            }
            else
            {
                foreach (var sourceTag in sourceTagsList)
                {
                    sourceTag.UsageCount = 0;
                    sourceTag.IsActive = false;
                    sourceTag.UpdateAuditFields();
                    _tagRepository.Update(sourceTag);
                }
            }

            _tagRepository.Update(targetTag);
            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tags merged: {SourceTags} into {TargetTag} by user {UserId}",
                string.Join(", ", sourceTagsList.Select(t => t.Name)), targetTag.Name, userId);

            return OperationResult.CreateSuccess($"Successfully merged {sourceTagsList.Count} tags into {targetTag.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging tags");
            return OperationResult.Failure("An error occurred while merging tags");
        }
    }

    /// <summary>
    /// Creates or finds tags from a list of names
    /// </summary>
    public async Task<List<TagDto>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new List<TagDto>();
            var normalizedNames = tagNames.Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

            var existingTags = await _tagRepository.FindAsync(t =>
                normalizedNames.Contains(t.Name), cancellationToken);
            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Add existing tags
            result.AddRange(existingTags.Select(MapToDto));

            // Create new tags for names that don't exist
            var newTagNames = normalizedNames.Where(n => !existingTagNames.Contains(n)).ToList();

            foreach (var tagName in newTagNames)
            {
                var createRequest = new CreateTagRequest
                {
                    Name = tagName,
                    IsActive = true
                };

                var newTag = await CreateTagAsync(createRequest, userId, cancellationToken);
                result.Add(newTag);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating tags");
            throw;
        }
    }

    /// <summary>
    /// Validates tag slug uniqueness
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeTagId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var existingTags = await _tagRepository.FindAsync(t =>
                t.Slug.ToLowerInvariant() == normalizedSlug, cancellationToken);

            return !existingTags.Any(t => excludeTagId == null || t.Id != excludeTagId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking slug uniqueness for {Slug}", slug);
            throw;
        }
    }

    /// <summary>
    /// Generates a unique slug from name
    /// </summary>
    public async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeTagId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseSlug = GenerateSlug(name);
            var slug = baseSlug;
            var counter = 1;

            while (!await IsSlugUniqueAsync(slug, excludeTagId, cancellationToken))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating unique slug for {Name}", name);
            throw;
        }
    }

    /// <summary>
    /// Updates usage counts for all tags (maintenance operation)
    /// </summary>
    public async Task<OperationResult> RecalculateUsageCountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            var updatedCount = 0;

            foreach (var tag in tags)
            {
                var actualUsage = await _postRepository.CountAsync(p =>
                    p.PostTags.Any(pt => pt.TagId == tag.Id), cancellationToken);

                if (tag.UsageCount != actualUsage)
                {
                    tag.UsageCount = actualUsage;
                    tag.UpdateAuditFields();
                    _tagRepository.Update(tag);
                    updatedCount++;
                }
            }

            await _tagRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recalculated usage counts for {UpdatedCount} tags by user {UserId}", updatedCount, userId);
            return OperationResult.CreateSuccess($"Updated usage counts for {updatedCount} tags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating usage counts");
            return OperationResult.Failure("An error occurred while recalculating usage counts");
        }
    }

    /// <summary>
    /// Bulk operations on multiple tags
    /// </summary>
    public async Task<BulkOperationResult> BulkOperationAsync(BulkTagOperationRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return BulkOperationResult.Failure(new[] { "User not found." });

            var tags = await _tagRepository.FindAsync(t =>
                request.TagIds.Contains(t.Id), cancellationToken);
            var tagsList = tags.ToList();

            var errors = new List<string>();
            var successCount = 0;

            foreach (var tag in tagsList)
            {
                try
                {
                    switch (request.Operation.ToLowerInvariant())
                    {
                        case "activate":
                            tag.IsActive = true;
                            tag.UpdatedBy = userId;
                            tag.UpdateAuditFields();
                            _tagRepository.Update(tag);
                            successCount++;
                            break;

                        case "deactivate":
                            tag.IsActive = false;
                            tag.UpdatedBy = userId;
                            tag.UpdateAuditFields();
                            _tagRepository.Update(tag);
                            successCount++;
                            break;

                        case "delete":
                            tag.SoftDelete(userId);
                            _tagRepository.Update(tag);
                            successCount++;
                            break;

                        case "merge":
                            if (request.TargetTagId.HasValue)
                            {
                                // This is simplified - full merge logic is in MergeTagsAsync
                                var mergeRequest = new MergeTagsRequest
                                {
                                    SourceTagIds = new List<Guid> { tag.Id },
                                    TargetTagId = request.TargetTagId.Value,
                                    DeleteSourceTags = true
                                };

                                var mergeResult = await MergeTagsAsync(mergeRequest, userId, cancellationToken);
                                if (mergeResult.Success)
                                    successCount++;
                                else
                                    errors.Add($"Failed to merge tag {tag.Name}: {mergeResult.Message}");
                            }
                            else
                            {
                                errors.Add($"Target tag ID required for merge operation on tag {tag.Name}");
                            }
                            break;

                        default:
                            errors.Add($"Unknown operation: {request.Operation}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in bulk operation {Operation} for tag {TagId}", request.Operation, tag.Id);
                    errors.Add($"Error processing tag {tag.Name}: {ex.Message}");
                }
            }

            await _tagRepository.SaveChangesAsync(cancellationToken);

            var failureCount = request.TagIds.Count - successCount;

            if (successCount > 0 && failureCount == 0)
                return BulkOperationResult.CreateSuccess(successCount, $"Successfully {request.Operation}ed {successCount} tags.");

            if (successCount == 0)
                return BulkOperationResult.Failure(errors, 0, failureCount);

            return BulkOperationResult.Mixed(successCount, failureCount, errors,
                $"Processed {successCount} tags successfully, {failureCount} failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk operation {Operation}", request.Operation);
            return BulkOperationResult.Failure(new[] { "An unexpected error occurred during bulk operation." });
        }
    }

    /// <summary>
    /// Searches tags by name and description
    /// </summary>
    public async Task<List<TagDto>> SearchTagsAsync(string searchQuery, bool includeInactive = false, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return new List<TagDto>();

            var searchTerm = searchQuery.Trim().ToLowerInvariant();
            var tags = await _tagRepository.FindAsync(t =>
                (includeInactive || t.IsActive) &&
                (t.Name.ToLowerInvariant().Contains(searchTerm) ||
                 (t.Description != null && t.Description.ToLowerInvariant().Contains(searchTerm))),
                cancellationToken);

            return tags
                .OrderByDescending(t => t.UsageCount)
                .ThenBy(t => t.Name)
                .Take(limit)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching tags with query {SearchQuery}", searchQuery);
            throw;
        }
    }

    /// <summary>
    /// Gets tag statistics
    /// </summary>
    public async Task<TagStatsDto> GetTagStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allTags = await _tagRepository.GetAllAsync(cancellationToken);
            var tagsList = allTags.ToList();

            var totalTags = tagsList.Count;
            var activeTags = tagsList.Count(t => t.IsActive);
            var usedTags = tagsList.Count(t => t.UsageCount > 0);
            var unusedTags = tagsList.Count(t => t.UsageCount == 0);
            var averageUsage = tagsList.Any() ? tagsList.Average(t => t.UsageCount) : 0;

            var mostUsedTags = tagsList
                .Where(t => t.IsActive && t.UsageCount > 0)
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .Select(t => new TagCloudDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    Color = t.Color,
                    UsageCount = t.UsageCount,
                    Weight = 1.0
                })
                .ToList();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentlyCreated = tagsList
                .Where(t => t.CreatedAt >= thirtyDaysAgo)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(MapToListDto)
                .ToList();

            return new TagStatsDto
            {
                TotalTags = totalTags,
                ActiveTags = activeTags,
                UsedTags = usedTags,
                UnusedTags = unusedTags,
                AverageUsage = averageUsage,
                MostUsedTags = mostUsedTags,
                RecentlyCreated = recentlyCreated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag statistics");
            throw;
        }
    }

    #region Private Helper Methods

    private static TagDto MapToDto(Tag tag)
    {
        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            Description = tag.Description,
            Color = tag.Color,
            UsageCount = tag.UsageCount,
            IsActive = tag.IsActive,
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt ?? tag.CreatedAt
        };
    }

    private static TagListDto MapToListDto(Tag tag)
    {
        return new TagListDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Slug = tag.Slug,
            Description = tag.Description,
            Color = tag.Color,
            UsageCount = tag.UsageCount,
            IsActive = tag.IsActive,
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt ?? tag.CreatedAt
        };
    }

    private static double CalculateWeight(int usage, int minUsage, int maxUsage)
    {
        if (maxUsage == minUsage)
            return 1.0;

        var normalizedUsage = (double)(usage - minUsage) / (maxUsage - minUsage);
        return Math.Max(0.1, Math.Min(1.0, 0.3 + (normalizedUsage * 0.7))); // Weight between 0.3 and 1.0
    }

    private static List<string> ExtractKeywords(string content, string? title = null)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var text = $"{title} {content}".ToLowerInvariant();

        // Remove common words and extract meaningful terms
        var words = Regex.Matches(text, @"\b\w{3,}\b")
            .Cast<Match>()
            .Select(m => m.Value)
            .Where(w => !IsCommonWord(w))
            .ToList();

        // Add individual words
        foreach (var word in words.Take(20))
        {
            keywords.Add(word);
        }

        // Add common programming terms if found
        var techTerms = new[] { "javascript", "python", "react", "angular", "nodejs", "docker", "api", "database" };
        foreach (var term in techTerms)
        {
            if (text.Contains(term))
                keywords.Add(term);
        }

        return keywords.Take(10).ToList();
    }

    private static bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "way", "who", "boy", "did", "don", "end", "few", "got", "let", "man", "new", "old", "put", "say", "she", "too", "use"
        };
        return commonWords.Contains(word);
    }

    private static double CalculateRelevanceScore(string tagName, List<string> keywords)
    {
        var score = 0.0;
        var normalizedTagName = tagName.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            var normalizedKeyword = keyword.ToLowerInvariant();

            if (normalizedTagName.Equals(normalizedKeyword))
            {
                score += 1.0;
            }
            else if (normalizedTagName.Contains(normalizedKeyword) || normalizedKeyword.Contains(normalizedTagName))
            {
                score += 0.7;
            }
        }

        return Math.Min(1.0, score / keywords.Count);
    }

    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\-]", "-")
                   .Trim('-')
                   .Replace("--", "-");
    }

    #endregion

    #region Interface Compatibility Methods

    /// <summary>
    /// Gets popular tags (most used)
    /// </summary>
    public async Task<List<TagDto>> GetPopularTagsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var tags = await _tagRepository.FindAsync(t => t.IsActive && t.UsageCount > 0, cancellationToken);
            return tags
                .OrderByDescending(t => t.UsageCount)
                .Take(limit)
                .Select(MapToDto)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular tags");
            throw;
        }
    }

    /// <summary>
    /// Gets tag statistics for a specific tag
    /// </summary>
    public async Task<TagStatsDto?> GetTagStatsAsync(Guid tagId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(tagId, cancellationToken);
            if (tag == null)
                return null;

            // Get posts using this tag for additional statistics
            var postsWithTag = await _postRepository.FindAsync(p =>
                p.PostTags.Any(pt => pt.TagId == tagId), cancellationToken);
            var postsList = postsWithTag.ToList();

            var firstUsed = postsList.Any() ? postsList.Min(p => p.CreatedAt) : tag.CreatedAt;
            var lastUsed = postsList.Any() ? postsList.Max(p => p.CreatedAt) : tag.CreatedAt;

            return new TagStatsDto
            {
                TotalTags = 1,
                ActiveTags = tag.IsActive ? 1 : 0,
                UsedTags = tag.UsageCount > 0 ? 1 : 0,
                UnusedTags = tag.UsageCount == 0 ? 1 : 0,
                AverageUsage = tag.UsageCount,
                MostUsedTags = new List<TagCloudDto>(),
                RecentlyCreated = new List<TagListDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag statistics for tag {TagId}", tagId);
            throw;
        }
    }

    /// <summary>
    /// Gets tag analytics data
    /// </summary>
    public async Task<TagAnalyticsDto> GetTagAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var allTags = await _tagRepository.GetAllAsync(cancellationToken);
            var tagsList = allTags.ToList();

            var totalTags = tagsList.Count;
            var activeTags = tagsList.Count(t => t.IsActive);
            var usedTags = tagsList.Count(t => t.UsageCount > 0);

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentlyCreated = tagsList.Count(t => t.CreatedAt >= thirtyDaysAgo);

            var topTags = tagsList
                .Where(t => t.IsActive && t.UsageCount > 0)
                .OrderByDescending(t => t.UsageCount)
                .Take(10)
                .Select(t => new TagUsageInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    UsageCount = t.UsageCount,
                    CreatedAt = t.CreatedAt,
                    Color = t.Color
                })
                .ToList();

            // Calculate least used tags (active tags with low usage)
            var leastUsedTags = tagsList
                .Where(t => t.IsActive && t.UsageCount > 0)
                .OrderBy(t => t.UsageCount)
                .Take(10)
                .Select(t => new TagUsageInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    UsageCount = t.UsageCount,
                    CreatedAt = t.CreatedAt,
                    Color = t.Color
                })
                .ToList();

            // Recently created tags (last 30 days)
            var recentlyCreatedTags = tagsList
                .Where(t => t.CreatedAt >= thirtyDaysAgo)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new TagUsageInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    UsageCount = t.UsageCount,
                    CreatedAt = t.CreatedAt,
                    Color = t.Color
                })
                .ToList();

            // Calculate tag creation trends (last 12 months)
            var tagCreationTrends = new Dictionary<string, int>();
            var tagUsageTrends = new Dictionary<string, int>();
            var now = DateTime.UtcNow;

            for (int i = 0; i < 12; i++)
            {
                var month = now.AddMonths(-i);
                var monthKey = month.ToString("yyyy-MM");

                var createdInMonth = tagsList.Count(t =>
                    t.CreatedAt.Year == month.Year &&
                    t.CreatedAt.Month == month.Month);

                tagCreationTrends[monthKey] = createdInMonth;

                // For usage trends, we'll estimate based on current usage distributed across time
                var avgUsageInMonth = tagsList
                    .Where(t => t.CreatedAt <= month.AddMonths(1))
                    .Sum(t => t.UsageCount) / 12; // Rough distribution
                tagUsageTrends[monthKey] = avgUsageInMonth;
            }

            // Usage distribution by range
            var usageDistribution = new Dictionary<string, int>
            {
                ["0"] = tagsList.Count(t => t.UsageCount == 0),
                ["1-5"] = tagsList.Count(t => t.UsageCount >= 1 && t.UsageCount <= 5),
                ["6-20"] = tagsList.Count(t => t.UsageCount >= 6 && t.UsageCount <= 20),
                ["21-50"] = tagsList.Count(t => t.UsageCount >= 21 && t.UsageCount <= 50),
                ["51+"] = tagsList.Count(t => t.UsageCount > 50)
            };

            // Get all posts to analyze tag combinations
            var allPosts = await _postRepository.GetAllAsync(cancellationToken);
            var postsList = allPosts.Where(p => !p.IsDeleted).ToList();

            // Calculate common tag combinations (simplified version)
            var tagCombinations = new Dictionary<string, int>();
            foreach (var post in postsList)
            {
                if (post.PostTags != null && post.PostTags.Count > 1)
                {
                    var postTagIds = post.PostTags.Select(pt => pt.TagId).OrderBy(id => id).ToList();
                    for (int i = 0; i < postTagIds.Count - 1; i++)
                    {
                        for (int j = i + 1; j < postTagIds.Count; j++)
                        {
                            var tag1 = tagsList.FirstOrDefault(t => t.Id == postTagIds[i]);
                            var tag2 = tagsList.FirstOrDefault(t => t.Id == postTagIds[j]);
                            if (tag1 != null && tag2 != null)
                            {
                                var combination = $"{tag1.Name},{tag2.Name}";
                                tagCombinations[combination] = tagCombinations.GetValueOrDefault(combination, 0) + 1;
                            }
                        }
                    }
                }
            }

            var commonTagCombinations = tagCombinations
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => new TagCombinationInfo
                {
                    TagNames = kv.Key.Split(',').ToList(),
                    CombinationCount = kv.Value,
                    Posts = new List<Guid>() // Could be populated with actual post IDs
                })
                .ToList();

            // Calculate tag creators (simplified - using CreatedBy if available)
            var tagCreators = tagsList
                .Where(t => t.CreatedBy.HasValue)
                .GroupBy(t => t.CreatedBy.Value)
                .Select(g => new TagCreatorInfo
                {
                    UserId = g.Key,
                    UserName = "User", // Would need to join with Users table for actual names
                    TagsCreated = g.Count(),
                    TotalUsage = g.Sum(t => t.UsageCount),
                    LastTagCreated = g.Max(t => t.CreatedAt)
                })
                .OrderByDescending(tc => tc.TagsCreated)
                .Take(10)
                .ToList();

            // Calculate metrics
            var averageTagLifespanDays = tagsList.Any()
                ? (int)tagsList.Average(t => (DateTime.UtcNow - t.CreatedAt).TotalDays)
                : 0;

            var tagAbandonmentRate = totalTags > 0
                ? (double)(totalTags - usedTags) / totalTags * 100
                : 0;

            // Generate cleanup suggestions
            var cleanupSuggestions = new List<TagCleanupSuggestion>();

            var unusedTags = tagsList.Where(t => t.UsageCount == 0 && t.CreatedAt < thirtyDaysAgo).ToList();
            if (unusedTags.Any())
            {
                cleanupSuggestions.Add(new TagCleanupSuggestion
                {
                    Type = "unused_tags",
                    Description = $"Delete {unusedTags.Count} unused tags older than 30 days",
                    TagIds = unusedTags.Select(t => t.Id).ToList(),
                    Priority = unusedTags.Count > 10 ? 5 : 3
                });
            }

            var duplicateTags = tagsList
                .GroupBy(t => t.Name.ToLowerInvariant())
                .Where(g => g.Count() > 1)
                .ToList();
            if (duplicateTags.Any())
            {
                cleanupSuggestions.Add(new TagCleanupSuggestion
                {
                    Type = "duplicate_tags",
                    Description = $"Merge {duplicateTags.Count} groups of duplicate tags",
                    TagIds = duplicateTags.SelectMany(g => g.Select(t => t.Id)).ToList(),
                    Priority = 3
                });
            }

            // Calculate health score (0-100)
            var healthScore = CalculateTagHealthScore(totalTags, activeTags, usedTags, tagAbandonmentRate);

            return new TagAnalyticsDto
            {
                TotalTags = totalTags,
                ActiveTags = activeTags,
                InactiveTags = totalTags - activeTags,
                UnusedTags = totalTags - usedTags,
                AverageTagsPerPost = postsList.Any() ? postsList.Where(p => p.PostTags != null).Average(p => p.PostTags?.Count ?? 0) : 0,
                MostUsedTags = topTags,
                LeastUsedTags = leastUsedTags,
                RecentlyCreatedTags = recentlyCreatedTags,
                FastestGrowingTags = new List<TagGrowthInfo>(), // Would require historical data
                DecliningTags = new List<TagGrowthInfo>(), // Would require historical data
                TagCreationTrends = tagCreationTrends,
                TagUsageTrends = tagUsageTrends,
                UsageDistribution = usageDistribution,
                CommonTagCombinations = commonTagCombinations,
                TopTagCreators = tagCreators,
                AverageTagLifespanDays = averageTagLifespanDays,
                TagAbandonmentRate = tagAbandonmentRate,
                CleanupSuggestions = cleanupSuggestions,
                TagHealthScore = healthScore
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag analytics");
            throw;
        }
    }

    /// <summary>
    /// Deletes a tag (returns boolean for backward compatibility)
    /// </summary>
    public async Task<bool> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Deleting tag without authenticated user context");
                // Use the admin user ID from seed data as fallback
                currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
            }
            var result = await DeleteTagAsync(id, currentUserId.Value, cancellationToken);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Merges tags (for backward compatibility)
    /// </summary>
    public async Task<TagDto> MergeTagsAsync(MergeTagsDto mergeDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new MergeTagsRequest
            {
                SourceTagIds = mergeDto.SourceTagIds,
                TargetTagId = mergeDto.TargetTagId,
                DeleteSourceTags = true // Default to true since not available in DTO
            };

            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Merging tags without authenticated user context");
                // Use the admin user ID from seed data as fallback
                currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
            }
            var result = await MergeTagsAsync(request, currentUserId.Value, cancellationToken);

            if (result.Success)
            {
                var targetTag = await _tagRepository.GetByIdAsync(mergeDto.TargetTagId, cancellationToken);
                return targetTag != null ? MapToDto(targetTag) : throw new InvalidOperationException("Target tag not found after merge");
            }
            else
            {
                throw new InvalidOperationException(result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging tags");
            throw;
        }
    }

    /// <summary>
    /// Creates a tag using DTO (for backward compatibility)
    /// </summary>
    public async Task<TagDto> CreateTagAsync(CreateTagDto createDto, CancellationToken cancellationToken = default)
    {
        var request = new CreateTagRequest
        {
            Name = createDto.Name,
            Slug = createDto.Slug,
            Description = createDto.Description,
            Color = createDto.Color,
            IsActive = createDto.IsVisible // Map IsVisible to IsActive
        };

        var currentUserId = _userContextService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Creating tag without authenticated user context");
            // Use the admin user ID from seed data as fallback
            currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
        }
        return await CreateTagAsync(request, currentUserId.Value, cancellationToken);
    }

    /// <summary>
    /// Updates a tag using DTO (for backward compatibility)
    /// </summary>
    public async Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagDto updateDto, CancellationToken cancellationToken = default)
    {
        var request = new UpdateTagRequest
        {
            Name = updateDto.Name,
            Slug = updateDto.Slug,
            Description = updateDto.Description,
            Color = updateDto.Color,
            IsActive = updateDto.IsVisible // Map IsVisible to IsActive
        };

        var currentUserId = _userContextService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Updating tag without authenticated user context");
            // Use the admin user ID from seed data as fallback
            currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
        }
        return await UpdateTagAsync(id, request, currentUserId.Value, cancellationToken);
    }

    /// <summary>
    /// Gets paginated tags for backward compatibility
    /// </summary>
    public async Task<PagedResultDto<TagDto>> GetTagsAsync(int pageNumber, int pageSize, string? searchTerm, string sortBy, bool includeHidden, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new TagQueryDto
            {
                Page = pageNumber,
                PageSize = pageSize,
                Search = searchTerm,
                SortBy = sortBy,
                SortOrder = "ASC",
                IncludeInactive = includeHidden,
                IncludeUnused = true
            };

            var response = await GetTagsAsync(query, cancellationToken);

            return PagedResultDto<TagDto>.Create(
                response.Items.Select(tld => new TagDto
                {
                    Id = tld.Id,
                    Name = tld.Name,
                    Slug = tld.Slug,
                    Description = tld.Description,
                    Color = tld.Color,
                    UsageCount = tld.UsageCount,
                    IsActive = tld.IsActive,
                    CreatedAt = tld.CreatedAt,
                    UpdatedAt = tld.UpdatedAt
                }),
                response.TotalCount,
                response.CurrentPage,
                response.PageSize
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paginated tags");
            throw;
        }
    }

    #endregion

    /// <summary>
    /// Calculates a health score for the tag system (0-100)
    /// </summary>
    private static int CalculateTagHealthScore(int totalTags, int activeTags, int usedTags, double abandonmentRate)
    {
        if (totalTags == 0) return 100; // Perfect score if no tags yet

        var score = 100;

        // Penalize high abandonment rate
        if (abandonmentRate > 50) score -= 30;
        else if (abandonmentRate > 25) score -= 15;
        else if (abandonmentRate > 10) score -= 5;

        // Reward high usage ratio
        var usageRatio = totalTags > 0 ? (double)usedTags / totalTags : 0;
        if (usageRatio > 0.8) score += 10;
        else if (usageRatio < 0.3) score -= 10;

        // Penalize if too many inactive tags
        var activeRatio = totalTags > 0 ? (double)activeTags / totalTags : 1;
        if (activeRatio < 0.7) score -= 10;

        // Ensure score is within bounds
        return Math.Max(0, Math.Min(100, score));
    }
}