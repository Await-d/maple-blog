using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// 搜索索引管理器实现
/// </summary>
public class SearchIndexManager : ISearchIndexManager
{
    private readonly BlogDbContext _dbContext;
    private readonly ILogger<SearchIndexManager> _logger;

    // 在内存中维护的索引数据结构
    private readonly ConcurrentDictionary<Guid, SearchIndexEntry> _indexEntries;
    private readonly ConcurrentDictionary<string, ConcurrentBag<Guid>> _entityTypeIndex;
    private DateTime _lastIndexUpdate;
    private readonly object _indexLock = new object();

    public SearchIndexManager(
        BlogDbContext dbContext,
        ILogger<SearchIndexManager> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _indexEntries = new ConcurrentDictionary<Guid, SearchIndexEntry>();
        _entityTypeIndex = new ConcurrentDictionary<string, ConcurrentBag<Guid>>();
        _lastIndexUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// 索引单个实体
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> IndexEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            _logger.LogWarning("Entity type cannot be null or empty");
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 根据实体类型处理不同的实体
            switch (entityType.ToLowerInvariant())
            {
                case "post":
                    var post = await _dbContext.Posts
                        .Include(p => p.Category)
                        .Include(p => p.PostTags)
                            .ThenInclude(pt => pt.Tag)
                        .FirstOrDefaultAsync(p => p.Id == entityId, cancellationToken);

                    if (post != null)
                    {
                        return await IndexPostAsync(post, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported entity type: {EntityType}", entityType);
                    return false;
            }

            _logger.LogWarning("Entity not found: {EntityType} {EntityId}", entityType, entityId);
            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Index entity operation was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing entity: {EntityType} {EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 索引文章
    /// </summary>
    /// <param name="post">文章实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> IndexPostAsync(Post post, CancellationToken cancellationToken = default)
    {
        if (post == null)
        {
            _logger.LogWarning("Post cannot be null");
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 创建索引条目
            var indexEntry = new SearchIndexEntry
            {
                EntityId = post.Id,
                EntityType = "post",
                Title = post.Title ?? string.Empty,
                Content = ExtractTextContent(post.Content ?? string.Empty),
                Summary = post.Summary ?? string.Empty,
                Tags = post.PostTags?.Select(pt => pt.Tag?.Name ?? string.Empty).ToList() ?? new List<string>(),
                Category = post.Category?.Name ?? string.Empty,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt ?? post.CreatedAt,
                IsPublished = post.IsPublished,
                IndexedAt = DateTime.UtcNow
            };

            // 更新索引
            lock (_indexLock)
            {
                _indexEntries.AddOrUpdate(post.Id, indexEntry, (key, oldValue) => indexEntry);
                _entityTypeIndex.AddOrUpdate("post",
                    new ConcurrentBag<Guid> { post.Id },
                    (key, oldBag) =>
                    {
                        if (!oldBag.Contains(post.Id))
                        {
                            oldBag.Add(post.Id);
                        }
                        return oldBag;
                    });
                _lastIndexUpdate = DateTime.UtcNow;
            }

            _logger.LogDebug("Successfully indexed post: {PostId} - {Title}", post.Id, post.Title);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Index post operation was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing post: {PostId}", post.Id);
            return false;
        }
    }

    /// <summary>
    /// 批量索引文章
    /// </summary>
    /// <param name="posts">文章列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功索引的数量</returns>
    public async Task<int> BulkIndexPostsAsync(IEnumerable<Post> posts, CancellationToken cancellationToken = default)
    {
        if (posts == null)
        {
            _logger.LogWarning("Posts collection cannot be null");
            return 0;
        }

        var postList = posts.ToList();
        if (!postList.Any())
        {
            _logger.LogDebug("No posts to index");
            return 0;
        }

        int successCount = 0;
        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting bulk indexing of {Count} posts", postList.Count);

            var tasks = postList.Select(async post =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await IndexPostAsync(post, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            successCount = results.Count(r => r);

            sw.Stop();
            _logger.LogInformation("Bulk indexing completed: {SuccessCount}/{TotalCount} posts indexed in {ElapsedMs}ms",
                successCount, postList.Count, sw.ElapsedMilliseconds);

            return successCount;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Bulk index operation was cancelled after {ElapsedMs}ms. {SuccessCount} posts were indexed.",
                sw.ElapsedMilliseconds, successCount);
            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing. {SuccessCount} posts were successfully indexed.", successCount);
            return successCount;
        }
    }

    /// <summary>
    /// 删除实体索引
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> RemoveEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            _logger.LogWarning("Entity type cannot be null or empty");
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_indexLock)
            {
                bool removed = _indexEntries.TryRemove(entityId, out _);

                // 从类型索引中移除
                if (_entityTypeIndex.TryGetValue(entityType.ToLowerInvariant(), out var entityBag))
                {
                    // 由于ConcurrentBag不支持移除，我们重新创建一个不包含该ID的集合
                    var newBag = new ConcurrentBag<Guid>();
                    foreach (var id in entityBag.Where(id => id != entityId))
                    {
                        newBag.Add(id);
                    }
                    _entityTypeIndex.TryUpdate(entityType.ToLowerInvariant(), newBag, entityBag);
                }

                _lastIndexUpdate = DateTime.UtcNow;
            }

            _logger.LogDebug("Successfully removed entity from index: {EntityType} {EntityId}", entityType, entityId);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Remove entity operation was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing entity from index: {EntityType} {EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新实体索引
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        // 更新实际上就是先删除再索引
        return await IndexEntityAsync(entityType, entityId, cancellationToken);
    }

    /// <summary>
    /// 重建所有索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重建结果</returns>
    public async Task<IndexRebuildResult> RebuildAllIndexesAsync(CancellationToken cancellationToken = default)
    {
        var result = new IndexRebuildResult
        {
            Success = false,
            TotalCount = 0,
            SuccessCount = 0,
            FailureCount = 0,
            Errors = new List<string>(),
            EntityTypeCounts = new Dictionary<string, int>()
        };

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting full index rebuild");

            // 清空现有索引
            lock (_indexLock)
            {
                _indexEntries.Clear();
                _entityTypeIndex.Clear();
            }

            // 索引所有文章
            var posts = await _dbContext.Posts
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsPublished)
                .ToListAsync(cancellationToken);

            result.TotalCount = posts.Count;
            result.EntityTypeCounts["post"] = posts.Count;

            if (posts.Any())
            {
                int postSuccessCount = await BulkIndexPostsAsync(posts, cancellationToken);
                result.SuccessCount += postSuccessCount;
                result.FailureCount += posts.Count - postSuccessCount;
            }

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Success = result.FailureCount == 0;

            _logger.LogInformation("Index rebuild completed: {SuccessCount}/{TotalCount} entities indexed in {ElapsedMs}ms",
                result.SuccessCount, result.TotalCount, result.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Errors.Add("Operation was cancelled");
            _logger.LogInformation("Index rebuild was cancelled after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during index rebuild");
            return result;
        }
    }

    /// <summary>
    /// 清理无效索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的索引数量</returns>
    public async Task<int> CleanupInvalidIndexesAsync(CancellationToken cancellationToken = default)
    {
        int cleanedCount = 0;

        try
        {
            _logger.LogInformation("Starting cleanup of invalid indexes");

            var indexEntriesToCheck = _indexEntries.Values.ToList();
            var invalidEntries = new List<Guid>();

            foreach (var entry in indexEntriesToCheck)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 检查文章是否还存在
                if (entry.EntityType.Equals("post", StringComparison.OrdinalIgnoreCase))
                {
                    bool postExists = await _dbContext.Posts
                        .AnyAsync(p => p.Id == entry.EntityId, cancellationToken);

                    if (!postExists)
                    {
                        invalidEntries.Add(entry.EntityId);
                    }
                }
            }

            // 移除无效条目
            lock (_indexLock)
            {
                foreach (var invalidId in invalidEntries)
                {
                    if (_indexEntries.TryRemove(invalidId, out var removedEntry))
                    {
                        cleanedCount++;

                        // 从类型索引中移除
                        if (_entityTypeIndex.TryGetValue(removedEntry.EntityType, out var entityBag))
                        {
                            var newBag = new ConcurrentBag<Guid>();
                            foreach (var id in entityBag.Where(id => id != invalidId))
                            {
                                newBag.Add(id);
                            }
                            _entityTypeIndex.TryUpdate(removedEntry.EntityType, newBag, entityBag);
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    _lastIndexUpdate = DateTime.UtcNow;
                }
            }

            _logger.LogInformation("Cleanup completed: {CleanedCount} invalid indexes removed", cleanedCount);
            return cleanedCount;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cleanup operation was cancelled. {CleanedCount} indexes were cleaned.", cleanedCount);
            return cleanedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during index cleanup. {CleanedCount} indexes were cleaned.", cleanedCount);
            return cleanedCount;
        }
    }

    /// <summary>
    /// 获取索引状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>索引状态</returns>
    public async Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = new IndexStatus
            {
                IsSearchEngineHealthy = true, // 简单的健康检查
                LastUpdatedAt = _lastIndexUpdate,
                EntityStats = new Dictionary<string, EntityIndexStats>()
            };

            // 获取数据库中的数据统计
            var dbPostCount = await _dbContext.Posts
                .Where(p => p.IsPublished)
                .CountAsync(cancellationToken);

            // 获取索引中的数据统计
            var indexedPostCount = _entityTypeIndex.TryGetValue("post", out var postBag) ? postBag.Count : 0;

            status.DatabaseIndexCount = dbPostCount;
            status.SearchEngineIndexCount = _indexEntries.Count;

            status.EntityStats["post"] = new EntityIndexStats
            {
                DatabaseCount = dbPostCount,
                SearchEngineCount = indexedPostCount
            };

            // 计算索引大小（简单估算）
            status.IndexSizeInBytes = _indexEntries.Values
                .Sum(entry => (entry.Title?.Length ?? 0) +
                             (entry.Content?.Length ?? 0) +
                             (entry.Summary?.Length ?? 0)) * sizeof(char);

            return status;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Get index status operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index status");
            return new IndexStatus
            {
                IsSearchEngineHealthy = false,
                LastUpdatedAt = _lastIndexUpdate,
                EntityStats = new Dictionary<string, EntityIndexStats>()
            };
        }
    }

    /// <summary>
    /// 优化索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    public async Task<bool> OptimizeIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting index optimization");
            var sw = Stopwatch.StartNew();

            cancellationToken.ThrowIfCancellationRequested();

            // 在实际实现中，这里可能会有复杂的优化逻辑
            // 目前简单地清理无效索引
            int cleanedCount = await CleanupInvalidIndexesAsync(cancellationToken);

            lock (_indexLock)
            {
                // 可以添加更多优化逻辑，比如重新组织数据结构、压缩数据等
                _lastIndexUpdate = DateTime.UtcNow;
            }

            sw.Stop();
            _logger.LogInformation("Index optimization completed in {ElapsedMs}ms. Cleaned {CleanedCount} entries.",
                sw.ElapsedMilliseconds, cleanedCount);

            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Index optimization was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during index optimization");
            return false;
        }
    }

    /// <summary>
    /// 同步索引（确保数据库和搜索引擎一致）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果</returns>
    public async Task<IndexSyncResult> SyncIndexesAsync(CancellationToken cancellationToken = default)
    {
        var result = new IndexSyncResult
        {
            Success = false,
            AddedToSearchEngine = 0,
            RemovedFromSearchEngine = 0,
            UpdatedInSearchEngine = 0,
            Errors = new List<string>()
        };

        var sw = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting index synchronization");

            cancellationToken.ThrowIfCancellationRequested();

            // 获取数据库中的所有已发布文章
            var dbPosts = await _dbContext.Posts
                .Include(p => p.Category)
                .Include(p => p.PostTags)
                    .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsPublished)
                .Select(p => new { p.Id, p.UpdatedAt })
                .ToListAsync(cancellationToken);

            var dbPostIds = dbPosts.Select(p => p.Id).ToHashSet();
            var indexedPostIds = _entityTypeIndex.TryGetValue("post", out var postBag)
                ? postBag.ToHashSet()
                : new HashSet<Guid>();

            // 找到需要添加到索引的文章
            var postsToAdd = dbPostIds.Except(indexedPostIds).ToList();
            foreach (var postId in postsToAdd)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool indexed = await IndexEntityAsync("post", postId, cancellationToken);
                if (indexed)
                {
                    result.AddedToSearchEngine++;
                }
                else
                {
                    result.Errors.Add($"Failed to add post {postId} to search engine");
                }
            }

            // 找到需要从索引中移除的文章
            var postsToRemove = indexedPostIds.Except(dbPostIds).ToList();
            foreach (var postId in postsToRemove)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool removed = await RemoveEntityAsync("post", postId, cancellationToken);
                if (removed)
                {
                    result.RemovedFromSearchEngine++;
                }
                else
                {
                    result.Errors.Add($"Failed to remove post {postId} from search engine");
                }
            }

            // 检查需要更新的文章（数据库中的更新时间比索引中的新）
            var commonPostIds = dbPostIds.Intersect(indexedPostIds);
            foreach (var postId in commonPostIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dbPost = dbPosts.First(p => p.Id == postId);
                if (_indexEntries.TryGetValue(postId, out var indexEntry))
                {
                    // 如果数据库中的更新时间比索引中的更新时间新，则需要更新
                    if (dbPost.UpdatedAt > indexEntry.IndexedAt)
                    {
                        bool updated = await UpdateEntityAsync("post", postId, cancellationToken);
                        if (updated)
                        {
                            result.UpdatedInSearchEngine++;
                        }
                        else
                        {
                            result.Errors.Add($"Failed to update post {postId} in search engine");
                        }
                    }
                }
            }

            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation("Index synchronization completed in {ElapsedMs}ms: +{Added} -{Removed} ~{Updated} posts",
                result.ElapsedMilliseconds, result.AddedToSearchEngine, result.RemovedFromSearchEngine, result.UpdatedInSearchEngine);

            return result;
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Errors.Add("Operation was cancelled");
            _logger.LogInformation("Index synchronization was cancelled after {ElapsedMs}ms", sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during index synchronization");
            return result;
        }
    }

    /// <summary>
    /// 提取文本内容（移除HTML标签）
    /// </summary>
    /// <param name="htmlContent">HTML内容</param>
    /// <returns>纯文本内容</returns>
    private static string ExtractTextContent(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        // 简单的HTML标签移除（在实际应用中可能需要使用更强大的HTML解析器）
        var textContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]*>", string.Empty);

        // 清理多余的空白字符
        textContent = System.Text.RegularExpressions.Regex.Replace(textContent, @"\s+", " ");

        return textContent.Trim();
    }
}

/// <summary>
/// 搜索索引条目
/// </summary>
internal class SearchIndexEntry
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsPublished { get; set; }
    public DateTime IndexedAt { get; set; }
}