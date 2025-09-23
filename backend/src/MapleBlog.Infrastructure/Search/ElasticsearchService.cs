using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Transport;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MapleBlog.Infrastructure.Search;

/// <summary>
/// Elasticsearch搜索引擎服务实现
/// </summary>
public class ElasticsearchService : ISearchEngine
{
    private readonly ElasticsearchClient _elasticClient;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        _indexName = configuration.GetValue<string>("Elasticsearch:IndexName") ?? "maple_blog";

        var settings = CreateClientSettings(configuration);
        _elasticClient = new ElasticsearchClient(settings);

        // 确保索引存在
        _ = EnsureIndexExistsAsync();
    }

    /// <summary>
    /// 搜索内容
    /// </summary>
    public async Task<SearchResult> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await _elasticClient.SearchAsync<SearchIndex>(s => s
                .Index(_indexName)
                .From(criteria.GetSkip())
                .Size(criteria.PageSize)
                .Query(q => BuildQuery(criteria))
                .Sort(ss => BuildSort(ss, criteria))
            , cancellationToken);

            stopwatch.Stop();

            if (!response.IsValidResponse)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.ElasticsearchServerError?.Error?.Reason);
                return new SearchResult
                {
                    Items = new List<SearchResultItem>(),
                    TotalCount = 0,
                    ExecutionTime = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var result = new SearchResult
            {
                Items = MapSearchResults(response.Documents),
                TotalCount = response.Total,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds
            };

            _logger.LogDebug("Search completed in {Time}ms with {Count} results", 
                stopwatch.ElapsedMilliseconds, result.Items.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Elasticsearch search");
            return new SearchResult
            {
                Items = new List<SearchResultItem>(),
                TotalCount = 0,
                ExecutionTime = 0
            };
        }
    }

    /// <summary>
    /// 索引文档
    /// </summary>
    public async Task<bool> IndexDocumentAsync(SearchIndex document, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _elasticClient.IndexAsync(document, idx => idx
                .Index(_indexName)
                .Id(document.Id.ToString()), cancellationToken);

            if (response.IsValidResponse)
            {
                _logger.LogDebug("Document {Id} indexed successfully", document.Id);
                return true;
            }

            _logger.LogError("Failed to index document {Id}: {Error}", 
                document.Id, response.ElasticsearchServerError?.Error?.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {Id}", document.Id);
            return false;
        }
    }

    /// <summary>
    /// 批量索引文档
    /// </summary>
    public async Task<int> BulkIndexAsync(IEnumerable<SearchIndex> documents, CancellationToken cancellationToken = default)
    {
        try
        {
            var bulkResponse = await _elasticClient.BulkAsync(b => b
                .Index(_indexName)
                .IndexMany(documents, (descriptor, doc) => descriptor.Id(doc.Id.ToString()))
            , cancellationToken);

            if (bulkResponse.IsValidResponse)
            {
                var successCount = bulkResponse.Items.Count(i => i.IsValid);
                _logger.LogInformation("Bulk indexed {Success}/{Total} documents", 
                    successCount, bulkResponse.Items.Count);
                return successCount;
            }

            _logger.LogError("Bulk indexing failed: {Error}", 
                bulkResponse.ElasticsearchServerError?.Error?.Reason);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk indexing");
            return 0;
        }
    }

    /// <summary>
    /// 删除文档
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            // First, find the document by entityType and entityId
            var searchResponse = await _elasticClient.SearchAsync<SearchIndex>(s => s
                .Index(_indexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field(f => f.EntityType).Value(entityType)),
                            m => m.Term(t => t.Field(f => f.EntityId).Value(entityId.ToString()))
                        )
                    )
                )
                .Size(1), cancellationToken);

            if (!searchResponse.IsValidResponse || searchResponse.Documents.Count == 0)
            {
                _logger.LogWarning("Document not found for deletion: EntityType={Type}, EntityId={Id}", entityType, entityId);
                return true; // Consider it success if document doesn't exist
            }

            var docId = searchResponse.Documents.First().Id.ToString();
            var deleteResponse = await _elasticClient.DeleteAsync<SearchIndex>(docId, d => d
                .Index(_indexName), cancellationToken);

            if (deleteResponse.IsValidResponse || deleteResponse.Result == Result.NotFound)
            {
                _logger.LogDebug("Document deleted: EntityType={Type}, EntityId={Id}", entityType, entityId);
                return true;
            }

            _logger.LogError("Failed to delete document: EntityType={Type}, EntityId={Id}, Error={Error}", 
                entityType, entityId, deleteResponse.ElasticsearchServerError?.Error?.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: EntityType={Type}, EntityId={Id}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新文档
    /// </summary>
    public async Task<bool> UpdateDocumentAsync(SearchIndex document, CancellationToken cancellationToken = default)
    {
        try
        {
            document.LastUpdatedAt = DateTime.UtcNow;
            
            var response = await _elasticClient.UpdateAsync<SearchIndex, SearchIndex>(
                _indexName,
                new Id(document.Id.ToString()),
                u => u.Doc(document),
                cancellationToken);

            if (response.IsValidResponse)
            {
                _logger.LogDebug("Document {Id} updated successfully", document.Id);
                return true;
            }

            _logger.LogError("Failed to update document {Id}: {Error}", 
                document.Id, response.ElasticsearchServerError?.Error?.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {Id}", document.Id);
            return false;
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string query, int size = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _elasticClient.SearchAsync<SearchIndex>(s => s
                .Index(_indexName)
                .Query(q => q.Prefix(p => p.Field(f => f.Title).Value(query)))
                .Size(size)
                .Source(false)
            , cancellationToken);

            if (!response.IsValidResponse)
            {
                _logger.LogError("Failed to get suggestions: {Error}", response.ElasticsearchServerError?.Error?.Reason);
                return Enumerable.Empty<string>();
            }

            return response.Documents
                .Where(d => !string.IsNullOrEmpty(d.Title))
                .Select(d => d.Title!)
                .Distinct()
                .Take(size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for query: {Query}", query);
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 检查连接状态
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _elasticClient.Cluster.HealthAsync(cancellationToken);
            return response.IsValidResponse && 
                   (response.Status == HealthStatus.Green || response.Status == HealthStatus.Yellow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Elasticsearch health");
            return false;
        }
    }

    /// <summary>
    /// 重建索引
    /// </summary>
    public async Task<bool> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Delete the existing index
            var deleteResponse = await _elasticClient.Indices.DeleteAsync(_indexName, cancellationToken);
            
            if (!deleteResponse.IsValidResponse && !deleteResponse.ApiCallDetails?.OriginalException?.Message?.Contains("index_not_found") == true)
            {
                _logger.LogError("Failed to delete index for rebuild: {Error}", 
                    deleteResponse.ElasticsearchServerError?.Error?.Reason);
                return false;
            }

            // Recreate the index
            return await EnsureIndexExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding index");
            return false;
        }
    }

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    public async Task<IndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var statsResponse = await _elasticClient.Indices.StatsAsync((Indices)_indexName, cancellationToken);
            var healthResponse = await _elasticClient.Cluster.HealthAsync(cancellationToken);

            if (!statsResponse.IsValidResponse)
            {
                _logger.LogError("Failed to get index stats: {Error}", 
                    statsResponse.ElasticsearchServerError?.Error?.Reason);
                return new IndexStats();
            }

            var indexStats = statsResponse.Indices.GetValueOrDefault(_indexName);
            
            return new IndexStats
            {
                DocumentCount = indexStats?.Total?.Docs?.Count ?? 0,
                SizeInBytes = indexStats?.Total?.Store?.SizeInBytes ?? 0,
                ShardCount = indexStats?.Shards?.Count ?? 0,
                HealthStatus = healthResponse.IsValidResponse ? healthResponse.Status.ToString() : "unknown",
                LastUpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index stats");
            return new IndexStats();
        }
    }

    /// <summary>
    /// 创建连接设置
    /// </summary>
    private ElasticsearchClientSettings CreateClientSettings(IConfiguration configuration)
    {
        var uri = configuration.GetValue<string>("Elasticsearch:Uri") ?? "http://localhost:9200";
        var username = configuration.GetValue<string>("Elasticsearch:Username");
        var password = configuration.GetValue<string>("Elasticsearch:Password");

        var settings = new ElasticsearchClientSettings(new Uri(uri))
            .DefaultIndex(_indexName)
            .EnableDebugMode()
            .PrettyJson()
            .RequestTimeout(TimeSpan.FromSeconds(30));

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            settings.Authentication(new BasicAuthentication(username, password));
        }

        return settings;
    }

    /// <summary>
    /// 确保索引存在
    /// </summary>
    private async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existsResponse = await _elasticClient.Indices.ExistsAsync(_indexName, cancellationToken);
            
            if (existsResponse.Exists)
            {
                return true;
            }

            var createResponse = await _elasticClient.Indices.CreateAsync(_indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                )
                .Mappings(m => m
                    .Properties<SearchIndex>(p => p
                        .Keyword(k => k.Id)
                        .Keyword(k => k.EntityType)
                        .Keyword(k => k.EntityId)
                        .Text(t => t.Title)
                        .Text(t => t.Content)
                        .Text(t => t.Keywords)
                        .Keyword(k => k.Language)
                        .DoubleNumber(n => n.TitleWeight)
                        .DoubleNumber(n => n.ContentWeight)
                        .DoubleNumber(n => n.KeywordWeight)
                        .Date(d => d.IndexedAt)
                        .Date(d => d.LastUpdatedAt)
                        .Boolean(b => b.IsActive)
                    )
                ), cancellationToken);

            if (createResponse.IsValidResponse)
            {
                _logger.LogInformation("Index {Index} created successfully", _indexName);
                return true;
            }

            _logger.LogError("Failed to create index {Index}: {Error}", 
                _indexName, createResponse.ElasticsearchServerError?.Error?.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring index exists");
            return false;
        }
    }

    /// <summary>
    /// 构建查询
    /// </summary>
    private Query BuildQuery(SearchCriteria criteria)
    {
        var queries = new List<Query>();

        // Main query
        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            queries.Add(new MultiMatchQuery
            {
                Query = criteria.Query,
                Fields = new Field[] { "title", "content", "keywords" },
                Type = TextQueryType.BestFields
            });
        }

        // Add filters
        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            queries.Add(new TermQuery
            {
                Field = "entityType",
                Value = criteria.ContentType
            });
        }

        // Date range filter
        if (criteria.StartDate.HasValue || criteria.EndDate.HasValue)
        {
            var dateRange = new DateRangeQuery
            {
                Field = "indexedAt"
            };
            
            if (criteria.StartDate.HasValue)
                dateRange.Gte = DateMath.Anchored(criteria.StartDate.Value);
            
            if (criteria.EndDate.HasValue)
                dateRange.Lte = DateMath.Anchored(criteria.EndDate.Value);
            
            queries.Add(dateRange);
        }

        // Active filter
        queries.Add(new TermQuery
        {
            Field = "isActive",
            Value = true
        });

        // Combine all queries
        if (queries.Count == 0)
        {
            return new MatchAllQuery();
        }
        else if (queries.Count == 1)
        {
            return queries[0];
        }
        else
        {
            return new BoolQuery
            {
                Must = queries
            };
        }
    }

    /// <summary>
    /// 构建排序
    /// </summary>
    private SortOptionsDescriptor<SearchIndex> BuildSort(SortOptionsDescriptor<SearchIndex> sortDescriptor, SearchCriteria criteria)
    {
        var order = criteria.SortDirection.ToLower() == "asc" ? SortOrder.Asc : SortOrder.Desc;

        switch (criteria.SortBy.ToLower())
        {
            case "date":
            case "indexedat":
                sortDescriptor.Field(f => f.IndexedAt, fd => fd.Order(order));
                break;
            case "title":
                sortDescriptor.Field(f => f.Title, fd => fd.Order(order));
                break;
            case "relevance":
            default:
                sortDescriptor.Score(s => s.Order(SortOrder.Desc));
                break;
        }
        
        return sortDescriptor;
    }

    /// <summary>
    /// 映射搜索结果
    /// </summary>
    private List<SearchResultItem> MapSearchResults(IReadOnlyCollection<SearchIndex> documents)
    {
        return documents.Select(doc => new SearchResultItem
        {
            EntityId = doc.EntityId,
            EntityType = doc.EntityType,
            Title = doc.Title,
            Summary = doc.Content?.Length > 200 
                ? doc.Content.Substring(0, 200) + "..." 
                : doc.Content,
            Score = 0, // Score not easily accessible in simplified version
            CreatedAt = doc.IndexedAt,
            ExtraData = new Dictionary<string, object>
            {
                ["language"] = doc.Language,
                ["keywords"] = doc.Keywords ?? string.Empty
            }
        }).ToList();
    }
}