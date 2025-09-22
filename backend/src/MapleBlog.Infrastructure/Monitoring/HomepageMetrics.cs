using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using MapleBlog.Application.DTOs;

namespace MapleBlog.Infrastructure.Monitoring;

/// <summary>
/// 首页指标监控服务
/// 收集和处理首页相关的性能指标、用户行为数据和业务指标
/// </summary>
public interface IHomepageMetricsService
{
    // 性能指标
    Task RecordPageLoadTimeAsync(double loadTimeMs, string? userId = null);
    Task RecordApiResponseTimeAsync(string endpoint, double responseTimeMs);
    Task RecordCacheHitRateAsync(string cacheType, bool isHit);

    // 用户行为指标
    Task RecordUserInteractionAsync(string action, string element, string? userId = null);
    Task RecordContentEngagementAsync(string contentType, string contentId, string action, string? userId = null);
    Task RecordSearchQueryAsync(string query, int resultCount, string? userId = null);

    // 业务指标
    Task RecordConversionEventAsync(string goalName, double? value = null, string? userId = null);
    Task RecordErrorEventAsync(string errorType, string errorMessage, string? userId = null);

    // A/B测试指标
    Task RecordABTestExposureAsync(string testId, string variant, string? userId = null);
    Task RecordABTestConversionAsync(string testId, string variant, string goalName, string? userId = null);

    // 获取指标摘要
    Task<HomepageMetricsSummaryDto> GetMetricsSummaryAsync(DateTime from, DateTime to);

    // 实时指标
    Task<Dictionary<string, object>> GetRealTimeMetricsAsync();
}

/// <summary>
/// 监控配置选项
/// </summary>
public class HomepageMetricsOptions
{
    public const string SectionName = "Monitoring:Homepage";

    /// <summary>
    /// 是否启用监控
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 数据保留天数
    /// </summary>
    public int DataRetentionDays { get; set; } = 90;

    /// <summary>
    /// 批量处理大小
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// 刷新间隔（秒）
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 异常阈值配置
    /// </summary>
    public ThresholdConfig Thresholds { get; set; } = new();

    /// <summary>
    /// 采样率 (0.0 - 1.0)
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// 监控端点
    /// </summary>
    public string MetricsEndpoint { get; set; } = "/metrics";
}

public class ThresholdConfig
{
    /// <summary>
    /// 页面加载时间阈值（毫秒）
    /// </summary>
    public double PageLoadTimeThreshold { get; set; } = 3000;

    /// <summary>
    /// API响应时间阈值（毫秒）
    /// </summary>
    public double ApiResponseTimeThreshold { get; set; } = 1000;

    /// <summary>
    /// 缓存命中率阈值
    /// </summary>
    public double CacheHitRateThreshold { get; set; } = 0.8;

    /// <summary>
    /// 错误率阈值
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 0.05;
}

/// <summary>
/// 指标事件
/// </summary>
public record MetricEvent
{
    public string EventType { get; init; } = string.Empty;
    public string EventName { get; init; } = string.Empty;
    public Dictionary<string, object> Properties { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

/// <summary>
/// 首页指标摘要DTO
/// </summary>
public class HomepageMetricsSummaryDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // 性能指标
    public PerformanceMetricsDto Performance { get; set; } = new();

    // 用户行为指标
    public UserBehaviorMetricsDto UserBehavior { get; set; } = new();

    // 业务指标
    public BusinessMetricsDto Business { get; set; } = new();

    // A/B测试指标
    public Dictionary<string, ABTestMetricsDto> ABTests { get; set; } = new();
}

public class PerformanceMetricsDto
{
    public double AveragePageLoadTime { get; set; }
    public double MedianPageLoadTime { get; set; }
    public double P95PageLoadTime { get; set; }
    public double AverageApiResponseTime { get; set; }
    public double CacheHitRate { get; set; }
    public double ErrorRate { get; set; }
    public int TotalPageViews { get; set; }
    public int UniqueVisitors { get; set; }
}

public class UserBehaviorMetricsDto
{
    public int TotalInteractions { get; set; }
    public Dictionary<string, int> InteractionsByType { get; set; } = new();
    public Dictionary<string, int> ContentEngagement { get; set; } = new();
    public int SearchQueries { get; set; }
    public List<string> TopSearchQueries { get; set; } = new();
    public double AverageSessionDuration { get; set; }
    public double BounceRate { get; set; }
}

public class BusinessMetricsDto
{
    public int TotalConversions { get; set; }
    public Dictionary<string, int> ConversionsByGoal { get; set; } = new();
    public double ConversionRate { get; set; }
    public double AverageConversionValue { get; set; }
    public int NewUserRegistrations { get; set; }
    public int ReturnVisitors { get; set; }
}

public class ABTestMetricsDto
{
    public string TestId { get; set; } = string.Empty;
    public Dictionary<string, int> ExposuresByVariant { get; set; } = new();
    public Dictionary<string, int> ConversionsByVariant { get; set; } = new();
    public Dictionary<string, double> ConversionRatesByVariant { get; set; } = new();
    public bool IsStatisticallySignificant { get; set; }
    public double ConfidenceLevel { get; set; }
}

public class HomepageMetricsService : IHomepageMetricsService
{
    private readonly ILogger<HomepageMetricsService> _logger;
    private readonly HomepageMetricsOptions _options;
    private readonly Meter _meter;
    private readonly Queue<MetricEvent> _eventQueue;
    private readonly Timer _flushTimer;
    private readonly object _queueLock = new();

