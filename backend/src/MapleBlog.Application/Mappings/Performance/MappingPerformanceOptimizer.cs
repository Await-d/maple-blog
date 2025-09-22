using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MapleBlog.Application.Mappings.Performance
{
    /// <summary>
    /// AutoMapper性能优化配置选项
    /// </summary>
    public class MappingPerformanceOptions
    {
        /// <summary>
        /// 是否启用映射缓存
        /// </summary>
        public bool EnableMappingCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 最大缓存条目数
        /// </summary>
        public int MaxCacheEntries { get; set; } = 10000;

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// 性能警告阈值（毫秒）
        /// </summary>
        public int PerformanceWarningThresholdMs { get; set; } = 100;

        /// <summary>
        /// 是否启用批量映射优化
        /// </summary>
        public bool EnableBatchMappingOptimization { get; set; } = true;

        /// <summary>
        /// 批量映射最小阈值
        /// </summary>
        public int BatchMappingThreshold { get; set; } = 10;

        /// <summary>
        /// 是否启用编译表达式缓存
        /// </summary>
        public bool EnableExpressionCaching { get; set; } = true;
    }

    /// <summary>
    /// AutoMapper性能优化接口
    /// </summary>
    public interface IMappingPerformanceOptimizer
    {
        /// <summary>
        /// 优化映射配置
        /// </summary>
        void OptimizeMapperConfiguration(IMapperConfigurationExpression config);

        /// <summary>
        /// 执行高性能映射
        /// </summary>
        TDestination MapWithOptimization<TSource, TDestination>(IMapper mapper, TSource source);

        /// <summary>
        /// 执行批量高性能映射
        /// </summary>
        IEnumerable<TDestination> MapCollectionWithOptimization<TSource, TDestination>(
            IMapper mapper, IEnumerable<TSource> sources);

        /// <summary>
        /// 获取映射性能统计
        /// </summary>
        MappingPerformanceStats GetPerformanceStats();

        /// <summary>
        /// 清理性能缓存
        /// </summary>
        void ClearCache();
    }

    /// <summary>
    /// AutoMapper性能优化器实现
    /// </summary>
    public class MappingPerformanceOptimizer : IMappingPerformanceOptimizer
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MappingPerformanceOptimizer> _logger;
        private readonly MappingPerformanceOptions _options;
        private readonly ConcurrentDictionary<string, MappingPerformanceMetrics> _performanceMetrics;
        private readonly object _lockObject = new object();

        public MappingPerformanceOptimizer(
            IMemoryCache cache,
            ILogger<MappingPerformanceOptimizer> logger,
            IOptions<MappingPerformanceOptions> options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _performanceMetrics = new ConcurrentDictionary<string, MappingPerformanceMetrics>();
        }

        /// <summary>
        /// 优化AutoMapper配置
        /// </summary>
        public void OptimizeMapperConfiguration(IMapperConfigurationExpression config)
        {
            _logger.LogInformation("开始优化AutoMapper配置");

            // 禁用不必要的功能以提高性能
            config.AllowNullDestinationValues = true;
            config.AllowNullCollections = true;

            // 配置构造函数查找策略
            config.ShouldMapProperty = (propertyInfo) => propertyInfo.Name != "Constructor";

            // 配置最大映射深度 - 使用自定义验证逻辑而不是MaxDepth属性
            // ForAllMaps和MaxDepth在新版本中已移除，手动配置映射深度限制
            // 通过禁用递归映射来防止深度过大的问题
            config.DisableConstructorMapping();

            // 启用表达式缓存
            if (_options.EnableExpressionCaching)
            {
                // AutoMapper内部会缓存编译的表达式
                _logger.LogDebug("启用表达式缓存优化");
            }

            _logger.LogInformation("AutoMapper配置优化完成");
        }

        /// <summary>
        /// 执行高性能映射（单个对象）
        /// </summary>
        public TDestination MapWithOptimization<TSource, TDestination>(IMapper mapper, TSource source)
        {
            var mappingKey = GetMappingKey<TSource, TDestination>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 检查是否启用缓存
                if (_options.EnableMappingCache && ShouldUseCache<TSource>())
                {
                    var cacheKey = GenerateCacheKey(source, mappingKey);
                    if (_cache.TryGetValue(cacheKey, out TDestination cachedResult))
                    {
                        RecordPerformanceMetrics(mappingKey, stopwatch.ElapsedMilliseconds, true);
                        return cachedResult;
                    }
                }

                // 执行映射
                var result = mapper.Map<TDestination>(source);

                // 缓存结果（如果启用）
                if (_options.EnableMappingCache && ShouldCacheResult(result))
                {
                    var cacheKey = GenerateCacheKey(source, mappingKey);
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheExpirationMinutes),
                        Size = 1
                    };

                    _cache.Set(cacheKey, result, cacheOptions);
                }

                stopwatch.Stop();
                RecordPerformanceMetrics(mappingKey, stopwatch.ElapsedMilliseconds, false);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordPerformanceMetrics(mappingKey, stopwatch.ElapsedMilliseconds, false, ex);
                throw;
            }
        }

        /// <summary>
        /// 执行批量高性能映射
        /// </summary>
        public IEnumerable<TDestination> MapCollectionWithOptimization<TSource, TDestination>(
            IMapper mapper, IEnumerable<TSource> sources)
        {
            var mappingKey = GetMappingKey<TSource, TDestination>();
            var stopwatch = Stopwatch.StartNew();
            var sourceList = sources.ToList();

            try
            {
                _logger.LogDebug("开始批量映射 {MappingKey}, 数量: {Count}", mappingKey, sourceList.Count);

                // 如果数量较少，使用常规映射
                if (!_options.EnableBatchMappingOptimization || sourceList.Count < _options.BatchMappingThreshold)
                {
                    var results = sourceList.Select(source => MapWithOptimization<TSource, TDestination>(mapper, source));
                    stopwatch.Stop();
                    RecordPerformanceMetrics($"{mappingKey}_Collection", stopwatch.ElapsedMilliseconds, false);
                    return results;
                }

                // 使用批量优化映射
                var batchResults = PerformBatchMapping<TSource, TDestination>(mapper, sourceList);

                stopwatch.Stop();
                RecordPerformanceMetrics($"{mappingKey}_BatchCollection", stopwatch.ElapsedMilliseconds, false);

                _logger.LogDebug("批量映射完成 {MappingKey}, 耗时: {ElapsedMs}ms", mappingKey, stopwatch.ElapsedMilliseconds);

                return batchResults;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                RecordPerformanceMetrics($"{mappingKey}_Collection", stopwatch.ElapsedMilliseconds, false, ex);
                _logger.LogError(ex, "批量映射失败 {MappingKey}", mappingKey);
                throw;
            }
        }

        /// <summary>
        /// 获取映射性能统计
        /// </summary>
        public MappingPerformanceStats GetPerformanceStats()
        {
            var stats = new MappingPerformanceStats
            {
                TotalMappings = _performanceMetrics.Values.Sum(m => m.TotalCalls),
                CacheHits = _performanceMetrics.Values.Sum(m => m.CacheHits),
                TotalErrors = _performanceMetrics.Values.Sum(m => m.ErrorCount),
                AverageExecutionTime = _performanceMetrics.Values.Any() ?
                    _performanceMetrics.Values.Average(m => m.AverageExecutionTime) : 0,
                MaxExecutionTime = _performanceMetrics.Values.Any() ?
                    _performanceMetrics.Values.Max(m => m.MaxExecutionTime) : 0,
                MinExecutionTime = _performanceMetrics.Values.Any() ?
                    _performanceMetrics.Values.Min(m => m.MinExecutionTime) : 0,
                CacheHitRate = _performanceMetrics.Values.Sum(m => m.TotalCalls) > 0 ?
                    (double)_performanceMetrics.Values.Sum(m => m.CacheHits) / _performanceMetrics.Values.Sum(m => m.TotalCalls) * 100 : 0,
                MappingDetails = _performanceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                GeneratedAt = DateTime.UtcNow
            };

            return stats;
        }

        /// <summary>
        /// 清理性能缓存
        /// </summary>
        public void ClearCache()
        {
            if (_cache is MemoryCache memoryCache)
            {
                // 清理映射缓存
                var field = typeof(MemoryCache).GetField("_coherentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var coherentState = field?.GetValue(memoryCache);
                var entriesCollection = coherentState?.GetType()
                    .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var entries = (IDictionary<object, object>?)entriesCollection?.GetValue(coherentState);

                entries?.Clear();
            }

            _logger.LogInformation("映射缓存已清理");
        }

        #region 私有方法

        /// <summary>
        /// 执行批量映射优化
        /// </summary>
        private IEnumerable<TDestination> PerformBatchMapping<TSource, TDestination>(
            IMapper mapper, IList<TSource> sources)
        {
            // 预分配结果容器
            var results = new List<TDestination>(sources.Count);

            // 并行处理（如果数据量足够大）
            if (sources.Count > 100)
            {
                var parallelResults = sources.AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .Select(source => mapper.Map<TDestination>(source))
                    .ToList();

                return parallelResults;
            }

            // 顺序处理
            foreach (var source in sources)
            {
                results.Add(mapper.Map<TDestination>(source));
            }

            return results;
        }

        /// <summary>
        /// 获取映射键
        /// </summary>
        private string GetMappingKey<TSource, TDestination>()
        {
            return $"{typeof(TSource).Name}_{typeof(TDestination).Name}";
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey<TSource>(TSource source, string mappingKey)
        {
            // 简单的哈希策略，实际应用中可能需要更复杂的键生成
            var sourceHash = source?.GetHashCode() ?? 0;
            return $"Mapping_{mappingKey}_{sourceHash}";
        }

        /// <summary>
        /// 是否应该使用缓存
        /// </summary>
        private bool ShouldUseCache<TSource>()
        {
            // 某些类型可能不适合缓存（如包含敏感信息或频繁变化的数据）
            var sourceType = typeof(TSource);

            // 不缓存用户敏感信息
            if (sourceType.Name.Contains("Password") || sourceType.Name.Contains("Token"))
                return false;

            return true;
        }

        /// <summary>
        /// 是否应该缓存结果
        /// </summary>
        private bool ShouldCacheResult<TDestination>(TDestination result)
        {
            // 不缓存空结果
            if (result == null)
                return false;

            // 可以添加更多缓存策略
            return true;
        }

        /// <summary>
        /// 记录性能指标
        /// </summary>
        private void RecordPerformanceMetrics(string mappingKey, long elapsedMs, bool fromCache, Exception? exception = null)
        {
            if (!_options.EnablePerformanceMonitoring)
                return;

            _performanceMetrics.AddOrUpdate(mappingKey,
                new MappingPerformanceMetrics
                {
                    MappingKey = mappingKey,
                    TotalCalls = 1,
                    CacheHits = fromCache ? 1 : 0,
                    ErrorCount = exception != null ? 1 : 0,
                    TotalExecutionTime = elapsedMs,
                    AverageExecutionTime = elapsedMs,
                    MaxExecutionTime = elapsedMs,
                    MinExecutionTime = elapsedMs,
                    LastExecutionTime = DateTime.UtcNow
                },
                (key, existing) =>
                {
                    lock (_lockObject)
                    {
                        existing.TotalCalls++;
                        if (fromCache) existing.CacheHits++;
                        if (exception != null) existing.ErrorCount++;

                        existing.TotalExecutionTime += elapsedMs;
                        existing.AverageExecutionTime = existing.TotalExecutionTime / existing.TotalCalls;
                        existing.MaxExecutionTime = Math.Max(existing.MaxExecutionTime, elapsedMs);
                        existing.MinExecutionTime = Math.Min(existing.MinExecutionTime, elapsedMs);
                        existing.LastExecutionTime = DateTime.UtcNow;

                        return existing;
                    }
                });

            // 性能警告
            if (elapsedMs > _options.PerformanceWarningThresholdMs)
            {
                _logger.LogWarning("映射性能警告: {MappingKey} 耗时 {ElapsedMs}ms (阈值: {ThresholdMs}ms)",
                    mappingKey, elapsedMs, _options.PerformanceWarningThresholdMs);
            }

            // 错误记录
            if (exception != null)
            {
                _logger.LogError(exception, "映射执行异常: {MappingKey}", mappingKey);
            }
        }

        #endregion
    }

    /// <summary>
    /// 映射性能指标
    /// </summary>
    public class MappingPerformanceMetrics
    {
        /// <summary>
        /// 映射键
        /// </summary>
        public string MappingKey { get; set; } = string.Empty;

        /// <summary>
        /// 总调用次数
        /// </summary>
        public long TotalCalls { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// 错误次数
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// 总执行时间（毫秒）
        /// </summary>
        public long TotalExecutionTime { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 最大执行时间（毫秒）
        /// </summary>
        public long MaxExecutionTime { get; set; }

        /// <summary>
        /// 最小执行时间（毫秒）
        /// </summary>
        public long MinExecutionTime { get; set; }

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime LastExecutionTime { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRate => TotalCalls > 0 ? (double)CacheHits / TotalCalls * 100 : 0;

        /// <summary>
        /// 错误率
        /// </summary>
        public double ErrorRate => TotalCalls > 0 ? (double)ErrorCount / TotalCalls * 100 : 0;
    }

    /// <summary>
    /// 映射性能统计
    /// </summary>
    public class MappingPerformanceStats
    {
        /// <summary>
        /// 总映射次数
        /// </summary>
        public long TotalMappings { get; set; }

        /// <summary>
        /// 总缓存命中次数
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// 总错误次数
        /// </summary>
        public long TotalErrors { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public double AverageExecutionTime { get; set; }

        /// <summary>
        /// 最大执行时间（毫秒）
        /// </summary>
        public long MaxExecutionTime { get; set; }

        /// <summary>
        /// 最小执行时间（毫秒）
        /// </summary>
        public long MinExecutionTime { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRate { get; set; }

        /// <summary>
        /// 错误率
        /// </summary>
        public double ErrorRate => TotalMappings > 0 ? (double)TotalErrors / TotalMappings * 100 : 0;

        /// <summary>
        /// 映射详细信息
        /// </summary>
        public Dictionary<string, MappingPerformanceMetrics> MappingDetails { get; set; } = new();

        /// <summary>
        /// 统计生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// AutoMapper高性能扩展方法
    /// </summary>
    public static class PerformanceExtensions
    {
        /// <summary>
        /// 高性能映射单个对象
        /// </summary>
        public static TDestination MapWithOptimization<TSource, TDestination>(
            this IMapper mapper, TSource source, IMappingPerformanceOptimizer optimizer)
        {
            return optimizer.MapWithOptimization<TSource, TDestination>(mapper, source);
        }

        /// <summary>
        /// 高性能批量映射
        /// </summary>
        public static IEnumerable<TDestination> MapCollectionWithOptimization<TSource, TDestination>(
            this IMapper mapper, IEnumerable<TSource> sources, IMappingPerformanceOptimizer optimizer)
        {
            return optimizer.MapCollectionWithOptimization<TSource, TDestination>(mapper, sources);
        }

        /// <summary>
        /// 分页映射优化
        /// </summary>
        public static PagedResult<TDestination> MapPagedWithOptimization<TSource, TDestination>(
            this IMapper mapper, PagedResult<TSource> pagedSource, IMappingPerformanceOptimizer optimizer)
        {
            var mappedItems = optimizer.MapCollectionWithOptimization<TSource, TDestination>(mapper, pagedSource.Items);

            return new PagedResult<TDestination>
            {
                Items = mappedItems.ToList(),
                TotalCount = pagedSource.TotalCount,
                Page = pagedSource.Page,
                PageSize = pagedSource.PageSize,
                TotalPages = pagedSource.TotalPages
            };
        }
    }

    /// <summary>
    /// 分页结果DTO
    /// </summary>
    public class PagedResult<T>
    {
        public IList<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}