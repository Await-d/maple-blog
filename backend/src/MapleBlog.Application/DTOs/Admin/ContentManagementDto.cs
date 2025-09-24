namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 内容管理概览DTO
    /// </summary>
    public class ContentManagementOverviewDto
    {
        /// <summary>
        /// 总内容数
        /// </summary>
        public int TotalContent { get; set; }

        /// <summary>
        /// 已发布内容数
        /// </summary>
        public int PublishedContent { get; set; }

        /// <summary>
        /// 草稿数
        /// </summary>
        public int DraftContent { get; set; }

        /// <summary>
        /// 待审核内容数
        /// </summary>
        public int PendingApproval { get; set; }

        /// <summary>
        /// 被拒绝内容数
        /// </summary>
        public int RejectedContent { get; set; }

        /// <summary>
        /// 已删除内容数
        /// </summary>
        public int DeletedContent { get; set; }

        /// <summary>
        /// 今日新增内容数
        /// </summary>
        public int TodayNewContent { get; set; }

        /// <summary>
        /// 本周新增内容数
        /// </summary>
        public int WeekNewContent { get; set; }

        /// <summary>
        /// 内容类型分布
        /// </summary>
        public IEnumerable<ContentTypeDistributionDto> ContentTypeDistribution { get; set; } = new List<ContentTypeDistributionDto>();

        /// <summary>
        /// 审核统计
        /// </summary>
        public ModerationStatsDto ModerationStats { get; set; } = new();

        /// <summary>
        /// 近期内容趋势
        /// </summary>
        public IEnumerable<ContentTrendDto> ContentTrends { get; set; } = new List<ContentTrendDto>();
    }

    /// <summary>
    /// 待审核内容DTO
    /// </summary>
    public class PendingContentDto
    {
        /// <summary>
        /// 内容ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 作者信息
        /// </summary>
        public ContentAuthorDto Author { get; set; } = new();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// 分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 内容摘要
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// 字数统计
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// 是否包含敏感内容
        /// </summary>
        public bool HasSensitiveContent { get; set; }

        /// <summary>
        /// 风险评分
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// 审核备注
        /// </summary>
        public string? ModerationNotes { get; set; }
    }

    /// <summary>
    /// 批量审核结果DTO
    /// </summary>
    public class BatchModerationResultDto
    {
        /// <summary>
        /// 成功处理的数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败处理的数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 跳过处理的数量
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 处理结果详情
        /// </summary>
        public IEnumerable<ModerationResultDetailDto> Details { get; set; } = new List<ModerationResultDetailDto>();

        /// <summary>
        /// 总处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 内容审核详情DTO
    /// </summary>
    public class ContentModerationDetailDto
    {
        /// <summary>
        /// 内容基本信息
        /// </summary>
        public PendingContentDto ContentInfo { get; set; } = new();

        /// <summary>
        /// 完整内容
        /// </summary>
        public string FullContent { get; set; } = string.Empty;

        /// <summary>
        /// 媒体文件列表
        /// </summary>
        public IEnumerable<MediaFileDto> MediaFiles { get; set; } = new List<MediaFileDto>();

        /// <summary>
        /// 外部链接列表
        /// </summary>
        public IEnumerable<string> ExternalLinks { get; set; } = new List<string>();

        /// <summary>
        /// 自动检测结果
        /// </summary>
        public AutoModerationResultDto AutoModerationResult { get; set; } = new();

        /// <summary>
        /// 相似内容检测结果
        /// </summary>
        public IEnumerable<SimilarContentDto> SimilarContent { get; set; } = new List<SimilarContentDto>();

        /// <summary>
        /// 历史审核记录
        /// </summary>
        public IEnumerable<ModerationHistoryDto> ModerationHistory { get; set; } = new List<ModerationHistoryDto>();

        /// <summary>
        /// SEO分析结果
        /// </summary>
        public SeoAnalysisResultDto SeoAnalysis { get; set; } = new();

        /// <summary>
        /// 可读性分析
        /// </summary>
        public ReadabilityAnalysisDto ReadabilityAnalysis { get; set; } = new();
    }

    /// <summary>
    /// 批量操作结果DTO
    /// </summary>
    public class BatchOperationResultDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 成功处理的数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败处理的数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 处理详情
        /// </summary>
        public IEnumerable<BatchItemResultDto> ItemResults { get; set; } = new List<BatchItemResultDto>();

        /// <summary>
        /// 错误信息
        /// </summary>
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// 处理详情（别名，用于兼容性）
        /// </summary>
        public IEnumerable<BatchOperationDetailDto> Details { get; set; } = new List<BatchOperationDetailDto>();
    }

    /// <summary>
    /// 批量操作详细结果DTO
    /// </summary>
    public class BatchOperationDetailDto
    {
        public string ResourceId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 内容审核历史DTO
    /// </summary>
    public class ContentModerationHistoryDto
    {
        /// <summary>
        /// 审核ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 审核人
        /// </summary>
        public string ModeratorName { get; set; } = string.Empty;

        /// <summary>
        /// 审核动作
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 审核状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 审核理由
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 审核备注
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime ModeratedAt { get; set; }

        /// <summary>
        /// 变更详情
        /// </summary>
        public object? ChangeDetails { get; set; }
    }

    /// <summary>
    /// 内容搜索请求DTO
    /// </summary>
    public class ContentSearchRequestDto
    {
        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// 内容类型过滤
        /// </summary>
        public IEnumerable<string> ContentTypes { get; set; } = new List<string>();

        /// <summary>
        /// 状态过滤
        /// </summary>
        public IEnumerable<string> Status { get; set; } = new List<string>();

        /// <summary>
        /// 分类过滤
        /// </summary>
        public IEnumerable<Guid> CategoryIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 标签过滤
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 作者过滤
        /// </summary>
        public IEnumerable<Guid> AuthorIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 创建时间范围
        /// </summary>
        public DateRangeDto? CreatedDateRange { get; set; }

        /// <summary>
        /// 发布时间范围
        /// </summary>
        public DateRangeDto? PublishedDateRange { get; set; }

        /// <summary>
        /// 是否包含已删除内容
        /// </summary>
        public bool IncludeDeleted { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// 排序方向
        /// </summary>
        public string SortDirection { get; set; } = "desc";

        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 内容搜索结果DTO
    /// </summary>
    public class ContentSearchResultDto
    {
        /// <summary>
        /// 内容ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 内容类型
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// 作者
        /// </summary>
        public ContentAuthorDto Author { get; set; } = new();

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// 浏览量
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// 评论数
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 内容摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 搜索相关性评分
        /// </summary>
        public double RelevanceScore { get; set; }

        /// <summary>
        /// 高亮片段
        /// </summary>
        public IEnumerable<string> HighlightSnippets { get; set; } = new List<string>();
    }

    /// <summary>
    /// 内容统计DTO
    /// </summary>
    public class ContentStatisticsDto
    {
        /// <summary>
        /// 总内容数
        /// </summary>
        public int TotalContent { get; set; }

        /// <summary>
        /// 新增内容数
        /// </summary>
        public int NewContent { get; set; }

        /// <summary>
        /// 按状态分布
        /// </summary>
        public IEnumerable<StatusDistributionDto> StatusDistribution { get; set; } = new List<StatusDistributionDto>();

        /// <summary>
        /// 按类型分布
        /// </summary>
        public IEnumerable<ContentTypeDistributionDto> TypeDistribution { get; set; } = new List<ContentTypeDistributionDto>();

        /// <summary>
        /// 按作者分布
        /// </summary>
        public IEnumerable<AuthorDistributionDto> AuthorDistribution { get; set; } = new List<AuthorDistributionDto>();

        /// <summary>
        /// 发布趋势
        /// </summary>
        public IEnumerable<ContentTrendDto> PublishingTrends { get; set; } = new List<ContentTrendDto>();

        /// <summary>
        /// 平均字数
        /// </summary>
        public double AverageWordCount { get; set; }

        /// <summary>
        /// 平均浏览量
        /// </summary>
        public double AverageViewCount { get; set; }

        /// <summary>
        /// 平均评论数
        /// </summary>
        public double AverageCommentCount { get; set; }
    }

    /// <summary>
    /// 内容导入请求DTO
    /// </summary>
    public class ContentImportRequestDto
    {
        /// <summary>
        /// 导入格式
        /// </summary>
        public string ImportFormat { get; set; } = string.Empty;

        /// <summary>
        /// 文件数据（Base64编码）
        /// </summary>
        public string FileData { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 导入选项
        /// </summary>
        public ContentImportOptionsDto Options { get; set; } = new();

        /// <summary>
        /// 默认作者ID
        /// </summary>
        public Guid? DefaultAuthorId { get; set; }

        /// <summary>
        /// 默认分类ID
        /// </summary>
        public Guid? DefaultCategoryId { get; set; }

        /// <summary>
        /// 是否覆盖已存在内容
        /// </summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>
        /// 导入后的状态
        /// </summary>
        public string ImportStatus { get; set; } = "Draft";
    }

    /// <summary>
    /// 内容导入结果DTO
    /// </summary>
    public class ContentImportResultDto
    {
        /// <summary>
        /// 导入ID
        /// </summary>
        public Guid ImportId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 成功导入数
        /// </summary>
        public int SuccessRecords { get; set; }

        /// <summary>
        /// 失败数
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 跳过数
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 导入的记录
        /// </summary>
        public IEnumerable<ImportItemResultDto> ImportedRecords { get; set; } = new List<ImportItemResultDto>();

        /// <summary>
        /// 失败的记录
        /// </summary>
        public IEnumerable<ImportItemResultDto> FailedRecords { get; set; } = new List<ImportItemResultDto>();

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime => EndTime.HasValue && StartTime.HasValue
            ? EndTime.Value - StartTime.Value
            : TimeSpan.Zero;

        /// <summary>
        /// 导入摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 内容导入项DTO
    /// </summary>
    public class ContentImportItemDto
    {
        /// <summary>
        /// 行号
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 摘要
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 发布日期
        /// </summary>
        public DateTime? PublishDate { get; set; }

        /// <summary>
        /// 元关键词
        /// </summary>
        public string? MetaKeywords { get; set; }

        /// <summary>
        /// 元描述
        /// </summary>
        public string? MetaDescription { get; set; }
    }

    /// <summary>
    /// 内容过滤器DTO
    /// </summary>
    public class ContentFilterDto
    {
        /// <summary>
        /// 状态过滤
        /// </summary>
        public IEnumerable<string>? Statuses { get; set; }

        /// <summary>
        /// 分类ID过滤
        /// </summary>
        public IEnumerable<Guid>? CategoryIds { get; set; }

        /// <summary>
        /// 作者ID过滤
        /// </summary>
        public IEnumerable<Guid>? AuthorIds { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 关键词搜索
        /// </summary>
        public string? Keywords { get; set; }

        /// <summary>
        /// 页面大小
        /// </summary>
        public int PageSize { get; set; } = 1000;
    }

    /// <summary>
    /// 内容导出请求DTO
    /// </summary>
    public class ContentExportRequestDto
    {
        /// <summary>
        /// 导出格式
        /// </summary>
        public string ExportFormat { get; set; } = "Excel";

        /// <summary>
        /// 内容过滤条件
        /// </summary>
        public ContentFilterDto? ContentFilter { get; set; }

        /// <summary>
        /// 导出字段
        /// </summary>
        public IEnumerable<string> ExportFields { get; set; } = new List<string>();

        /// <summary>
        /// 是否包含内容正文
        /// </summary>
        public bool IncludeContent { get; set; } = true;

        /// <summary>
        /// 是否包含媒体文件
        /// </summary>
        public bool IncludeMedia { get; set; }

        /// <summary>
        /// 是否包含评论
        /// </summary>
        public bool IncludeComments { get; set; }

        /// <summary>
        /// 是否包含统计数据
        /// </summary>
        public bool IncludeStatistics { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string? FileName { get; set; }
    }

    /// <summary>
    /// 内容导出结果DTO
    /// </summary>
    public class ContentExportResultDto
    {
        /// <summary>
        /// 导出ID
        /// </summary>
        public Guid ExportId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 导出的记录数
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// 导出状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 内容SEO分析DTO
    /// </summary>
    public class ContentSeoAnalysisDto
    {
        /// <summary>
        /// SEO评分
        /// </summary>
        public int SeoScore { get; set; }

        /// <summary>
        /// 标题分析
        /// </summary>
        public SeoTitleAnalysisDto TitleAnalysis { get; set; } = new();

        /// <summary>
        /// 描述分析
        /// </summary>
        public SeoDescriptionAnalysisDto DescriptionAnalysis { get; set; } = new();

        /// <summary>
        /// 关键词分析
        /// </summary>
        public SeoKeywordAnalysisDto KeywordAnalysis { get; set; } = new();

        /// <summary>
        /// 内容结构分析
        /// </summary>
        public SeoContentStructureDto ContentStructure { get; set; } = new();

        /// <summary>
        /// 链接分析
        /// </summary>
        public SeoLinkAnalysisDto LinkAnalysis { get; set; } = new();

        /// <summary>
        /// 图片分析
        /// </summary>
        public SeoImageAnalysisDto ImageAnalysis { get; set; } = new();

        /// <summary>
        /// 可读性分析
        /// </summary>
        public ReadabilityAnalysisDto ReadabilityAnalysis { get; set; } = new();

        /// <summary>
        /// SEO建议
        /// </summary>
        public IEnumerable<SeoRecommendationDto> Recommendations { get; set; } = new List<SeoRecommendationDto>();
    }

    /// <summary>
    /// 批量SEO优化结果DTO
    /// </summary>
    public class BatchSeoOptimizationResultDto
    {
        /// <summary>
        /// 优化任务ID
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 总数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 成功优化数量
        /// </summary>
        public int OptimizedCount { get; set; }

        /// <summary>
        /// 跳过数量
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 优化详情
        /// </summary>
        public IEnumerable<SeoOptimizationDetailDto> Details { get; set; } = new List<SeoOptimizationDetailDto>();

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// 优化摘要
        /// </summary>
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// SEO优化选项DTO
    /// </summary>
    public class SeoOptimizationOptionsDto
    {
        /// <summary>
        /// 是否优化标题
        /// </summary>
        public bool OptimizeTitle { get; set; } = true;

        /// <summary>
        /// 是否优化描述
        /// </summary>
        public bool OptimizeDescription { get; set; } = true;

        /// <summary>
        /// 是否优化关键词
        /// </summary>
        public bool OptimizeKeywords { get; set; } = true;

        /// <summary>
        /// 是否优化内容结构
        /// </summary>
        public bool OptimizeContentStructure { get; set; } = true;

        /// <summary>
        /// 是否优化图片alt标签
        /// </summary>
        public bool OptimizeImageAlt { get; set; } = true;

        /// <summary>
        /// 是否生成自动摘要
        /// </summary>
        public bool GenerateAutoSummary { get; set; }

        /// <summary>
        /// 目标关键词密度
        /// </summary>
        public double TargetKeywordDensity { get; set; } = 2.5;

        /// <summary>
        /// 最大标题长度
        /// </summary>
        public int MaxTitleLength { get; set; } = 60;

        /// <summary>
        /// 最大描述长度
        /// </summary>
        public int MaxDescriptionLength { get; set; } = 160;
    }

    /// <summary>
    /// 重复内容DTO
    /// </summary>
    public class DuplicateContentDto
    {
        /// <summary>
        /// 原始内容ID
        /// </summary>
        public Guid OriginalContentId { get; set; }

        /// <summary>
        /// 重复内容ID
        /// </summary>
        public Guid DuplicateContentId { get; set; }

        /// <summary>
        /// 原始内容标题
        /// </summary>
        public string OriginalTitle { get; set; } = string.Empty;

        /// <summary>
        /// 重复内容标题
        /// </summary>
        public string DuplicateTitle { get; set; } = string.Empty;

        /// <summary>
        /// 相似度评分
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// 重复类型
        /// </summary>
        public string DuplicateType { get; set; } = string.Empty;

        /// <summary>
        /// 相似片段
        /// </summary>
        public IEnumerable<string> SimilarSnippets { get; set; } = new List<string>();

        /// <summary>
        /// 建议处理方式
        /// </summary>
        public string SuggestedAction { get; set; } = string.Empty;
    }

    /// <summary>
    /// 自动标签结果DTO
    /// </summary>
    public class AutoTaggingResultDto
    {
        /// <summary>
        /// 处理总数
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// 成功标签数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 跳过数
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 标签结果详情
        /// </summary>
        public IEnumerable<ContentTaggingResultDto> TaggingResults { get; set; } = new List<ContentTaggingResultDto>();

        /// <summary>
        /// 新发现的标签
        /// </summary>
        public IEnumerable<string> NewTags { get; set; } = new List<string>();

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// 标签置信度阈值
        /// </summary>
        public double ConfidenceThreshold { get; set; }
    }

    #region 辅助DTO类

    /// <summary>
    /// 内容类型分布DTO
    /// </summary>
    public class ContentTypeDistributionDto
    {
        public string ContentType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 审核统计DTO
    /// </summary>
    public class ModerationStatsDto
    {
        public int PendingModerations { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedToday { get; set; }
        public double AverageProcessingTime { get; set; }
        public IEnumerable<ModeratorPerformanceDto> ModeratorPerformance { get; set; } = new List<ModeratorPerformanceDto>();
    }

    /// <summary>
    /// 内容趋势DTO
    /// </summary>
    public class ContentTrendDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 内容作者DTO
    /// </summary>
    public class ContentAuthorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 审核结果详情DTO
    /// </summary>
    public class ModerationResultDetailDto
    {
        public Guid ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// 媒体文件DTO
    /// </summary>
    public class MediaFileDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public string Directory { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string AccessLevel { get; set; } = string.Empty;
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsInUse { get; set; }
        public int ReferenceCount { get; set; }
        public long AccessCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public string UploaderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 自动审核结果DTO
    /// </summary>
    public class AutoModerationResultDto
    {
        public double ToxicityScore { get; set; }
        public double SpamScore { get; set; }
        public bool ContainsProfanity { get; set; }
        public bool ContainsSensitiveContent { get; set; }
        public IEnumerable<string> DetectedIssues { get; set; } = new List<string>();
        public string OverallAssessment { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// 相似内容DTO
    /// </summary>
    public class SimilarContentDto
    {
        public Guid ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// 审核历史DTO
    /// </summary>
    public class ModerationHistoryDto
    {
        public Guid Id { get; set; }
        public string ModeratorName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime ModeratedAt { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// SEO分析结果DTO
    /// </summary>
    public class SeoAnalysisResultDto
    {
        public int SeoScore { get; set; }
        public string TitleOptimization { get; set; } = string.Empty;
        public string MetaDescriptionOptimization { get; set; } = string.Empty;
        public string KeywordOptimization { get; set; } = string.Empty;
        public string ContentStructure { get; set; } = string.Empty;
        public IEnumerable<string> Recommendations { get; set; } = new List<string>();
    }

    /// <summary>
    /// 可读性分析DTO
    /// </summary>
    public class ReadabilityAnalysisDto
    {
        public double ReadabilityScore { get; set; }
        public string ReadingLevel { get; set; } = string.Empty;
        public double AverageSentenceLength { get; set; }
        public double AverageWordsPerSentence { get; set; }
        public double ComplexWordsPercentage { get; set; }
        public IEnumerable<string> ReadabilityIssues { get; set; } = new List<string>();
        public IEnumerable<string> Improvements { get; set; } = new List<string>();
    }

    /// <summary>
    /// 批量项结果DTO
    /// </summary>
    public class BatchItemResultDto
    {
        public Guid ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? ResultData { get; set; }
    }

    /// <summary>
    /// 状态分布DTO
    /// </summary>
    public class StatusDistributionDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 作者分布DTO
    /// </summary>
    public class AuthorDistributionDto
    {
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int ContentCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 内容导入选项DTO
    /// </summary>
    public class ContentImportOptionsDto
    {
        public bool ValidateBeforeImport { get; set; } = true;
        public bool AutoGenerateTags { get; set; }
        public bool PreserveTimestamps { get; set; } = true;
        public bool ImportMedia { get; set; }
        public bool AutoOptimizeSeo { get; set; }
        public string DefaultStatus { get; set; } = "Draft";
    }

    /// <summary>
    /// 导入项结果DTO
    /// </summary>
    public class ImportItemResultDto
    {
        public int RowNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Success { get; set; }
        public Guid? ContentId { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// SEO标题分析DTO
    /// </summary>
    public class SeoTitleAnalysisDto
    {
        public int Length { get; set; }
        public bool IsOptimalLength { get; set; }
        public bool ContainsKeyword { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO描述分析DTO
    /// </summary>
    public class SeoDescriptionAnalysisDto
    {
        public int Length { get; set; }
        public bool IsOptimalLength { get; set; }
        public bool ContainsKeyword { get; set; }
        public bool IsUnique { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO关键词分析DTO
    /// </summary>
    public class SeoKeywordAnalysisDto
    {
        public string PrimaryKeyword { get; set; } = string.Empty;
        public double KeywordDensity { get; set; }
        public bool IsOptimalDensity { get; set; }
        public IEnumerable<string> RelatedKeywords { get; set; } = new List<string>();
        public IEnumerable<string> MissingKeywords { get; set; } = new List<string>();
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO内容结构DTO
    /// </summary>
    public class SeoContentStructureDto
    {
        public int HeadingCount { get; set; }
        public bool HasH1 { get; set; }
        public bool HasProperHeadingHierarchy { get; set; }
        public int WordCount { get; set; }
        public bool IsOptimalLength { get; set; }
        public int ParagraphCount { get; set; }
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO链接分析DTO
    /// </summary>
    public class SeoLinkAnalysisDto
    {
        public int InternalLinks { get; set; }
        public int ExternalLinks { get; set; }
        public bool HasOutboundLinks { get; set; }
        public IEnumerable<string> BrokenLinks { get; set; } = new List<string>();
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO图片分析DTO
    /// </summary>
    public class SeoImageAnalysisDto
    {
        public int TotalImages { get; set; }
        public int ImagesWithAlt { get; set; }
        public int ImagesWithoutAlt { get; set; }
        public double AltTextCoverage { get; set; }
        public IEnumerable<string> OptimizationSuggestions { get; set; } = new List<string>();
        public int Score { get; set; }
    }

    /// <summary>
    /// SEO建议DTO
    /// </summary>
    public class SeoRecommendationDto
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int ImpactScore { get; set; }
    }

    /// <summary>
    /// SEO优化详情DTO
    /// </summary>
    public class SeoOptimizationDetailDto
    {
        public Guid ContentId { get; set; }
        public string ContentTitle { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<string> Changes { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public int ScoreImprovement { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 内容标签结果DTO
    /// </summary>
    public class ContentTaggingResultDto
    {
        public Guid ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public IEnumerable<TagSuggestionDto> SuggestedTags { get; set; } = new List<TagSuggestionDto>();
        public IEnumerable<string> AppliedTags { get; set; } = new List<string>();
        public bool Success { get; set; }
    }

    /// <summary>
    /// 标签建议DTO
    /// </summary>
    public class TagSuggestionDto
    {
        public string Tag { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool Applied { get; set; }
    }

    /// <summary>
    /// 审核员表现DTO
    /// </summary>
    public class ModeratorPerformanceDto
    {
        public Guid ModeratorId { get; set; }
        public string ModeratorName { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public double AverageProcessingTime { get; set; }
        public double AccuracyRate { get; set; }
    }

    /// <summary>
    /// 分页结果DTO
    /// </summary>
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    #endregion
}