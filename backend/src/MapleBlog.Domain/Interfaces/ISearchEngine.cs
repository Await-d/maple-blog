using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 搜索引擎接口
/// </summary>
public interface ISearchEngine
{
    /// <summary>
    /// 搜索内容
    /// </summary>
    /// <param name="criteria">搜索条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    Task<SearchResult> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// 索引文档
    /// </summary>
    /// <param name="searchIndex">搜索索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> IndexDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量索引文档
    /// </summary>
    /// <param name="searchIndexes">搜索索引列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功索引的数量</returns>
    Task<int> BulkIndexAsync(IEnumerable<SearchIndex> searchIndexes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文档
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteDocumentAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新文档
    /// </summary>
    /// <param name="searchIndex">搜索索引</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="size">建议数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索建议列表</returns>
    Task<IEnumerable<string>> GetSuggestionsAsync(string query, int size = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查连接状态
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否连接正常</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 重建索引
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RebuildIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>索引统计信息</returns>
    Task<IndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 搜索结果
/// </summary>
public class SearchResult
{
    /// <summary>
    /// 搜索结果项
    /// </summary>
    public List<SearchResultItem> Items { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public int ExecutionTime { get; set; }

    /// <summary>
    /// 搜索建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 聚合结果
    /// </summary>
    public Dictionary<string, object> Aggregations { get; set; } = new();

    /// <summary>
    /// 是否有更多结果
    /// </summary>
    public bool HasMore => Items.Count < TotalCount;
}

/// <summary>
/// 搜索结果项
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// 实体ID
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 内容摘要
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 高亮内容
    /// </summary>
    public Dictionary<string, List<string>>? Highlights { get; set; }

    /// <summary>
    /// 相关性得分
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// 匹配字段
    /// </summary>
    public List<string> MatchedFields { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 额外数据
    /// </summary>
    public Dictionary<string, object> ExtraData { get; set; } = new();
}

/// <summary>
/// 索引统计信息
/// </summary>
public class IndexStats
{
    /// <summary>
    /// 文档总数
    /// </summary>
    public long DocumentCount { get; set; }

    /// <summary>
    /// 索引大小（字节）
    /// </summary>
    public long SizeInBytes { get; set; }

    /// <summary>
    /// 分片数量
    /// </summary>
    public int ShardCount { get; set; }

    /// <summary>
    /// 副本数量
    /// </summary>
    public int ReplicaCount { get; set; }

    /// <summary>
    /// 索引健康状态
    /// </summary>
    public string HealthStatus { get; set; } = "unknown";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}