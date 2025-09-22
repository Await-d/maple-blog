using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 数据分析服务接口
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// 获取网站访问分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>访问分析数据</returns>
        Task<WebsiteAnalyticsDto> GetWebsiteAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取用户行为分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>用户行为分析数据</returns>
        Task<UserBehaviorAnalyticsDto> GetUserBehaviorAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取内容表现分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>内容表现分析数据</returns>
        Task<ContentPerformanceAnalyticsDto> GetContentPerformanceAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取搜索关键词分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>搜索关键词分析数据</returns>
        Task<IEnumerable<SearchKeywordAnalyticsDto>> GetSearchKeywordAnalyticsAsync(DateTime startDate, DateTime endDate, int limit = 50);

        /// <summary>
        /// 获取地理位置分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>地理位置分析数据</returns>
        Task<IEnumerable<GeographicAnalyticsDto>> GetGeographicAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取设备和浏览器分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>设备和浏览器分析数据</returns>
        Task<DeviceBrowserAnalyticsDto> GetDeviceBrowserAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取流量来源分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>流量来源分析数据</returns>
        Task<IEnumerable<TrafficSourceAnalyticsDto>> GetTrafficSourceAnalyticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取用户留存分析
        /// </summary>
        /// <param name="cohortDate">队列日期</param>
        /// <param name="periodType">周期类型（daily, weekly, monthly）</param>
        /// <returns>用户留存分析数据</returns>
        Task<UserRetentionAnalyticsDto> GetUserRetentionAnalyticsAsync(DateTime cohortDate, string periodType = "weekly");

        /// <summary>
        /// 获取转化漏斗分析
        /// </summary>
        /// <param name="funnelSteps">漏斗步骤</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>转化漏斗分析数据</returns>
        Task<ConversionFunnelDto> GetConversionFunnelAsync(IEnumerable<string> funnelSteps, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取A/B测试结果分析
        /// </summary>
        /// <param name="testId">测试ID</param>
        /// <returns>A/B测试结果数据</returns>
        Task<ABTestResultDto> GetABTestResultAsync(Guid testId);

        /// <summary>
        /// 生成定制报告
        /// </summary>
        /// <param name="reportRequest">报告请求参数</param>
        /// <returns>报告数据</returns>
        Task<CustomReportDto> GenerateCustomReportAsync(CustomReportRequestDto reportRequest);

        /// <summary>
        /// 导出分析数据
        /// </summary>
        /// <param name="exportRequest">导出请求参数</param>
        /// <returns>导出文件数据</returns>
        Task<ExportResultDto> ExportAnalyticsDataAsync(ExportRequestDto exportRequest);

        /// <summary>
        /// 获取实时访客数据
        /// </summary>
        /// <returns>实时访客数据</returns>
        Task<RealTimeVisitorDto> GetRealTimeVisitorsAsync();

        /// <summary>
        /// 获取性能指标分析
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>性能指标分析数据</returns>
        Task<PerformanceMetricsDto> GetPerformanceMetricsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取目标完成情况分析
        /// </summary>
        /// <param name="goalId">目标ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>目标完成情况数据</returns>
        Task<GoalCompletionDto> GetGoalCompletionAsync(Guid goalId, DateTime startDate, DateTime endDate);
    }
}