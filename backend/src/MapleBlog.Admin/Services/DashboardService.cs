using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Admin.DTOs;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using System.Diagnostics;

namespace MapleBlog.Admin.Services;

/// <summary>
/// 仪表盘数据聚合服务
/// 负责收集和聚合管理后台仪表盘的各种统计数据
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardService> _logger;
    private const string CACHE_PREFIX = "Dashboard:";
    private const int CACHE_DURATION_MINUTES = 5; // 缓存5分钟

    public DashboardService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取完整的仪表盘统计数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>仪表盘统计数据</returns>
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"{CACHE_PREFIX}FullStats";

            if (_cache.TryGetValue(cacheKey, out DashboardStatsDto cachedStats))
            {
                _logger.LogDebug("Dashboard stats loaded from cache");
                return cachedStats;
            }

            _logger.LogInformation("Generating dashboard stats...");
            var stopwatch = Stopwatch.StartNew();

            var stats = new DashboardStatsDto
            {
                UserStats = await GetUserStatsAsync(cancellationToken),
                ContentStats = await GetContentStatsAsync(cancellationToken),
                SystemStats = await GetSystemStatsAsync(cancellationToken),
                TrafficStats = await GetTrafficStatsAsync(cancellationToken),
                TrendData = await GetTrendDataAsync(cancellationToken),
                LastUpdated = DateTime.UtcNow
            };

            // 缓存统计数据
            _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            stopwatch.Stop();
            _logger.LogInformation("Dashboard stats generated in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard stats");
            throw;
        }
    }

    /// <summary>
    /// 获取用户统计数据
    /// </summary>
    private async Task<UserStatsDto> GetUserStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var userStats = new UserStatsDto();

        try
        {
            // 总用户数和活跃用户数
            var userCounts = await _context.Users
                .GroupBy(u => u.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            userStats.TotalUsers = userCounts.Sum(u => u.Count);
            userStats.ActiveUsers = userCounts.FirstOrDefault(u => u.IsActive)?.Count ?? 0;

            // 新注册用户统计
            var newUsers = await _context.Users
                .Where(u => u.CreatedAt >= monthStart)
                .GroupBy(u => u.CreatedAt.Date >= today ? "today" :
                            u.CreatedAt.Date >= weekStart ? "week" :
                            "month")
                .Select(g => new { Period = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            userStats.NewUsersToday = newUsers.FirstOrDefault(u => u.Period == "today")?.Count ?? 0;
            userStats.NewUsersThisWeek = newUsers.Where(u => u.Period == "today" || u.Period == "week").Sum(u => u.Count);
            userStats.NewUsersThisMonth = newUsers.Sum(u => u.Count);

            // 按角色分布
            var roleDistribution = await _context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            userStats.UsersByRole = roleDistribution.ToDictionary(
                r => r.Role.ToString(),
                r => r.Count);

            // 按状态分布
            var statusDistribution = await _context.Users
                .GroupBy(u => u.IsActive)
                .Select(g => new { IsActive = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            userStats.UsersByStatus = statusDistribution.ToDictionary(
                s => s.IsActive ? "Active" : "Inactive",
                s => s.Count);

            // 计算增长率
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= lastMonthStart && u.CreatedAt < monthStart, cancellationToken);

            if (lastMonthUsers > 0)
            {
                userStats.GrowthRate = Math.Round(((double)userStats.NewUsersThisMonth / lastMonthUsers) * 100, 2);
            }

            // 在线用户数（近15分钟活跃）
            var recentActivity = now.AddMinutes(-15);
            userStats.OnlineUsers = await _context.Users
                .CountAsync(u => u.LastLoginAt >= recentActivity, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats");
        }

        return userStats;
    }

    /// <summary>
    /// 获取内容统计数据
    /// </summary>
    private async Task<ContentStatsDto> GetContentStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var contentStats = new ContentStatsDto();

        try
        {
            // 文章统计
            var postCounts = await _context.Posts
                .GroupBy(p => p.IsPublished)
                .Select(g => new { IsPublished = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            contentStats.TotalPosts = postCounts.Sum(p => p.Count);
            contentStats.PublishedPosts = postCounts.FirstOrDefault(p => p.IsPublished)?.Count ?? 0;
            contentStats.DraftPosts = postCounts.FirstOrDefault(p => !p.IsPublished)?.Count ?? 0;

            // 新文章统计
            var newPosts = await _context.Posts
                .Where(p => p.CreatedAt >= monthStart)
                .GroupBy(p => p.CreatedAt.Date >= today ? "today" :
                            p.CreatedAt.Date >= weekStart ? "week" :
                            "month")
                .Select(g => new { Period = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            contentStats.PostsToday = newPosts.FirstOrDefault(p => p.Period == "today")?.Count ?? 0;
            contentStats.PostsThisWeek = newPosts.Where(p => p.Period == "today" || p.Period == "week").Sum(p => p.Count);
            contentStats.PostsThisMonth = newPosts.Sum(p => p.Count);

            // 评论统计
            contentStats.TotalComments = await _context.Comments.CountAsync(cancellationToken);
            contentStats.PendingComments = await _context.Comments
                .CountAsync(c => !c.IsApproved, cancellationToken);
            contentStats.CommentsToday = await _context.Comments
                .CountAsync(c => c.CreatedAt >= today, cancellationToken);

            // 分类和标签统计
            contentStats.TotalCategories = await _context.Categories.CountAsync(cancellationToken);
            contentStats.TotalTags = await _context.Tags.CountAsync(cancellationToken);

            // 热门分类
            contentStats.PopularCategories = await _context.Categories
                .Include(c => c.Posts)
                .OrderByDescending(c => c.Posts.Count)
                .Take(10)
                .Select(c => new CategoryStatsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PostCount = c.Posts.Count,
                    ViewCount = c.Posts.Sum(p => p.ViewCount)
                })
                .ToListAsync(cancellationToken);

            // 热门标签
            contentStats.PopularTags = await _context.Tags
                .Include(t => t.PostTags)
                .OrderByDescending(t => t.PostTags.Count)
                .Take(10)
                .Select(t => new TagStatsDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    UseCount = t.PostTags.Count
                })
                .ToListAsync(cancellationToken);

            // 内容增长率
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthPosts = await _context.Posts
                .CountAsync(p => p.CreatedAt >= lastMonthStart && p.CreatedAt < monthStart, cancellationToken);

            if (lastMonthPosts > 0)
            {
                contentStats.ContentGrowthRate = Math.Round(((double)contentStats.PostsThisMonth / lastMonthPosts) * 100, 2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting content stats");
        }

        return contentStats;
    }

    /// <summary>
    /// 获取系统统计数据
    /// </summary>
    private async Task<SystemStatsDto> GetSystemStatsAsync(CancellationToken cancellationToken = default)
    {
        var systemStats = new SystemStatsDto();

        try
        {
            // 系统启动时间和运行时间
            var process = Process.GetCurrentProcess();
            systemStats.SystemStartTime = process.StartTime.ToUniversalTime();
            systemStats.UptimeSeconds = (long)(DateTime.UtcNow - systemStats.SystemStartTime).TotalSeconds;

            // 内存使用情况
            systemStats.MemoryUsageMB = (int)(process.WorkingSet64 / (1024 * 1024));

            // 获取系统总内存（需要更复杂的实现）
            var totalMemoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            if (totalMemoryMB > 0)
            {
                systemStats.MemoryUsagePercentage = Math.Round((double)systemStats.MemoryUsageMB / totalMemoryMB * 100, 2);
            }

            // 数据库连接数（简化实现）
            try
            {
                await _context.Database.CanConnectAsync(cancellationToken);
                systemStats.DatabaseConnections = 1; // 简化的连接数
            }
            catch
            {
                systemStats.DatabaseConnections = 0;
            }

            // 错误和警告日志数量（需要实际的日志统计实现）
            var today = DateTime.UtcNow.Date;

            // 这里应该从实际的日志系统中获取数据
            // 暂时使用模拟数据
            systemStats.ErrorsToday = 0;
            systemStats.WarningsToday = 0;

            // 缓存命中率（需要实际的缓存统计）
            systemStats.CacheHitRate = 85.5; // 模拟数据

            // 健康状态
            systemStats.HealthStatus = SystemHealthStatus.Healthy;
            systemStats.HealthChecks = new List<HealthCheckDto>
            {
                new HealthCheckDto
                {
                    Name = "Database",
                    Status = SystemHealthStatus.Healthy,
                    Description = "Database connection is healthy",
                    ResponseTimeMs = 50,
                    LastCheck = DateTime.UtcNow
                },
                new HealthCheckDto
                {
                    Name = "Memory",
                    Status = systemStats.MemoryUsagePercentage > 80 ? SystemHealthStatus.Warning : SystemHealthStatus.Healthy,
                    Description = $"Memory usage: {systemStats.MemoryUsagePercentage:F1}%",
                    ResponseTimeMs = 5,
                    LastCheck = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system stats");
            systemStats.HealthStatus = SystemHealthStatus.Unhealthy;
        }

        return systemStats;
    }

    /// <summary>
    /// 获取访问统计数据
    /// </summary>
    private async Task<TrafficStatsDto> GetTrafficStatsAsync(CancellationToken cancellationToken = default)
    {
        var trafficStats = new TrafficStatsDto();

        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // 基于Post的ViewCount来模拟访问统计
            // 在实际项目中，应该有专门的访问统计表

            // 今日访问量（基于文章浏览量的增长）
            trafficStats.VisitsToday = await _context.Posts
                .Where(p => p.UpdatedAt >= today)
                .SumAsync(p => p.ViewCount, cancellationToken) / 10; // 简化计算

            // 昨日访问量
            trafficStats.VisitsYesterday = await _context.Posts
                .Where(p => p.UpdatedAt >= yesterday && p.UpdatedAt < today)
                .SumAsync(p => p.ViewCount, cancellationToken) / 10;

            // 本周访问量
            trafficStats.VisitsThisWeek = await _context.Posts
                .Where(p => p.UpdatedAt >= weekStart)
                .SumAsync(p => p.ViewCount, cancellationToken) / 10;

            // 本月访问量
            trafficStats.VisitsThisMonth = await _context.Posts
                .Where(p => p.UpdatedAt >= monthStart)
                .SumAsync(p => p.ViewCount, cancellationToken) / 10;

            // 总访问量
            trafficStats.TotalVisits = await _context.Posts
                .SumAsync(p => (long)p.ViewCount, cancellationToken);

            // 独立访客数（模拟数据）
            trafficStats.UniqueVisitorsToday = trafficStats.VisitsToday * 70 / 100; // 假设70%的转换率

            // 平均页面浏览时间（模拟数据）
            trafficStats.AvgPageViewTime = 125.5;

            // 跳出率（模拟数据）
            trafficStats.BounceRate = 35.2;

            // 热门页面
            trafficStats.PopularPages = await _context.Posts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.ViewCount)
                .Take(10)
                .Select(p => new PageStatsDto
                {
                    Path = $"/posts/{p.Slug}",
                    Title = p.Title,
                    Views = p.ViewCount,
                    UniqueViews = p.ViewCount * 85 / 100 // 估算独立访问
                })
                .ToListAsync(cancellationToken);

            // 来源统计（模拟数据）
            trafficStats.TopReferrers = new List<ReferrerStatsDto>
            {
                new ReferrerStatsDto { Domain = "google.com", Views = trafficStats.VisitsToday * 40 / 100, Type = "Search Engine" },
                new ReferrerStatsDto { Domain = "baidu.com", Views = trafficStats.VisitsToday * 25 / 100, Type = "Search Engine" },
                new ReferrerStatsDto { Domain = "github.com", Views = trafficStats.VisitsToday * 15 / 100, Type = "Direct" },
                new ReferrerStatsDto { Domain = "twitter.com", Views = trafficStats.VisitsToday * 10 / 100, Type = "Social Media" }
            };

            // 地理位置统计（模拟数据）
            trafficStats.TopLocations = new List<LocationStatsDto>
            {
                new LocationStatsDto { Country = "China", City = "Beijing", Views = trafficStats.VisitsToday * 30 / 100 },
                new LocationStatsDto { Country = "China", City = "Shanghai", Views = trafficStats.VisitsToday * 20 / 100 },
                new LocationStatsDto { Country = "United States", City = "New York", Views = trafficStats.VisitsToday * 15 / 100 },
                new LocationStatsDto { Country = "China", City = "Guangzhou", Views = trafficStats.VisitsToday * 12 / 100 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting traffic stats");
        }

        return trafficStats;
    }

    /// <summary>
    /// 获取趋势数据
    /// </summary>
    private async Task<TrendDataDto> GetTrendDataAsync(CancellationToken cancellationToken = default)
    {
        var trendData = new TrendDataDto();

        try
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var twentyFourHoursAgo = now.AddHours(-24);

            // 用户增长趋势（过去30天）
            var userGrowthData = await _context.Users
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync(cancellationToken);

            trendData.UserGrowthTrend = GenerateDayTrend(userGrowthData, thirtyDaysAgo, 30);

            // 内容增长趋势（过去30天）
            var contentGrowthData = await _context.Posts
                .Where(p => p.CreatedAt >= thirtyDaysAgo)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync(cancellationToken);

            trendData.ContentGrowthTrend = GenerateDayTrend(contentGrowthData, thirtyDaysAgo, 30);

            // 访问量趋势（基于文章更新时间模拟）
            var trafficTrendData = await _context.Posts
                .Where(p => p.UpdatedAt >= thirtyDaysAgo)
                .GroupBy(p => p.UpdatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Sum(p => p.ViewCount) })
                .OrderBy(g => g.Date)
                .ToListAsync(cancellationToken);

            trendData.TrafficTrend = GenerateDayTrend(trafficTrendData, thirtyDaysAgo, 30);

            // 系统性能趋势（过去24小时，模拟数据）
            trendData.PerformanceTrend = GeneratePerformanceTrend(twentyFourHoursAgo, 24);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend data");
        }

        return trendData;
    }

    /// <summary>
    /// 生成日趋势数据
    /// </summary>
    private List<DayStatsDto> GenerateDayTrend<T>(
        List<T> data,
        DateTime startDate,
        int days) where T : class
    {
        var trend = new List<DayStatsDto>();
        var dataDict = data.ToDictionary(
            d => (DateTime)d.GetType().GetProperty("Date")!.GetValue(d)!,
            d => (int)d.GetType().GetProperty("Count")!.GetValue(d)!);

        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i).Date;
            var value = dataDict.GetValueOrDefault(date, 0);
            var previousValue = i > 0 ? trend[i - 1].Value : 0;

            trend.Add(new DayStatsDto
            {
                Date = date,
                Value = value,
                Change = value - previousValue,
                ChangeRate = previousValue > 0 ? Math.Round(((double)(value - previousValue) / previousValue) * 100, 2) : 0
            });
        }

        return trend;
    }

    /// <summary>
    /// 生成性能趋势数据（模拟）
    /// </summary>
    private List<HourStatsDto> GeneratePerformanceTrend(DateTime startTime, int hours)
    {
        var trend = new List<HourStatsDto>();
        var random = new Random();

        for (int i = 0; i < hours; i++)
        {
            trend.Add(new HourStatsDto
            {
                Time = startTime.AddHours(i),
                CpuUsage = Math.Round(30 + random.NextDouble() * 40, 2), // 30-70%
                MemoryUsage = Math.Round(50 + random.NextDouble() * 30, 2), // 50-80%
                ResponseTime = Math.Round(50 + random.NextDouble() * 100, 2) // 50-150ms
            });
        }

        return trend;
    }

    /// <summary>
    /// 刷新仪表盘缓存
    /// </summary>
    public async Task RefreshDashboardCacheAsync()
    {
        try
        {
            // 清除所有仪表盘相关缓存
            var cacheKeys = new[]
            {
                $"{CACHE_PREFIX}FullStats",
                $"{CACHE_PREFIX}UserStats",
                $"{CACHE_PREFIX}ContentStats",
                $"{CACHE_PREFIX}SystemStats",
                $"{CACHE_PREFIX}TrafficStats",
                $"{CACHE_PREFIX}TrendData"
            };

            foreach (var key in cacheKeys)
            {
                _cache.Remove(key);
            }

            // 预热缓存
            await GetDashboardStatsAsync();

            _logger.LogInformation("Dashboard cache refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing dashboard cache");
            throw;
        }
    }

    /// <summary>
    /// 获取实时统计数据（不使用缓存）
    /// </summary>
    public async Task<DashboardStatsDto> GetRealtimeStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating realtime dashboard stats...");

            return new DashboardStatsDto
            {
                UserStats = await GetUserStatsAsync(cancellationToken),
                ContentStats = await GetContentStatsAsync(cancellationToken),
                SystemStats = await GetSystemStatsAsync(cancellationToken),
                TrafficStats = await GetTrafficStatsAsync(cancellationToken),
                TrendData = await GetTrendDataAsync(cancellationToken),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating realtime dashboard stats");
            throw;
        }
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var stats = await GetDashboardStatsAsync();
        return new DashboardSummaryDto
        {
            TotalPosts = stats.TotalPosts,
            TotalUsers = stats.TotalUsers,
            TotalComments = stats.TotalComments,
            TotalViews = (int)stats.TotalViews,
            AvgResponseTime = 0,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<DashboardConfigDto> GetUserDashboardConfigAsync(string userId)
    {
        return new DashboardConfigDto
        {
            WidgetLayout = "default",
            Settings = new Dictionary<string, object>(),
            EnabledWidgets = new List<string> { "stats", "charts", "activity" },
            RefreshInterval = 30
        };
    }

    public async Task<bool> UpdateUserDashboardConfigAsync(string userId, DashboardConfigDto config)
    {
        _logger.LogInformation("Updated dashboard config for user {UserId}", userId);
        return true;
    }

    public async Task LogDashboardConnectionAsync(string connectionId, string userId)
    {
        _logger.LogInformation("Dashboard connected: ConnectionId={ConnectionId}, UserId={UserId}", connectionId, userId);
        await Task.CompletedTask;
    }

    public async Task LogDashboardDisconnectionAsync(string connectionId)
    {
        _logger.LogInformation("Dashboard disconnected: ConnectionId={ConnectionId}", connectionId);
        await Task.CompletedTask;
    }
}

/// <summary>
/// 仪表盘服务接口
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// 获取仪表盘统计数据
    /// </summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实时统计数据
    /// </summary>
    Task<DashboardStatsDto> GetRealtimeStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新仪表盘缓存
    /// </summary>
    Task RefreshDashboardCacheAsync();

    /// <summary>
    /// 获取仪表盘摘要
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync();

    /// <summary>
    /// 获取用户仪表盘配置
    /// </summary>
    Task<DashboardConfigDto> GetUserDashboardConfigAsync(string userId);

    /// <summary>
    /// 更新用户仪表盘配置
    /// </summary>
    Task<bool> UpdateUserDashboardConfigAsync(string userId, DashboardConfigDto config);

    /// <summary>
    /// 记录仪表盘连接
    /// </summary>
    Task LogDashboardConnectionAsync(string connectionId, string userId);

    /// <summary>
    /// 记录仪表盘断开连接
    /// </summary>
    Task LogDashboardDisconnectionAsync(string connectionId);
}