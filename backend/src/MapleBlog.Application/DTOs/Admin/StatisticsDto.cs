namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 小时统计DTO
    /// </summary>
    public class HourlyStatsDto
    {
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime Hour { get; set; }

        /// <summary>
        /// 访问量
        /// </summary>
        public long Visits { get; set; }

        /// <summary>
        /// 独立访客数
        /// </summary>
        public int UniqueVisitors { get; set; }

        /// <summary>
        /// 页面浏览量
        /// </summary>
        public long PageViews { get; set; }

        /// <summary>
        /// 新用户数
        /// </summary>
        public int NewUsers { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }
    }

    /// <summary>
    /// 日访问统计DTO
    /// </summary>
    public class DailyVisitStatsDto
    {
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 页面浏览量
        /// </summary>
        public long PageViews { get; set; }

        /// <summary>
        /// 独立访客数
        /// </summary>
        public int UniqueVisitors { get; set; }

        /// <summary>
        /// 新访客数
        /// </summary>
        public int NewVisitors { get; set; }

        /// <summary>
        /// 回访客数
        /// </summary>
        public int ReturningVisitors { get; set; }

        /// <summary>
        /// 会话数
        /// </summary>
        public int Sessions { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 转化率
        /// </summary>
        public double ConversionRate { get; set; }

        /// <summary>
        /// 总访问量
        /// </summary>
        public long TotalViews { get; set; }
    }
}