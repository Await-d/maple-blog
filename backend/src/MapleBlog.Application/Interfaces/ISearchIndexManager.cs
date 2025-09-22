using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 搜索索引管理器接口
/// </summary>
public interface ISearchIndexManager
{
    /// <summary>
    /// 索引单个实体
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> IndexEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 索引文章
    /// </summary>
    /// <param name="post">文章实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> IndexPostAsync(Post post, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量索引文章
    /// </summary>
    /// <param name="posts">文章列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功索引的数量</returns>
    Task<int> BulkIndexPostsAsync(IEnumerable<Post> posts, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体索引
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RemoveEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体索引
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建所有索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重建结果</returns>
    Task<IndexRebuildResult> RebuildAllIndexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理无效索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清理的索引数量</returns>
    Task<int> CleanupInvalidIndexesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取索引状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>索引状态</returns>
    Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 优化索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> OptimizeIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步索引（确保数据库和搜索引擎一致）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果</returns>
    Task<IndexSyncResult> SyncIndexesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 索引重建结果
/// </summary>
public class IndexRebuildResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 详细信息
    /// </summary>
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
}

/// <summary>
/// 索引状态
/// </summary>
public class IndexStatus
{
    /// <summary>
    /// 搜索引擎是否健康
    /// </summary>
    public bool IsSearchEngineHealthy { get; set; }

    /// <summary>
    /// 数据库索引数量
    /// </summary>
    public long DatabaseIndexCount { get; set; }

    /// <summary>
    /// 搜索引擎索引数量
    /// </summary>
    public long SearchEngineIndexCount { get; set; }

    /// <summary>
    /// 是否同步
    /// </summary>
    public bool IsSynced => DatabaseIndexCount == SearchEngineIndexCount;

    /// <summary>
    /// 按实体类型统计
    /// </summary>
    public Dictionary<string, EntityIndexStats> EntityStats { get; set; } = new();

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// 索引大小（字节）
    /// </summary>
    public long IndexSizeInBytes { get; set; }
}

/// <summary>
/// 实体索引统计
/// </summary>
public class EntityIndexStats
{
    /// <summary>
    /// 数据库数量
    /// </summary>
    public long DatabaseCount { get; set; }

    /// <summary>
    /// 搜索引擎数量
    /// </summary>
    public long SearchEngineCount { get; set; }

    /// <summary>
    /// 是否同步
    /// </summary>
    public bool IsSynced => DatabaseCount == SearchEngineCount;
}

/// <summary>
/// 索引同步结果
/// </summary>
public class IndexSyncResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 添加到搜索引擎的数量
    /// </summary>
    public int AddedToSearchEngine { get; set; }

    /// <summary>
    /// 从搜索引擎删除的数量
    /// </summary>
    public int RemovedFromSearchEngine { get; set; }

    /// <summary>
    /// 在搜索引擎中更新的数量
    /// </summary>
    public int UpdatedInSearchEngine { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 总处理数量
    /// </summary>
    public int TotalProcessed => AddedToSearchEngine + RemovedFromSearchEngine + UpdatedInSearchEngine;
}