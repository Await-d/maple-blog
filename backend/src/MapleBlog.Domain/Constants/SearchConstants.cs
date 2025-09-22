namespace MapleBlog.Domain.Constants;

/// <summary>
/// 搜索相关常量
/// </summary>
public static class SearchConstants
{
    /// <summary>
    /// 搜索引擎
    /// </summary>
    public static class Engines
    {
        public const string Elasticsearch = "elasticsearch";
        public const string Database = "database";
        public const string Memory = "memory";
    }

    /// <summary>
    /// 搜索类型
    /// </summary>
    public static class Types
    {
        public const string FullText = "fulltext";
        public const string Exact = "exact";
        public const string Fuzzy = "fuzzy";
        public const string Prefix = "prefix";
        public const string Phrase = "phrase";
        public const string Wildcard = "wildcard";
    }

    /// <summary>
    /// 实体类型
    /// </summary>
    public static class EntityTypes
    {
        public const string Post = "post";
        public const string Comment = "comment";
        public const string User = "user";
        public const string Category = "category";
        public const string Tag = "tag";
        public const string Page = "page";
        public const string Attachment = "attachment";
    }

    /// <summary>
    /// 排序字段
    /// </summary>
    public static class SortFields
    {
        public const string Relevance = "relevance";
        public const string Date = "date";
        public const string Title = "title";
        public const string Popularity = "popularity";
        public const string CommentCount = "comment_count";
        public const string ViewCount = "view_count";
        public const string UpdatedAt = "updated_at";
        public const string CreatedAt = "created_at";
    }

    /// <summary>
    /// 排序方向
    /// </summary>
    public static class SortDirections
    {
        public const string Ascending = "asc";
        public const string Descending = "desc";
    }

    /// <summary>
    /// 搜索字段
    /// </summary>
    public static class Fields
    {
        public const string Title = "title";
        public const string Content = "content";
        public const string Keywords = "keywords";
        public const string Tags = "tags";
        public const string Category = "category";
        public const string Author = "author";
        public const string Summary = "summary";
        public const string Description = "description";
    }

    /// <summary>
    /// 索引配置
    /// </summary>
    public static class Index
    {
        public const string DefaultIndexName = "mapleblog";
        public const string PostIndex = "posts";
        public const string CommentIndex = "comments";
        public const string UserIndex = "users";

        // 字段权重
        public const float TitleWeight = 3.0f;
        public const float ContentWeight = 1.0f;
        public const float KeywordWeight = 2.0f;
        public const float TagWeight = 1.5f;
        public const float CategoryWeight = 1.2f;

        // 分片和副本
        public const int DefaultShards = 1;
        public const int DefaultReplicas = 0;

        // 性能设置
        public const int MaxResultWindow = 10000;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }

    /// <summary>
    /// 搜索限制
    /// </summary>
    public static class Limits
    {
        public const int MinQueryLength = 1;
        public const int MaxQueryLength = 500;
        public const int MaxSuggestions = 10;
        public const int MaxFilters = 20;
        public const int MaxSearchHistory = 100;

        // 超时设置（毫秒）
        public const int SearchTimeout = 30000;
        public const int IndexTimeout = 60000;
        public const int BulkTimeout = 300000;

        // 重试次数
        public const int MaxRetries = 3;
        public const int RetryDelayMs = 1000;
    }

    /// <summary>
    /// 分析器配置
    /// </summary>
    public static class Analyzers
    {
        public const string Standard = "standard";
        public const string Simple = "simple";
        public const string Keyword = "keyword";
        public const string IkMaxWord = "ik_max_word";
        public const string IkSmart = "ik_smart";
        public const string Chinese = "chinese";
        public const string English = "english";
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public static class Cache
    {
        public const string SearchPrefix = "search:";
        public const string SuggestionPrefix = "suggestion:";
        public const string PopularPrefix = "popular:";
        public const string StatsPrefix = "stats:";

        // 过期时间（秒）
        public const int SearchCacheExpiry = 300; // 5分钟
        public const int SuggestionCacheExpiry = 600; // 10分钟
        public const int PopularCacheExpiry = 3600; // 1小时
        public const int StatsCacheExpiry = 1800; // 30分钟
    }

    /// <summary>
    /// 事件名称
    /// </summary>
    public static class Events
    {
        public const string SearchPerformed = "search_performed";
        public const string IndexUpdated = "index_updated";
        public const string IndexDeleted = "index_deleted";
        public const string BulkIndexCompleted = "bulk_index_completed";
        public const string SearchFailed = "search_failed";
        public const string IndexRebuildStarted = "index_rebuild_started";
        public const string IndexRebuildCompleted = "index_rebuild_completed";
    }

    /// <summary>
    /// 正则表达式模式
    /// </summary>
    public static class Patterns
    {
        public const string EmailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        public const string UrlPattern = @"https?://[^\s/$.?#].[^\s]*";
        public const string ChinesePattern = @"[\u4e00-\u9fff]";
        public const string SpecialCharsPattern = @"[^\w\s\u4e00-\u9fff]";
    }

    /// <summary>
    /// 语言代码
    /// </summary>
    public static class Languages
    {
        public const string Chinese = "zh-CN";
        public const string English = "en-US";
        public const string Auto = "auto";
    }

    /// <summary>
    /// HTTP状态相关
    /// </summary>
    public static class Http
    {
        public const string ApplicationJson = "application/json";
        public const string ContentType = "Content-Type";
        public const string UserAgent = "User-Agent";
        public const string XForwardedFor = "X-Forwarded-For";
    }

    /// <summary>
    /// 默认配置值
    /// </summary>
    public static class Defaults
    {
        public const int Page = 1;
        public const int PageSize = 20;
        public const string SortBy = SortFields.Relevance;
        public const string SortDirection = SortDirections.Descending;
        public const string SearchType = Types.FullText;
        public const string Language = Languages.Chinese;
        public const bool EnableHighlight = true;
        public const bool ExpandSynonyms = false;
        public const int Fuzziness = 0;
        public const float MinScore = 0.1f;
    }
}