    // Meters for different metric types
    private readonly Counter<long> _pageViewCounter;
    private readonly Histogram<double> _pageLoadTimeHistogram;
    private readonly Histogram<double> _apiResponseTimeHistogram;
    private readonly Counter<long> _interactionCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _conversionCounter;
    private readonly Counter<long> _abTestExposureCounter;

    public HomepageMetricsService(
        ILogger<HomepageMetricsService> logger,
        IOptions<HomepageMetricsOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _eventQueue = new Queue<MetricEvent>();

        // 初始化 OpenTelemetry Meter
        _meter = new Meter("MapleBlog.Homepage", "1.0.0");

        // 初始化指标
        _pageViewCounter = _meter.CreateCounter<long>("homepage_page_views_total", "次", "首页页面浏览总数");
        _pageLoadTimeHistogram = _meter.CreateHistogram<double>("homepage_page_load_time", "ms", "首页加载时间");
        _apiResponseTimeHistogram = _meter.CreateHistogram<double>("homepage_api_response_time", "ms", "API响应时间");
        _interactionCounter = _meter.CreateCounter<long>("homepage_interactions_total", "次", "用户交互总数");
        _errorCounter = _meter.CreateCounter<long>("homepage_errors_total", "次", "错误总数");
        _conversionCounter = _meter.CreateCounter<long>("homepage_conversions_total", "次", "转化总数");
        _abTestExposureCounter = _meter.CreateCounter<long>("homepage_ab_test_exposures_total", "次", "A/B测试曝光总数");

        // 启动定时刷新
        _flushTimer = new Timer(FlushEvents, null, TimeSpan.Zero, TimeSpan.FromSeconds(_options.FlushIntervalSeconds));
    }

    public async Task RecordPageLoadTimeAsync(double loadTimeMs, string? userId = null)
    {
        if (!_options.Enabled || !ShouldSample()) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("user_type", userId != null ? "authenticated" : "anonymous"),
            new("page", "homepage")
        };

        _pageViewCounter.Add(1, tags);
        _pageLoadTimeHistogram.Record(loadTimeMs, tags);

        // 检查性能阈值
        if (loadTimeMs > _options.Thresholds.PageLoadTimeThreshold)
        {
            _logger.LogWarning("Slow page load detected: {LoadTime}ms for user {UserId}", loadTimeMs, userId ?? "anonymous");
        }

        var evt = new MetricEvent
        {
            EventType = "performance",
            EventName = "page_load_time",
            Properties = new Dictionary<string, object>
            {
                ["load_time_ms"] = loadTimeMs,
                ["page"] = "homepage",
                ["threshold_exceeded"] = loadTimeMs > _options.Thresholds.PageLoadTimeThreshold
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordApiResponseTimeAsync(string endpoint, double responseTimeMs)
    {
        if (!_options.Enabled || !ShouldSample()) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint),
            new("status", responseTimeMs < _options.Thresholds.ApiResponseTimeThreshold ? "ok" : "slow")
        };

        _apiResponseTimeHistogram.Record(responseTimeMs, tags);

        if (responseTimeMs > _options.Thresholds.ApiResponseTimeThreshold)
        {
            _logger.LogWarning("Slow API response: {Endpoint} took {ResponseTime}ms", endpoint, responseTimeMs);
        }

        var evt = new MetricEvent
        {
            EventType = "performance",
            EventName = "api_response_time",
            Properties = new Dictionary<string, object>
            {
                ["endpoint"] = endpoint,
                ["response_time_ms"] = responseTimeMs,
                ["threshold_exceeded"] = responseTimeMs > _options.Thresholds.ApiResponseTimeThreshold
            }
        };

        EnqueueEvent(evt);
    }

