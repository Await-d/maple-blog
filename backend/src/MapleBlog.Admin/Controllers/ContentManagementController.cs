using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.BulkOperation;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using MapleBlog.Admin.DTOs;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// 内容管理控制器 - 提供全面的内容管理功能
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    public class ContentManagementController : BaseAdminController
    {
        private readonly IContentManagementService _contentManagementService;
        private readonly IBlogService _blogService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IUserManagementService _userManagementService;
        private readonly ISearchService _searchService;
        private readonly ICommentModerationService _commentModerationService;
        private readonly IMemoryCache _memoryCache;
        private readonly IMapper _mapper;

        public ContentManagementController(
            ILogger<ContentManagementController> logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService,
            IContentManagementService contentManagementService,
            IBlogService blogService,
            ICategoryService categoryService,
            ITagService tagService,
            IUserManagementService userManagementService,
            ISearchService searchService,
            ICommentModerationService commentModerationService,
            IMemoryCache memoryCache,
            IMapper mapper)
            : base(logger, permissionService, auditLogService)
        {
            _contentManagementService = contentManagementService ?? throw new ArgumentNullException(nameof(contentManagementService));
            _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));
            _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _commentModerationService = commentModerationService ?? throw new ArgumentNullException(nameof(commentModerationService));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region 内容概览和统计

        /// <summary>
        /// 获取内容管理概览
        /// </summary>
        /// <returns>内容管理概览数据</returns>
        [HttpGet("overview")]
        public async Task<IActionResult> GetContentOverview()
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var cacheKey = "content_overview";
                if (_memoryCache.TryGetValue(cacheKey, out ContentManagementOverviewDto? cachedOverview))
                {
                    return Success(cachedOverview);
                }

                var overview = await _contentManagementService.GetContentOverviewAsync();
                
                _memoryCache.Set(cacheKey, overview, TimeSpan.FromMinutes(5));

                await LogAuditAsync("VIEW", "ContentOverview", description: "查看内容管理概览");

                return Success(overview);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取内容概览");
            }
        }

        /// <summary>
        /// 获取内容统计数据
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="groupBy">分组方式 (day/week/month)</param>
        /// <returns>内容统计数据</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetContentStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string groupBy = "day")
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var cacheKey = $"content_statistics_{startDate}_{endDate}_{groupBy}";
                if (_memoryCache.TryGetValue(cacheKey, out ContentStatisticsDto? cachedStats))
                {
                    return Success(cachedStats);
                }

                var statistics = await _contentManagementService.GetContentStatisticsAsync(
                    startDate ?? DateTime.UtcNow.AddDays(-30),
                    endDate ?? DateTime.UtcNow,
                    groupBy);

                _memoryCache.Set(cacheKey, statistics, TimeSpan.FromMinutes(10));

                await LogAuditAsync("VIEW", "ContentStatistics", description: $"查看内容统计数据 ({groupBy})");

                return Success(statistics);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取内容统计");
            }
        }

        #endregion

        #region 内容搜索和筛选

        /// <summary>
        /// 高级内容搜索
        /// </summary>
        /// <param name="request">搜索请求</param>
        /// <returns>搜索结果</returns>
        [HttpPost("search")]
        public async Task<IActionResult> SearchContent([FromBody] ContentSearchRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var searchResults = await _contentManagementService.SearchContentAsync(request);

                await LogAuditAsync("SEARCH", "Content", description: $"搜索内容: {request.SearchTerm}");

                return SuccessWithPagination(
                    searchResults.Items,
                    searchResults.TotalCount,
                    request.PageNumber,
                    request.PageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "内容搜索");
            }
        }

        /// <summary>
        /// 获取内容列表（带筛选）
        /// </summary>
        /// <param name="status">状态筛选</param>
        /// <param name="categoryId">分类筛选</param>
        /// <param name="authorId">作者筛选</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="sortBy">排序字段</param>
        /// <param name="sortDirection">排序方向</param>
        /// <returns>内容列表</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetContentList(
            [FromQuery] string? status = null,
            [FromQuery] Guid? categoryId = null,
            [FromQuery] Guid? authorId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var searchRequest = new ContentSearchRequestDto
                {
                    Status = string.IsNullOrEmpty(status) ? new List<string>() : new[] { status },
                    CategoryIds = categoryId.HasValue ? new[] { categoryId.Value } : new List<Guid>(),
                    AuthorIds = authorId.HasValue ? new[] { authorId.Value } : new List<Guid>(),
                    CreatedDateRange = startDate.HasValue && endDate.HasValue
                        ? new DateRangeDto { StartDate = startDate.Value, EndDate = endDate.Value }
                        : null,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                var result = await _contentManagementService.SearchContentAsync(searchRequest);

                return SuccessWithPagination(
                    result.Items,
                    result.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取内容列表");
            }
        }

        /// <summary>
        /// 获取待审核内容
        /// </summary>
        /// <param name="priority">优先级筛选</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>待审核内容列表</returns>
        [HttpGet("pending-review")]
        public async Task<IActionResult> GetPendingContent(
            [FromQuery] string? priority = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.moderate");
                if (permissionCheck != null) return permissionCheck;

                var pendingContent = await _contentManagementService.GetPendingContentAsync(
                    priority, pageNumber, pageSize);

                return SuccessWithPagination(
                    pendingContent.Items,
                    pendingContent.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取待审核内容");
            }
        }

        #endregion

        #region 内容审核和工作流

        /// <summary>
        /// 获取内容审核详情
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <returns>内容审核详情</returns>
        [HttpGet("moderation/{contentId:guid}")]
        public async Task<IActionResult> GetContentModerationDetail(Guid contentId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.moderate");
                if (permissionCheck != null) return permissionCheck;

                var moderationDetail = await _contentManagementService.GetContentModerationDetailAsync(contentId);
                if (moderationDetail == null)
                {
                    return NotFoundResult("内容", contentId);
                }

                await LogAuditAsync("VIEW", "ContentModeration", contentId.ToString(), "查看内容审核详情");

                return Success(moderationDetail);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取内容审核详情");
            }
        }

        /// <summary>
        /// 审核内容
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="request">审核请求</param>
        /// <returns>审核结果</returns>
        [HttpPost("moderation/{contentId:guid}/moderate")]
        public async Task<IActionResult> ModerateContent(
            Guid contentId,
            [FromBody] ContentModerationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.moderate");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.ModerateContentAsync(
                    contentId, request, CurrentUserId!.Value);

                await LogAuditAsync("MODERATE", "Content", contentId.ToString(),
                    $"审核内容: {request.Action}", null, request);

                return Success(result, "内容审核完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "审核内容");
            }
        }

        /// <summary>
        /// 批量审核内容
        /// </summary>
        /// <param name="request">批量审核请求</param>
        /// <returns>批量审核结果</returns>
        [HttpPost("moderation/batch")]
        public async Task<IActionResult> BatchModerateContent([FromBody] BatchContentModerationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.moderate");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchModerateContentAsync(
                    request, CurrentUserId!.Value);

                await LogAuditAsync("BATCH_MODERATE", "Content", null,
                    $"批量审核内容: {request.ContentIds.Count()} 项");

                return Success(result, $"批量审核完成: 成功 {result.SuccessCount} 项，失败 {result.FailCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量审核内容");
            }
        }

        /// <summary>
        /// 获取内容审核历史
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <returns>审核历史</returns>
        [HttpGet("moderation/{contentId:guid}/history")]
        public async Task<IActionResult> GetContentModerationHistory(Guid contentId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var history = await _contentManagementService.GetContentModerationHistoryAsync(contentId);

                return Success(history);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取审核历史");
            }
        }

        #endregion

        #region 内容CRUD操作

        /// <summary>
        /// 创建内容
        /// </summary>
        /// <param name="request">创建内容请求</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> CreateContent([FromBody] CreatePostDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.create");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                // 设置当前用户为作者
                request.AuthorId = CurrentUserId!.Value;

                var result = await _blogService.CreatePostAsync(request);

                await LogAuditAsync("CREATE", "Content", result.Id.ToString(),
                    $"创建内容: {request.Title}");

                return Success(result, "内容创建成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建内容");
            }
        }

        /// <summary>
        /// 更新内容
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="request">更新内容请求</param>
        /// <returns>更新结果</returns>
        [HttpPut("{contentId:guid}")]
        public async Task<IActionResult> UpdateContent(Guid contentId, [FromBody] UpdatePostDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var existingPost = await _blogService.GetPostByIdAsync(contentId);
                if (existingPost == null)
                {
                    return NotFoundResult("内容", contentId);
                }

                var result = await _blogService.UpdatePostAsync(contentId, request);

                await LogAuditAsync("UPDATE", "Content", contentId.ToString(),
                    $"更新内容: {request.Title}", existingPost, result);

                return Success(result, "内容更新成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新内容");
            }
        }

        /// <summary>
        /// 删除内容
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="permanent">是否永久删除</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{contentId:guid}")]
        public async Task<IActionResult> DeleteContent(Guid contentId, [FromQuery] bool permanent = false)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync(permanent ? "content.delete.permanent" : "content.delete");
                if (permissionCheck != null) return permissionCheck;

                var existingPost = await _blogService.GetPostByIdAsync(contentId);
                if (existingPost == null)
                {
                    return NotFoundResult("内容", contentId);
                }

                var result = await _blogService.DeletePostAsync(contentId, permanent);

                await LogAuditAsync("DELETE", "Content", contentId.ToString(),
                    $"{(permanent ? "永久" : "软")}删除内容: {existingPost.Title}");

                return Success(result, $"内容{(permanent ? "永久" : "")}删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除内容");
            }
        }

        /// <summary>
        /// 恢复已删除内容
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <returns>恢复结果</returns>
        [HttpPost("{contentId:guid}/restore")]
        public async Task<IActionResult> RestoreContent(Guid contentId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.restore");
                if (permissionCheck != null) return permissionCheck;

                var result = await _blogService.RestorePostAsync(contentId);

                await LogAuditAsync("RESTORE", "Content", contentId.ToString(), "恢复已删除内容");

                return Success(result, "内容恢复成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "恢复内容");
            }
        }

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除内容
        /// </summary>
        /// <param name="request">批量删除请求</param>
        /// <returns>批量删除结果</returns>
        [HttpPost("batch/delete")]
        public async Task<IActionResult> BatchDeleteContent([FromBody] BatchContentOperationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.delete");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchDeleteContentAsync(
                    request.ContentIds, request.Permanent);

                await LogAuditAsync("BATCH_DELETE", "Content", null,
                    $"批量删除内容: {request.ContentIds.Count()} 项");

                return Success(result, $"批量删除完成: 成功 {result.SuccessCount} 项，失败 {result.FailCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量删除内容");
            }
        }

        /// <summary>
        /// 批量更新内容状态
        /// </summary>
        /// <param name="request">批量状态更新请求</param>
        /// <returns>批量更新结果</returns>
        [HttpPost("batch/status")]
        public async Task<IActionResult> BatchUpdateContentStatus([FromBody] BatchContentStatusUpdateRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchUpdateContentStatusAsync(
                    request.ContentIds, request.Status, CurrentUserId!.Value);

                await LogAuditAsync("BATCH_UPDATE_STATUS", "Content", null,
                    $"批量更新内容状态: {request.ContentIds.Count()} 项 -> {request.Status}");

                return Success(result, $"批量状态更新完成: 成功 {result.SuccessCount} 项，失败 {result.FailCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量更新内容状态");
            }
        }

        /// <summary>
        /// 批量分配分类
        /// </summary>
        /// <param name="request">批量分类分配请求</param>
        /// <returns>批量分配结果</returns>
        [HttpPost("batch/category")]
        public async Task<IActionResult> BatchAssignCategory([FromBody] BatchContentCategoryAssignRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchAssignCategoryAsync(
                    request.ContentIds, request.CategoryId);

                await LogAuditAsync("BATCH_ASSIGN_CATEGORY", "Content", null,
                    $"批量分配分类: {request.ContentIds.Count()} 项 -> {request.CategoryId}");

                return Success(result, $"批量分类分配完成: 成功 {result.SuccessCount} 项，失败 {result.FailCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量分配分类");
            }
        }

        /// <summary>
        /// 批量添加标签
        /// </summary>
        /// <param name="request">批量标签添加请求</param>
        /// <returns>批量添加结果</returns>
        [HttpPost("batch/tags")]
        public async Task<IActionResult> BatchAddTags([FromBody] BatchContentTagsOperationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchAddTagsAsync(
                    request.ContentIds, request.TagNames);

                await LogAuditAsync("BATCH_ADD_TAGS", "Content", null,
                    $"批量添加标签: {request.ContentIds.Count()} 项");

                return Success(result, $"批量标签添加完成: 成功 {result.SuccessCount} 项，失败 {result.FailCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量添加标签");
            }
        }

        #endregion

        #region SEO优化

        /// <summary>
        /// 获取内容SEO分析
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <returns>SEO分析结果</returns>
        [HttpGet("{contentId:guid}/seo-analysis")]
        public async Task<IActionResult> GetContentSeoAnalysis(Guid contentId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var seoAnalysis = await _contentManagementService.GetContentSeoAnalysisAsync(contentId);
                if (seoAnalysis == null)
                {
                    return NotFoundResult("内容", contentId);
                }

                return Success(seoAnalysis);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取SEO分析");
            }
        }

        /// <summary>
        /// 优化内容SEO
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="options">优化选项</param>
        /// <returns>优化结果</returns>
        [HttpPost("{contentId:guid}/seo-optimize")]
        public async Task<IActionResult> OptimizeContentSeo(
            Guid contentId,
            [FromBody] SeoOptimizationOptionsDto options)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.OptimizeContentSeoAsync(contentId, options);

                await LogAuditAsync("SEO_OPTIMIZE", "Content", contentId.ToString(), "SEO优化");

                return Success(result, "SEO优化完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "SEO优化");
            }
        }

        /// <summary>
        /// 批量SEO优化
        /// </summary>
        /// <param name="request">批量SEO优化请求</param>
        /// <returns>批量优化结果</returns>
        [HttpPost("batch/seo-optimize")]
        public async Task<IActionResult> BatchOptimizeSeo([FromBody] BatchSeoOptimizationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.BatchOptimizeSeoAsync(
                    request.ContentIds, request.Options);

                await LogAuditAsync("BATCH_SEO_OPTIMIZE", "Content", null,
                    $"批量SEO优化: {request.ContentIds.Count()} 项");

                return Success(result, $"批量SEO优化完成: 成功 {result.OptimizedCount} 项，跳过 {result.SkippedCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量SEO优化");
            }
        }

        #endregion

        #region 内容分析和报告

        /// <summary>
        /// 检测重复内容
        /// </summary>
        /// <param name="threshold">相似度阈值</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>重复内容检测结果</returns>
        [HttpGet("duplicate-detection")]
        public async Task<IActionResult> DetectDuplicateContent(
            [FromQuery] double threshold = 0.8,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var duplicates = await _contentManagementService.DetectDuplicateContentAsync(
                    threshold, pageNumber, pageSize);

                return SuccessWithPagination(
                    duplicates.Items,
                    duplicates.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "重复内容检测");
            }
        }

        /// <summary>
        /// 自动标签建议
        /// </summary>
        /// <param name="request">自动标签请求</param>
        /// <returns>标签建议结果</returns>
        [HttpPost("auto-tagging")]
        public async Task<IActionResult> AutoTagContent([FromBody] AutoTaggingRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.update");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.AutoTagContentAsync(
                    request.ContentIds, request.ConfidenceThreshold, request.MaxTags);

                await LogAuditAsync("AUTO_TAG", "Content", null,
                    $"自动标签: {request.ContentIds.Count()} 项");

                return Success(result, $"自动标签完成: 成功 {result.SuccessCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "自动标签");
            }
        }

        #endregion

        #region 内容导入导出

        /// <summary>
        /// 导入内容
        /// </summary>
        /// <param name="request">内容导入请求</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        public async Task<IActionResult> ImportContent([FromBody] ContentImportRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.import");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.ImportContentAsync(request, CurrentUserId!.Value);

                await LogAuditAsync("IMPORT", "Content", null,
                    $"导入内容: {result.TotalRecords} 条记录");

                return Success(result, $"内容导入完成: 成功 {result.SuccessRecords} 项，失败 {result.FailedCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导入内容");
            }
        }

        /// <summary>
        /// 导出内容
        /// </summary>
        /// <param name="request">内容导出请求</param>
        /// <returns>导出结果</returns>
        [HttpPost("export")]
        public async Task<IActionResult> ExportContent([FromBody] ContentExportRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.export");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.ExportContentAsync(request);

                await LogAuditAsync("EXPORT", "Content", null,
                    $"导出内容: {result.RecordCount} 条记录");

                return Success(result, "内容导出完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导出内容");
            }
        }

        #endregion

        #region 内容调度和发布

        /// <summary>
        /// 设置定时发布
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="request">定时发布请求</param>
        /// <returns>设置结果</returns>
        [HttpPost("{contentId:guid}/schedule")]
        public async Task<IActionResult> ScheduleContent(
            Guid contentId,
            [FromBody] ContentScheduleRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.schedule");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _contentManagementService.ScheduleContentAsync(
                    contentId, request.ScheduledTime, CurrentUserId!.Value);

                await LogAuditAsync("SCHEDULE", "Content", contentId.ToString(),
                    $"设置定时发布: {request.ScheduledTime}");

                return Success(result, "定时发布设置成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "设置定时发布");
            }
        }

        /// <summary>
        /// 取消定时发布
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <returns>取消结果</returns>
        [HttpDelete("{contentId:guid}/schedule")]
        public async Task<IActionResult> CancelScheduledContent(Guid contentId)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.schedule");
                if (permissionCheck != null) return permissionCheck;

                var result = await _contentManagementService.CancelScheduledContentAsync(contentId);

                await LogAuditAsync("CANCEL_SCHEDULE", "Content", contentId.ToString(), "取消定时发布");

                return Success(result, "定时发布取消成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "取消定时发布");
            }
        }

        /// <summary>
        /// 获取定时发布列表
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>定时发布列表</returns>
        [HttpGet("scheduled")]
        public async Task<IActionResult> GetScheduledContent(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("content.read");
                if (permissionCheck != null) return permissionCheck;

                var scheduledContent = await _contentManagementService.GetScheduledContentAsync(
                    pageNumber, pageSize);

                return SuccessWithPagination(
                    scheduledContent.Items,
                    scheduledContent.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取定时发布列表");
            }
        }

        #endregion

        #region 评论管理

        /// <summary>
        /// 获取内容评论
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>评论列表</returns>
        [HttpGet("{contentId:guid}/comments")]
        public async Task<IActionResult> GetContentComments(
            Guid contentId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("comment.read");
                if (permissionCheck != null) return permissionCheck;

                var comments = await _commentModerationService.GetPostCommentsAsync(
                    contentId, pageNumber, pageSize);

                return SuccessWithPagination(
                    comments.Items,
                    comments.TotalCount,
                    pageNumber,
                    pageSize);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取内容评论");
            }
        }

        /// <summary>
        /// 批量管理评论
        /// </summary>
        /// <param name="request">批量评论管理请求</param>
        /// <returns>批量管理结果</returns>
        [HttpPost("comments/batch")]
        public async Task<IActionResult> BatchManageComments([FromBody] BatchCommentModerationRequestDto request)
        {
            try
            {
                var permissionCheck = await ValidatePermissionAsync("comment.moderate");
                if (permissionCheck != null) return permissionCheck;

                var validationResult = ValidateModelState();
                if (validationResult != null) return validationResult;

                var result = await _commentModerationService.BatchModerateCommentsAsync(
                    request.CommentIds, request.Action, request.Reason, CurrentUserId!.Value);

                await LogAuditAsync("BATCH_MODERATE_COMMENTS", "Comment", null,
                    $"批量管理评论: {request.CommentIds.Count()} 项 - {request.Action}");

                return Success(result, $"批量评论管理完成: 成功 {result.ProcessedCount} 项");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量管理评论");
            }
        }

        #endregion
    }

    #region DTO类定义

    /// <summary>
    /// 内容审核请求DTO
    /// </summary>
    public class ContentModerationRequestDto
    {
        [Required]
        public string Action { get; set; } = string.Empty; // approve, reject, request_changes

        public string? Reason { get; set; }

        public string? Notes { get; set; }

        public DateTime? ScheduledTime { get; set; }
    }

    /// <summary>
    /// 批量内容审核请求DTO
    /// </summary>
    public class BatchContentModerationRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? Reason { get; set; }

        public string? Notes { get; set; }
    }

    /// <summary>
    /// 批量内容操作请求DTO
    /// </summary>
    public class BatchContentOperationRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        public bool Permanent { get; set; } = false;
    }

    /// <summary>
    /// 批量内容状态更新请求DTO
    /// </summary>
    public class BatchContentStatusUpdateRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Required]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量内容分类分配请求DTO
    /// </summary>
    public class BatchContentCategoryAssignRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Required]
        public Guid CategoryId { get; set; }
    }

    /// <summary>
    /// 批量内容标签操作请求DTO
    /// </summary>
    public class BatchContentTagsOperationRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Required]
        public IEnumerable<string> TagNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// 批量SEO优化请求DTO
    /// </summary>
    public class BatchSeoOptimizationRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Required]
        public SeoOptimizationOptionsDto Options { get; set; } = new();
    }

    /// <summary>
    /// 自动标签请求DTO
    /// </summary>
    public class AutoTaggingRequestDto
    {
        [Required]
        public IEnumerable<Guid> ContentIds { get; set; } = new List<Guid>();

        [Range(0.0, 1.0)]
        public double ConfidenceThreshold { get; set; } = 0.7;

        [Range(1, 20)]
        public int MaxTags { get; set; } = 10;
    }

    /// <summary>
    /// 内容调度请求DTO
    /// </summary>
    public class ContentScheduleRequestDto
    {
        [Required]
        public DateTime ScheduledTime { get; set; }
    }

    /// <summary>
    /// 批量评论审核请求DTO
    /// </summary>
    public class BatchCommentModerationRequestDto
    {
        [Required]
        public IEnumerable<Guid> CommentIds { get; set; } = new List<Guid>();

        [Required]
        public string Action { get; set; } = string.Empty; // approve, reject, delete

        public string? Reason { get; set; }
    }



    #endregion
}