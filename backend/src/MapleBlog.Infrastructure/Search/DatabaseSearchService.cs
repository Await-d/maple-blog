using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MapleBlog.Infrastructure.Search;

/// <summary>
/// 数据库搜索服务 - Elasticsearch降级方案
/// </summary>
public class DatabaseSearchService : ISearchEngine
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSearchService> _logger;

    public DatabaseSearchService(ApplicationDbContext context, ILogger<DatabaseSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 搜索内容
    /// </summary>
    public async Task<SearchResult> SearchAsync(SearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var query = BuildQuery(criteria);
            var totalCount = await query.CountAsync(cancellationToken);

            var documents = await query
                .Skip(criteria.GetSkip())
                .Take(criteria.PageSize)
                .ToListAsync(cancellationToken);

            stopwatch.Stop();

            var result = new SearchResult
            {
                Items = await ConvertToSearchResultItemsAsync(documents, criteria, cancellationToken),
                TotalCount = totalCount,
                ExecutionTime = (int)stopwatch.ElapsedMilliseconds
            };

            _logger.LogInformation("Database search completed: Query={Query}, Results={Results}, Time={Time}ms",
                criteria.Query, result.TotalCount, result.ExecutionTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database search: {Query}", criteria.Query);
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
            var existingIndex = await _context.SearchIndexes
                .FirstOrDefaultAsync(si => si.EntityType == searchIndex.EntityType &&
                                         si.EntityId == searchIndex.EntityId, cancellationToken);

            if (existingIndex != null)
            {
                // 更新现有索引
                existingIndex.UpdateIndex(searchIndex.Title, searchIndex.Content, searchIndex.Keywords);
                existingIndex.Language = searchIndex.Language;
                existingIndex.SetWeights(searchIndex.TitleWeight, searchIndex.ContentWeight, searchIndex.KeywordWeight);
            }
            else
            {
                // 创建新索引
                _context.SearchIndexes.Add(searchIndex);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Document indexed in database: {EntityType}:{EntityId}",
                searchIndex.EntityType, searchIndex.EntityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document in database: {EntityType}:{EntityId}",
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

            var successCount = 0;

            // 分批处理以避免内存问题
            const int batchSize = 100;
            for (int i = 0; i < documents.Count; i += batchSize)
            {
                var batch = documents.Skip(i).Take(batchSize).ToList();

                // 获取现有索引
                var entityKeys = batch.Select(d => new { d.EntityType, d.EntityId }).ToList();
                var existingIndexes = await _context.SearchIndexes
                    .Where(si => entityKeys.Any(k => k.EntityType == si.EntityType && k.EntityId == si.EntityId))
                    .ToListAsync(cancellationToken);

                foreach (var document in batch)
                {
                    var existing = existingIndexes.FirstOrDefault(ei =>
                        ei.EntityType == document.EntityType && ei.EntityId == document.EntityId);

                    if (existing != null)
                    {
                        existing.UpdateIndex(document.Title, document.Content, document.Keywords);
                        existing.Language = document.Language;
                        existing.SetWeights(document.TitleWeight, document.ContentWeight, document.KeywordWeight);
                    }
                    else
                    {
                        _context.SearchIndexes.Add(document);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                successCount += batch.Count;
            }

            _logger.LogInformation("Bulk index completed in database: {Count} documents", successCount);
            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk index operation in database");
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
            var existingIndex = await _context.SearchIndexes
                .FirstOrDefaultAsync(si => si.EntityType == entityType && si.EntityId == entityId, cancellationToken);

            if (existingIndex == null)
            {
                _logger.LogWarning("Document not found for deletion in database: {EntityType}:{EntityId}", entityType, entityId);
                return false;
            }

            _context.SearchIndexes.Remove(existingIndex);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Document deleted from database: {EntityType}:{EntityId}", entityType, entityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document from database: {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }

    /// <summary>
    /// 更新文档
    /// </summary>
    public async Task<bool> UpdateDocumentAsync(SearchIndex searchIndex, CancellationToken cancellationToken = default)
    {
        return await IndexDocumentAsync(searchIndex, cancellationToken);
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    public async Task<IEnumerable<string>> GetSuggestionsAsync(string query, int size = 5, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Enumerable.Empty<string>();
            }

            var normalizedQuery = query.Trim().ToLowerInvariant();

            // 获取热门搜索建议
            var popularSearches = await _context.PopularSearches
                .Where(ps => ps.Query.ToLower().Contains(normalizedQuery))
                .OrderByDescending(ps => ps.SearchCount)
                .Take(size)
                .Select(ps => ps.Query)
                .ToListAsync(cancellationToken);

            // 如果热门搜索不够，从索引中获取更多建议
            if (popularSearches.Count < size)
            {
                var remaining = size - popularSearches.Count;
                var titleSuggestions = await _context.SearchIndexes
                    .Where(si => si.IsActive &&
                               !string.IsNullOrEmpty(si.Title) &&
                               si.Title.ToLower().Contains(normalizedQuery))
                    .Select(si => si.Title!)
                    .Distinct()
                    .Take(remaining)
                    .ToListAsync(cancellationToken);

                popularSearches.AddRange(titleSuggestions.Where(t => !popularSearches.Contains(t, StringComparer.OrdinalIgnoreCase)));
            }

            return popularSearches.Take(size);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions from database for query: {Query}", query);
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
            // 尝试执行简单的数据库查询
            await _context.SearchIndexes.Take(1).CountAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
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
            _logger.LogInformation("Starting database index rebuild");

            // 清除现有索引
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM SearchIndexes", cancellationToken);

            // 重新索引所有文章
            var posts = await _context.Posts
                .Where(p => p.IsPublished && !p.IsDeleted)
                .Select(p => new { p.Id, p.Title, p.Content, p.CreatedAt, p.AuthorId })
                .ToListAsync(cancellationToken);

            foreach (var post in posts)
            {
                var searchIndex = SearchIndex.Create(
                    "Post",
                    post.Id,
                    post.Title,
                    post.Content,
                    null, // keywords可以从内容中提取
                    "zh-CN"
                );

                _context.SearchIndexes.Add(searchIndex);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Database index rebuild completed successfully: {Count} documents", posts.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database index rebuild");
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
            var totalCount = await _context.SearchIndexes.CountAsync(cancellationToken);
            var activeCount = await _context.SearchIndexes.CountAsync(si => si.IsActive, cancellationToken);
            var lastUpdated = await _context.SearchIndexes
                .Where(si => si.LastUpdatedAt.HasValue)
                .MaxAsync(si => si.LastUpdatedAt, cancellationToken) ?? DateTime.UtcNow;

            return new IndexStats
            {
                DocumentCount = totalCount,
                SizeInBytes = 0, // 数据库大小计算复杂，暂时设为0
                ShardCount = 1,
                ReplicaCount = 0,
                HealthStatus = activeCount > 0 ? "green" : "yellow",
                LastUpdatedAt = lastUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database index statistics");
            return new IndexStats
            {
                HealthStatus = "red",
                LastUpdatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 构建数据库查询
    /// </summary>
    private IQueryable<SearchIndex> BuildQuery(SearchCriteria criteria)
    {
        var query = _context.SearchIndexes.Where(si => si.IsActive);

        // 全文搜索
        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var searchTerms = criteria.Query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // SQL Server全文搜索（如果支持）
            if (_context.Database.IsSqlServer())
            {
                // 使用CONTAINS进行全文搜索
                var containsQuery = string.Join(" AND ", searchTerms.Select(term => $"\"{term}*\""));
                query = query.Where(si =>
                    EF.Functions.Contains(si.Title, containsQuery) ||
                    EF.Functions.Contains(si.Content, containsQuery) ||
                    EF.Functions.Contains(si.Keywords, containsQuery));
            }
            else
            {
                // 使用LIKE进行模糊搜索（SQLite等）
                foreach (var term in searchTerms)
                {
                    var normalizedTerm = term.ToLowerInvariant();
                    query = query.Where(si =>
                        si.Title != null && si.Title.ToLower().Contains(normalizedTerm) ||
                        si.Content != null && si.Content.ToLower().Contains(normalizedTerm) ||
                        si.Keywords != null && si.Keywords.ToLower().Contains(normalizedTerm));
                }
            }
        }

        // 内容类型过滤
        if (!string.IsNullOrWhiteSpace(criteria.ContentType))
        {
            query = query.Where(si => si.EntityType == criteria.ContentType);
        }

        // 日期范围过滤
        if (criteria.StartDate.HasValue)
        {
            query = query.Where(si => si.IndexedAt >= criteria.StartDate.Value);
        }

        if (criteria.EndDate.HasValue)
        {
            query = query.Where(si => si.IndexedAt <= criteria.EndDate.Value);
        }

        // 排序
        query = ApplySorting(query, criteria);

        return query;
    }

    /// <summary>
    /// 应用排序
    /// </summary>
    private IQueryable<SearchIndex> ApplySorting(IQueryable<SearchIndex> query, SearchCriteria criteria)
    {
        return criteria.SortBy?.ToLowerInvariant() switch
        {
            "date" or "createdat" => criteria.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(si => si.IndexedAt)
                : query.OrderByDescending(si => si.IndexedAt),

            "title" => criteria.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(si => si.Title)
                : query.OrderByDescending(si => si.Title),

            _ => query.OrderByDescending(si => si.LastUpdatedAt ?? si.IndexedAt)
                     .ThenByDescending(si => si.TitleWeight + si.ContentWeight + si.KeywordWeight)
        };
    }

    /// <summary>
    /// 转换为搜索结果项
    /// </summary>
    private async Task<List<SearchResultItem>> ConvertToSearchResultItemsAsync(
        IEnumerable<SearchIndex> documents,
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SearchResultItem>();
        var searchTerms = criteria.Query?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

        foreach (var document in documents)
        {
            var item = new SearchResultItem
            {
                EntityId = document.EntityId,
                EntityType = document.EntityType,
                Title = document.Title,
                Summary = GenerateSummary(document.Content),
                Score = CalculateRelevanceScore(document, searchTerms),
                CreatedAt = document.IndexedAt,
                MatchedFields = GetMatchedFields(document, searchTerms)
            };

            // 生成高亮内容
            if (criteria.EnableHighlight && searchTerms.Any())
            {
                item.Highlights = GenerateHighlights(document, searchTerms);
            }

            results.Add(item);
        }

        return results;
    }

    /// <summary>
    /// 计算相关性得分
    /// </summary>
    private float CalculateRelevanceScore(SearchIndex document, string[] searchTerms)
    {
        if (!searchTerms.Any())
            return 1.0f;

        float score = 0;
        var title = document.Title?.ToLowerInvariant() ?? "";
        var content = document.Content?.ToLowerInvariant() ?? "";
        var keywords = document.Keywords?.ToLowerInvariant() ?? "";

        foreach (var term in searchTerms)
        {
            var normalizedTerm = term.ToLowerInvariant();

            // 标题匹配权重更高
            if (title.Contains(normalizedTerm))
            {
                score += (float)document.TitleWeight * 10;
                if (title.StartsWith(normalizedTerm))
                    score += 5; // 前缀匹配额外加分
            }

            // 关键词匹配
            if (keywords.Contains(normalizedTerm))
            {
                score += (float)document.KeywordWeight * 8;
            }

            // 内容匹配
            if (content.Contains(normalizedTerm))
            {
                score += (float)document.ContentWeight * 5;
                // 计算词频
                var matches = Regex.Matches(content, Regex.Escape(normalizedTerm), RegexOptions.IgnoreCase);
                score += matches.Count * 0.1f;
            }
        }

        return Math.Max(score, 0.1f); // 确保最低分数
    }

    /// <summary>
    /// 获取匹配字段
    /// </summary>
    private List<string> GetMatchedFields(SearchIndex document, string[] searchTerms)
    {
        var matchedFields = new List<string>();

        if (searchTerms.Any())
        {
            var title = document.Title?.ToLowerInvariant() ?? "";
            var content = document.Content?.ToLowerInvariant() ?? "";
            var keywords = document.Keywords?.ToLowerInvariant() ?? "";

            foreach (var term in searchTerms)
            {
                var normalizedTerm = term.ToLowerInvariant();

                if (title.Contains(normalizedTerm) && !matchedFields.Contains("title"))
                    matchedFields.Add("title");

                if (content.Contains(normalizedTerm) && !matchedFields.Contains("content"))
                    matchedFields.Add("content");

                if (keywords.Contains(normalizedTerm) && !matchedFields.Contains("keywords"))
                    matchedFields.Add("keywords");
            }
        }

        return matchedFields;
    }

    /// <summary>
    /// 生成高亮内容
    /// </summary>
    private Dictionary<string, List<string>> GenerateHighlights(SearchIndex document, string[] searchTerms)
    {
        var highlights = new Dictionary<string, List<string>>();

        foreach (var term in searchTerms)
        {
            var pattern = $"({Regex.Escape(term)})";
            var replacement = "<mark>$1</mark>";

            // 标题高亮
            if (!string.IsNullOrEmpty(document.Title))
            {
                var highlightedTitle = Regex.Replace(document.Title, pattern, replacement, RegexOptions.IgnoreCase);
                if (highlightedTitle != document.Title)
                {
                    if (!highlights.ContainsKey("title"))
                        highlights["title"] = new List<string>();
                    highlights["title"].Add(highlightedTitle);
                }
            }

            // 内容高亮（提取片段）
            if (!string.IsNullOrEmpty(document.Content))
            {
                var contentFragments = ExtractHighlightFragments(document.Content, term);
                if (contentFragments.Any())
                {
                    if (!highlights.ContainsKey("content"))
                        highlights["content"] = new List<string>();
                    highlights["content"].AddRange(contentFragments);
                }
            }
        }

        return highlights;
    }

    /// <summary>
    /// 提取高亮片段
    /// </summary>
    private List<string> ExtractHighlightFragments(string content, string term, int fragmentSize = 100)
    {
        var fragments = new List<string>();
        var pattern = $"({Regex.Escape(term)})";
        var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches.Take(3)) // 最多3个片段
        {
            var start = Math.Max(0, match.Index - fragmentSize / 2);
            var length = Math.Min(fragmentSize, content.Length - start);
            var fragment = content.Substring(start, length);

            var highlightedFragment = Regex.Replace(fragment, pattern, "<mark>$1</mark>", RegexOptions.IgnoreCase);

            if (start > 0) highlightedFragment = "..." + highlightedFragment;
            if (start + length < content.Length) highlightedFragment += "...";

            fragments.Add(highlightedFragment);
        }

        return fragments;
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
}