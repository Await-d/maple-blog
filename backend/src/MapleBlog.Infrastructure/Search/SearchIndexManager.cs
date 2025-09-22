using MapleBlog.Domain.Constants;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Events;
using MapleBlog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MapleBlog.Infrastructure.Search;

/// <summary>
/// 搜索索引管理服务
/// 负责管理搜索索引的生命周期，包括创建、更新、删除和重建
/// </summary>
public class SearchIndexManager : ISearchIndexManager
{
    private readonly ILogger<SearchIndexManager> _logger;
    private readonly ISearchEngine _primarySearchEngine;
    private readonly ISearchEngine _fallbackSearchEngine;
    private readonly DbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _rebuildSemaphore;
    private readonly ConcurrentQueue<IndexOperation> _indexQueue;
    private readonly Timer _healthCheckTimer;
    private readonly Timer _syncTimer;

    // 健康状态跟踪
    private volatile bool _primaryEngineHealthy = true;
    private volatile bool _isRebuilding = false;
    private DateTime _lastSyncTime = DateTime.UtcNow;

    public SearchIndexManager(
        ILogger<SearchIndexManager> logger,
        IEnumerable<ISearchEngine> searchEngines,
        DbContext context,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _rebuildSemaphore = new SemaphoreSlim(1, 1);
        _indexQueue = new ConcurrentQueue<IndexOperation>();

        // 获取主搜索引擎和备用搜索引擎
        var engines = searchEngines.ToList();
        _primarySearchEngine = engines.FirstOrDefault(e => e.GetType().Name.Contains("Elasticsearch")) ?? engines.First();
        _fallbackSearchEngine = engines.FirstOrDefault(e => e.GetType().Name.Contains("Database")) ?? engines.Last();

        // 启动健康检查定时器（每分钟检查一次）
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        // 启动增量同步定时器（每5分钟同步一次）
        _syncTimer = new Timer(PerformIncrementalSync, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 索引单个文档
    /// </summary>
    public async Task<bool> IndexDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Indexing document {EntityType}:{EntityId}", searchIndex.EntityType, searchIndex.EntityId);

            var tasks = new List<Task<bool>>();

            // 主搜索引擎索引
            if (_primaryEngineHealthy)
            {
                tasks.Add(_primarySearchEngine.IndexDocumentAsync(searchIndex, cancellationToken));
            }

            // 备用搜索引擎总是索引
            tasks.Add(_fallbackSearchEngine.IndexDocumentAsync(searchIndex, cancellationToken));

            var results = await Task.WhenAll(tasks);
            var success = results.Any(r => r);

            if (success)
            {
                // 更新数据库中的索引记录
                await UpdateSearchIndexRecord(searchIndex, IndexStatus.Indexed, cancellationToken);
                _logger.LogDebug("Successfully indexed document {EntityType}:{EntityId}", searchIndex.EntityType, searchIndex.EntityId);
            }
            else
            {
                await UpdateSearchIndexRecord(searchIndex, IndexStatus.Failed, cancellationToken);
                _logger.LogWarning("Failed to index document {EntityType}:{EntityId}", searchIndex.EntityType, searchIndex.EntityId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {EntityType}:{EntityId}", searchIndex.EntityType, searchIndex.EntityId);
            await UpdateSearchIndexRecord(searchIndex, IndexStatus.Failed, cancellationToken);
            return false;
        }
    }

    /// <summary>
    /// 批量索引文档
    /// </summary>
    public async Task<int> BulkIndexAsync(IEnumerable<SearchIndex> searchIndexes, CancellationToken cancellationToken = default)
    {
        var indexList = searchIndexes.ToList();
        if (!indexList.Any())
        {
            return 0;
        }

        try
        {
            _logger.LogInformation("Starting bulk index operation for {Count} documents", indexList.Count);

            var tasks = new List<Task<int>>();

            // 主搜索引擎批量索引
            if (_primaryEngineHealthy)
            {
                tasks.Add(_primarySearchEngine.BulkIndexAsync(indexList, cancellationToken));
            }

            // 备用搜索引擎批量索引
            tasks.Add(_fallbackSearchEngine.BulkIndexAsync(indexList, cancellationToken));

            var results = await Task.WhenAll(tasks);
            var successCount = results.Max();

            // 更新索引状态
            await UpdateBulkIndexStatus(indexList, successCount, cancellationToken);

            _logger.LogInformation("Bulk index completed: {SuccessCount}/{TotalCount} documents", successCount, indexList.Count);
            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk index operation");
            return 0;
        }
    }

    /// <summary>
    /// 删除文档索引
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting document index {EntityType}:{EntityId}", entityType, entityId);

            var tasks = new List<Task<bool>>();

            // 从主搜索引擎删除
            if (_primaryEngineHealthy)
            {
                tasks.Add(_primarySearchEngine.DeleteDocumentAsync(entityType, entityId, cancellationToken));
            }

            // 从备用搜索引擎删除
            tasks.Add(_fallbackSearchEngine.DeleteDocumentAsync(entityType, entityId, cancellationToken));

            var results = await Task.WhenAll(tasks);
            var success = results.Any(r => r);

            if (success)
            {
                // 从数据库中删除索引记录
                await RemoveSearchIndexRecord(entityType, entityId, cancellationToken);
                _logger.LogDebug("Successfully deleted document index {EntityType}:{EntityId}", entityType, entityId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document index {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新文档索引
    /// </summary>
    public async Task<bool> UpdateDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default)
    {
        // 更新操作等同于重新索引
        return await IndexDocumentAsync(searchIndex, cancellationToken);
    }

    /// <summary>
    /// 重建所有索引
    /// </summary>
    public async Task<bool> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        if (!await _rebuildSemaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("Index rebuild is already in progress");
            return false;
        }

        try
        {
            _isRebuilding = true;
            _logger.LogInformation("Starting index rebuild process");

            // 清空现有索引
            var clearTasks = new List<Task<bool>>();

            if (_primaryEngineHealthy)
            {
                clearTasks.Add(_primarySearchEngine.RebuildIndexAsync(cancellationToken));
            }

            clearTasks.Add(_fallbackSearchEngine.RebuildIndexAsync(cancellationToken));

            await Task.WhenAll(clearTasks);

            // 从数据库重新构建索引
            await RebuildFromDatabase(cancellationToken);

            _logger.LogInformation("Index rebuild completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during index rebuild");
            return false;
        }
        finally
        {
            _isRebuilding = false;
            _rebuildSemaphore.Release();
        }
    }

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    public async Task<IndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryStats = _primaryEngineHealthy
                ? await _primarySearchEngine.GetIndexStatsAsync(cancellationToken)
                : new IndexStats();

            var fallbackStats = await _fallbackSearchEngine.GetIndexStatsAsync(cancellationToken);

            // 合并统计信息
            return new IndexStats
            {
                DocumentCount = Math.Max(primaryStats.DocumentCount, fallbackStats.DocumentCount),
                SizeInBytes = primaryStats.SizeInBytes + fallbackStats.SizeInBytes,
                ShardCount = primaryStats.ShardCount,
                ReplicaCount = primaryStats.ReplicaCount,
                HealthStatus = _primaryEngineHealthy ? primaryStats.HealthStatus : "degraded",
                LastUpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index stats");
            return new IndexStats { HealthStatus = "unknown" };
        }
    }

    /// <summary>
    /// 增量同步索引
    /// </summary>
    public async Task<bool> IncrementalSyncAsync(DateTime? since = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var sincePinch = since ?? _lastSyncTime;
            _logger.LogDebug("Starting incremental sync since {SinceTime}", sincePinch);

            // 查找需要同步的文档
            var needSyncIndexes = await _context.Set<SearchIndex>()
                .Where(x => x.LastUpdatedAt > sincePinch || x.IndexedAt > sincePinch)
                .ToListAsync(cancellationToken);

            if (needSyncIndexes.Any())
            {
                var syncCount = await BulkIndexAsync(needSyncIndexes, cancellationToken);
                _logger.LogInformation("Incremental sync completed: {SyncCount}/{TotalCount} documents", syncCount, needSyncIndexes.Count);
            }

            _lastSyncTime = DateTime.UtcNow;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during incremental sync");
            return false;
        }
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryHealthy = await _primarySearchEngine.IsHealthyAsync(cancellationToken);
            var fallbackHealthy = await _fallbackSearchEngine.IsHealthyAsync(cancellationToken);

            _primaryEngineHealthy = primaryHealthy;

            _logger.LogDebug("Health check - Primary: {PrimaryHealth}, Fallback: {FallbackHealth}",
                primaryHealthy, fallbackHealthy);

            return primaryHealthy || fallbackHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return false;
        }
    }

