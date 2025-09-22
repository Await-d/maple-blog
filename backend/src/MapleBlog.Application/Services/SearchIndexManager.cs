using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Services;

/// <summary>
/// 搜索索引管理器实现
/// </summary>
public class SearchIndexManager : ISearchIndexManager
{
    private readonly ILogger<SearchIndexManager> _logger;
    private readonly ISearchEngine _primarySearchEngine;
    private readonly ISearchEngine _fallbackSearchEngine;
    private readonly IPostRepository _postRepository;
    private readonly DbContext _context;

    public SearchIndexManager(
        ILogger<SearchIndexManager> logger,
        IEnumerable<ISearchEngine> searchEngines,
        IPostRepository postRepository,
        DbContext context)
    {
        _logger = logger;
        _postRepository = postRepository;
        _context = context;

        var engines = searchEngines.ToList();

        // 优先使用Elasticsearch，降级到数据库搜索
        _primarySearchEngine = engines.FirstOrDefault(e => e.GetType().Name.Contains("Elasticsearch"))
                               ?? engines.First();
        _fallbackSearchEngine = engines.FirstOrDefault(e => e.GetType().Name.Contains("Database"))
                                ?? engines.Last();
    }

    /// <summary>
    /// 索引单个实体
    /// </summary>
    public async Task<bool> IndexEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (entityType.ToLowerInvariant())
            {
                case "post":
                    var post = await _postRepository.GetByIdAsync(entityId, cancellationToken);
                    if (post != null)
                    {
                        return await IndexPostAsync(post, cancellationToken);
                    }
                    break;

                // 可以扩展支持其他实体类型
                default:
                    _logger.LogWarning("Unsupported entity type for indexing: {EntityType}", entityType);
                    return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while indexing entity {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 索引文章
    /// </summary>
    public async Task<bool> IndexPostAsync(Post post, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchIndex = await CreatePostSearchIndexAsync(post, cancellationToken);

            // 尝试使用主搜索引擎
            var primaryResult = await _primarySearchEngine.IndexDocumentAsync(searchIndex, cancellationToken);

            // 如果主搜索引擎失败，使用降级搜索引擎
            if (!primaryResult)
            {
                _logger.LogWarning("Primary search engine failed for post {PostId}, using fallback", post.Id);
                return await _fallbackSearchEngine.IndexDocumentAsync(searchIndex, cancellationToken);
            }

            // 同时更新到数据库搜索引擎以保持一致性
            await _fallbackSearchEngine.IndexDocumentAsync(searchIndex, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while indexing post {PostId}", post.Id);
            return false;
        }
    }

    /// <summary>
    /// 批量索引文章
    /// </summary>
    public async Task<int> BulkIndexPostsAsync(IEnumerable<Post> posts, CancellationToken cancellationToken = default)
    {
        var postList = posts.ToList();
        if (!postList.Any())
        {
            return 0;
        }

        try
        {
            var searchIndexes = new List<SearchIndex>();

            foreach (var post in postList)
            {
                var searchIndex = await CreatePostSearchIndexAsync(post, cancellationToken);
                searchIndexes.Add(searchIndex);
            }

            // 使用主搜索引擎批量索引
            var primaryResult = await _primarySearchEngine.BulkIndexAsync(searchIndexes, cancellationToken);

            // 同时更新到数据库搜索引擎
            await _fallbackSearchEngine.BulkIndexAsync(searchIndexes, cancellationToken);

            _logger.LogInformation("Bulk indexed {SuccessCount}/{TotalCount} posts", primaryResult, postList.Count);
            return primaryResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during bulk indexing posts");
            return 0;
        }
    }

    /// <summary>
    /// 删除实体索引
    /// </summary>
    public async Task<bool> RemoveEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryResult = await _primarySearchEngine.DeleteDocumentAsync(entityType, entityId, cancellationToken);
            var fallbackResult = await _fallbackSearchEngine.DeleteDocumentAsync(entityType, entityId, cancellationToken);

            return primaryResult || fallbackResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing entity {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新实体索引
    /// </summary>
    public async Task<bool> UpdateEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        // 更新操作就是重新索引
        return await IndexEntityAsync(entityType, entityId, cancellationToken);
    }

    /// <summary>
    /// 重建所有索引
    /// </summary>
    public async Task<IndexRebuildResult> RebuildAllIndexesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new IndexRebuildResult();

        try
        {
            _logger.LogInformation("Starting index rebuild process");

            // 重建搜索引擎索引
            await _primarySearchEngine.RebuildIndexAsync(cancellationToken);
            await _fallbackSearchEngine.RebuildIndexAsync(cancellationToken);

            // 重建文章索引
            var posts = await _postRepository.GetAllPublishedAsync(cancellationToken);
            result.TotalCount = posts.Count();
            result.EntityTypeCounts["post"] = posts.Count();

            if (posts.Any())
            {
                result.SuccessCount = await BulkIndexPostsAsync(posts, cancellationToken);
                result.FailureCount = result.TotalCount - result.SuccessCount;
            }

            // 重建其他实体类型的索引

            // 重建分类索引
            var categories = await _context.Set<Category>().ToListAsync(cancellationToken);
            foreach (var category in categories)
            {
                var categoryIndex = SearchIndex.Create(
                    "category",
                    category.Id,
                    category.Name,
                    category.Description ?? string.Empty,
                    $"{category.Name}, category",
                    "zh-CN");
                await _primarySearchEngine.IndexDocumentAsync(categoryIndex, cancellationToken);
                await _fallbackSearchEngine.IndexDocumentAsync(categoryIndex, cancellationToken);
                result.SuccessCount++;
            }
            result.EntityTypeCounts["category"] = categories.Count;

            // 重建标签索引
            var tags = await _context.Set<Tag>().ToListAsync(cancellationToken);
            foreach (var tag in tags)
            {
                var tagIndex = SearchIndex.Create(
                    "tag",
                    tag.Id,
                    tag.Name,
                    tag.Description ?? string.Empty,
                    $"{tag.Name}, tag",
                    "zh-CN");
                await _primarySearchEngine.IndexDocumentAsync(tagIndex, cancellationToken);
                await _fallbackSearchEngine.IndexDocumentAsync(tagIndex, cancellationToken);
                result.SuccessCount++;
            }
            result.EntityTypeCounts["tag"] = tags.Count;

            // 重建用户索引（只索引公开的用户信息）
            var users = await _context.Set<User>()
                .Where(u => !string.IsNullOrEmpty(u.Bio) || !string.IsNullOrEmpty(u.DisplayName))
                .ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                var userIndex = SearchIndex.Create(
                    "user",
                    user.Id,
                    user.DisplayName ?? user.UserName,
                    user.Bio ?? string.Empty,
                    $"{user.UserName}, {user.DisplayName}, author",
                    "zh-CN");
                await _primarySearchEngine.IndexDocumentAsync(userIndex, cancellationToken);
                await _fallbackSearchEngine.IndexDocumentAsync(userIndex, cancellationToken);
                result.SuccessCount++;
            }
            result.EntityTypeCounts["user"] = users.Count;

            // 更新总数
            result.TotalCount = result.EntityTypeCounts.Values.Sum();

            result.Success = result.FailureCount == 0;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Index rebuild completed: {SuccessCount}/{TotalCount} in {ElapsedMs}ms",
                result.SuccessCount, result.TotalCount, result.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during index rebuild");

            stopwatch.Stop();
            result.Success = false;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.Errors.Add(ex.Message);

            return result;
        }
    }

