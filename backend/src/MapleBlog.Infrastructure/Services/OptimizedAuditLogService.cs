using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Configuration;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Infrastructure.Services
{
    /// <summary>
    /// 优化的审计日志服务 - 支持批处理、异步处理和性能优化
    /// </summary>
    public class OptimizedAuditLogService : IAuditLogService, IDisposable
    {
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<OptimizedAuditLogService> _logger;
        private readonly AuditConfiguration _config;
        private readonly Channel<AuditLog> _auditChannel;
        private readonly ChannelWriter<AuditLog> _writer;
        private readonly ChannelReader<AuditLog> _reader;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task[] _processingTasks;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _semaphore;
        private bool _disposed = false;

        // 性能统计
        private long _totalProcessed = 0;
        private long _totalFailed = 0;
        private readonly ConcurrentDictionary<string, long> _actionStats = new();

        public OptimizedAuditLogService(
            IAuditLogRepository repository,
            ILogger<OptimizedAuditLogService> logger,
            IOptions<AuditConfiguration> config)
        {
            _repository = repository;
            _logger = logger;
            _config = config.Value;

            // 配置JSON序列化选项
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // 创建高性能通道
            var options = new BoundedChannelOptions(_config.MaxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _auditChannel = Channel.CreateBounded<AuditLog>(options);
            _writer = _auditChannel.Writer;
            _reader = _auditChannel.Reader;

            _cancellationTokenSource = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(_config.AsyncProcessorThreads);

            // 启动后台处理任务
            if (_config.EnableAsyncProcessing)
            {
                _processingTasks = new Task[_config.AsyncProcessorThreads];
                for (int i = 0; i < _config.AsyncProcessorThreads; i++)
                {
                    _processingTasks[i] = Task.Run(() => ProcessAuditLogsAsync(_cancellationTokenSource.Token));
                }

                _logger.LogInformation("审计日志服务已启动，启用 {ThreadCount} 个处理线程", _config.AsyncProcessorThreads);
            }
            else
            {
                _processingTasks = Array.Empty<Task>();
            }
        }

        /// <summary>
        /// 记录审计日志
        /// </summary>
        public async Task<bool> LogAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            if (_disposed || auditLog == null || !_config.Enabled)
                return false;

            try
            {
                // 设置创建时间
                if (auditLog.CreatedAt == default)
                    auditLog.CreatedAt = DateTime.UtcNow;

                // 应用敏感数据脱敏
                ApplySensitiveDataMasking(auditLog);

                if (_config.EnableAsyncProcessing)
                {
                    // 异步处理：加入队列
                    var success = _writer.TryWrite(auditLog);
                    if (!success)
                    {
                        _logger.LogWarning("审计日志队列已满，丢弃日志: {Summary}", auditLog.GetSummary());
                        return false;
                    }

                    // 更新统计
                    Interlocked.Increment(ref _totalProcessed);
                    _actionStats.AddOrUpdate(auditLog.Action, 1, (key, oldValue) => oldValue + 1);

                    return true;
                }
                else
                {
                    // 同步处理：直接保存
                    await _repository.AddAsync(auditLog, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    Interlocked.Increment(ref _totalProcessed);
                    _actionStats.AddOrUpdate(auditLog.Action, 1, (key, oldValue) => oldValue + 1);

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录审计日志失败: {Summary}", auditLog?.GetSummary() ?? "未知操作");
                Interlocked.Increment(ref _totalFailed);
                return false;
            }
        }

        /// <summary>
        /// 批量记录审计日志
        /// </summary>
        public async Task<int> LogBatchAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
        {
            if (_disposed || !_config.Enabled)
                return 0;

            var logs = auditLogs.ToList();
            if (!logs.Any())
                return 0;

            var successCount = 0;

            try
            {
                var now = DateTime.UtcNow;
                foreach (var log in logs)
                {
                    if (log.CreatedAt == default)
                        log.CreatedAt = now;

                    ApplySensitiveDataMasking(log);
                }

                if (_config.EnableAsyncProcessing)
                {
                    // 异步处理：批量加入队列
                    foreach (var log in logs)
                    {
                        var success = _writer.TryWrite(log);
                        if (success)
                        {
                            successCount++;
                            _actionStats.AddOrUpdate(log.Action, 1, (key, oldValue) => oldValue + 1);
                        }
                        else
                        {
                            _logger.LogWarning("审计日志队列已满，丢弃日志: {Summary}", log.GetSummary());
                        }
                    }
                }
                else
                {
                    // 同步处理：批量保存
                    successCount = await _repository.AddBatchAsync(logs, cancellationToken);
                    foreach (var log in logs)
                    {
                        _actionStats.AddOrUpdate(log.Action, 1, (key, oldValue) => oldValue + 1);
                    }
                }

                Interlocked.Add(ref _totalProcessed, successCount);
                _logger.LogDebug("批量记录审计日志完成: 成功 {SuccessCount}/{TotalCount} 条", successCount, logs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量记录审计日志失败");
                Interlocked.Increment(ref _totalFailed);
            }

            return successCount;
        }

        /// <summary>
        /// 记录用户操作
        /// </summary>
        public async Task<bool> LogUserActionAsync(
            Guid? userId,
            string? userName,
            string action,
            string resourceType,
            string? resourceId = null,
            string? description = null,
            object? oldValues = null,
            object? newValues = null,
            string? ipAddress = null,
            string? userAgent = null,
            CancellationToken cancellationToken = default)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Description = description,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Category = GetActionCategory(action, resourceType),
                RiskLevel = GetRiskLevel(action, resourceType),
                IsSensitive = IsSensitiveAction(action, resourceType)
            };

            // 序列化变更数据
            if (oldValues != null)
                auditLog.OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions);

            if (newValues != null)
                auditLog.NewValues = JsonSerializer.Serialize(newValues, _jsonOptions);

            return await LogAsync(auditLog, cancellationToken);
        }

        /// <summary>
        /// 记录认证事件
        /// </summary>
        public async Task<bool> LogAuthenticationAsync(
            Guid? userId,
            string? userName,
            string action,
            string result,
            string? ipAddress = null,
            string? userAgent = null,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (!_config.EnableAuthenticationAudit)
                return true;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                ResourceType = "Authentication",
                Result = result,
                ErrorMessage = errorMessage,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Category = "Authentication",
                RiskLevel = result == "Failed" ? "High" : "Medium",
                IsSensitive = true
            };

            return await LogAsync(auditLog, cancellationToken);
        }

        /// <summary>
        /// 查询审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(Application.DTOs.Admin.AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            // 转换为Domain层的过滤器
            var domainFilter = new Domain.ValueObjects.AuditLogFilter
            {
                UserId = filter.UserId,
                UserName = filter.UserName,
                Action = filter.Action,
                ResourceType = filter.ResourceType,
                ResourceId = filter.ResourceId,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                IpAddress = filter.IpAddress,
                Result = filter.Result,
                Category = filter.Category,
                RiskLevel = filter.RiskLevel,
                IsSensitive = filter.IsSensitive,
                Page = filter.Page,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                IsDescending = filter.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase)
            };

            return await _repository.GetByFilterAsync(domainFilter, cancellationToken);
        }

        /// <summary>
        /// 获取审计统计信息
        /// </summary>
        public async Task<Application.DTOs.Admin.AuditLogStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var domainStats = await _repository.GetStatisticsAsync(startDate, endDate, cancellationToken);

            // 转换为Application层DTO
            var stats = new Application.DTOs.Admin.AuditLogStatistics
            {
                TotalLogs = domainStats.TotalLogs,
                SuccessCount = domainStats.SuccessfulOperations,
                FailureCount = domainStats.FailedOperations,
                HighRiskOperationCount = domainStats.HighRiskOperations,
                SensitiveOperationCount = domainStats.SensitiveOperations,
                UniqueUserCount = (int)domainStats.UniqueUsers,
                UniqueIpCount = (int)domainStats.UniqueIpAddresses,
                ActionStatistics = domainStats.TopActions ?? new Dictionary<string, long>(),
                ResourceTypeStatistics = domainStats.TopResourceTypes ?? new Dictionary<string, long>(),
                CategoryStatistics = new Dictionary<string, long>(), // Domain层暂时没有这个统计
                RiskLevelStatistics = new Dictionary<string, long>(), // Domain层暂时没有这个统计
                HourlyStatistics = domainStats.ActivityByHour ?? new Dictionary<int, long>(),
                DailyStatistics = domainStats.ActivityByDate ?? new Dictionary<DateTime, long>(),
                TopActiveUsers = new List<Application.DTOs.Admin.UserActivityStats>(), // 需要单独查询
                TopActiveIps = new List<Application.DTOs.Admin.IpActivityStats>() // 需要单独查询
            };

            // 添加内存统计
            foreach (var kvp in _actionStats)
            {
                if (!stats.ActionStatistics.ContainsKey(kvp.Key))
                    stats.ActionStatistics[kvp.Key] = 0;
            }

            return stats;
        }

        /// <summary>
        /// 清理过期审计日志
        /// </summary>
        public async Task<int> CleanupOldLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default)
        {
            return await _repository.DeleteOldLogsAsync(retentionDays, _config.BatchSize, cancellationToken);
        }

        /// <summary>
        /// 获取最近的审计日志
        /// </summary>
        public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            return await _repository.GetRecentAsync(limit, cancellationToken);
        }

        /// <summary>
        /// 统计审计日志数量
        /// </summary>
        public async Task<int> CountAsync(string? action = null, string? resourceType = null, DateTime? startTime = null, DateTime? endTime = null, CancellationToken cancellationToken = default)
        {
            var filter = new Domain.ValueObjects.AuditLogFilter
            {
                Action = action,
                ResourceType = resourceType,
                StartDate = startTime,
                EndDate = endTime
            };

            var count = await _repository.CountByFilterAsync(filter, cancellationToken);
            return (int)Math.Min(count, int.MaxValue);
        }

        /// <summary>
        /// 获取服务统计信息
        /// </summary>
        public ServiceStatistics GetServiceStatistics()
        {
            return new ServiceStatistics
            {
                TotalProcessed = _totalProcessed,
                TotalFailed = _totalFailed,
                QueueSize = _auditChannel.Reader.CanCount ? _auditChannel.Reader.Count : -1,
                ActionStats = new Dictionary<string, long>(_actionStats),
                IsAsyncEnabled = _config.EnableAsyncProcessing,
                ProcessorThreads = _config.AsyncProcessorThreads
            };
        }

        /// <summary>
        /// 后台处理审计日志
        /// </summary>
        private async Task ProcessAuditLogsAsync(CancellationToken cancellationToken)
        {
            var batch = new List<AuditLog>(_config.BatchSize);
            var lastSaveTime = DateTime.UtcNow;

            try
            {
                await foreach (var auditLog in _reader.ReadAllAsync(cancellationToken))
                {
                    batch.Add(auditLog);

                    // 检查是否需要保存批次
                    var shouldSave = batch.Count >= _config.BatchSize ||
                                   DateTime.UtcNow - lastSaveTime > TimeSpan.FromSeconds(_config.BatchTimeout);

                    if (shouldSave && batch.Count > 0)
                    {
                        await SaveBatchAsync(batch, cancellationToken);
                        batch.Clear();
                        lastSaveTime = DateTime.UtcNow;
                    }
                }

                // 处理剩余的日志
                if (batch.Count > 0)
                {
                    await SaveBatchAsync(batch, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("审计日志处理任务已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审计日志处理任务异常");
            }
        }

        /// <summary>
        /// 保存批次
        /// </summary>
        private async Task SaveBatchAsync(List<AuditLog> batch, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var savedCount = await _repository.AddBatchAsync(batch, cancellationToken);
                _logger.LogDebug("保存审计日志批次: {SavedCount}/{TotalCount}", savedCount, batch.Count);

                if (savedCount < batch.Count)
                {
                    Interlocked.Add(ref _totalFailed, batch.Count - savedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存审计日志批次失败: {Count} 条", batch.Count);
                Interlocked.Add(ref _totalFailed, batch.Count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 应用敏感数据脱敏
        /// </summary>
        private void ApplySensitiveDataMasking(AuditLog auditLog)
        {
            if (!_config.SensitiveData.EnableMasking)
                return;

            try
            {
                // 脱敏描述
                if (!string.IsNullOrEmpty(auditLog.Description))
                    auditLog.Description = MaskSensitiveData(auditLog.Description);

                // 脱敏变更数据
                if (!string.IsNullOrEmpty(auditLog.OldValues))
                    auditLog.OldValues = MaskSensitiveData(auditLog.OldValues);

                if (!string.IsNullOrEmpty(auditLog.NewValues))
                    auditLog.NewValues = MaskSensitiveData(auditLog.NewValues);

                // 脱敏额外数据
                if (!string.IsNullOrEmpty(auditLog.AdditionalData))
                    auditLog.AdditionalData = MaskSensitiveData(auditLog.AdditionalData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "敏感数据脱敏失败: {Summary}", auditLog.GetSummary());
            }
        }

        /// <summary>
        /// 脱敏敏感数据
        /// </summary>
        private string MaskSensitiveData(string data)
        {
            var result = data;

            // 按字段名脱敏
            foreach (var field in _config.SensitiveData.SensitiveFields)
            {
                var pattern = $@"""{field}"":\s*""[^""]*""";
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    pattern,
                    $@"""{field}"":""***MASKED***""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // 按正则表达式脱敏
            foreach (var pattern in _config.SensitiveData.SensitivePatterns)
            {
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    pattern,
                    new string(_config.SensitiveData.MaskCharacter[0], 10));
            }

            return result;
        }

        /// <summary>
        /// 获取操作分类
        /// </summary>
        private static string GetActionCategory(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();

            return actionLower switch
            {
                var a when a.Contains("login") || a.Contains("logout") || a.Contains("register") => "Authentication",
                var a when a.Contains("permission") || a.Contains("role") || a.Contains("access") => "Authorization",
                var a when a.Contains("create") || a.Contains("update") || a.Contains("delete") || a.Contains("modify") => "DataModification",
                var a when a.Contains("config") || a.Contains("setting") || a.Contains("system") => "SystemConfiguration",
                _ => "General"
            };
        }

        /// <summary>
        /// 获取风险级别
        /// </summary>
        private static string GetRiskLevel(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            if (actionLower.Contains("delete") || actionLower.Contains("remove"))
                return "High";

            if (resourceLower.Contains("user") || resourceLower.Contains("role") || resourceLower.Contains("permission"))
            {
                if (actionLower.Contains("create") || actionLower.Contains("update"))
                    return "Medium";
            }

            if (resourceLower.Contains("system") || resourceLower.Contains("config"))
                return "Medium";

            return "Low";
        }

        /// <summary>
        /// 检查是否为敏感操作
        /// </summary>
        private static bool IsSensitiveAction(string action, string resourceType)
        {
            var actionLower = action.ToLowerInvariant();
            var resourceLower = resourceType.ToLowerInvariant();

            return actionLower.Contains("login") || actionLower.Contains("logout") || actionLower.Contains("register") ||
                   resourceLower.Contains("role") || resourceLower.Contains("permission") ||
                   actionLower.Contains("delete") || actionLower.Contains("remove") ||
                   resourceLower.Contains("system") || resourceLower.Contains("config");
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // 停止接收新的日志
                _writer.Complete();

                // 等待处理任务完成
                _cancellationTokenSource.Cancel();
                Task.WaitAll(_processingTasks, TimeSpan.FromSeconds(30));

                _cancellationTokenSource.Dispose();
                _semaphore.Dispose();

                _logger.LogInformation("审计日志服务已停止，总处理: {TotalProcessed} 条，失败: {TotalFailed} 条",
                    _totalProcessed, _totalFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审计日志服务停止时发生错误");
            }
        }
    }

    /// <summary>
    /// 服务统计信息
    /// </summary>
    public class ServiceStatistics
    {
        public long TotalProcessed { get; set; }
        public long TotalFailed { get; set; }
        public int QueueSize { get; set; }
        public Dictionary<string, long> ActionStats { get; set; } = new();
        public bool IsAsyncEnabled { get; set; }
        public int ProcessorThreads { get; set; }
    }
}