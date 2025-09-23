using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace MapleBlog.Infrastructure.Search;

/// <summary>
/// Elasticsearch搜索引擎服务实现
/// </summary>
public class ElasticsearchService : ISearchEngine
{
    private readonly ElasticClient _elasticClient;
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly string _indexName;

    public ElasticsearchService(IConfiguration configuration, ILogger<ElasticsearchService> logger)
    {
        _logger = logger;
        _indexName = configuration.GetValue<string>("Elasticsearch:IndexName") ?? "maple_blog";

        var connectionSettings = CreateConnectionSettings(configuration);
        _elasticClient = new ElasticClient(connectionSettings);

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

            var searchRequest = BuildSearchRequest(criteria);
            var response = await _elasticClient.SearchAsync<SearchIndex>(searchRequest, cancellationToken);

            stopwatch.Stop();

            if (!response.IsValid)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.OriginalException?.Message);
                return new SearchResult
                {
                    Items = new List<SearchResultItem>(),
                    TotalCount = 0,
                    ExecutionTime = (int)stopwatch.ElapsedMilliseconds
                };
            }

            var result = new SearchResult
            {
                Items = ConvertToSearchResultItems(response.Documents, response.Hits),
                TotalCount = response.Total,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds,
                Aggregations = ExtractAggregations(response.Aggregations)
            };

            // 添加搜索建议
            if (response.Suggest != null)
            {
                try
                {
                    var suggestDict = new Dictionary<string, Suggest<object>[]>();
                    // For now, skip suggestions as the NEST API is complex
                    result.Suggestions = new List<string>();
                }
                catch
                {
                    result.Suggestions = new List<string>();
                }
            }

            _logger.LogInformation("Search completed: Query={Query}, Results={Results}, Time={Time}ms",
                criteria.Query, result.TotalCount, result.ExecutionTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during search: {Query}", criteria.Query);
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
    public async Task<bool> IndexDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _elasticClient.IndexDocumentAsync(searchIndex, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index document: {Error}", response.OriginalException?.Message);
                return false;
            }

            _logger.LogDebug("Document indexed successfully: {EntityType}:{EntityId}",
                searchIndex.EntityType, searchIndex.EntityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document: {EntityType}:{EntityId}",
                searchIndex.EntityType, searchIndex.EntityId);
            return false;
        }
    }

    /// <summary>
    /// 批量索引文档
    /// </summary>
    public async Task<int> BulkIndexAsync(IEnumerable<SearchIndex> searchIndexes, CancellationToken cancellationToken = default)
    {
        try
        {
            var documents = searchIndexes.ToList();
            if (!documents.Any())
            {
                return 0;
            }

            var bulkRequest = new BulkRequest(_indexName)
            {
                Operations = documents.Select(doc => new BulkIndexOperation<SearchIndex>(doc)
                {
                    Id = doc.Id.ToString()
                }).Cast<IBulkOperation>().ToList()
            };

            var response = await _elasticClient.BulkAsync(bulkRequest, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Bulk index failed: {Error}", response.OriginalException?.Message);
                return 0;
            }

            var successCount = response.Items.Count(i => i.IsValid);
            var failureCount = documents.Count - successCount;

            if (failureCount > 0)
            {
                _logger.LogWarning("Bulk index completed with {Success} successes and {Failures} failures",
                    successCount, failureCount);
            }
            else
            {
                _logger.LogInformation("Bulk index completed successfully: {Count} documents", successCount);
            }

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk index operation");
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
            // 先查找要删除的文档
            var searchResponse = await _elasticClient.SearchAsync<SearchIndex>(s => s
                .Index(_indexName)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Field(f => f.EntityType).Value(entityType)),
                            m => m.Term(t => t.Field(f => f.EntityId).Value(entityId))
                        )
                    )
                )
                .Size(1), cancellationToken);

            if (!searchResponse.IsValid || !searchResponse.Documents.Any())
            {
                _logger.LogWarning("Document not found for deletion: {EntityType}:{EntityId}", entityType, entityId);
                return false;
            }

            var documentToDelete = searchResponse.Documents.First();
            var deleteResponse = await _elasticClient.DeleteAsync<SearchIndex>(documentToDelete.Id, d => d.Index(_indexName), cancellationToken);

            if (!deleteResponse.IsValid)
            {
                _logger.LogError("Failed to delete document: {Error}", deleteResponse.OriginalException?.Message);
                return false;
            }

            _logger.LogDebug("Document deleted successfully: {EntityType}:{EntityId}", entityType, entityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新文档
    /// </summary>
    public async Task<bool> UpdateDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default)
    {
        try
        {
            // Elasticsearch中更新就是重新索引
            return await IndexDocumentAsync(searchIndex, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document: {EntityType}:{EntityId}",
                searchIndex.EntityType, searchIndex.EntityId);
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
                .Size(0)
                .Suggest(su => su
                    .Term("title_suggestion", ts => ts
                        .Field(f => f.Title)
                        .Text(query)
                        .Size(size)
                    )
                    .Term("content_suggestion", cs => cs
                        .Field(f => f.Content)
                        .Text(query)
                        .Size(size)
                    )
                ), cancellationToken);

            if (!response.IsValid || response.Suggest == null)
            {
                return Enumerable.Empty<string>();
            }

            // For now, return empty suggestions due to NEST API complexity
            // This can be implemented later with proper NEST suggest handling
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
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
            var response = await _elasticClient.Cluster.HealthAsync();
            return response.IsValid && (response.Status == Health.Green || response.Status == Health.Yellow);
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
            _logger.LogInformation("Starting index rebuild for {IndexName}", _indexName);

            // 删除现有索引
            var deleteResponse = await _elasticClient.Indices.DeleteAsync(_indexName);
            if (!deleteResponse.IsValid && !deleteResponse.ServerError.Error.Type.Contains("index_not_found"))
            {
                _logger.LogError("Failed to delete existing index: {Error}", deleteResponse.OriginalException?.Message);
                return false;
            }

            // 重新创建索引
            var createResponse = await CreateIndexAsync(cancellationToken);
            if (!createResponse)
            {
                _logger.LogError("Failed to recreate index during rebuild");
                return false;
            }

            _logger.LogInformation("Index rebuild completed successfully for {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during index rebuild");
            return false;
        }
    }

    /// <summary>
    /// 获取索引统计信息
    /// </summary>
    public async Task<Domain.Interfaces.IndexStats> GetIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var statsResponse = await _elasticClient.Indices.StatsAsync(_indexName);
            var healthResponse = await _elasticClient.Cluster.HealthAsync(new ClusterHealthRequest(_indexName));

            if (!statsResponse.IsValid || !healthResponse.IsValid)
            {
                return new Domain.Interfaces.IndexStats
                {
                    HealthStatus = "unknown",
                    LastUpdatedAt = DateTime.UtcNow
                };
            }

            var indexStats = statsResponse.Indices[_indexName];
            var primaryStats = indexStats.Primaries;

            return new Domain.Interfaces.IndexStats
            {
                DocumentCount = primaryStats.Documents.Count,
                SizeInBytes = (long)primaryStats.Store.SizeInBytes,
                ShardCount = healthResponse.NumberOfDataNodes,
                ReplicaCount = healthResponse.NumberOfNodes - healthResponse.NumberOfDataNodes,
                HealthStatus = healthResponse.Status.ToString().ToLowerInvariant(),
                LastUpdatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index statistics");
            return new Domain.Interfaces.IndexStats
            {
                HealthStatus = "error",
                LastUpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 创建连接设置
    /// </summary>
    private ConnectionSettings CreateConnectionSettings(IConfiguration configuration)
    {
        var elasticsearchUrl = configuration.GetValue<string>("Elasticsearch:Url") ?? "http://localhost:9200";
        var uri = new Uri(elasticsearchUrl);

        var settings = new ConnectionSettings(uri)
            .DefaultIndex(_indexName)
            .EnableDebugMode()
            .PrettyJson()
            .RequestTimeout(TimeSpan.FromSeconds(30))
            .MaximumRetries(3)
            .DisableDirectStreaming(); // 方便调试

        // 配置认证（如果需要）
        var username = configuration.GetValue<string>("Elasticsearch:Username");
        var password = configuration.GetValue<string>("Elasticsearch:Password");
        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            settings.BasicAuthentication(username, password);
        }

        // 配置字段映射
        settings.DefaultMappingFor<SearchIndex>(m => m
            .IdProperty(p => p.Id)
            .PropertyName(p => p.EntityType, "entity_type")
            .PropertyName(p => p.EntityId, "entity_id")
            .PropertyName(p => p.IndexedAt, "indexed_at")
            .PropertyName(p => p.LastUpdatedAt, "last_updated_at")
            .PropertyName(p => p.IsActive, "is_active")
        );

        return settings;
    }

    /// <summary>
    /// 确保索引存在
    /// </summary>
    private async Task EnsureIndexExistsAsync()
    {
        try
        {
            var existsResponse = await _elasticClient.Indices.ExistsAsync(_indexName);
            if (!existsResponse.Exists)
            {
                await CreateIndexAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring index exists");
        }
    }

    /// <summary>
    /// 创建索引
    /// </summary>
    private async Task<bool> CreateIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var createResponse = await _elasticClient.Indices.CreateAsync(_indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("chinese_analyzer", ca => ca
                                .Tokenizer("ik_max_word")
                                .Filters("lowercase", "stop")
                            )
                            .Custom("english_analyzer", ea => ea
                                .Tokenizer("standard")
                                .Filters("lowercase", "stop", "snowball")
                            )
                        )
                    )
                )
                .Map<SearchIndex>(m => m
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.Id))
                        .Keyword(k => k.Name(n => n.EntityType))
                        .Keyword(k => k.Name(n => n.EntityId))
                        .Text(t => t
                            .Name(n => n.Title)
                            .Analyzer("chinese_analyzer")
                            .SearchAnalyzer("chinese_analyzer")
                            .Fields(f => f
                                .Text(tt => tt
                                    .Name("english")
                                    .Analyzer("english_analyzer")
                                )
                            )
                        )
                        .Text(t => t
                            .Name(n => n.Content)
                            .Analyzer("chinese_analyzer")
                            .SearchAnalyzer("chinese_analyzer")
                            .Fields(f => f
                                .Text(tt => tt
                                    .Name("english")
                                    .Analyzer("english_analyzer")
                                )
                            )
                        )
                        .Text(t => t
                            .Name(n => n.Keywords)
                            .Analyzer("chinese_analyzer")
                            .SearchAnalyzer("chinese_analyzer")
                        )
                        .Keyword(k => k.Name(n => n.Language))
                        .Number(n => n.Name(nn => nn.TitleWeight).Type(NumberType.Float))
                        .Number(n => n.Name(nn => nn.ContentWeight).Type(NumberType.Float))
                        .Number(n => n.Name(nn => nn.KeywordWeight).Type(NumberType.Float))
                        .Date(d => d.Name(n => n.IndexedAt))
                        .Date(d => d.Name(n => n.LastUpdatedAt))
                        .Boolean(b => b.Name(n => n.IsActive))
                    )
                ));

            if (!createResponse.IsValid)
            {
                _logger.LogError("Failed to create index: {Error}", createResponse.OriginalException?.Message);
                return false;
            }

            _logger.LogInformation("Index created successfully: {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index");
            return false;
        }
    }

    /// <summary>
    /// 构建搜索请求
    /// </summary>
    private SearchRequest<SearchIndex> BuildSearchRequest(SearchCriteria criteria)
    {
        var searchRequest = new SearchRequest<SearchIndex>(_indexName)
        {
            Size = criteria.PageSize,
            From = criteria.GetSkip(),
            TrackTotalHits = true,
            Highlight = criteria.EnableHighlight ? new Highlight
            {
                Fields = new Dictionary<Field, IHighlightField>
                {
                    { "title", new HighlightField() },
                    { "content", new HighlightField { FragmentSize = 150, NumberOfFragments = 3 } },
                    { "keywords", new HighlightField() }
                }
            } : null
        };

        // 构建查询
        searchRequest.Query = BuildQuery(criteria);

        // 构建排序
        searchRequest.Sort = BuildSort(criteria);

        // 添加聚合（用于统计分析）
        if (criteria.HasFilters())
        {
            searchRequest.Aggregations = new AggregationDictionary
            {
                ["by_entity_type"] = (IAggregationContainer)new TermsAggregation("by_entity_type")
                {
                    Field = "entity_type",
                    Size = 10
                },
                ["by_language"] = (IAggregationContainer)new TermsAggregation("by_language")
                {
                    Field = "language",
                    Size = 10
                }
            };
        }

        return searchRequest;
    }

    /// <summary>
    /// 构建查询条件
    /// </summary>
    private QueryContainer BuildQuery(SearchCriteria criteria)
    {
        var queries = new List<QueryContainer>();

        // 主查询
        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var multiMatchQuery = new MultiMatchQuery
            {
                Query = criteria.Query,
                Fields = new[]
                {
                    "title^3.0",
                    "title.english^2.5",
                    "content^1.0",
                    "content.english^0.8",
                    "keywords^2.0"
                },
                Type = TextQueryType.BestFields,
                Fuzziness = criteria.Fuzziness.HasValue ? Fuzziness.EditDistance(criteria.Fuzziness.Value) : Fuzziness.Auto,
                Operator = Operator.And,
                MinimumShouldMatch = "75%"
            };

            queries.Add(multiMatchQuery);
        }
        else
        {
            queries.Add(new MatchAllQuery());
        }

        // 过滤条件
        var filters = new List<QueryContainer>
        {
            new TermQuery { Field = "is_active", Value = true }
        };

        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            filters.Add(new TermQuery { Field = "entity_type", Value = criteria.ContentType });
        }

        if (criteria.CategoryId.HasValue)
        {
            // 注意：这里需要在索引时存储分类信息
            filters.Add(new TermQuery { Field = "category_id", Value = criteria.CategoryId.Value });
        }

        if (criteria.TagIds?.Any() == true)
        {
            filters.Add(new TermsQuery { Field = "tag_ids", Terms = criteria.TagIds.Select(id => id.ToString()) });
        }

        if (criteria.AuthorId.HasValue)
        {
            filters.Add(new TermQuery { Field = "author_id", Value = criteria.AuthorId.Value });
        }

        if (criteria.StartDate.HasValue || criteria.EndDate.HasValue)
        {
            var dateRange = new DateRangeQuery { Field = "indexed_at" };
            if (criteria.StartDate.HasValue)
                dateRange.GreaterThanOrEqualTo = criteria.StartDate.Value;
            if (criteria.EndDate.HasValue)
                dateRange.LessThanOrEqualTo = criteria.EndDate.Value;

            filters.Add(dateRange);
        }

        // 组合查询和过滤器
        return new BoolQuery
        {
            Must = queries,
            Filter = filters
        };
    }

    /// <summary>
    /// 构建排序条件
    /// </summary>
    private IList<ISort> BuildSort(SearchCriteria criteria)
    {
        var sorts = new List<ISort>();

        switch (criteria.SortBy?.ToLowerInvariant())
        {
            case "relevance":
            case "_score":
                sorts.Add(new FieldSort { Field = "_score", Order = SortOrder.Descending });
                break;

            case "date":
            case "createdat":
                sorts.Add(new FieldSort
                {
                    Field = "indexed_at",
                    Order = criteria.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                        ? SortOrder.Ascending : SortOrder.Descending
                });
                break;

            case "title":
                sorts.Add(new FieldSort
                {
                    Field = "title.keyword",
                    Order = criteria.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                        ? SortOrder.Ascending : SortOrder.Descending
                });
                break;

            default:
                sorts.Add(new FieldSort { Field = "_score", Order = SortOrder.Descending });
                sorts.Add(new FieldSort { Field = "indexed_at", Order = SortOrder.Descending });
                break;
        }

        return sorts;
    }

    /// <summary>
    /// 转换为搜索结果项
    /// </summary>
    private List<SearchResultItem> ConvertToSearchResultItems(IEnumerable<SearchIndex> documents, IEnumerable<IHit<SearchIndex>> hits)
    {
        var results = new List<SearchResultItem>();
        var hitsList = hits.ToList();

        foreach (var (document, hit) in documents.Zip(hitsList, (d, h) => (d, h)))
        {
            var item = new SearchResultItem
            {
                EntityId = document.EntityId,
                EntityType = document.EntityType,
                Title = document.Title,
                Summary = GenerateSummary(document.Content),
                Score = (float)(hit.Score ?? 0),
                CreatedAt = document.IndexedAt,
                MatchedFields = ExtractMatchedFields(hit)
            };

            // 添加高亮内容
            if (hit.Highlight?.Any() == true)
            {
                item.Highlights = hit.Highlight.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                );
            }

            results.Add(item);
        }

        return results;
    }

    /// <summary>
    /// 生成内容摘要
    /// </summary>
    private string? GenerateSummary(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        const int maxLength = 200;
        if (content.Length <= maxLength)
            return content;

        var summary = content.Substring(0, maxLength);
        var lastSpace = summary.LastIndexOf(' ');
        if (lastSpace > maxLength * 0.8)
        {
            summary = summary.Substring(0, lastSpace);
        }

        return summary + "...";
    }

    /// <summary>
    /// 提取匹配字段
    /// </summary>
    private List<string> ExtractMatchedFields(IHit<SearchIndex> hit)
    {
        var fields = new List<string>();

        if (hit.Highlight?.Any() == true)
        {
            fields.AddRange(hit.Highlight.Keys);
        }

        return fields;
    }

    /// <summary>
    /// 提取聚合结果
    /// </summary>
    private Dictionary<string, object> ExtractAggregations(IReadOnlyDictionary<string, IAggregate> aggregations)
    {
        var result = new Dictionary<string, object>();

        foreach (var aggregation in aggregations)
        {
            if (aggregation.Value is BucketAggregate bucketAggregate)
            {
                var buckets = bucketAggregate.Items.Cast<KeyedBucket<object>>()
                    .Select(b => new { Key = b.Key, Count = b.DocCount })
                    .ToList();
                result[aggregation.Key] = buckets;
            }
        }

        return result;
    }

    /// <summary>
    /// 提取搜索建议
    /// </summary>
    private List<string> ExtractSuggestions(IReadOnlyDictionary<string, Suggest<object>[]> suggests)
    {
        var suggestions = new HashSet<string>();

        foreach (var suggest in suggests.Values)
        {
            foreach (var suggestion in suggest)
            {
                foreach (var option in suggestion.Options)
                {
                    if (!string.IsNullOrWhiteSpace(option.Text))
                    {
                        suggestions.Add(option.Text);
                    }
                }
            }
        }

        return suggestions.ToList();
    }
}