    public async Task RecordCacheHitRateAsync(string cacheType, bool isHit)
    {
        if (!_options.Enabled) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("cache_type", cacheType),
            new("result", isHit ? "hit" : "miss")
        };

        var counter = _meter.CreateCounter<long>("homepage_cache_operations_total");
        counter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "performance",
            EventName = "cache_operation",
            Properties = new Dictionary<string, object>
            {
                ["cache_type"] = cacheType,
                ["is_hit"] = isHit
            }
        };

        EnqueueEvent(evt);
    }

    public async Task RecordUserInteractionAsync(string action, string element, string? userId = null)
    {
        if (!_options.Enabled || !ShouldSample()) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("action", action),
            new("element", element),
            new("user_type", userId != null ? "authenticated" : "anonymous")
        };

        _interactionCounter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "interaction",
            EventName = "user_interaction",
            Properties = new Dictionary<string, object>
            {
                ["action"] = action,
                ["element"] = element
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordContentEngagementAsync(string contentType, string contentId, string action, string? userId = null)
    {
        if (!_options.Enabled || !ShouldSample()) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("content_type", contentType),
            new("action", action),
            new("user_type", userId != null ? "authenticated" : "anonymous")
        };

        var counter = _meter.CreateCounter<long>("homepage_content_engagement_total");
        counter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "engagement",
            EventName = "content_engagement",
            Properties = new Dictionary<string, object>
            {
                ["content_type"] = contentType,
                ["content_id"] = contentId,
                ["action"] = action
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordSearchQueryAsync(string query, int resultCount, string? userId = null)
    {
        if (!_options.Enabled || !ShouldSample()) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("has_results", resultCount > 0 ? "true" : "false"),
            new("user_type", userId != null ? "authenticated" : "anonymous")
        };

        var counter = _meter.CreateCounter<long>("homepage_searches_total");
        counter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "search",
            EventName = "search_query",
            Properties = new Dictionary<string, object>
            {
                ["query"] = query,
                ["result_count"] = resultCount,
                ["query_length"] = query.Length
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordConversionEventAsync(string goalName, double? value = null, string? userId = null)
    {
        if (!_options.Enabled) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("goal", goalName),
            new("has_value", value.HasValue ? "true" : "false")
        };

        _conversionCounter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "conversion",
            EventName = "conversion_event",
            Properties = new Dictionary<string, object>
            {
                ["goal_name"] = goalName,
                ["value"] = value ?? 0,
                ["currency"] = "USD"
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordErrorEventAsync(string errorType, string errorMessage, string? userId = null)
    {
        if (!_options.Enabled) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("error_type", errorType),
            new("user_type", userId != null ? "authenticated" : "anonymous")
        };

        _errorCounter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "error",
            EventName = "error_event",
            Properties = new Dictionary<string, object>
            {
                ["error_type"] = errorType,
                ["error_message"] = errorMessage,
                ["stack_trace"] = Environment.StackTrace
            },
            UserId = userId
        };

        EnqueueEvent(evt);

        _logger.LogError("Homepage error recorded: {ErrorType} - {ErrorMessage}", errorType, errorMessage);
    }

    public async Task RecordABTestExposureAsync(string testId, string variant, string? userId = null)
    {
        if (!_options.Enabled) return;

        var tags = new KeyValuePair<string, object?>[]
        {
            new("test_id", testId),
            new("variant", variant)
        };

        _abTestExposureCounter.Add(1, tags);

        var evt = new MetricEvent
        {
            EventType = "ab_test",
            EventName = "ab_test_exposure",
            Properties = new Dictionary<string, object>
            {
                ["test_id"] = testId,
                ["variant"] = variant
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task RecordABTestConversionAsync(string testId, string variant, string goalName, string? userId = null)
    {
        if (!_options.Enabled) return;

        var evt = new MetricEvent
        {
            EventType = "ab_test",
            EventName = "ab_test_conversion",
            Properties = new Dictionary<string, object>
            {
                ["test_id"] = testId,
                ["variant"] = variant,
                ["goal_name"] = goalName
            },
            UserId = userId
        };

        EnqueueEvent(evt);
    }

    public async Task<HomepageMetricsSummaryDto> GetMetricsSummaryAsync(DateTime from, DateTime to)
    {
        // 这里应该从数据库或时间序列数据库中查询数据
        // 为了示例，返回模拟数据
        return new HomepageMetricsSummaryDto
        {
            FromDate = from,
            ToDate = to,
            Performance = new PerformanceMetricsDto
            {
                AveragePageLoadTime = 1250.0,
                MedianPageLoadTime = 980.0,
                P95PageLoadTime = 2800.0,
                AverageApiResponseTime = 150.0,
                CacheHitRate = 0.85,
                ErrorRate = 0.02,
                TotalPageViews = 15420,
                UniqueVisitors = 8934
            },
            UserBehavior = new UserBehaviorMetricsDto
            {
                TotalInteractions = 45680,
                InteractionsByType = new Dictionary<string, int>
                {
                    ["click"] = 32100,
                    ["scroll"] = 8900,
                    ["search"] = 4680
                },
                SearchQueries = 4680,
                TopSearchQueries = new List<string> { "react", "javascript", "performance", "optimization" },
                AverageSessionDuration = 180.5,
                BounceRate = 0.35
            },
            Business = new BusinessMetricsDto
            {
                TotalConversions = 234,
                ConversionsByGoal = new Dictionary<string, int>
                {
                    ["newsletter_signup"] = 156,
                    ["account_creation"] = 78
                },
                ConversionRate = 0.015,
                AverageConversionValue = 25.50,
                NewUserRegistrations = 78,
                ReturnVisitors = 3456
            }
        };
    }

    public async Task<Dictionary<string, object>> GetRealTimeMetricsAsync()
    {
        // 实时指标通常从内存中的计数器或快速缓存中获取
        return new Dictionary<string, object>
        {
            ["current_active_users"] = 145,
            ["page_views_last_hour"] = 892,
            ["average_load_time_last_5_minutes"] = 1180.0,
            ["cache_hit_rate_last_hour"] = 0.87,
            ["error_count_last_hour"] = 3,
            ["conversion_count_last_hour"] = 12
        };
    }

    private void EnqueueEvent(MetricEvent evt)
    {
        lock (_queueLock)
        {
            _eventQueue.Enqueue(evt);

            // 防止队列过大
            while (_eventQueue.Count > 10000)
            {
                _eventQueue.Dequeue();
            }
        }
    }

    private void FlushEvents(object? state)
    {
        List<MetricEvent> eventsToFlush;

        lock (_queueLock)
        {
            if (_eventQueue.Count == 0) return;

            eventsToFlush = new List<MetricEvent>();
            var batchSize = Math.Min(_options.BatchSize, _eventQueue.Count);

            for (int i = 0; i < batchSize; i++)
            {
                eventsToFlush.Add(_eventQueue.Dequeue());
            }
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessEventBatchAsync(eventsToFlush);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process metric events batch");
            }
        });
    }

    private async Task ProcessEventBatchAsync(List<MetricEvent> events)
    {
        // 这里应该将事件批量写入持久化存储
        // 例如：数据库、时间序列数据库、消息队列等

        _logger.LogDebug("Processing {Count} metric events", events.Count);

        // 模拟处理延迟
        await Task.Delay(50);

        // 实际实现中，这里会：
        // 1. 将事件写入数据库或时间序列数据库
        // 2. 发送到消息队列进行进一步处理
        // 3. 触发实时警报（如果有阈值违规）
        // 4. 更新仪表板的实时指标
    }

    private bool ShouldSample()
    {
        return Random.Shared.NextDouble() < _options.SamplingRate;
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
        _meter?.Dispose();
    }
}

/// <summary>
/// 监控中间件
/// 自动收集HTTP请求的性能指标
/// </summary>
public class HomepageMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHomepageMetricsService _metricsService;
    private readonly ILogger<HomepageMetricsMiddleware> _logger;

    public HomepageMetricsMiddleware(
        RequestDelegate next,
        IHomepageMetricsService metricsService,
        ILogger<HomepageMetricsMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await _metricsService.RecordErrorEventAsync(
                ex.GetType().Name,
                ex.Message,
                context.User?.Identity?.Name
            );
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // 只监控首页相关的请求
            if (IsHomepageRelatedRequest(context.Request.Path))
            {
                await _metricsService.RecordApiResponseTimeAsync(
                    context.Request.Path,
                    stopwatch.Elapsed.TotalMilliseconds
                );
            }
        }
    }

    private static bool IsHomepageRelatedRequest(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        return pathValue == "/" ||
               pathValue?.StartsWith("/api/home") == true ||
               pathValue?.StartsWith("/api/posts/popular") == true ||
               pathValue?.StartsWith("/api/posts/featured") == true ||
               pathValue?.StartsWith("/api/categories") == true ||
               pathValue?.StartsWith("/api/stats") == true;
    }
}