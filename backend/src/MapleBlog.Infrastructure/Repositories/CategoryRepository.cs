using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Category repository implementation with Entity Framework Core
    /// </summary>
    public class CategoryRepository : BlogBaseRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            return await _dbSet
                .Include(c => c.Parent)
                .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), cancellationToken);
        }

        public async Task<IReadOnlyList<Category>> GetRootCategoriesAsync(
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.ParentId == null);

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Category>> GetChildCategoriesAsync(
            Guid parentId,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.ParentId == parentId);

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Category>> GetDescendantsAsync(
            Guid parentId,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // This is a simplified implementation. For large hierarchies,
            // consider using a Common Table Expression (CTE) or materialized path pattern
            var descendants = new List<Category>();
            var toProcess = new Queue<Guid>();
            toProcess.Enqueue(parentId);

            while (toProcess.Count > 0)
            {
                var currentParentId = toProcess.Dequeue();
                var children = await GetChildCategoriesAsync(currentParentId, activeOnly, cancellationToken);

                foreach (var child in children)
                {
                    descendants.Add(child);
                    toProcess.Enqueue(child.Id);
                }
            }

            return descendants;
        }

        public async Task<IReadOnlyList<Category>> GetAncestorsAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var ancestors = new List<Category>();
            var currentCategory = await GetByIdAsync(categoryId, cancellationToken);

            while (currentCategory?.ParentId != null)
            {
                var parent = await GetByIdAsync(currentCategory.ParentId.Value, cancellationToken);
                if (parent != null)
                {
                    ancestors.Insert(0, parent); // Insert at beginning to maintain order from root
                    currentCategory = parent;
                }
                else
                {
                    break;
                }
            }

            return ancestors;
        }

        public async Task<IReadOnlyList<CategoryTreeNode>> GetCategoryTreeAsync(
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // Load all categories at once for efficiency
            var query = _dbSet.AsQueryable();

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            var allCategories = await query
                .Include(c => c.Posts.Where(p => p.Status == PostStatus.Published))
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var categoryDict = allCategories.ToDictionary(c => c.Id, c => c);
            var rootNodes = new List<CategoryTreeNode>();

            // Build tree structure
            foreach (var category in allCategories.Where(c => c.ParentId == null))
            {
                var node = BuildCategoryTreeNode(category, categoryDict, 0);
                rootNodes.Add(node);
            }

            return rootNodes;
        }

        public async Task<IReadOnlyList<CategoryWithPostCount>> GetCategoriesWithPostCountsAsync(
            bool activeOnly = true,
            bool publishedPostsOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = from category in _dbSet
                        select new
                        {
                            Category = category,
                            DirectPostCount = publishedPostsOnly
                                ? category.Posts.Count(p => p.Status == PostStatus.Published)
                                : category.Posts.Count()
                        };

            if (activeOnly)
                query = query.Where(c => c.Category.IsActive);

            var results = await query.ToListAsync(cancellationToken);

            var categoriesWithCounts = new List<CategoryWithPostCount>();

            foreach (var result in results)
            {
                // For total count including descendants, we'd need a more complex query
                // This is a simplified version showing direct posts only
                var categoryWithCount = new CategoryWithPostCount
                {
                    Category = result.Category,
                    PostCount = result.DirectPostCount,
                    DirectPostCount = result.DirectPostCount,
                    TotalPostCount = result.DirectPostCount // Simplified - should include descendant posts
                };
                categoriesWithCounts.Add(categoryWithCount);
            }

            return categoriesWithCounts;
        }

        public async Task<IReadOnlyList<Category>> GetOrderedAsync(
            Guid? parentId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.ParentId == parentId);

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Category>> SearchAsync(
            string searchTerm,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Category>();

            var query = _dbSet.Where(c =>
                EF.Functions.Like(c.Name, $"%{searchTerm}%") ||
                EF.Functions.Like(c.Description, $"%{searchTerm}%"));

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetMaxSortOrderAsync(
            Guid? parentId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(c => c.ParentId == parentId);

            if (!await query.AnyAsync(cancellationToken))
                return 0;

            return await query.MaxAsync(c => c.SortOrder, cancellationToken);
        }

        public async Task<int> UpdateSortOrdersAsync(
            IEnumerable<Guid> categoryIds,
            Guid? parentId = null,
            CancellationToken cancellationToken = default)
        {
            var categoryIdList = categoryIds.ToList();
            var categories = await _dbSet
                .Where(c => categoryIdList.Contains(c.Id) && c.ParentId == parentId)
                .ToListAsync(cancellationToken);

            var sortOrder = 1;
            foreach (var categoryId in categoryIdList)
            {
                var category = categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    category.UpdateSortOrder(sortOrder++);
                }
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> WouldCreateCircularReferenceAsync(
            Guid categoryId,
            Guid? newParentId,
            CancellationToken cancellationToken = default)
        {
            if (!newParentId.HasValue)
                return false;

            if (categoryId == newParentId.Value)
                return true;

            // Check if the new parent is a descendant of the current category
            var descendants = await GetDescendantsAsync(categoryId, false, cancellationToken);
            return descendants.Any(d => d.Id == newParentId.Value);
        }

        public async Task<bool> IsSlugAvailableAsync(
            string slug,
            Guid? parentId = null,
            Guid? excludeCategoryId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var query = _dbSet.Where(c => c.Slug == slug.ToLowerInvariant() && c.ParentId == parentId);

            if (excludeCategoryId.HasValue)
                query = query.Where(c => c.Id != excludeCategoryId.Value);

            return !await query.AnyAsync(cancellationToken);
        }

        public async Task<CategoryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var totalCategories = await _dbSet.CountAsync(cancellationToken);
            var activeCategories = await _dbSet.CountAsync(c => c.IsActive, cancellationToken);
            var inactiveCategories = totalCategories - activeCategories;
            var rootCategories = await _dbSet.CountAsync(c => c.ParentId == null, cancellationToken);

            var categoriesWithPosts = await _dbSet.CountAsync(c => c.Posts.Any(), cancellationToken);
            var categoriesWithoutPosts = totalCategories - categoriesWithPosts;

            // Calculate max depth level
            var maxDepthLevel = await CalculateMaxDepthLevel(cancellationToken);

            // Find category with most posts
            var categoryWithMostPosts = await _dbSet
                .Where(c => c.Posts.Any(p => p.Status == PostStatus.Published))
                .OrderByDescending(c => c.Posts.Count(p => p.Status == PostStatus.Published))
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(cancellationToken);

            var mostPostsCount = categoryWithMostPosts?.Posts.Count(p => p.Status == PostStatus.Published) ?? 0;

            return new CategoryStatistics
            {
                TotalCategories = totalCategories,
                ActiveCategories = activeCategories,
                InactiveCategories = inactiveCategories,
                RootCategories = rootCategories,
                CategoriesWithPosts = categoriesWithPosts,
                CategoriesWithoutPosts = categoriesWithoutPosts,
                MaxDepthLevel = maxDepthLevel,
                CategoryWithMostPosts = categoryWithMostPosts,
                MostPostsCount = mostPostsCount
            };
        }

        /// <summary>
        /// Builds a category tree node recursively
        /// </summary>
        private CategoryTreeNode BuildCategoryTreeNode(
            Category category,
            Dictionary<Guid, Category> categoryDict,
            int depth)
        {
            var children = new List<CategoryTreeNode>();

            foreach (var child in categoryDict.Values.Where(c => c.ParentId == category.Id)
                        .OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
            {
                var childNode = BuildCategoryTreeNode(child, categoryDict, depth + 1);
                children.Add(childNode);
            }

            return new CategoryTreeNode
            {
                Category = category,
                Children = children,
                Depth = depth,
                PostCount = category.Posts.Count(p => p.Status == PostStatus.Published)
            };
        }

        /// <summary>
        /// Calculates the maximum depth level in the category hierarchy
        /// </summary>
        private async Task<int> CalculateMaxDepthLevel(CancellationToken cancellationToken)
        {
            var rootCategories = await GetRootCategoriesAsync(false, cancellationToken);
            var maxDepth = 0;

            foreach (var rootCategory in rootCategories)
            {
                var depth = await CalculateCategoryDepth(rootCategory.Id, cancellationToken);
                maxDepth = Math.Max(maxDepth, depth);
            }

            return maxDepth;
        }

        /// <summary>
        /// Calculates the depth of a specific category branch
        /// </summary>
        private async Task<int> CalculateCategoryDepth(Guid categoryId, CancellationToken cancellationToken)
        {
            var children = await GetChildCategoriesAsync(categoryId, false, cancellationToken);

            if (!children.Any())
                return 0;

            var maxChildDepth = 0;
            foreach (var child in children)
            {
                var childDepth = await CalculateCategoryDepth(child.Id, cancellationToken);
                maxChildDepth = Math.Max(maxChildDepth, childDepth);
            }

            return maxChildDepth + 1;
        }
    }
}