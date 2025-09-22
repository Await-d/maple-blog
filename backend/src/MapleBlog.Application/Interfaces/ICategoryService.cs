using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Category service interface for category management operations
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets a category by its ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category DTO if found, null otherwise</returns>
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a category by its slug
    /// </summary>
    /// <param name="slug">Category slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category DTO if found, null otherwise</returns>
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all categories with filtering and pagination
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated category list response</returns>
    Task<CategoryListResponse> GetCategoriesAsync(CategoryQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets categories as a hierarchical tree structure
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hierarchical category tree</returns>
    Task<List<CategoryTreeDto>> GetCategoryTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets root categories (categories without parent)
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root categories</returns>
    Task<List<CategoryDto>> GetRootCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child categories for a parent category
    /// </summary>
    /// <param name="parentId">Parent category ID</param>
    /// <param name="includeInactive">Whether to include inactive categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child categories</returns>
    Task<List<CategoryDto>> GetChildCategoriesAsync(Guid parentId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all ancestor categories for a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ancestor categories ordered from root to parent</returns>
    Task<List<CategoryDto>> GetAncestorCategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new category
    /// </summary>
    /// <param name="request">Create category request</param>
    /// <param name="userId">User ID creating the category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category DTO</returns>
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Update category request</param>
    /// <param name="userId">User ID performing the update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category DTO</returns>
    Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a category to a new parent or changes its order
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Move category request</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> MoveCategoryAsync(Guid id, MoveCategoryRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates or deactivates a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="isActive">New active status</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> SetCategoryStatusAsync(Guid id, bool isActive, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="userId">User ID performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> DeleteCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes a category (admin only)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="userId">User ID performing the deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> PermanentlyDeleteCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="userId">User ID performing the restoration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> RestoreCategoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates category slug uniqueness
    /// </summary>
    /// <param name="slug">Slug to validate</param>
    /// <param name="excludeCategoryId">Category ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if slug is unique, false otherwise</returns>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique slug from name
    /// </summary>
    /// <param name="name">Category name</param>
    /// <param name="excludeCategoryId">Category ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Unique slug</returns>
    Task<string> GenerateUniqueSlugAsync(string name, Guid? excludeCategoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds category hierarchy paths and levels (maintenance operation)
    /// </summary>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with number of categories updated</returns>
    Task<OperationResult> RebuildCategoryHierarchyAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates post counts for all categories (maintenance operation)
    /// </summary>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with number of categories updated</returns>
    Task<OperationResult> UpdatePostCountsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations on multiple categories
    /// </summary>
    /// <param name="categoryIds">Category IDs to operate on</param>
    /// <param name="operation">Operation to perform (activate, deactivate, delete)</param>
    /// <param name="userId">User ID performing the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    Task<BulkOperationResult> BulkOperationAsync(IEnumerable<Guid> categoryIds, string operation, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches categories by name and description
    /// </summary>
    /// <param name="searchQuery">Search query</param>
    /// <param name="includeInactive">Whether to include inactive categories</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching categories</returns>
    Task<List<CategoryDto>> SearchCategoriesAsync(string searchQuery, bool includeInactive = false, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders categories for display order management
    /// </summary>
    /// <param name="categoryOrders">List of category IDs with their new order positions</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> ReorderCategoriesAsync(List<CategoryOrderDto> categoryOrders, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category statistics
    /// </summary>
    /// <param name="categoryId">Category ID (optional, if not provided returns stats for all categories)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category statistics</returns>
    Task<CategoryStatsDto> GetCategoryStatsAsync(Guid? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a category using DTO (for backward compatibility)
    /// </summary>
    /// <param name="createDto">Create category DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created category</returns>
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a category using DTO (for backward compatibility)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="updateDto">Update category DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated category</returns>
    Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category (for backward compatibility)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteCategoryAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subcategories for controller compatibility
    /// </summary>
    /// <param name="parentId">Parent category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of subcategories</returns>
    Task<IEnumerable<CategoryDto>> GetSubcategoriesAsync(Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders categories for controller compatibility
    /// </summary>
    /// <param name="reorderDto">Reorder categories DTO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation success</returns>
    Task ReorderCategoriesAsync(ReorderCategoriesDto reorderDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets categories with simple boolean parameter
    /// </summary>
    /// <param name="includeHidden">Include hidden categories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of categories</returns>
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(bool includeHidden, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets category statistics by ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category statistics</returns>
    Task<CategoryStatsDto?> GetCategoryStatsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category with optional post move (controller compatibility)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="movePostsToCategory">Move posts to this category instead of deleting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteCategoryWithPostMoveAsync(Guid id, Guid? movePostsToCategory, CancellationToken cancellationToken = default);

    // Post Migration Methods

    /// <summary>
    /// Migrates posts from one category to another
    /// </summary>
    /// <param name="request">Post migration request</param>
    /// <param name="userId">User ID performing the migration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Post migration result</returns>
    Task<PostMigrationResult> MigratePostsAsync(PostMigrationRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of posts in a category
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of posts in the category</returns>
    Task<int> GetCategoryPostCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a category can be safely deleted
    /// </summary>
    /// <param name="categoryId">Category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category deletion validation result</returns>
    Task<CategoryDeletionValidation> ValidateCategoryDeletionAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a category with enhanced migration options
    /// </summary>
    /// <param name="request">Delete category with migration request</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with migration details</returns>
    Task<OperationResult<PostMigrationResult>> DeleteCategoryWithMigrationAsync(DeleteCategoryWithMigrationRequest request, Guid userId, CancellationToken cancellationToken = default);

    // Category Merge Methods

    /// <summary>
    /// Merges multiple categories into a target category
    /// </summary>
    /// <param name="request">Category merge request</param>
    /// <param name="userId">User ID performing the merge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Category merge result</returns>
    Task<CategoryMergeResult> MergeCategoriesAsync(CategoryMergeRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if categories can be merged
    /// </summary>
    /// <param name="sourceCategoryIds">Source category IDs</param>
    /// <param name="targetCategoryId">Target category ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with potential conflicts</returns>
    Task<OperationResult> ValidateCategoryMergeAsync(List<Guid> sourceCategoryIds, Guid targetCategoryId, CancellationToken cancellationToken = default);

    // Advanced Category Management

    /// <summary>
    /// Gets a default uncategorized category (creates if not exists)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Default uncategorized category</returns>
    Task<CategoryDto> GetOrCreateUncategorizedCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds post counts for all categories affected by migration
    /// </summary>
    /// <param name="categoryIds">Category IDs to update</param>
    /// <param name="userId">User ID performing the operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<OperationResult> UpdatePostCountsForCategoriesAsync(IEnumerable<Guid> categoryIds, Guid userId, CancellationToken cancellationToken = default);
}