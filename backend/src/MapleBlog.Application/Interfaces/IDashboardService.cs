using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 仪表盘服务接口
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// 获取仪表盘概览数据
        /// </summary>
        /// <returns>仪表盘概览数据</returns>
        Task<DashboardOverviewDto> GetOverviewAsync();

        /// <summary>
        /// 获取实时统计数据
        /// </summary>
        /// <returns>实时统计数据</returns>
        Task<RealTimeStatsDto> GetRealTimeStatsAsync();

        /// <summary>
        /// 获取系统性能监控数据
        /// </summary>
        /// <param name="timeRange">时间范围（小时）</param>
        /// <returns>性能监控数据</returns>
        Task<SystemPerformanceDto> GetSystemPerformanceAsync(int timeRange = 24);

        /// <summary>
        /// 获取内容统计数据
        /// </summary>
        /// <param name="days">天数范围</param>
        /// <returns>内容统计数据</returns>
        Task<ContentStatsDto> GetContentStatsAsync(int days = 30);

        /// <summary>
        /// 获取用户活跃度数据
        /// </summary>
        /// <param name="days">天数范围</param>
        /// <returns>用户活跃度数据</returns>
        Task<UserActivityDto> GetUserActivityAsync(int days = 30);

        /// <summary>
        /// 获取热门内容排行
        /// </summary>
        /// <param name="limit">数量限制</param>
        /// <param name="days">天数范围</param>
        /// <returns>热门内容列表</returns>
        Task<IEnumerable<PopularContentDto>> GetPopularContentAsync(int limit = 10, int days = 7);

        /// <summary>
        /// 获取近期操作日志
        /// </summary>
        /// <param name="limit">数量限制</param>
        /// <returns>操作日志列表</returns>
        Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 20);

        /// <summary>
        /// 获取系统告警信息
        /// </summary>
        /// <returns>告警信息列表</returns>
        Task<IEnumerable<SystemAlertDto>> GetSystemAlertsAsync();

        /// <summary>
        /// 获取待处理任务数量
        /// </summary>
        /// <returns>待处理任务统计</returns>
        Task<PendingTasksDto> GetPendingTasksAsync();

        /// <summary>
        /// 获取网站访问趋势
        /// </summary>
        /// <param name="days">天数范围</param>
        /// <returns>访问趋势数据</returns>
        Task<IEnumerable<VisitTrendDto>> GetVisitTrendsAsync(int days = 30);

        /// <summary>
        /// 获取系统健康检查结果
        /// </summary>
        /// <returns>健康检查结果</returns>
        Task<SystemHealthDto> GetSystemHealthAsync();

        /// <summary>
        /// 标记告警为已读
        /// </summary>
        /// <param name="alertId">告警ID</param>
        /// <param name="userId">用户ID</param>
        /// <returns>操作结果</returns>
        Task<bool> MarkAlertAsReadAsync(Guid alertId, Guid userId);

        /// <summary>
        /// 清除过期数据
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <returns>清除结果</returns>
        Task<DataCleanupResultDto> CleanupExpiredDataAsync(int days = 90);
    }
}