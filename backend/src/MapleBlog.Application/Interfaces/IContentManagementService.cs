using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 内容管理服务接口
    /// </summary>
    public interface IContentManagementService
    {
        /// <summary>
        /// 获取内容管理概览
        /// </summary>
        /// <returns>内容管理概览数据</returns>
        Task<ContentManagementOverviewDto> GetOverviewAsync();

        /// <summary>
        /// 获取待审核内容列表
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>待审核内容分页列表</returns>
        Task<PagedResultDto<PendingContentDto>> GetPendingContentAsync(int pageNumber = 1, int pageSize = 20, string? contentType = null);

        /// <summary>
        /// 批量审核内容
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="action">审核动作（approve, reject, hold）</param>
        /// <param name="reason">审核理由</param>
        /// <param name="userId">审核人ID</param>
        /// <returns>批量审核结果</returns>
        Task<BatchModerationResultDto> BatchModerateContentAsync(IEnumerable<Guid> contentIds, string action, string? reason, Guid userId);

        /// <summary>
        /// 审核单个内容
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="action">审核动作</param>
        /// <param name="reason">审核理由</param>
        /// <param name="userId">审核人ID</param>
        /// <returns>审核结果</returns>
        Task<bool> ModerateContentAsync(Guid contentId, string contentType, string action, string? reason, Guid userId);

        /// <summary>
        /// 获取内容详细信息用于审核
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>内容详细信息</returns>
        Task<ContentModerationDetailDto?> GetContentForModerationAsync(Guid contentId, string contentType);

        /// <summary>
        /// 批量删除内容
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="softDelete">是否软删除</param>
        /// <param name="userId">操作人ID</param>
        /// <returns>批量删除结果</returns>
        Task<BatchOperationResultDto> BatchDeleteContentAsync(IEnumerable<Guid> contentIds, string contentType, bool softDelete, Guid userId);

        /// <summary>
        /// 批量恢复已删除内容
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="userId">操作人ID</param>
        /// <returns>批量恢复结果</returns>
        Task<BatchOperationResultDto> BatchRestoreContentAsync(IEnumerable<Guid> contentIds, string contentType, Guid userId);

        /// <summary>
        /// 批量更新内容状态
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="contentType">内容类型</param>
        /// <param name="status">新状态</param>
        /// <param name="userId">操作人ID</param>
        /// <returns>批量更新结果</returns>
        Task<BatchOperationResultDto> BatchUpdateContentStatusAsync(IEnumerable<Guid> contentIds, string contentType, string status, Guid userId);

        /// <summary>
        /// 获取内容审核历史
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>审核历史列表</returns>
        Task<IEnumerable<ContentModerationHistoryDto>> GetContentModerationHistoryAsync(Guid contentId, string contentType);

        /// <summary>
        /// 搜索内容
        /// </summary>
        /// <param name="searchRequest">搜索请求参数</param>
        /// <returns>搜索结果</returns>
        Task<PagedResultDto<ContentSearchResultDto>> SearchContentAsync(ContentSearchRequestDto searchRequest);

        /// <summary>
        /// 获取内容统计信息
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>内容统计信息</returns>
        Task<ContentStatisticsDto> GetContentStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 批量导入内容
        /// </summary>
        /// <param name="importRequest">导入请求参数</param>
        /// <returns>导入结果</returns>
        Task<ContentImportResultDto> BatchImportContentAsync(ContentImportRequestDto importRequest);

        /// <summary>
        /// 批量导出内容
        /// </summary>
        /// <param name="exportRequest">导出请求参数</param>
        /// <returns>导出结果</returns>
        Task<ContentExportResultDto> BatchExportContentAsync(ContentExportRequestDto exportRequest);

        /// <summary>
        /// 获取内容SEO分析
        /// </summary>
        /// <param name="contentId">内容ID</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>SEO分析结果</returns>
        Task<ContentSeoAnalysisDto> GetContentSeoAnalysisAsync(Guid contentId, string contentType);

        /// <summary>
        /// 批量优化内容SEO
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="optimizationOptions">优化选项</param>
        /// <returns>优化结果</returns>
        Task<BatchSeoOptimizationResultDto> BatchOptimizeContentSeoAsync(IEnumerable<Guid> contentIds, SeoOptimizationOptionsDto optimizationOptions);

        /// <summary>
        /// 获取重复内容检测结果
        /// </summary>
        /// <param name="threshold">相似度阈值</param>
        /// <returns>重复内容列表</returns>
        Task<IEnumerable<DuplicateContentDto>> GetDuplicateContentAsync(double threshold = 0.8);

        /// <summary>
        /// 自动标签内容
        /// </summary>
        /// <param name="contentIds">内容ID列表</param>
        /// <param name="contentType">内容类型</param>
        /// <returns>自动标签结果</returns>
        Task<AutoTaggingResultDto> AutoTagContentAsync(IEnumerable<Guid> contentIds, string contentType);
    }
}