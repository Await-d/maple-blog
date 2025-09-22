using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using System.Diagnostics;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 仪表盘服务实现
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IStatsService _statsService;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<DashboardService> _logger;

        private const int CACHE_DURATION_MINUTES = 5;
        private const string CACHE_KEY_PREFIX = "dashboard:";

        public DashboardService(
            IUserRepository userRepository,
            IPostRepository postRepository,
            ICommentRepository commentRepository,
            IAuditLogService auditLogService,
            IStatsService statsService,
            IMemoryCache cache,
            IMapper mapper,
            ILogger<DashboardService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取仪表盘概览数据
        /// </summary>
        public async Task<DashboardOverviewDto> GetOverviewAsync()
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}overview";

                if (_cache.TryGetValue(cacheKey, out DashboardOverviewDto? cachedOverview) && cachedOverview != null)
                {
                    return cachedOverview;
                }

                var today = DateTime.UtcNow.Date;

                // 并行获取各种统计数据
                var totalUsersTask = GetTotalUsersAsync();
                var todayNewUsersTask = GetTodayNewUsersAsync(today);
                var totalPostsTask = GetTotalPostsAsync();
                var todayNewPostsTask = GetTodayNewPostsAsync(today);
                var totalCommentsTask = GetTotalCommentsAsync();
                var todayNewCommentsTask = GetTodayNewCommentsAsync(today);
                var totalViewsTask = GetTotalViewsAsync();
                var todayViewsTask = GetTodayViewsAsync(today);
                var onlineUsersTask = GetOnlineUsersAsync();
                var systemStatusTask = GetSystemStatusAsync();

                await Task.WhenAll(
                    totalUsersTask,
                    todayNewUsersTask,
                    totalPostsTask,
                    todayNewPostsTask,
                    totalCommentsTask,
                    todayNewCommentsTask,
                    totalViewsTask,
                    todayViewsTask,
                    onlineUsersTask,
                    systemStatusTask
                );

                var overview = new DashboardOverviewDto
                {
                    TotalUsers = await totalUsersTask,
                    TodayNewUsers = await todayNewUsersTask,
                    TotalPosts = await totalPostsTask,
                    TodayNewPosts = await todayNewPostsTask,
                    TotalComments = await totalCommentsTask,
                    TodayNewComments = await todayNewCommentsTask,
                    TotalViews = await totalViewsTask,
                    TodayViews = await todayViewsTask,
                    OnlineUsers = await onlineUsersTask,
                    SystemStatus = await systemStatusTask
                };

                _cache.Set(cacheKey, overview, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取仪表盘概览数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取实时统计数据
        /// </summary>
        public async Task<RealTimeStatsDto> GetRealTimeStatsAsync()
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}realtime";

                if (_cache.TryGetValue(cacheKey, out RealTimeStatsDto? cachedStats) && cachedStats != null)
                {
                    return cachedStats;
                }

                var now = DateTime.UtcNow;
                var today = now.Date;
                var hourAgo = now.AddHours(-1);

                var stats = new RealTimeStatsDto
                {
                    CurrentOnlineUsers = await GetOnlineUsersAsync(),
                    TodayPageViews = await GetTodayViewsAsync(today),
                    TodayUniqueVisitors = await GetTodayUniqueVisitorsAsync(today),
                    CurrentCpuUsage = await GetCurrentCpuUsageAsync(),
                    CurrentMemoryUsage = await GetCurrentMemoryUsageAsync(),
                    HourlyTrends = await GetHourlyTrendsAsync(hourAgo, now),
                    LatestActivities = await GetLatestActivitiesAsync(10)
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(1)); // 实时数据缓存时间较短
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时统计数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取系统性能监控数据
        /// </summary>
        public async Task<SystemPerformanceDto> GetSystemPerformanceAsync(int timeRange = 24)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}performance:{timeRange}";

                if (_cache.TryGetValue(cacheKey, out SystemPerformanceDto? cachedPerformance) && cachedPerformance != null)
                {
                    return cachedPerformance;
                }

                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddHours(-timeRange);

                var performance = new SystemPerformanceDto
                {
                    CpuUsage = await GetCpuUsageHistoryAsync(startTime, endTime),
                    MemoryUsage = await GetMemoryUsageHistoryAsync(startTime, endTime),
                    DiskUsage = await GetCurrentDiskUsageAsync(),
                    NetworkIo = await GetNetworkIoStatsAsync(startTime, endTime),
                    DatabaseStats = await GetDatabaseStatsAsync(),
                    CacheHitRate = await GetCacheHitRateAsync(),
                    ResponseTimes = await GetResponseTimeStatsAsync(startTime, endTime)
                };

                _cache.Set(cacheKey, performance, TimeSpan.FromMinutes(10));
                return performance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统性能监控数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取内容统计数据
        /// </summary>
        public async Task<ContentStatsDto> GetContentStatsAsync(int days = 30)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}content:{days}";

                if (_cache.TryGetValue(cacheKey, out ContentStatsDto? cachedStats) && cachedStats != null)
                {
                    return cachedStats;
                }

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                var stats = new ContentStatsDto
                {
                    TotalContent = await GetTotalPostsAsync(),
                    PublishedContent = await GetPublishedPostsCountAsync(),
                    DraftContent = await GetDraftPostsCountAsync(),
                    PendingContent = await GetPendingPostsCountAsync(),
                    ContentTypeDistribution = await GetContentTypeDistributionAsync(),
                    PublishTrends = await GetPublishTrendsAsync(startDate, endDate),
                    PopularCategories = await GetPopularCategoriesAsync(10),
                    PopularTags = await GetPopularTagsAsync(10)
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容统计数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取用户活跃度数据
        /// </summary>
        public async Task<UserActivityDto> GetUserActivityAsync(int days = 30)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}useractivity:{days}";

                if (_cache.TryGetValue(cacheKey, out UserActivityDto? cachedActivity) && cachedActivity != null)
                {
                    return cachedActivity;
                }

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                var activity = new UserActivityDto
                {
                    ActiveUsers = await GetActiveUsersCountAsync(startDate, endDate),
                    NewUsers = await GetNewUsersCountAsync(startDate, endDate),
                    ActivityTrends = await GetUserActivityTrendsAsync(startDate, endDate),
                    BehaviorStats = await GetUserBehaviorStatsAsync(startDate, endDate),
                    RetentionStats = await GetUserRetentionStatsAsync(),
                    GeographicDistribution = await GetUserGeographicDistributionAsync(startDate, endDate)
                };

                _cache.Set(cacheKey, activity, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return activity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户活跃度数据失败");
                throw;
            }
        }

        /// <summary>
        /// 获取热门内容排行
        /// </summary>
        public async Task<IEnumerable<PopularContentDto>> GetPopularContentAsync(int limit = 10, int days = 7)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}popular:{limit}:{days}";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<PopularContentDto>? cachedContent) && cachedContent != null)
                {
                    return cachedContent;
                }

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // 获取最受欢迎的文章
                var popularPosts = await _postRepository.GetMostViewedPostsAsync(limit);

                var popularContent = popularPosts.Select(post => new PopularContentDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    ContentType = "Post",
                    Author = post.Author?.DisplayName ?? post.Author?.UserName ?? "Unknown",
                    ViewCount = post.ViewCount,
                    CommentCount = post.Comments?.Count ?? 0,
                    LikeCount = post.LikeCount,
                    PublishedAt = post.PublishedAt ?? post.CreatedAt,
                    Url = $"/blog/{post.Slug}"
                }).ToList();

                _cache.Set(cacheKey, popularContent, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return popularContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门内容排行失败");
                throw;
            }
        }

        /// <summary>
        /// 获取近期操作日志
        /// </summary>
        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 20)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}activities:{limit}";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<RecentActivityDto>? cachedActivities) && cachedActivities != null)
                {
                    return cachedActivities;
                }

                var recentLogs = await _auditLogService.GetRecentLogsAsync(limit);

                var activities = recentLogs.Select(log => new RecentActivityDto
                {
                    Id = log.Id,
                    UserName = log.UserName ?? "System",
                    ActivityType = log.Action,
                    Description = log.Description ?? $"{log.Action} {log.ResourceType}",
                    ResourceType = log.ResourceType,
                    ResourceId = log.ResourceId,
                    IpAddress = log.IpAddress,
                    Timestamp = log.CreatedAt,
                    RiskLevel = DetermineRiskLevel(log.Action, log.ResourceType)
                }).ToList();

                _cache.Set(cacheKey, activities, TimeSpan.FromMinutes(2));
                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取近期操作日志失败");
                throw;
            }
        }

        /// <summary>
        /// 获取系统告警信息
        /// </summary>
        public async Task<IEnumerable<SystemAlertDto>> GetSystemAlertsAsync()
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}alerts";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<SystemAlertDto>? cachedAlerts) && cachedAlerts != null)
                {
                    return cachedAlerts;
                }

                var alerts = new List<SystemAlertDto>();

                // 检查系统资源使用情况
                var cpuUsage = await GetCurrentCpuUsageAsync();
                var memoryUsage = await GetCurrentMemoryUsageAsync();
                var diskUsage = await GetCurrentDiskUsageAsync();

                if (cpuUsage > 80)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "CPU使用率过高",
                        Message = $"当前CPU使用率为 {cpuUsage:F1}%",
                        Severity = "Warning",
                        AlertType = "Performance",
                        CreatedAt = DateTime.UtcNow,
                        SuggestedAction = "检查系统负载，考虑扩容或优化"
                    });
                }

                if (memoryUsage > 85)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "内存使用率过高",
                        Message = $"当前内存使用率为 {memoryUsage:F1}%",
                        Severity = "Warning",
                        AlertType = "Performance",
                        CreatedAt = DateTime.UtcNow,
                        SuggestedAction = "检查内存泄漏，考虑增加内存容量"
                    });
                }

                if (diskUsage > 90)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "磁盘空间不足",
                        Message = $"当前磁盘使用率为 {diskUsage:F1}%",
                        Severity = "Critical",
                        AlertType = "Storage",
                        CreatedAt = DateTime.UtcNow,
                        SuggestedAction = "清理磁盘空间或扩容"
                    });
                }

                // 检查数据库连接
                var dbStats = await GetDatabaseStatsAsync();
                if (dbStats.ActiveConnections > dbStats.MaxConnections * 0.9)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "数据库连接池接近满载",
                        Message = $"当前活跃连接数 {dbStats.ActiveConnections}/{dbStats.MaxConnections}",
                        Severity = "Warning",
                        AlertType = "Database",
                        CreatedAt = DateTime.UtcNow,
                        SuggestedAction = "检查连接泄漏或增加连接池大小"
                    });
                }

                // 检查失败的登录尝试
                var recentFailedLogins = await GetRecentFailedLoginsAsync();
                if (recentFailedLogins > 50)
                {
                    alerts.Add(new SystemAlertDto
                    {
                        Id = Guid.NewGuid(),
                        Title = "大量登录失败",
                        Message = $"最近1小时内有 {recentFailedLogins} 次登录失败",
                        Severity = "Warning",
                        AlertType = "Security",
                        CreatedAt = DateTime.UtcNow,
                        SuggestedAction = "检查是否存在暴力攻击，考虑启用验证码"
                    });
                }

                _cache.Set(cacheKey, alerts, TimeSpan.FromMinutes(2));
                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统告警信息失败");
                throw;
            }
        }

        /// <summary>
        /// 获取待处理任务数量
        /// </summary>
        public async Task<PendingTasksDto> GetPendingTasksAsync()
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}pendingtasks";

                if (_cache.TryGetValue(cacheKey, out PendingTasksDto? cachedTasks) && cachedTasks != null)
                {
                    return cachedTasks;
                }

                var tasks = new PendingTasksDto
                {
                    PendingContent = await GetPendingPostsCountAsync(),
                    PendingComments = await GetPendingCommentsCountAsync(),
                    PendingReports = await GetPendingReportsCountAsync(),
                    PendingMessages = await GetPendingMessagesCountAsync(),
                    SystemErrors = await GetSystemErrorsCountAsync(),
                    PendingUpdates = await GetPendingUpdatesCountAsync(),
                    TaskSummaries = await GetTaskSummariesAsync()
                };

                _cache.Set(cacheKey, tasks, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待处理任务数量失败");
                throw;
            }
        }

        /// <summary>
        /// 获取网站访问趋势
        /// </summary>
        public async Task<IEnumerable<VisitTrendDto>> GetVisitTrendsAsync(int days = 30)
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}visittrends:{days}";

                if (_cache.TryGetValue(cacheKey, out IEnumerable<VisitTrendDto>? cachedTrends) && cachedTrends != null)
                {
                    return cachedTrends;
                }

                var endDate = DateTime.UtcNow.Date;
                var startDate = endDate.AddDays(-days);

                // 从统计服务获取访问趋势数据
                var trends = await _statsService.GetDailyVisitStatsAsync(startDate, endDate);

                var visitTrends = trends.Select(stat => new VisitTrendDto
                {
                    Date = stat.Date,
                    PageViews = stat.PageViews,
                    UniqueVisitors = stat.UniqueVisitors,
                    NewVisitors = stat.NewVisitors,
                    ReturningVisitors = stat.ReturningVisitors,
                    AverageSessionDuration = stat.AverageSessionDuration,
                    BounceRate = stat.BounceRate
                }).ToList();

                _cache.Set(cacheKey, visitTrends, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return visitTrends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取网站访问趋势失败");
                throw;
            }
        }

        /// <summary>
        /// 获取系统健康检查结果
        /// </summary>
        public async Task<SystemHealthDto> GetSystemHealthAsync()
        {
            try
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}health";

                if (_cache.TryGetValue(cacheKey, out SystemHealthDto? cachedHealth) && cachedHealth != null)
                {
                    return cachedHealth;
                }

                var health = new SystemHealthDto
                {
                    Database = await CheckDatabaseHealthAsync(),
                    Cache = await CheckCacheHealthAsync(),
                    FileSystem = await CheckFileSystemHealthAsync(),
                    ExternalServices = await CheckExternalServicesHealthAsync(),
                    LastChecked = DateTime.UtcNow,
                    SystemInfo = await GetSystemInfoAsync()
                };

                // 确定整体健康状态
                var healthItems = new[] { health.Database, health.Cache, health.FileSystem }
                    .Concat(health.ExternalServices)
                    .ToList();

                if (healthItems.Any(h => h.Status == "Critical"))
                    health.OverallStatus = "Critical";
                else if (healthItems.Any(h => h.Status == "Warning"))
                    health.OverallStatus = "Warning";
                else if (healthItems.All(h => h.Status == "Healthy"))
                    health.OverallStatus = "Healthy";
                else
                    health.OverallStatus = "Unknown";

                _cache.Set(cacheKey, health, TimeSpan.FromMinutes(2));
                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统健康检查结果失败");
                throw;
            }
        }

        /// <summary>
        /// 标记告警为已读
        /// </summary>
        public async Task<bool> MarkAlertAsReadAsync(Guid alertId, Guid userId)
        {
            try
            {
                // 这里应该有一个AlertRepository来处理告警的持久化
                // 目前先记录到审计日志
                await _auditLogService.LogUserActionAsync(
                    userId,
                    null,
                    "MarkAlertRead",
                    "Alert",
                    alertId.ToString(),
                    "用户标记告警为已读",
                    null,
                    new { AlertId = alertId },
                    null,
                    null
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记告警为已读失败: AlertId={AlertId}, UserId={UserId}", alertId, userId);
                return false;
            }
        }

        /// <summary>
        /// 清除过期数据
        /// </summary>
        public async Task<DataCleanupResultDto> CleanupExpiredDataAsync(int days = 90)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var cleanupResult = new DataCleanupResultDto
                {
                    Success = true,
                    CleanedRecords = new Dictionary<string, int>()
                };

                var details = new List<string>();

                // 清理过期的审计日志
                var cleanedAuditLogs = await CleanupAuditLogsAsync(cutoffDate);
                if (cleanedAuditLogs > 0)
                {
                    cleanupResult.CleanedRecords["AuditLogs"] = cleanedAuditLogs;
                    details.Add($"清理了 {cleanedAuditLogs} 条审计日志");
                }

                // 清理过期的统计数据
                var cleanedStats = await CleanupStatsDataAsync(cutoffDate);
                if (cleanedStats > 0)
                {
                    cleanupResult.CleanedRecords["Statistics"] = cleanedStats;
                    details.Add($"清理了 {cleanedStats} 条统计数据");
                }

                // 清理过期的缓存文件
                var cleanedCacheFiles = await CleanupCacheFilesAsync(cutoffDate);
                if (cleanedCacheFiles > 0)
                {
                    cleanupResult.CleanedRecords["CacheFiles"] = cleanedCacheFiles;
                    details.Add($"清理了 {cleanedCacheFiles} 个缓存文件");
                }

                // 清理过期的临时文件
                var cleanedTempFiles = await CleanupTempFilesAsync(cutoffDate);
                if (cleanedTempFiles > 0)
                {
                    cleanupResult.CleanedRecords["TempFiles"] = cleanedTempFiles;
                    details.Add($"清理了 {cleanedTempFiles} 个临时文件");
                }

                cleanupResult.Duration = DateTime.UtcNow - startTime;
                cleanupResult.Details = details;
                cleanupResult.FreedSpace = await CalculateFreedSpaceAsync();

                _logger.LogInformation("数据清理完成: {Details}", string.Join(", ", details));

                return cleanupResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除过期数据失败");
                return new DataCleanupResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Duration = TimeSpan.Zero
                };
            }
        }

        #region 私有辅助方法

        private async Task<int> GetTotalUsersAsync()
        {
            return await _userRepository.CountAsync(u => !u.IsDeleted);
        }

        private async Task<int> GetTodayNewUsersAsync(DateTime today)
        {
            return await _userRepository.CountAsync(u => u.CreatedAt.Date == today && !u.IsDeleted);
        }

        private async Task<int> GetTotalPostsAsync()
        {
            return await _postRepository.CountAsync(p => !p.IsDeleted);
        }

        private async Task<int> GetTodayNewPostsAsync(DateTime today)
        {
            return await _postRepository.CountAsync(p => p.CreatedAt.Date == today && !p.IsDeleted);
        }

        private async Task<int> GetTotalCommentsAsync()
        {
            return await _commentRepository.CountAsync(c => !c.IsDeleted);
        }

        private async Task<int> GetTodayNewCommentsAsync(DateTime today)
        {
            return await _commentRepository.CountAsync(c => c.CreatedAt.Date == today && !c.IsDeleted);
        }

        private async Task<long> GetTotalViewsAsync()
        {
            return await _statsService.GetTotalPageViewsAsync();
        }

        private async Task<long> GetTodayViewsAsync(DateTime today)
        {
            return await _statsService.GetPageViewsAsync(today, today.AddDays(1));
        }

        private async Task<int> GetOnlineUsersAsync()
        {
            // 这里需要实现在线用户统计逻辑
            // 可以基于最近活动时间来判断
            var onlineThreshold = DateTime.UtcNow.AddMinutes(-15);
            return await _userRepository.CountAsync(u => u.LastLoginAt > onlineThreshold && !u.IsDeleted);
        }

        private async Task<SystemStatusDto> GetSystemStatusAsync()
        {
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime;

            return new SystemStatusDto
            {
                CpuUsage = await GetCurrentCpuUsageAsync(),
                MemoryUsage = await GetCurrentMemoryUsageAsync(),
                DiskUsage = await GetCurrentDiskUsageAsync(),
                Uptime = uptime,
                ServiceStatus = "Running"
            };
        }

        private async Task<double> GetCurrentCpuUsageAsync()
        {
            // 实现CPU使用率获取逻辑
            return await Task.FromResult(45.2); // 示例值
        }

        private async Task<double> GetCurrentMemoryUsageAsync()
        {
            // 实现内存使用率获取逻辑
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            // 这里需要获取系统总内存来计算百分比
            return await Task.FromResult(62.8); // 示例值
        }

        private async Task<double> GetCurrentDiskUsageAsync()
        {
            // 实现磁盘使用率获取逻辑
            return await Task.FromResult(78.5); // 示例值
        }

        private async Task<int> GetTodayUniqueVisitorsAsync(DateTime today)
        {
            return await _statsService.GetUniqueVisitorsAsync(today, today.AddDays(1));
        }

        private async Task<IEnumerable<HourlyStatsDto>> GetHourlyTrendsAsync(DateTime startTime, DateTime endTime)
        {
            return await _statsService.GetHourlyStatsAsync(startTime, endTime);
        }

        private async Task<IEnumerable<RealtimeActivityDto>> GetLatestActivitiesAsync(int limit)
        {
            var activities = await _auditLogService.GetRecentLogsAsync(limit);
            return activities.Select(a => new RealtimeActivityDto
            {
                UserName = a.UserName ?? "System",
                ActivityType = a.Action,
                Description = a.Description ?? $"{a.Action} {a.ResourceType}",
                Timestamp = a.CreatedAt
            });
        }

        private string DetermineRiskLevel(string action, string resourceType)
        {
            var highRiskActions = new[] { "Delete", "Ban", "Lock", "ForceLogout" };
            var sensitiveResources = new[] { "User", "Post", "System" };

            if (highRiskActions.Contains(action) || sensitiveResources.Contains(resourceType))
                return "High";

            if (action.Contains("Update") || action.Contains("Create"))
                return "Medium";

            return "Low";
        }

        // 其他私有方法的占位符实现...
        private async Task<IEnumerable<PerformanceDataPointDto>> GetCpuUsageHistoryAsync(DateTime startTime, DateTime endTime)
        {
            // 实现CPU使用率历史数据获取
            return new List<PerformanceDataPointDto>();
        }

        private async Task<IEnumerable<PerformanceDataPointDto>> GetMemoryUsageHistoryAsync(DateTime startTime, DateTime endTime)
        {
            // 实现内存使用率历史数据获取
            return new List<PerformanceDataPointDto>();
        }

        private async Task<NetworkIoStatsDto> GetNetworkIoStatsAsync(DateTime startTime, DateTime endTime)
        {
            return new NetworkIoStatsDto();
        }

        private async Task<DatabaseConnectionStatsDto> GetDatabaseStatsAsync()
        {
            return new DatabaseConnectionStatsDto
            {
                ActiveConnections = 15,
                MaxConnections = 100,
                AverageQueryTime = 25.5,
                SlowQueries = 2
            };
        }

        private async Task<double> GetCacheHitRateAsync()
        {
            return 85.6; // 示例值
        }

        private async Task<ResponseTimeStatsDto> GetResponseTimeStatsAsync(DateTime startTime, DateTime endTime)
        {
            return new ResponseTimeStatsDto();
        }

        private async Task<int> GetPublishedPostsCountAsync()
        {
            return await _postRepository.CountAsync(p => p.IsPublished && !p.IsDeleted);
        }

        private async Task<int> GetDraftPostsCountAsync()
        {
            return await _postRepository.CountAsync(p => !p.IsPublished && !p.IsDeleted);
        }

        private async Task<int> GetPendingPostsCountAsync()
        {
            return await _postRepository.CountAsync(p => p.Status == PostStatus.Draft && !p.IsDeleted);
        }

        private async Task<IEnumerable<ContentTypeStatsDto>> GetContentTypeDistributionAsync()
        {
            return new List<ContentTypeStatsDto>();
        }

        private async Task<IEnumerable<DailyStatsDto>> GetPublishTrendsAsync(DateTime startDate, DateTime endDate)
        {
            return new List<DailyStatsDto>();
        }

        private async Task<IEnumerable<CategoryStatsDto>> GetPopularCategoriesAsync(int limit)
        {
            return new List<CategoryStatsDto>();
        }

        private async Task<IEnumerable<TagStatsDto>> GetPopularTagsAsync(int limit)
        {
            return new List<TagStatsDto>();
        }

        private async Task<int> GetActiveUsersCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _userRepository.CountAsync(u => u.LastLoginAt >= startDate && u.LastLoginAt <= endDate && !u.IsDeleted);
        }

        private async Task<int> GetNewUsersCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _userRepository.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate && !u.IsDeleted);
        }

        private async Task<IEnumerable<DailyStatsDto>> GetUserActivityTrendsAsync(DateTime startDate, DateTime endDate)
        {
            return new List<DailyStatsDto>();
        }

        private async Task<UserBehaviorStatsDto> GetUserBehaviorStatsAsync(DateTime startDate, DateTime endDate)
        {
            return new UserBehaviorStatsDto();
        }

        private async Task<UserRetentionDto> GetUserRetentionStatsAsync()
        {
            return new UserRetentionDto();
        }

        private async Task<IEnumerable<GeographicStatsDto>> GetUserGeographicDistributionAsync(DateTime startDate, DateTime endDate)
        {
            return new List<GeographicStatsDto>();
        }

        private async Task<int> GetPendingCommentsCountAsync()
        {
            return await _commentRepository.CountAsync(c => c.Status == CommentStatus.Pending && !c.IsDeleted);
        }

        private async Task<int> GetPendingReportsCountAsync()
        {
            return 0; // 需要实现举报系统
        }

        private async Task<int> GetPendingMessagesCountAsync()
        {
            return 0; // 需要实现消息系统
        }

        private async Task<int> GetSystemErrorsCountAsync()
        {
            return 0; // 需要实现错误跟踪
        }

        private async Task<int> GetPendingUpdatesCountAsync()
        {
            return 0; // 需要实现更新系统
        }

        private async Task<IEnumerable<TaskSummaryDto>> GetTaskSummariesAsync()
        {
            return new List<TaskSummaryDto>();
        }

        private async Task<int> GetRecentFailedLoginsAsync()
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            return await _auditLogService.CountAsync("LoginFailed", null, oneHourAgo, DateTime.UtcNow);
        }

        private async Task<HealthCheckItemDto> CheckDatabaseHealthAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                // 简单的数据库连通性检查
                await _userRepository.CountAsync(u => true);

                return new HealthCheckItemDto
                {
                    Name = "Database",
                    Status = "Healthy",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "数据库连接正常"
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckItemDto
                {
                    Name = "Database",
                    Status = "Critical",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "数据库连接异常",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<HealthCheckItemDto> CheckCacheHealthAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var testKey = "health-check";
                var testValue = "ok";

                _cache.Set(testKey, testValue, TimeSpan.FromSeconds(1));
                var retrieved = _cache.Get<string>(testKey);

                return new HealthCheckItemDto
                {
                    Name = "Cache",
                    Status = retrieved == testValue ? "Healthy" : "Warning",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "缓存服务正常"
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckItemDto
                {
                    Name = "Cache",
                    Status = "Warning",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "缓存服务异常",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<HealthCheckItemDto> CheckFileSystemHealthAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var tempFile = Path.GetTempFileName();
                await System.IO.File.WriteAllTextAsync(tempFile, "health check");
                var content = await System.IO.File.ReadAllTextAsync(tempFile);
                System.IO.File.Delete(tempFile);

                return new HealthCheckItemDto
                {
                    Name = "FileSystem",
                    Status = "Healthy",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "文件系统正常"
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckItemDto
                {
                    Name = "FileSystem",
                    Status = "Critical",
                    ResponseTime = DateTime.UtcNow - startTime,
                    Description = "文件系统异常",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<IEnumerable<HealthCheckItemDto>> CheckExternalServicesHealthAsync()
        {
            return new List<HealthCheckItemDto>();
        }

        private async Task<SystemInfoDto> GetSystemInfoAsync()
        {
            return new SystemInfoDto
            {
                OperatingSystem = Environment.OSVersion.ToString(),
                DotNetVersion = Environment.Version.ToString(),
                ApplicationVersion = "1.0.0", // 从程序集获取
                ServerTime = DateTime.UtcNow,
                TimeZone = TimeZoneInfo.Local.DisplayName
            };
        }

        private async Task<int> CleanupAuditLogsAsync(DateTime cutoffDate)
        {
            // 实现审计日志清理
            return 0;
        }

        private async Task<int> CleanupStatsDataAsync(DateTime cutoffDate)
        {
            // 实现统计数据清理
            return 0;
        }

        private async Task<int> CleanupCacheFilesAsync(DateTime cutoffDate)
        {
            // 实现缓存文件清理
            return 0;
        }

        private async Task<int> CleanupTempFilesAsync(DateTime cutoffDate)
        {
            // 实现临时文件清理
            return 0;
        }

        private async Task<long> CalculateFreedSpaceAsync()
        {
            // 计算释放的空间
            return 0;
        }

        #endregion
    }
}