    /// <summary>
    /// 队列索引操作
    /// </summary>
    public void QueueIndexOperation(IndexOperation operation)
    {
        _indexQueue.Enqueue(operation);
        _logger.LogDebug("Queued index operation: {OperationType} for {EntityType}:{EntityId}",
            operation.Type, operation.EntityType, operation.EntityId);
    }

    /// <summary>
    /// 处理索引队列
    /// </summary>
    public async Task ProcessIndexQueueAsync(CancellationToken cancellationToken = default)
    {
        var operations = new List<IndexOperation>();

        // 批量出队操作
        while (_indexQueue.TryDequeue(out var operation) && operations.Count < 100)
        {
            operations.Add(operation);
        }

        if (!operations.Any())
        {
            return;
        }

        try
        {
            _logger.LogDebug("Processing {Count} queued index operations", operations.Count);

            // 按操作类型分组处理
            var grouped = operations.GroupBy(op => op.Type);

            foreach (var group in grouped)
            {
                switch (group.Key)
                {
                    case IndexOperationType.Index:
                    case IndexOperationType.Update:
                        await ProcessIndexOperations(group.ToList(), cancellationToken);
                        break;

                    case IndexOperationType.Delete:
                        await ProcessDeleteOperations(group.ToList(), cancellationToken);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing index queue");
        }
    }

    #region 私有方法

    /// <summary>
    /// 执行健康检查（定时器回调）
    /// </summary>
    private async void PerformHealthCheck(object? state)
    {
        try
        {
            await HealthCheckAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled health check");
        }
    }

    /// <summary>
    /// 执行增量同步（定时器回调）
    /// </summary>
    private async void PerformIncrementalSync(object? state)
    {
        if (_isRebuilding)
        {
            return;
        }

        try
        {
            await IncrementalSyncAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled incremental sync");
        }
    }

    /// <summary>
    /// 从数据库重建索引
    /// </summary>
    private async Task RebuildFromDatabase(CancellationToken cancellationToken)
    {
        const int batchSize = 1000;
        var offset = 0;

        while (true)
        {
            // 获取需要重建的实体（从Posts表）
            var posts = await _context.Set<Post>()
                .Where(p => p.IsPublished)
                .OrderBy(p => p.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (!posts.Any())
            {
                break;
            }

            // 转换为搜索索引
            var searchIndexes = posts.Select(post => SearchIndex.Create(
                SearchConstants.EntityTypes.Post,
                post.Id,
                post.Title,
                post.Content,
                post.Tags?.Select(t => t.Name) is var tags ? string.Join(", ", tags) : "",
                SearchConstants.Languages.Chinese
            )).ToList();

            // 批量索引
            await BulkIndexAsync(searchIndexes, cancellationToken);

            offset += batchSize;
            _logger.LogInformation("Rebuilt {Count} documents, total processed: {Total}", posts.Count, offset);

            // 避免长时间运行导致的内存问题
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 更新搜索索引记录
    /// </summary>
    private async Task UpdateSearchIndexRecord(SearchIndex searchIndex, IndexStatus status, CancellationToken cancellationToken)
    {
        try
        {
            var existingIndex = await _context.Set<SearchIndex>()
                .FirstOrDefaultAsync(x => x.EntityType == searchIndex.EntityType && x.EntityId == searchIndex.EntityId, cancellationToken);

            if (existingIndex != null)
            {
                existingIndex.UpdateIndex(searchIndex.Title, searchIndex.Content, searchIndex.Keywords);
            }
            else
            {
                _context.Set<SearchIndex>().Add(searchIndex);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating search index record");
        }
    }

    /// <summary>
    /// 更新批量索引状态
    /// </summary>
    private async Task UpdateBulkIndexStatus(List<SearchIndex> indexes, int successCount, CancellationToken cancellationToken)
    {
        try
        {
            // 标记成功索引的文档
            for (int i = 0; i < Math.Min(successCount, indexes.Count); i++)
            {
                var index = indexes[i];
                var existingIndex = await _context.Set<SearchIndex>()
                    .FirstOrDefaultAsync(x => x.EntityType == index.EntityType && x.EntityId == index.EntityId, cancellationToken);

                if (existingIndex != null)
                {
                    existingIndex.UpdateIndex(index.Title, index.Content, index.Keywords);
                }
                else
                {
                    _context.Set<SearchIndex>().Add(index);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bulk index status");
        }
    }

    /// <summary>
    /// 移除搜索索引记录
    /// </summary>
    private async Task RemoveSearchIndexRecord(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        try
        {
            var indexes = await _context.Set<SearchIndex>()
                .Where(x => x.EntityType == entityType && x.EntityId == entityId)
                .ToListAsync(cancellationToken);

            if (indexes.Any())
            {
                _context.Set<SearchIndex>().RemoveRange(indexes);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing search index record");
        }
    }

    /// <summary>
    /// 处理索引操作
    /// </summary>
    private async Task ProcessIndexOperations(List<IndexOperation> operations, CancellationToken cancellationToken)
    {
        var searchIndexes = new List<SearchIndex>();

        foreach (var operation in operations)
        {
            if (operation.SearchIndex != null)
            {
                searchIndexes.Add(operation.SearchIndex);
            }
        }

        if (searchIndexes.Any())
        {
            await BulkIndexAsync(searchIndexes, cancellationToken);
        }
    }

    /// <summary>
    /// 处理删除操作
    /// </summary>
    private async Task ProcessDeleteOperations(List<IndexOperation> operations, CancellationToken cancellationToken)
    {
        foreach (var operation in operations)
        {
            await DeleteDocumentAsync(operation.EntityType, operation.EntityId, cancellationToken);
        }
    }

    #endregion

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _syncTimer?.Dispose();
        _rebuildSemaphore?.Dispose();
    }
}

/// <summary>
/// 搜索索引管理器接口
/// </summary>
public interface ISearchIndexManager : IDisposable
{
    /// <summary>
    /// 索引单个文档
    /// </summary>
    Task<bool> IndexDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量索引文档
    /// </summary>
    Task<int> BulkIndexAsync(IEnumerable<SearchIndex> searchIndexes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文档索引
    /// </summary>
    Task<bool> DeleteDocumentAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文档索引
    /// </summary>
    Task<bool> UpdateDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建所有索引
    /// </summary>
    Task<bool> RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    Task<IndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增量同步索引
    /// </summary>
    Task<bool> IncrementalSyncAsync(DateTime? since = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行健康检查
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 队列索引操作
    /// </summary>
    void QueueIndexOperation(IndexOperation operation);

    /// <summary>
    /// 处理索引队列
    /// </summary>
    Task ProcessIndexQueueAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 索引操作
/// </summary>
public class IndexOperation
{
    public IndexOperationType Type { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public SearchIndex? SearchIndex { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static IndexOperation CreateIndex(SearchIndex searchIndex)
    {
        return new IndexOperation
        {
            Type = IndexOperationType.Index,
            EntityType = searchIndex.EntityType,
            EntityId = searchIndex.EntityId,
            SearchIndex = searchIndex
        };
    }

    public static IndexOperation UpdateIndex(SearchIndex searchIndex)
    {
        return new IndexOperation
        {
            Type = IndexOperationType.Update,
            EntityType = searchIndex.EntityType,
            EntityId = searchIndex.EntityId,
            SearchIndex = searchIndex
        };
    }

    public static IndexOperation DeleteIndex(string entityType, Guid entityId)
    {
        return new IndexOperation
        {
            Type = IndexOperationType.Delete,
            EntityType = entityType,
            EntityId = entityId
        };
    }
}

/// <summary>
/// 索引操作类型
/// </summary>
public enum IndexOperationType
{
    Index,
    Update,
    Delete
}