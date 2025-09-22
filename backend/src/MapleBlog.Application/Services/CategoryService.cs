using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services;

/// <summary>
/// Category service implementation for category management operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPostRepository _postRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IPostRepository postRepository,
        IUserRepository userRepository,
        IUserContextService userContextService,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _postRepository = postRepository;
        _userRepository = userRepository;
        _userContextService = userContextService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a category by its ID
    /// </summary>
    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            return category != null ? await MapToDtoAsync(category, cancellationToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by ID {CategoryId}", id);
            throw;
        }
    }

    /// <summary>
    /// Gets a category by its slug
    /// </summary>
    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var categories = await _categoryRepository.FindAsync(c => c.Slug == slug.Trim(), cancellationToken);
            var category = categories.FirstOrDefault();
            return category != null ? await MapToDtoAsync(category, cancellationToken) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by slug {Slug}", slug);
            throw;
        }
    }

    /// <summary>
    /// Gets all categories with filtering and pagination
    /// </summary>
    public async Task<CategoryListResponse> GetCategoriesAsync(CategoryQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var filteredCategories = categories.AsQueryable();

            // Apply filters
            if (!query.IncludeInactive)
                filteredCategories = filteredCategories.Where(c => c.IsActive);

            if (query.ParentId.HasValue)
                filteredCategories = filteredCategories.Where(c => c.ParentId == query.ParentId.Value);
            else if (query.ParentId == null && !string.IsNullOrEmpty(query.Search))
            {
                // No specific parent filter, include all
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLowerInvariant();
                filteredCategories = filteredCategories.Where(c =>
                    c.Name.ToLowerInvariant().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLowerInvariant().Contains(searchTerm)));
            }

            if (!query.IncludeEmpty)
                filteredCategories = filteredCategories.Where(c => c.PostCount > 0);

            // Apply sorting
            filteredCategories = query.SortBy.ToLowerInvariant() switch
            {
                "name" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredCategories.OrderByDescending(c => c.Name) :
                    filteredCategories.OrderBy(c => c.Name),
                "postcount" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredCategories.OrderByDescending(c => c.PostCount) :
                    filteredCategories.OrderBy(c => c.PostCount),
                "level" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredCategories.OrderByDescending(c => c.Level) :
                    filteredCategories.OrderBy(c => c.Level),
                "createdat" => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredCategories.OrderByDescending(c => c.CreatedAt) :
                    filteredCategories.OrderBy(c => c.CreatedAt),
                _ => query.SortOrder.ToUpperInvariant() == "DESC" ?
                    filteredCategories.OrderByDescending(c => c.DisplayOrder).ThenByDescending(c => c.Name) :
                    filteredCategories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            };

            var totalCount = filteredCategories.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            var pagedCategories = filteredCategories
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var categoryDtos = new List<CategoryListDto>();
            foreach (var category in pagedCategories)
            {
                categoryDtos.Add(MapToListDto(category));
            }

            return new CategoryListResponse
            {
                Items = categoryDtos,
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
            _logger.LogError(ex, "Error getting categories with query {@Query}", query);
            throw;
        }
    }

    /// <summary>
    /// Gets categories as a hierarchical tree structure
    /// </summary>
    public async Task<List<CategoryTreeDto>> GetCategoryTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var filteredCategories = includeInactive ?
                categories.ToList() :
                categories.Where(c => c.IsActive).ToList();

            return BuildCategoryTree(filteredCategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category tree");
            throw;
        }
    }

    /// <summary>
    /// Gets root categories (categories without parent)
    /// </summary>
    public async Task<List<CategoryDto>> GetRootCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryRepository.FindAsync(c =>
                c.ParentId == null && (includeInactive || c.IsActive), cancellationToken);

            var result = new List<CategoryDto>();
            foreach (var category in categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
            {
                result.Add(await MapToDtoAsync(category, cancellationToken));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            throw;
        }
    }

    /// <summary>
    /// Gets child categories for a parent category
    /// </summary>
    public async Task<List<CategoryDto>> GetChildCategoriesAsync(Guid parentId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await _categoryRepository.FindAsync(c =>
                c.ParentId == parentId && (includeInactive || c.IsActive), cancellationToken);

            var result = new List<CategoryDto>();
            foreach (var category in categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
            {
                result.Add(await MapToDtoAsync(category, cancellationToken));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child categories for parent {ParentId}", parentId);
            throw;
        }
    }

    /// <summary>
    /// Gets all ancestor categories for a category
    /// </summary>
    public async Task<List<CategoryDto>> GetAncestorCategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
                return new List<CategoryDto>();

            var ancestors = new List<Category>();
            var ancestorIds = category.GetAncestorIds().ToList();

            if (ancestorIds.Any())
            {
                var ancestorCategories = await _categoryRepository.FindAsync(c =>
                    ancestorIds.Contains(c.Id), cancellationToken);

                // Sort by hierarchy level to get correct order from root to parent
                ancestors = ancestorCategories.OrderBy(c => c.Level).ToList();
            }

            var result = new List<CategoryDto>();
            foreach (var ancestor in ancestors)
            {
                result.Add(await MapToDtoAsync(ancestor, cancellationToken));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ancestor categories for category {CategoryId}", categoryId);
            throw;
        }
    }

    /// <summary>
    /// Creates a new category
    /// </summary>
    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Validate parent category if specified
            Category? parentCategory = null;
            if (request.ParentId.HasValue)
            {
                parentCategory = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
                if (parentCategory == null)
                    throw new InvalidOperationException("Parent category not found");
            }

            var slug = !string.IsNullOrWhiteSpace(request.Slug) ?
                request.Slug.Trim() :
                await GenerateUniqueSlugAsync(request.Name, cancellationToken: cancellationToken);

            var category = new Category
            {
                Name = request.Name.Trim(),
                Slug = slug,
                Description = request.Description?.Trim(),
                ParentId = request.ParentId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                Color = request.Color?.Trim(),
                Icon = request.Icon?.Trim(),
                CoverImageUrl = request.CoverImageUrl?.Trim(),
                MetaTitle = request.MetaTitle?.Trim(),
                MetaDescription = request.MetaDescription?.Trim(),
                MetaKeywords = request.MetaKeywords?.Trim(),
                CreatedBy = userId,
                UpdatedBy = userId
            };

            // Set hierarchy information
            category.SetParent(parentCategory);

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category created: {CategoryName} by user {UserId}", category.Name, userId);
            return await MapToDtoAsync(category, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category {CategoryName}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing category
    /// </summary>
    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return null;

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User not found");

            // Validate parent category if specified
            Category? parentCategory = null;
            if (request.ParentId.HasValue)
            {
                if (request.ParentId.Value == id)
                    throw new InvalidOperationException("Category cannot be its own parent");

                parentCategory = await _categoryRepository.GetByIdAsync(request.ParentId.Value, cancellationToken);
                if (parentCategory == null)
                    throw new InvalidOperationException("Parent category not found");

                // Check for circular reference
                if (parentCategory.GetAncestorIds().Contains(id))
                    throw new InvalidOperationException("Cannot create circular reference in category hierarchy");
            }

            var slug = !string.IsNullOrWhiteSpace(request.Slug) ?
                request.Slug.Trim() :
                await GenerateUniqueSlugAsync(request.Name, id, cancellationToken);

            category.Name = request.Name.Trim();
            category.Slug = slug;
            category.Description = request.Description?.Trim();
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;
            category.Color = request.Color?.Trim();
            category.Icon = request.Icon?.Trim();
            category.CoverImageUrl = request.CoverImageUrl?.Trim();
            category.MetaTitle = request.MetaTitle?.Trim();
            category.MetaDescription = request.MetaDescription?.Trim();
            category.MetaKeywords = request.MetaKeywords?.Trim();
            category.UpdatedBy = userId;

            // Update hierarchy if parent changed
            if (category.ParentId != request.ParentId)
            {
                category.SetParent(parentCategory);

                // Update all descendant categories' hierarchy
                await UpdateDescendantHierarchy(category, cancellationToken);
            }

            category.UpdateAuditFields();
            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category updated: {CategoryName} by user {UserId}", category.Name, userId);
            return await MapToDtoAsync(category, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            throw;
        }
    }

    /// <summary>
    /// Moves a category to a new parent or changes its order
    /// </summary>
    public async Task<OperationResult> MoveCategoryAsync(Guid id, MoveCategoryRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return OperationResult.Failure("Category not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            // Validate new parent category if specified
            Category? newParentCategory = null;
            if (request.NewParentId.HasValue)
            {
                if (request.NewParentId.Value == id)
                    return OperationResult.Failure("Category cannot be its own parent");

                newParentCategory = await _categoryRepository.GetByIdAsync(request.NewParentId.Value, cancellationToken);
                if (newParentCategory == null)
                    return OperationResult.Failure("New parent category not found");

                // Check for circular reference
                if (newParentCategory.GetAncestorIds().Contains(id))
                    return OperationResult.Failure("Cannot create circular reference in category hierarchy");
            }

            // Update hierarchy if parent changed
            if (category.ParentId != request.NewParentId)
            {
                category.SetParent(newParentCategory);
                await UpdateDescendantHierarchy(category, cancellationToken);
            }

            category.DisplayOrder = request.DisplayOrder;
            category.UpdatedBy = userId;
            category.UpdateAuditFields();

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category moved: {CategoryName} by user {UserId}", category.Name, userId);
            return OperationResult.CreateSuccess("Category moved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving category {CategoryId}", id);
            return OperationResult.Failure("An error occurred while moving category");
        }
    }

    /// <summary>
    /// Activates or deactivates a category
    /// </summary>
    public async Task<OperationResult> SetCategoryStatusAsync(Guid id, bool isActive, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return OperationResult.Failure("Category not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            category.IsActive = isActive;
            category.UpdatedBy = userId;
            category.UpdateAuditFields();

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            var action = isActive ? "activated" : "deactivated";
            _logger.LogInformation("Category {Action}: {CategoryName} by user {UserId}", action, category.Name, userId);

            return OperationResult.CreateSuccess($"Category {action} successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting category status {CategoryId}", id);
            return OperationResult.Failure("An error occurred while updating category status");
        }
    }

    /// <summary>
    /// Soft deletes a category
    /// </summary>
    public async Task<OperationResult> DeleteCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return OperationResult.Failure("Category not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            // Check if category has child categories
            var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == id, cancellationToken);
            if (childCategories.Any())
                return OperationResult.Failure("Cannot delete category with child categories. Please delete or move child categories first.");

            // Check if category has posts
            if (category.PostCount > 0)
                return OperationResult.Failure("Cannot delete category with posts. Please move posts to another category first.");

            category.SoftDelete(userId);
            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category soft deleted: {CategoryName} by user {UserId}", category.Name, userId);
            return OperationResult.CreateSuccess("Category deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return OperationResult.Failure("An error occurred while deleting category");
        }
    }

    /// <summary>
    /// Permanently deletes a category (admin only)
    /// </summary>
    public async Task<OperationResult> PermanentlyDeleteCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return OperationResult.Failure("Category not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            // Check if category has child categories
            var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == id, cancellationToken);
            if (childCategories.Any())
                return OperationResult.Failure("Cannot permanently delete category with child categories");

            // Check if category is being used
            if (category.PostCount > 0)
                return OperationResult.Failure("Cannot permanently delete a category that contains posts");

            await _categoryRepository.RemoveAsync(id, cancellationToken);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category permanently deleted: {CategoryName} by user {UserId}", category.Name, userId);
            return OperationResult.CreateSuccess("Category permanently deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting category {CategoryId}", id);
            return OperationResult.Failure("An error occurred while permanently deleting category");
        }
    }

    /// <summary>
    /// Restores a soft-deleted category
    /// </summary>
    public async Task<OperationResult> RestoreCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                return OperationResult.Failure("Category not found");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            if (!category.IsDeleted)
                return OperationResult.Failure("Category is not deleted");

            category.Restore(userId);
            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Category restored: {CategoryName} by user {UserId}", category.Name, userId);
            return OperationResult.CreateSuccess("Category restored successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring category {CategoryId}", id);
            return OperationResult.Failure("An error occurred while restoring category");
        }
    }

    /// <summary>
    /// Validates category slug uniqueness
    /// </summary>
    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var normalizedSlug = slug.Trim().ToLowerInvariant();
            var existingCategories = await _categoryRepository.FindAsync(c =>
                c.Slug.ToLowerInvariant() == normalizedSlug, cancellationToken);

            return !existingCategories.Any(c => excludeCategoryId == null || c.Id != excludeCategoryId.Value);
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
    public async Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseSlug = GenerateSlug(name);
            var slug = baseSlug;
            var counter = 1;

            while (!await IsSlugUniqueAsync(slug, excludeCategoryId, cancellationToken))
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
    /// Rebuilds category hierarchy paths and levels (maintenance operation)
    /// </summary>
    public async Task<OperationResult> RebuildCategoryHierarchyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
            var categories = allCategories.ToList();
            var updatedCount = 0;

            // First, handle root categories
            var rootCategories = categories.Where(c => c.ParentId == null).ToList();
            foreach (var rootCategory in rootCategories)
            {
                if (UpdateCategoryHierarchy(rootCategory, null, categories))
                {
                    _categoryRepository.Update(rootCategory);
                    updatedCount++;
                }
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Rebuilt hierarchy for {UpdatedCount} categories by user {UserId}", updatedCount, userId);
            return OperationResult.CreateSuccess($"Updated hierarchy for {updatedCount} categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding category hierarchy");
            return OperationResult.Failure("An error occurred while rebuilding category hierarchy");
        }
    }

    /// <summary>
    /// Updates post counts for all categories (maintenance operation)
    /// </summary>
    public async Task<OperationResult> UpdatePostCountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var categories = await _categoryRepository.GetAllAsync(cancellationToken);
            var updatedCount = 0;

            foreach (var category in categories)
            {
                var actualPostCount = await _postRepository.CountAsync(p => p.CategoryId == category.Id, cancellationToken);

                if (category.PostCount != actualPostCount)
                {
                    category.PostCount = actualPostCount;
                    category.UpdateAuditFields();
                    _categoryRepository.Update(category);
                    updatedCount++;
                }
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated post counts for {UpdatedCount} categories by user {UserId}", updatedCount, userId);
            return OperationResult.CreateSuccess($"Updated post counts for {updatedCount} categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post counts");
            return OperationResult.Failure("An error occurred while updating post counts");
        }
    }

    /// <summary>
    /// Bulk operations on multiple categories
    /// </summary>
    public async Task<BulkOperationResult> BulkOperationAsync(IEnumerable<Guid> categoryIds, string operation, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return BulkOperationResult.Failure(new[] { "User not found." });

            var categoryIdList = categoryIds.ToList();
            var categories = await _categoryRepository.FindAsync(c =>
                categoryIdList.Contains(c.Id), cancellationToken);
            var categoriesList = categories.ToList();

            var errors = new List<string>();
            var successCount = 0;

            foreach (var category in categoriesList)
            {
                try
                {
                    switch (operation.ToLowerInvariant())
                    {
                        case "activate":
                            category.IsActive = true;
                            category.UpdatedBy = userId;
                            category.UpdateAuditFields();
                            _categoryRepository.Update(category);
                            successCount++;
                            break;

                        case "deactivate":
                            category.IsActive = false;
                            category.UpdatedBy = userId;
                            category.UpdateAuditFields();
                            _categoryRepository.Update(category);
                            successCount++;
                            break;

                        case "delete":
                            // Check if category can be deleted
                            var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == category.Id, cancellationToken);
                            if (childCategories.Any())
                            {
                                errors.Add($"Cannot delete category {category.Name}: has child categories");
                                continue;
                            }

                            if (category.PostCount > 0)
                            {
                                errors.Add($"Cannot delete category {category.Name}: contains posts");
                                continue;
                            }

                            category.SoftDelete(userId);
                            _categoryRepository.Update(category);
                            successCount++;
                            break;

                        default:
                            errors.Add($"Unknown operation: {operation}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in bulk operation {Operation} for category {CategoryId}", operation, category.Id);
                    errors.Add($"Error processing category {category.Name}: {ex.Message}");
                }
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);

            var failureCount = categoryIdList.Count - successCount;

            if (successCount > 0 && failureCount == 0)
                return BulkOperationResult.CreateSuccess(successCount, $"Successfully {operation}ed {successCount} categories.");

            if (successCount == 0)
                return BulkOperationResult.Failure(errors, 0, failureCount);

            return BulkOperationResult.Mixed(successCount, failureCount, errors,
                $"Processed {successCount} categories successfully, {failureCount} failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk operation {Operation}", operation);
            return BulkOperationResult.Failure(new[] { "An unexpected error occurred during bulk operation." });
        }
    }

    /// <summary>
    /// Searches categories by name and description
    /// </summary>
    public async Task<List<CategoryDto>> SearchCategoriesAsync(string searchQuery, bool includeInactive = false, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                return new List<CategoryDto>();

            var searchTerm = searchQuery.Trim().ToLowerInvariant();
            var categories = await _categoryRepository.FindAsync(c =>
                (includeInactive || c.IsActive) &&
                (c.Name.ToLowerInvariant().Contains(searchTerm) ||
                 (c.Description != null && c.Description.ToLowerInvariant().Contains(searchTerm))),
                cancellationToken);

            var result = new List<CategoryDto>();
            var sortedCategories = categories
                .OrderByDescending(c => c.PostCount)
                .ThenBy(c => c.Level)
                .ThenBy(c => c.Name)
                .Take(limit);

            foreach (var category in sortedCategories)
            {
                result.Add(await MapToDtoAsync(category, cancellationToken));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching categories with query {SearchQuery}", searchQuery);
            throw;
        }
    }

    #region Private Helper Methods

    private async Task<CategoryDto> MapToDtoAsync(Category category, CancellationToken cancellationToken)
    {
        // Load parent if needed
        CategoryDto? parent = null;
        if (category.ParentId.HasValue)
        {
            var parentCategory = await _categoryRepository.GetByIdAsync(category.ParentId.Value, cancellationToken);
            if (parentCategory != null)
            {
                parent = new CategoryDto
                {
                    Id = parentCategory.Id,
                    Name = parentCategory.Name,
                    Slug = parentCategory.Slug,
                    Level = parentCategory.Level
                };
            }
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            Parent = parent,
            TreePath = category.TreePath,
            Level = category.Level,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            PostCount = category.PostCount,
            Appearance = new CategoryAppearanceDto
            {
                Color = category.Color,
                Icon = category.Icon,
                CoverImageUrl = category.CoverImageUrl
            },
            Seo = new CategorySeoDto
            {
                MetaTitle = category.MetaTitle,
                MetaDescription = category.MetaDescription,
                MetaKeywords = category.MetaKeywords
            },
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt ?? category.CreatedAt
        };
    }

    private static CategoryListDto MapToListDto(Category category)
    {
        return new CategoryListDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ParentId = category.ParentId,
            Level = category.Level,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            PostCount = category.PostCount,
            Appearance = new CategoryAppearanceDto
            {
                Color = category.Color,
                Icon = category.Icon,
                CoverImageUrl = category.CoverImageUrl
            },
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt ?? category.CreatedAt
        };
    }

    private static CategoryTreeDto MapToTreeDto(Category category)
    {
        return new CategoryTreeDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            Level = category.Level,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            PostCount = category.PostCount,
            Appearance = new CategoryAppearanceDto
            {
                Color = category.Color,
                Icon = category.Icon,
                CoverImageUrl = category.CoverImageUrl
            }
        };
    }

    private static List<CategoryTreeDto> BuildCategoryTree(List<Category> categories)
    {
        var categoryDict = categories.ToDictionary(c => c.Id);
        var rootCategories = new List<CategoryTreeDto>();

        // Create tree DTOs
        var treeDtos = categories.Select(MapToTreeDto).ToDictionary(c => c.Id);

        foreach (var category in categories.Where(c => c.ParentId == null).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
        {
            var treeDto = treeDtos[category.Id];
            BuildCategoryTreeRecursive(treeDto, categories, treeDtos);
            rootCategories.Add(treeDto);
        }

        return rootCategories;
    }

    private static void BuildCategoryTreeRecursive(CategoryTreeDto parent, List<Category> allCategories, Dictionary<Guid, CategoryTreeDto> treeDtos)
    {
        var children = allCategories
            .Where(c => c.ParentId == parent.Id)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        foreach (var child in children)
        {
            var childDto = treeDtos[child.Id];
            parent.Children.Add(childDto);
            BuildCategoryTreeRecursive(childDto, allCategories, treeDtos);
        }
    }

    private async Task UpdateDescendantHierarchy(Category parentCategory, CancellationToken cancellationToken)
    {
        var descendants = await _categoryRepository.FindAsync(c =>
            c.TreePath != null && c.TreePath.Contains($"/{parentCategory.Id}/") && c.Id != parentCategory.Id,
            cancellationToken);

        foreach (var descendant in descendants)
        {
            // Rebuild the tree path based on the new hierarchy
            await RebuildCategoryTreePath(descendant, cancellationToken);
            _categoryRepository.Update(descendant);
        }
    }

    private async Task RebuildCategoryTreePath(Category category, CancellationToken cancellationToken)
    {
        if (category.ParentId == null)
        {
            category.Level = 0;
            category.TreePath = $"/{category.Id}/";
        }
        else
        {
            var parent = await _categoryRepository.GetByIdAsync(category.ParentId.Value, cancellationToken);
            if (parent != null)
            {
                category.Level = parent.Level + 1;
                category.TreePath = $"{parent.TreePath}{category.Id}/";
            }
        }
    }

    private static bool UpdateCategoryHierarchy(Category category, Category? parent, List<Category> allCategories)
    {
        var oldLevel = category.Level;
        var oldTreePath = category.TreePath;

        if (parent == null)
        {
            category.Level = 0;
            category.TreePath = $"/{category.Id}/";
        }
        else
        {
            category.Level = parent.Level + 1;
            category.TreePath = $"{parent.TreePath}{category.Id}/";
        }

        var changed = category.Level != oldLevel || category.TreePath != oldTreePath;

        // Recursively update children
        var children = allCategories.Where(c => c.ParentId == category.Id).ToList();
        foreach (var child in children)
        {
            if (UpdateCategoryHierarchy(child, category, allCategories))
                changed = true;
        }

        return changed;
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
    /// Reorders categories for display order management
    /// </summary>
    public async Task<OperationResult> ReorderCategoriesAsync(List<CategoryOrderDto> categoryOrders, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var categoryIds = categoryOrders.Select(co => co.CategoryId).ToList();
            var categories = await _categoryRepository.FindAsync(c => categoryIds.Contains(c.Id), cancellationToken);
            var categoriesDict = categories.ToDictionary(c => c.Id);

            var updatedCount = 0;
            foreach (var orderDto in categoryOrders)
            {
                if (categoriesDict.TryGetValue(orderDto.CategoryId, out var category))
                {
                    if (category.DisplayOrder != orderDto.DisplayOrder)
                    {
                        category.DisplayOrder = orderDto.DisplayOrder;
                        category.UpdatedBy = userId;
                        category.UpdateAuditFields();
                        _categoryRepository.Update(category);
                        updatedCount++;
                    }
                }
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Reordered {UpdatedCount} categories by user {UserId}", updatedCount, userId);
            return OperationResult.CreateSuccess($"Successfully reordered {updatedCount} categories.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            return OperationResult.Failure("An error occurred while reordering categories");
        }
    }

    /// <summary>
    /// Gets category statistics
    /// </summary>
    public async Task<CategoryStatsDto> GetCategoryStatsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (categoryId.HasValue)
            {
                // Stats for specific category
                var category = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
                if (category == null)
                    throw new InvalidOperationException("Category not found");

                var childCount = await _categoryRepository.CountAsync(c => c.ParentId == categoryId.Value, cancellationToken);
                var descendantCount = await _categoryRepository.CountAsync(c =>
                    c.TreePath != null && c.TreePath.Contains($"/{categoryId.Value}/") && c.Id != categoryId.Value, cancellationToken);

                // Get posts for this category to calculate statistics
                var categoryPosts = await _postRepository.FindAsync(p => p.CategoryId == categoryId.Value && !p.IsDeleted, cancellationToken);
                var posts = categoryPosts.ToList();

                // Calculate statistics
                var publishedPosts = posts.Where(p => p.Status == Domain.Enums.PostStatus.Published).ToList();
                var draftPosts = posts.Where(p => p.Status == Domain.Enums.PostStatus.Draft).ToList();
                var totalViews = posts.Sum(p => p.ViewCount);
                var totalComments = posts.Sum(p => p.CommentCount);

                // Get child categories for basic info
                var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == categoryId.Value && c.IsActive, cancellationToken);
                var childCategoryInfos = childCategories.Select(c => new CategoryBasicInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    PostCount = c.PostCount,
                    Description = c.Description
                }).ToList();

                // Get popular posts (top 5 by view count)
                var popularPosts = publishedPosts
                    .OrderByDescending(p => p.ViewCount)
                    .Take(5)
                    .Select(p => new PostBasicInfo
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Slug = p.Slug,
                        ViewCount = p.ViewCount,
                        CommentCount = p.CommentCount,
                        PublishedAt = p.PublishedAt,
                        Summary = p.Summary
                    }).ToList();

                // Get recent posts (top 5 by publication date)
                var recentPosts = publishedPosts
                    .Where(p => p.PublishedAt.HasValue)
                    .OrderByDescending(p => p.PublishedAt)
                    .Take(5)
                    .Select(p => new PostBasicInfo
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Slug = p.Slug,
                        ViewCount = p.ViewCount,
                        CommentCount = p.CommentCount,
                        PublishedAt = p.PublishedAt,
                        Summary = p.Summary
                    }).ToList();

                // Calculate monthly post counts (last 12 months)
                var monthlyPostCounts = new Dictionary<string, int>();
                var now = DateTime.UtcNow;
                for (int i = 0; i < 12; i++)
                {
                    var month = now.AddMonths(-i);
                    var monthKey = month.ToString("yyyy-MM");
                    var monthlyCount = publishedPosts.Count(p =>
                        p.PublishedAt.HasValue &&
                        p.PublishedAt.Value.Year == month.Year &&
                        p.PublishedAt.Value.Month == month.Month);
                    monthlyPostCounts[monthKey] = monthlyCount;
                }

                return new CategoryStatsDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    CategorySlug = category.Slug,
                    PostCount = category.PostCount,
                    PublishedPostCount = publishedPosts.Count,
                    DraftPostCount = draftPosts.Count,
                    CommentCount = totalComments,
                    TotalViews = totalViews,
                    ChildCategoryCount = childCount,
                    ChildCategories = childCategoryInfos,
                    PopularPosts = popularPosts,
                    RecentPosts = recentPosts,
                    MonthlyPostCounts = monthlyPostCounts,
                    HierarchyDepth = category.Level
                };
            }
            else
            {
                // Stats for all categories
                var allCategories = await _categoryRepository.GetAllAsync(cancellationToken);
                var categoriesList = allCategories.ToList();

                var totalCategories = categoriesList.Count;
                var activeCategories = categoriesList.Count(c => c.IsActive);
                var totalPosts = categoriesList.Sum(c => c.PostCount);
                var maxDepth = categoriesList.Any() ? categoriesList.Max(c => c.Level) : 0;
                var avgPosts = totalCategories > 0 ? totalPosts / (double)totalCategories : 0;

                // For global stats, create a summary CategoryStatsDto
                var rootCategory = categoriesList.FirstOrDefault(c => c.ParentId == null);
                if (rootCategory == null)
                {
                    // Create a placeholder if no categories exist
                    return new CategoryStatsDto
                    {
                        CategoryId = Guid.Empty,
                        CategoryName = "All Categories",
                        CategorySlug = "all",
                        PostCount = totalPosts,
                        PublishedPostCount = totalPosts,
                        DraftPostCount = 0,
                        CommentCount = 0,
                        TotalViews = 0,
                        ChildCategoryCount = totalCategories,
                        ChildCategories = new List<CategoryBasicInfo>(),
                        PopularPosts = new List<PostBasicInfo>(),
                        RecentPosts = new List<PostBasicInfo>(),
                        MonthlyPostCounts = new Dictionary<string, int>(),
                        HierarchyDepth = 0
                    };
                }

                return new CategoryStatsDto
                {
                    CategoryId = rootCategory.Id,
                    CategoryName = "All Categories Summary",
                    CategorySlug = "summary",
                    PostCount = totalPosts,
                    PublishedPostCount = totalPosts,
                    DraftPostCount = 0,
                    CommentCount = 0,
                    TotalViews = 0,
                    ChildCategoryCount = totalCategories,
                    ChildCategories = new List<CategoryBasicInfo>(),
                    PopularPosts = new List<PostBasicInfo>(),
                    RecentPosts = new List<PostBasicInfo>(),
                    MonthlyPostCounts = new Dictionary<string, int>(),
                    HierarchyDepth = maxDepth
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category statistics");
            throw;
        }
    }

    /// <summary>
    /// Creates a category using DTO (for backward compatibility)
    /// </summary>
    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto, CancellationToken cancellationToken = default)
    {
        // Map DTO to Request object
        var request = new CreateCategoryRequest
        {
            Name = createDto.Name,
            Slug = createDto.Slug,
            Description = createDto.Description,
            ParentId = createDto.ParentId,
            DisplayOrder = createDto.DisplayOrder,
            IsActive = createDto.IsVisible, // Map IsVisible to IsActive
            Color = createDto.Color,
            Icon = createDto.Icon,
            CoverImageUrl = null, // Not available in CreateCategoryDto
            MetaTitle = null, // Not available in CreateCategoryDto
            MetaDescription = createDto.MetaDescription,
            MetaKeywords = createDto.MetaKeywords
        };

        // Get current user ID or use system admin if not authenticated
        var currentUserId = _userContextService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Creating category without authenticated user context");
            // Use the admin user ID from seed data as fallback
            currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
        }
        return await CreateCategoryAsync(request, currentUserId.Value, cancellationToken);
    }

    /// <summary>
    /// Updates a category using DTO (for backward compatibility)
    /// </summary>
    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryDto updateDto, CancellationToken cancellationToken = default)
    {
        // Map DTO to Request object
        var request = new UpdateCategoryRequest
        {
            Name = updateDto.Name,
            Slug = updateDto.Slug,
            Description = updateDto.Description,
            ParentId = updateDto.ParentId,
            DisplayOrder = updateDto.DisplayOrder,
            IsActive = updateDto.IsVisible, // Map IsVisible to IsActive
            Color = updateDto.Color,
            Icon = updateDto.Icon,
            CoverImageUrl = null, // Not available in UpdateCategoryDto
            MetaTitle = null, // Not available in UpdateCategoryDto
            MetaDescription = updateDto.MetaDescription,
            MetaKeywords = updateDto.MetaKeywords
        };

        // Get current user ID or use system admin if not authenticated
        var currentUserId = _userContextService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Updating category without authenticated user context");
            // Use the admin user ID from seed data as fallback
            currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
        }
        return await UpdateCategoryAsync(id, request, currentUserId.Value, cancellationToken);
    }

    /// <summary>
    /// Deletes a category (for backward compatibility)
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdToUse = userId ?? _userContextService.GetCurrentUserId();
            if (!userIdToUse.HasValue)
            {
                _logger.LogWarning("Deleting category without authenticated user context");
                // Use the admin user ID from seed data as fallback
                userIdToUse = new Guid("11111111-1111-1111-1111-111111111111");
            }
            var result = await DeleteCategoryAsync(id, userIdToUse.Value, cancellationToken);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    // Controller compatibility methods

    public async Task<IEnumerable<CategoryDto>> GetSubcategoriesAsync(Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subcategories = await GetChildCategoriesAsync(parentId, false, cancellationToken);
            return subcategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subcategories for parent {ParentId}", parentId);
            throw;
        }
    }

    public async Task ReorderCategoriesAsync(ReorderCategoriesDto reorderDto, CancellationToken cancellationToken = default)
    {
        try
        {
            if (reorderDto.Categories == null || !reorderDto.Categories.Any())
                return;

            var categoryOrders = reorderDto.Categories.Select(co => new CategoryOrderDto
            {
                CategoryId = co.Id,
                DisplayOrder = co.DisplayOrder
            }).ToList();

            // Use the existing method with current user or system user
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Reordering categories without authenticated user context");
                // Use the admin user ID from seed data as fallback
                currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
            }
            await ReorderCategoriesAsync(categoryOrders, currentUserId.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            throw;
        }
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(bool includeHidden, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new CategoryQueryDto
            {
                IncludeInactive = includeHidden,
                Page = 1,
                PageSize = 1000 // Large page size to get all categories
            };

            var response = await GetCategoriesAsync(query, cancellationToken);
            return response.Items.Select(item => new CategoryDto
            {
                Id = item.Id,
                Name = item.Name,
                Slug = item.Slug,
                Description = item.Description,
                IsActive = item.IsActive,
                ParentId = item.ParentId,
                Level = item.Level,
                PostCount = item.PostCount,
                DisplayOrder = item.DisplayOrder,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories with includeHidden={IncludeHidden}", includeHidden);
            throw;
        }
    }

    public async Task<CategoryStatsDto?> GetCategoryStatsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await GetCategoryStatsAsync((Guid?)id, cancellationToken);
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category stats for {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCategoryWithPostMoveAsync(Guid id, Guid? movePostsToCategory, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the existing delete method with current user or system user
            var currentUserId = _userContextService.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                _logger.LogWarning("Force deleting category without authenticated user context");
                // Use the admin user ID from seed data as fallback
                currentUserId = new Guid("11111111-1111-1111-1111-111111111111");
            }
            var result = await DeleteCategoryAsync(id, currentUserId.Value, cancellationToken);

            // Implement post moving logic if movePostsToCategory is specified
            if (movePostsToCategory.HasValue && result.Success)
            {
                var migrationRequest = new PostMigrationRequest
                {
                    FromCategoryId = id,
                    ToCategoryId = movePostsToCategory.Value,
                    Strategy = PostMigrationStrategy.MoveToTarget,
                    UpdateSeoRedirects = true,
                    PreserveTimestamps = true,
                    Notes = $"Automatic migration during category deletion"
                };

                var migrationResult = await MigratePostsAsync(migrationRequest, currentUserId.Value, cancellationToken);
                if (!migrationResult.Success)
                {
                    _logger.LogWarning("Failed to migrate posts from deleted category {CategoryId}: {Errors}",
                        id, string.Join(", ", migrationResult.Errors));
                }
                else
                {
                    _logger.LogInformation("Successfully migrated {PostCount} posts from deleted category {CategoryId} to {TargetCategoryId}",
                        migrationResult.PostsMigrated, id, movePostsToCategory.Value);
                }
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return false;
        }
    }

    #endregion

    #region Post Migration Methods

    /// <summary>
    /// Migrates posts from one category to another
    /// </summary>
    public async Task<PostMigrationResult> MigratePostsAsync(PostMigrationRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return PostMigrationResult.CreateFailure("User not found");

            var sourceCategory = await _categoryRepository.GetByIdAsync(request.FromCategoryId, cancellationToken);
            if (sourceCategory == null)
                return PostMigrationResult.CreateFailure("Source category not found");

            var sourceCategoryInfo = new CategoryBasicInfo
            {
                Id = sourceCategory.Id,
                Name = sourceCategory.Name,
                Slug = sourceCategory.Slug,
                Description = sourceCategory.Description,
                PostCount = sourceCategory.PostCount
            };

            CategoryBasicInfo? targetCategoryInfo = null;
            Category? targetCategory = null;

            // Determine target category based on strategy
            switch (request.Strategy)
            {
                case PostMigrationStrategy.MoveToTarget:
                    if (!request.ToCategoryId.HasValue)
                        return PostMigrationResult.CreateFailure("Target category ID is required for MoveToTarget strategy", sourceCategoryInfo);

                    targetCategory = await _categoryRepository.GetByIdAsync(request.ToCategoryId.Value, cancellationToken);
                    if (targetCategory == null)
                        return PostMigrationResult.CreateFailure("Target category not found", sourceCategoryInfo);
                    break;

                case PostMigrationStrategy.MoveToParent:
                    if (sourceCategory.ParentId.HasValue)
                    {
                        targetCategory = await _categoryRepository.GetByIdAsync(sourceCategory.ParentId.Value, cancellationToken);
                        if (targetCategory == null)
                            return PostMigrationResult.CreateFailure("Parent category not found", sourceCategoryInfo);
                    }
                    else
                    {
                        // No parent, use uncategorized
                        var uncategorizedCategory = await GetOrCreateUncategorizedCategoryAsync(cancellationToken);
                        targetCategory = await _categoryRepository.GetByIdAsync(uncategorizedCategory.Id, cancellationToken);
                    }
                    break;

                case PostMigrationStrategy.MoveToUncategorized:
                    var uncategorized = await GetOrCreateUncategorizedCategoryAsync(cancellationToken);
                    targetCategory = await _categoryRepository.GetByIdAsync(uncategorized.Id, cancellationToken);
                    break;

                case PostMigrationStrategy.DeletePosts:
                    // This is a dangerous operation - we'll just log and return
                    _logger.LogWarning("DeletePosts strategy requested for category {CategoryId}. This operation is not implemented for safety.", request.FromCategoryId);
                    return PostMigrationResult.CreateFailure("DeletePosts strategy is not supported for safety reasons", sourceCategoryInfo);

                default:
                    return PostMigrationResult.CreateFailure("Unknown migration strategy", sourceCategoryInfo);
            }

            if (targetCategory != null)
            {
                targetCategoryInfo = new CategoryBasicInfo
                {
                    Id = targetCategory.Id,
                    Name = targetCategory.Name,
                    Slug = targetCategory.Slug,
                    Description = targetCategory.Description,
                    PostCount = targetCategory.PostCount
                };
            }

            // Get posts to migrate
            var postsToMigrate = await _postRepository.FindAsync(p =>
                p.CategoryId == request.FromCategoryId && !p.IsDeleted, cancellationToken);
            var postsList = postsToMigrate.ToList();

            if (!postsList.Any())
            {
                return PostMigrationResult.CreateSuccess(0, sourceCategoryInfo, targetCategoryInfo, "No posts to migrate");
            }

            var migratedCount = 0;
            var errors = new List<string>();

            // Begin transaction for data integrity
            using var transaction = await _categoryRepository.BeginTransactionAsync(cancellationToken);
            try
            {
                foreach (var post in postsList)
                {
                    try
                    {
                        post.CategoryId = targetCategory?.Id;
                        post.UpdatedBy = userId;
                        if (!request.PreserveTimestamps)
                        {
                            post.UpdateAuditFields();
                        }

                        _postRepository.Update(post);
                        migratedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to migrate post {PostId} from category {FromCategoryId} to {ToCategoryId}",
                            post.Id, request.FromCategoryId, targetCategory?.Id);
                        errors.Add($"Failed to migrate post '{post.Title}': {ex.Message}");
                    }
                }

                await _postRepository.SaveChangesAsync(cancellationToken);

                // Update post counts
                sourceCategory.PostCount = Math.Max(0, sourceCategory.PostCount - migratedCount);
                _categoryRepository.Update(sourceCategory);

                if (targetCategory != null)
                {
                    targetCategory.PostCount += migratedCount;
                    _categoryRepository.Update(targetCategory);
                }

                await _categoryRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var result = new PostMigrationResult
                {
                    Success = migratedCount > 0,
                    PostsMigrated = migratedCount,
                    PostsFailed = errors.Count,
                    TotalPosts = postsList.Count,
                    SourceCategory = sourceCategoryInfo,
                    TargetCategory = targetCategoryInfo,
                    Message = errors.Any()
                        ? $"Migrated {migratedCount} posts with {errors.Count} errors"
                        : $"Successfully migrated {migratedCount} posts",
                    Errors = errors,
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Post migration completed: {MigratedCount} posts moved from {SourceCategory} to {TargetCategory}",
                    migratedCount, sourceCategory.Name, targetCategory?.Name ?? "Uncategorized");

                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during post migration from category {FromCategoryId} to {ToCategoryId}",
                request.FromCategoryId, request.ToCategoryId);
            return PostMigrationResult.CreateFailure($"Migration failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the number of posts in a category
    /// </summary>
    public async Task<int> GetCategoryPostCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _postRepository.CountAsync(p => p.CategoryId == categoryId && !p.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting post count for category {CategoryId}", categoryId);
            throw;
        }
    }

    /// <summary>
    /// Validates if a category can be safely deleted
    /// </summary>
    public async Task<CategoryDeletionValidation> ValidateCategoryDeletionAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category == null)
            {
                return new CategoryDeletionValidation
                {
                    CanDelete = false,
                    BlockingIssues = { "Category not found" }
                };
            }

            var validation = new CategoryDeletionValidation();

            // Check post count
            validation.PostCount = await GetCategoryPostCountAsync(categoryId, cancellationToken);

            // Check child categories
            var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == categoryId, cancellationToken);
            validation.ChildCategoryCount = childCategories.Count();

            // Check descendant categories
            var descendants = await _categoryRepository.FindAsync(c =>
                c.TreePath != null && c.TreePath.Contains($"/{categoryId}/") && c.Id != categoryId, cancellationToken);
            validation.DescendantCategoryCount = descendants.Count();

            // Determine if can delete
            if (validation.PostCount > 0)
            {
                validation.BlockingIssues.Add($"Category contains {validation.PostCount} posts that need to be migrated");
            }

            if (validation.ChildCategoryCount > 0)
            {
                validation.BlockingIssues.Add($"Category has {validation.ChildCategoryCount} child categories that need to be handled");
            }

            validation.CanDelete = !validation.BlockingIssues.Any();

            // Add warnings
            if (validation.PostCount > 0)
            {
                validation.Warnings.Add($"Deleting this category will require migrating {validation.PostCount} posts");
            }

            if (validation.DescendantCategoryCount > 0)
            {
                validation.Warnings.Add($"This category has {validation.DescendantCategoryCount} total descendant categories");
            }

            // Suggest target categories
            var suggestedTargets = new List<CategoryBasicInfo>();

            // Add parent category as suggestion if available
            if (category.ParentId.HasValue)
            {
                var parent = await _categoryRepository.GetByIdAsync(category.ParentId.Value, cancellationToken);
                if (parent != null)
                {
                    validation.ParentCategory = new CategoryBasicInfo
                    {
                        Id = parent.Id,
                        Name = parent.Name,
                        Slug = parent.Slug,
                        Description = parent.Description,
                        PostCount = parent.PostCount
                    };
                    suggestedTargets.Add(validation.ParentCategory);
                }
            }

            // Add sibling categories
            var siblings = await _categoryRepository.FindAsync(c =>
                c.ParentId == category.ParentId && c.Id != categoryId && c.IsActive, cancellationToken);

            foreach (var sibling in siblings.Take(5))
            {
                suggestedTargets.Add(new CategoryBasicInfo
                {
                    Id = sibling.Id,
                    Name = sibling.Name,
                    Slug = sibling.Slug,
                    Description = sibling.Description,
                    PostCount = sibling.PostCount
                });
            }

            validation.SuggestedTargetCategories = suggestedTargets;

            return validation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating category deletion for {CategoryId}", categoryId);
            throw;
        }
    }

    /// <summary>
    /// Deletes a category with enhanced migration options
    /// </summary>
    public async Task<OperationResult<PostMigrationResult>> DeleteCategoryWithMigrationAsync(DeleteCategoryWithMigrationRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult<PostMigrationResult>.Failure("User not found");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                return OperationResult<PostMigrationResult>.Failure("Category not found");

            // Validate deletion
            var validation = await ValidateCategoryDeletionAsync(request.CategoryId, cancellationToken);

            if (!validation.CanDelete && !request.ForceDelete)
            {
                return OperationResult<PostMigrationResult>.Failure(
                    $"Cannot delete category: {string.Join(", ", validation.BlockingIssues)}");
            }

            PostMigrationResult? migrationResult = null;

            using var transaction = await _categoryRepository.BeginTransactionAsync(cancellationToken);
            try
            {
                // Handle post migration if needed
                if (validation.PostCount > 0 && request.PostMigration != null)
                {
                    migrationResult = await MigratePostsAsync(request.PostMigration, userId, cancellationToken);
                    if (!migrationResult.Success)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return OperationResult<PostMigrationResult>.Failure(
                            $"Post migration failed: {string.Join(", ", migrationResult.Errors)}");
                    }
                }

                // Handle child categories
                if (validation.ChildCategoryCount > 0)
                {
                    var childCategories = await _categoryRepository.FindAsync(c => c.ParentId == request.CategoryId, cancellationToken);

                    switch (request.ChildHandling)
                    {
                        case ChildCategoryHandling.PreventDeletion:
                            if (!request.ForceDelete)
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return OperationResult<PostMigrationResult>.Failure("Category has child categories");
                            }
                            break;

                        case ChildCategoryHandling.MoveToParent:
                            foreach (var child in childCategories)
                            {
                                child.ParentId = category.ParentId;
                                child.SetParent(category.ParentId.HasValue ?
                                    await _categoryRepository.GetByIdAsync(category.ParentId.Value, cancellationToken) : null);
                                child.UpdatedBy = userId;
                                child.UpdateAuditFields();
                                _categoryRepository.Update(child);
                            }
                            break;

                        case ChildCategoryHandling.DeleteRecursively:
                            foreach (var child in childCategories)
                            {
                                child.SoftDelete(userId);
                                _categoryRepository.Update(child);
                            }
                            break;
                    }

                    await _categoryRepository.SaveChangesAsync(cancellationToken);
                    await UpdateDescendantHierarchy(category, cancellationToken);
                }

                // Delete the category
                category.SoftDelete(userId);
                _categoryRepository.Update(category);
                await _categoryRepository.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Category {CategoryName} deleted successfully with migration by user {UserId}",
                    category.Name, userId);

                return OperationResult<PostMigrationResult>.CreateSuccess(
                    migrationResult ?? PostMigrationResult.CreateSuccess(0, null, null, "No posts to migrate"),
                    "Category deleted successfully");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with migration {CategoryId}", request.CategoryId);
            return OperationResult<PostMigrationResult>.Failure($"Deletion failed: {ex.Message}");
        }
    }

    #endregion

    #region Category Merge Methods

    /// <summary>
    /// Merges multiple categories into a target category
    /// </summary>
    public async Task<CategoryMergeResult> MergeCategoriesAsync(CategoryMergeRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return CategoryMergeResult.CreateFailure("User not found");

            // Validate merge
            var validation = await ValidateCategoryMergeAsync(request.SourceCategoryIds, request.TargetCategoryId, cancellationToken);
            if (!validation.Success)
            {
                return CategoryMergeResult.CreateFailure(validation.Message ?? "Merge validation failed");
            }

            var targetCategory = await _categoryRepository.GetByIdAsync(request.TargetCategoryId, cancellationToken);
            if (targetCategory == null)
                return CategoryMergeResult.CreateFailure("Target category not found");

            var sourceCategories = await _categoryRepository.FindAsync(c =>
                request.SourceCategoryIds.Contains(c.Id), cancellationToken);
            var sourceCategoriesList = sourceCategories.ToList();

            if (sourceCategoriesList.Count != request.SourceCategoryIds.Count)
                return CategoryMergeResult.CreateFailure("One or more source categories not found");

            var targetCategoryInfo = new CategoryBasicInfo
            {
                Id = targetCategory.Id,
                Name = targetCategory.Name,
                Slug = targetCategory.Slug,
                Description = targetCategory.Description,
                PostCount = targetCategory.PostCount
            };

            var sourceCategoryInfos = sourceCategoriesList.Select(c => new CategoryBasicInfo
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                PostCount = c.PostCount
            }).ToList();

            using var transaction = await _categoryRepository.BeginTransactionAsync(cancellationToken);
            try
            {
                var totalPostsMigrated = 0;

                // Migrate posts from each source category
                foreach (var sourceCategory in sourceCategoriesList)
                {
                    if (sourceCategory.PostCount > 0)
                    {
                        var migrationRequest = new PostMigrationRequest
                        {
                            FromCategoryId = sourceCategory.Id,
                            ToCategoryId = request.TargetCategoryId,
                            Strategy = PostMigrationStrategy.MoveToTarget,
                            UpdateSeoRedirects = true,
                            PreserveTimestamps = true,
                            Notes = $"Merge operation from {sourceCategory.Name} to {targetCategory.Name}"
                        };

                        var migrationResult = await MigratePostsAsync(migrationRequest, userId, cancellationToken);
                        if (migrationResult.Success)
                        {
                            totalPostsMigrated += migrationResult.PostsMigrated;
                        }
                    }
                }

                // Handle category properties based on conflict resolution strategy
                switch (request.ConflictResolution)
                {
                    case CategoryMergeConflictResolution.KeepTarget:
                        // No changes to target category properties
                        break;

                    case CategoryMergeConflictResolution.KeepNewer:
                        var newestSource = sourceCategoriesList
                            .Where(c => c.UpdatedAt > targetCategory.UpdatedAt)
                            .OrderByDescending(c => c.UpdatedAt)
                            .FirstOrDefault();

                        if (newestSource != null)
                        {
                            if (string.IsNullOrEmpty(targetCategory.Description) && !string.IsNullOrEmpty(newestSource.Description))
                                targetCategory.Description = newestSource.Description;

                            if (string.IsNullOrEmpty(targetCategory.Color) && !string.IsNullOrEmpty(newestSource.Color))
                                targetCategory.Color = newestSource.Color;

                            if (string.IsNullOrEmpty(targetCategory.Icon) && !string.IsNullOrEmpty(newestSource.Icon))
                                targetCategory.Icon = newestSource.Icon;
                        }
                        break;

                    case CategoryMergeConflictResolution.MergeMetadata:
                        var descriptions = sourceCategoriesList
                            .Where(c => !string.IsNullOrEmpty(c.Description))
                            .Select(c => c.Description)
                            .ToList();

                        if (descriptions.Any() && string.IsNullOrEmpty(targetCategory.Description))
                        {
                            targetCategory.Description = string.Join(" | ", descriptions);
                        }
                        break;
                }

                targetCategory.UpdatedBy = userId;
                targetCategory.UpdateAuditFields();
                _categoryRepository.Update(targetCategory);

                // Delete source categories if requested
                if (request.DeleteSourceCategories)
                {
                    foreach (var sourceCategory in sourceCategoriesList)
                    {
                        sourceCategory.SoftDelete(userId);
                        _categoryRepository.Update(sourceCategory);
                    }
                }

                await _categoryRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                var result = CategoryMergeResult.CreateSuccess(
                    sourceCategoriesList.Count,
                    totalPostsMigrated,
                    targetCategoryInfo,
                    sourceCategoryInfos);

                result.StartedAt = startTime;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Category merge completed: {SourceCount} categories merged into {TargetCategory}, {PostCount} posts migrated",
                    sourceCategoriesList.Count, targetCategory.Name, totalPostsMigrated);

                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging categories into {TargetCategoryId}", request.TargetCategoryId);
            return CategoryMergeResult.CreateFailure($"Merge failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates if categories can be merged
    /// </summary>
    public async Task<OperationResult> ValidateCategoryMergeAsync(List<Guid> sourceCategoryIds, Guid targetCategoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!sourceCategoryIds.Any())
                return OperationResult.Failure("No source categories specified");

            if (sourceCategoryIds.Contains(targetCategoryId))
                return OperationResult.Failure("Cannot merge a category into itself");

            var targetCategory = await _categoryRepository.GetByIdAsync(targetCategoryId, cancellationToken);
            if (targetCategory == null)
                return OperationResult.Failure("Target category not found");

            var sourceCategories = await _categoryRepository.FindAsync(c =>
                sourceCategoryIds.Contains(c.Id), cancellationToken);
            var sourceCategoriesList = sourceCategories.ToList();

            if (sourceCategoriesList.Count != sourceCategoryIds.Count)
                return OperationResult.Failure("One or more source categories not found");

            // Check for hierarchy conflicts
            foreach (var sourceCategory in sourceCategoriesList)
            {
                // Check if target is a descendant of source
                if (targetCategory.TreePath?.Contains($"/{sourceCategory.Id}/") == true)
                {
                    return OperationResult.Failure($"Cannot merge category '{sourceCategory.Name}' into its descendant '{targetCategory.Name}'");
                }

                // Check if source is an ancestor of target
                if (sourceCategory.TreePath?.Contains($"/{targetCategoryId}/") == true)
                {
                    return OperationResult.Failure($"Cannot merge ancestor category '{sourceCategory.Name}' into '{targetCategory.Name}'");
                }
            }

            return OperationResult.CreateSuccess("Categories can be merged");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating category merge");
            return OperationResult.Failure($"Validation failed: {ex.Message}");
        }
    }

    #endregion

    #region Advanced Category Management

    /// <summary>
    /// Gets a default uncategorized category (creates if not exists)
    /// </summary>
    public async Task<CategoryDto> GetOrCreateUncategorizedCategoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Look for existing uncategorized category
            var existingUncategorized = await _categoryRepository.FindAsync(c =>
                c.Slug == "uncategorized" || c.Name.ToLowerInvariant() == "uncategorized", cancellationToken);

            var uncategorized = existingUncategorized.FirstOrDefault();

            if (uncategorized != null)
            {
                return await MapToDtoAsync(uncategorized, cancellationToken);
            }

            // Create uncategorized category
            var systemUserId = new Guid("11111111-1111-1111-1111-111111111111"); // System admin user

            var createRequest = new CreateCategoryRequest
            {
                Name = "Uncategorized",
                Slug = "uncategorized",
                Description = "Default category for posts without a specific category",
                IsActive = true,
                DisplayOrder = int.MaxValue, // Put at the end
                Color = "#6B7280", // Gray color
                Icon = "folder"
            };

            var createdCategory = await CreateCategoryAsync(createRequest, systemUserId, cancellationToken);

            _logger.LogInformation("Created default uncategorized category");

            return createdCategory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating uncategorized category");
            throw;
        }
    }

    /// <summary>
    /// Rebuilds post counts for all categories affected by migration
    /// </summary>
    public async Task<OperationResult> UpdatePostCountsForCategoriesAsync(IEnumerable<Guid> categoryIds, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return OperationResult.Failure("User not found");

            var categoryIdList = categoryIds.ToList();
            var categories = await _categoryRepository.FindAsync(c => categoryIdList.Contains(c.Id), cancellationToken);
            var updatedCount = 0;

            foreach (var category in categories)
            {
                var actualPostCount = await _postRepository.CountAsync(p =>
                    p.CategoryId == category.Id && !p.IsDeleted, cancellationToken);

                if (category.PostCount != actualPostCount)
                {
                    category.PostCount = actualPostCount;
                    category.UpdatedBy = userId;
                    category.UpdateAuditFields();
                    _categoryRepository.Update(category);
                    updatedCount++;
                }
            }

            await _categoryRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated post counts for {UpdatedCount} categories", updatedCount);
            return OperationResult.CreateSuccess($"Updated post counts for {updatedCount} categories");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating post counts for categories");
            return OperationResult.Failure($"Failed to update post counts: {ex.Message}");
        }
    }

    #endregion
}