    /// <summary>
    /// 清理无效索引
    /// </summary>
    public async Task<int> CleanupInvalidIndexesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanedCount = 0;

            // 获取所有搜索索引
            var searchIndexes = await _context.Set<SearchIndex>()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var searchIndex in searchIndexes)
            {
                var entityExists = await CheckEntityExistsAsync(searchIndex.EntityType, searchIndex.EntityId, cancellationToken);

                if (!entityExists)
                {
                    // 实体不存在，删除索引
                    await RemoveEntityAsync(searchIndex.EntityType, searchIndex.EntityId, cancellationToken);
                    cleanedCount++;
                }
            }

            _logger.LogInformation("Cleaned up {CleanedCount} invalid indexes", cleanedCount);
            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during index cleanup");
            return 0;
        }
    }

    /// <summary>
    /// 获取索引状态
    /// </summary>
    public async Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = new IndexStatus
            {
                LastUpdatedAt = DateTime.UtcNow
            };

            // 检查搜索引擎健康状态
            status.IsSearchEngineHealthy = await _primarySearchEngine.IsHealthyAsync(cancellationToken);

            // 获取数据库索引数量
            status.DatabaseIndexCount = await _context.Set<SearchIndex>()
                .Where(x => x.IsActive)
                .LongCountAsync(cancellationToken);

            // 获取搜索引擎索引统计
            var searchEngineStats = await _primarySearchEngine.GetIndexStatsAsync(cancellationToken);
            status.SearchEngineIndexCount = searchEngineStats.DocumentCount;
            status.IndexSizeInBytes = searchEngineStats.SizeInBytes;

            // 按实体类型统计
            var entityTypes = await _context.Set<SearchIndex>()
                .Where(x => x.IsActive)
                .GroupBy(x => x.EntityType)
                .Select(g => new { EntityType = g.Key, Count = g.LongCount() })
                .ToListAsync(cancellationToken);

            foreach (var entityType in entityTypes)
            {
                status.EntityStats[entityType.EntityType] = new EntityIndexStats
                {
                    DatabaseCount = entityType.Count,
                    SearchEngineCount = entityType.Count // 简化处理，假设一致
                };
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting index status");
            return new IndexStatus();
        }
    }

    /// <summary>
    /// 优化索引
    /// </summary>
    public async Task<bool> OptimizeIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting index optimization");

            // 清理无效索引
            await CleanupInvalidIndexesAsync(cancellationToken);

            // 优化搜索引擎索引（如果支持）
            // 注意：这里可以添加特定搜索引擎的优化逻辑

            _logger.LogInformation("Index optimization completed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during index optimization");
            return false;
        }
    }

    /// <summary>
    /// 同步索引
    /// </summary>
    public async Task<IndexSyncResult> SyncIndexesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new IndexSyncResult();

        try
        {
            _logger.LogInformation("Starting index synchronization");

            // 获取数据库中的所有活跃文章
            var posts = await _postRepository.GetAllPublishedAsync(cancellationToken);

            foreach (var post in posts)
            {
                var searchIndex = await CreatePostSearchIndexAsync(post, cancellationToken);

                // 检查搜索引擎中是否存在
                var existsInSearchEngine = await CheckDocumentExistsInSearchEngineAsync(searchIndex, cancellationToken);

                if (!existsInSearchEngine)
                {
                    // 添加到搜索引擎
                    var added = await _primarySearchEngine.IndexDocumentAsync(searchIndex, cancellationToken);
                    if (added)
                    {
                        result.AddedToSearchEngine++;
                    }
                }
                else
                {
                    // 更新搜索引擎中的文档
                    var updated = await _primarySearchEngine.UpdateDocumentAsync(searchIndex, cancellationToken);
                    if (updated)
                    {
                        result.UpdatedInSearchEngine++;
                    }
                }
            }

            result.Success = true;
            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Index synchronization completed: Added {Added}, Updated {Updated}, Removed {Removed} in {ElapsedMs}ms",
                result.AddedToSearchEngine, result.UpdatedInSearchEngine, result.RemovedFromSearchEngine, result.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during index synchronization");

            stopwatch.Stop();
            result.Success = false;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.Errors.Add(ex.Message);

            return result;
        }
    }

    #region 私有方法

    /// <summary>
    /// 为文章创建搜索索引
    /// </summary>
    private async Task<SearchIndex> CreatePostSearchIndexAsync(Post post, CancellationToken cancellationToken)
    {
        // 构建关键词（包括标签和分类）
        var keywords = new List<string>();

        // 添加标签
        if (post.PostTags?.Any() == true)
        {
            var tags = await _context.Set<Tag>()
                .Where(t => post.PostTags.Select(pt => pt.TagId).Contains(t.Id))
                .Select(t => t.Name)
                .ToListAsync(cancellationToken);
            keywords.AddRange(tags);
        }

        // 添加分类
        if (post.CategoryId.HasValue)
        {
            var category = await _context.Set<Category>()
                .Where(c => c.Id == post.CategoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrEmpty(category))
            {
                keywords.Add(category);
            }
        }

        return SearchIndex.Create(
            entityType: "post",
            entityId: post.Id,
            title: post.Title,
            content: post.Content,
            keywords: string.Join(", ", keywords),
            language: "zh-CN"
        );
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    private async Task<bool> CheckEntityExistsAsync(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        return entityType.ToLowerInvariant() switch
        {
            "post" => await _context.Set<Post>().AnyAsync(x => x.Id == entityId, cancellationToken),
            // 添加其他实体类型的检查
            _ => false
        };
    }

    /// <summary>
    /// 检查文档是否在搜索引擎中存在
    /// </summary>
    private async Task<bool> CheckDocumentExistsInSearchEngineAsync(SearchIndex searchIndex, CancellationToken cancellationToken)
    {
        try
        {
            // 这里简化处理，实际应该检查搜索引擎中是否存在该文档
            // 可以通过搜索引擎的API或者维护一个映射表来实现
            var criteria = SearchCriteria.Create($"id:{searchIndex.Id}", 1, 1);
            var result = await _primarySearchEngine.SearchAsync(criteria, cancellationToken);
            return result.Items.Any(x => x.EntityId == searchIndex.EntityId && x.EntityType == searchIndex.EntityType);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}