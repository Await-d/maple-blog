using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for Category entity with hierarchical operations
    /// </summary>
    public interface ICategoryRepository : IRepository<Category>
    {
        /// <summary>
        /// Gets a category by slug
        /// </summary>
        /// <param name="slug">Category slug</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category if found</returns>
        Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all root categories (categories without parents)
        /// </summary>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Root categories</returns>
        Task<IReadOnlyList<Category>> GetRootCategoriesAsync(
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets child categories of a parent category
        /// </summary>
        /// <param name="parentId">Parent category ID</param>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Child categories</returns>
        Task<IReadOnlyList<Category>> GetChildCategoriesAsync(
            Guid parentId,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all descendants of a category (recursive)
        /// </summary>
        /// <param name="parentId">Parent category ID</param>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>All descendant categories</returns>
        Task<IReadOnlyList<Category>> GetDescendantsAsync(
            Guid parentId,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all ancestors of a category (path to root)
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ancestor categories ordered from root to parent</returns>
        Task<IReadOnlyList<Category>> GetAncestorsAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the complete category tree structure
        /// </summary>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Hierarchical category tree</returns>
        Task<IReadOnlyList<CategoryTreeNode>> GetCategoryTreeAsync(
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories with their post counts
        /// </summary>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="publishedPostsOnly">Whether to count only published posts</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories with post counts</returns>
        Task<IReadOnlyList<CategoryWithPostCount>> GetCategoriesWithPostCountsAsync(
            bool activeOnly = true,
            bool publishedPostsOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories ordered by sort order and name
        /// </summary>
        /// <param name="parentId">Optional parent category filter</param>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Ordered categories</returns>
        Task<IReadOnlyList<Category>> GetOrderedAsync(
            Guid? parentId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches categories by name and description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="activeOnly">Whether to include only active categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching categories</returns>
        Task<IReadOnlyList<Category>> SearchAsync(
            string searchTerm,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the maximum sort order for categories at a specific level
        /// </summary>
        /// <param name="parentId">Parent category ID (null for root level)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Maximum sort order</returns>
        Task<int> GetMaxSortOrderAsync(
            Guid? parentId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates sort orders for categories
        /// </summary>
        /// <param name="categoryIds">Category IDs in the desired order</param>
        /// <param name="parentId">Parent category ID (null for root level)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of updated categories</returns>
        Task<int> UpdateSortOrdersAsync(
            IEnumerable<Guid> categoryIds,
            Guid? parentId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if moving a category would create a circular reference
        /// </summary>
        /// <param name="categoryId">Category to move</param>
        /// <param name="newParentId">New parent category</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if move would create circular reference</returns>
        Task<bool> WouldCreateCircularReferenceAsync(
            Guid categoryId,
            Guid? newParentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a slug is available within a specific parent category
        /// </summary>
        /// <param name="slug">Slug to check</param>
        /// <param name="parentId">Parent category ID</param>
        /// <param name="excludeCategoryId">Category ID to exclude from check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if slug is available</returns>
        Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? parentId = null,
            Guid? excludeCategoryId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets category statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category statistics</returns>
        Task<CategoryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a node in the category tree structure
    /// </summary>
    public class CategoryTreeNode
    {
        public Category Category { get; init; } = null!;
        public IReadOnlyList<CategoryTreeNode> Children { get; init; } = new List<CategoryTreeNode>();
        public int Depth { get; init; }
        public int PostCount { get; init; }
    }

    /// <summary>
    /// Category with post count information
    /// </summary>
    public class CategoryWithPostCount
    {
        public Category Category { get; init; } = null!;
        public int PostCount { get; init; }
        public int DirectPostCount { get; init; }
        public int TotalPostCount { get; init; } // Including descendant categories
    }

    /// <summary>
    /// Category statistics data
    /// </summary>
    public class CategoryStatistics
    {
        public int TotalCategories { get; init; }
        public int ActiveCategories { get; init; }
        public int InactiveCategories { get; init; }
        public int RootCategories { get; init; }
        public int CategoriesWithPosts { get; init; }
        public int CategoriesWithoutPosts { get; init; }
        public int MaxDepthLevel { get; init; }
        public Category? CategoryWithMostPosts { get; init; }
        public int MostPostsCount { get; init; }
    }
}