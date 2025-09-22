using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// Elasticsearch服务接口
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// 索引文档
    /// </summary>
    /// <param name="document">文档对象</param>
    /// <param name="index">索引名称</param>
    /// <returns>索引结果</returns>
    Task<bool> IndexDocumentAsync<T>(T document, string index) where T : class;

    /// <summary>
    /// 搜索文档
    /// </summary>
    /// <param name="query">搜索查询</param>
    /// <param name="index">索引名称</param>
    /// <returns>搜索结果</returns>
    Task<IEnumerable<T>> SearchAsync<T>(string query, string index) where T : class;

    /// <summary>
    /// 删除文档
    /// </summary>
    /// <param name="id">文档ID</param>
    /// <param name="index">索引名称</param>
    /// <returns>删除结果</returns>
    Task<bool> DeleteDocumentAsync(string id, string index);

    /// <summary>
    /// 创建索引
    /// </summary>
    /// <param name="index">索引名称</param>
    /// <returns>创建结果</returns>
    Task<bool> CreateIndexAsync(string index);

    /// <summary>
    /// 检查连接状态
    /// </summary>
    /// <returns>连接状态</returns>
    Task<bool> IsHealthyAsync();
}