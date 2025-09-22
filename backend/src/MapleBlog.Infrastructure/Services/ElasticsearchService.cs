using System.Text;
using System.Text.Json;
using MapleBlog.Domain.Entities;
using MapleBlog.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Elasticsearch服务实现
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly ILogger<ElasticsearchService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _indexName;
    private readonly JsonSerializerOptions _jsonOptions;

    public ElasticsearchService(
        ILogger<ElasticsearchService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
        _indexName = configuration["Elasticsearch:IndexName"] ?? "mapleblog";

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // 设置基础URL
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
    }

    /// <summary>
    /// 索引文档
    /// </summary>
    public async Task<bool> IndexDocumentAsync<T>(T document, string index) where T : class
    {
        try
        {
            if (document == null)
                return false;

            var documentId = GetDocumentId(document);
            var documentJson = JsonSerializer.Serialize(document, _jsonOptions);

            var response = await _httpClient.PutAsync(
                $"/{index}/_doc/{documentId}",
                new StringContent(documentJson, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully indexed document {DocumentId} to index {Index}", documentId, index);
                return true;
            }

            _logger.LogWarning("Failed to index document {DocumentId} to index {Index}, status: {StatusCode}",
                documentId, index, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while indexing document to index {Index}", index);
            return false;
        }
    }

    /// <summary>
    /// 搜索文档
    /// </summary>
    public async Task<IEnumerable<T>> SearchAsync<T>(string query, string index) where T : class
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<T>();

            var searchRequest = new
            {
                query = new
                {
                    multi_match = new
                    {
                        query = query,
                        fields = new[] { "*" },
                        type = "best_fields",
                        fuzziness = "AUTO"
                    }
                },
                size = 100
            };

            var requestJson = JsonSerializer.Serialize(searchRequest, _jsonOptions);
            var response = await _httpClient.PostAsync(
                $"/{index}/_search",
                new StringContent(requestJson, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Elasticsearch search failed with status: {StatusCode}", response.StatusCode);
                return new List<T>();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var searchResponse = JsonSerializer.Deserialize<ElasticsearchSearchResponse<T>>(responseJson, _jsonOptions);

            return searchResponse?.Hits?.Hits?.Select(hit => hit.Source) ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while searching with query: {Query} in index: {Index}", query, index);
            return new List<T>();
        }
    }

    /// <summary>
    /// 删除文档
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(string id, string index)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            var response = await _httpClient.DeleteAsync($"/{index}/_doc/{id}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Successfully deleted document {DocumentId} from index {Index}", id, index);
                return true;
            }

            _logger.LogWarning("Failed to delete document {DocumentId} from index {Index}, status: {StatusCode}",
                id, index, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting document {DocumentId} from index {Index}", id, index);
            return false;
        }
    }

    /// <summary>
    /// 创建索引
    /// </summary>
    public async Task<bool> CreateIndexAsync(string index)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(index))
                return false;

            // 检查索引是否已存在
            var checkResponse = await _httpClient.GetAsync($"/{index}");
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Index {Index} already exists", index);
                return true;
            }

            // 创建索引
            var indexSettings = new
            {
                settings = new
                {
                    number_of_shards = 1,
                    number_of_replicas = 0
                }
            };

            var settingsJson = JsonSerializer.Serialize(indexSettings, _jsonOptions);
            var response = await _httpClient.PutAsync(
                $"/{index}",
                new StringContent(settingsJson, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully created index {Index}", index);
                return true;
            }

            _logger.LogWarning("Failed to create index {Index}, status: {StatusCode}", index, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating index {Index}", index);
            return false;
        }
    }


    /// <summary>
    /// 检查连接状态
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/_cluster/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


    #region 私有方法

    /// <summary>
    /// 获取文档ID
    /// </summary>
    private static string GetDocumentId<T>(T document)
    {
        // 尝试通过反射获取Id属性
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null)
        {
            var idValue = idProperty.GetValue(document);
            return idValue?.ToString() ?? Guid.NewGuid().ToString();
        }

        // 如果没有Id属性，生成一个新的ID
        return Guid.NewGuid().ToString();
    }

    #endregion

    #region Elasticsearch响应模型

    private class ElasticsearchSearchResponse<T>
    {
        public HitsContainer<T>? Hits { get; set; }
    }

    private class HitsContainer<T>
    {
        public Hit<T>[]? Hits { get; set; }
    }

    private class Hit<T>
    {
        public T Source { get; set; } = default(T)!;
    }

    #endregion
}