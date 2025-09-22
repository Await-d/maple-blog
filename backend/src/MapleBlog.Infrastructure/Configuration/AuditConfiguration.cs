using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MapleBlog.Infrastructure.Configuration
{
    /// <summary>
    /// 审计系统配置
    /// </summary>
    public class AuditConfiguration
    {
        /// <summary>
        /// 是否启用审计系统
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 是否启用HTTP请求审计
        /// </summary>
        public bool EnableHttpAudit { get; set; } = true;

        /// <summary>
        /// 是否启用数据库操作审计
        /// </summary>
        public bool EnableDatabaseAudit { get; set; } = true;

        /// <summary>
        /// 是否启用认证审计
        /// </summary>
        public bool EnableAuthenticationAudit { get; set; } = true;

        /// <summary>
        /// 日志保留天数
        /// </summary>
        public int RetentionDays { get; set; } = 365;

        /// <summary>
        /// 批处理大小
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 批处理超时时间（秒）
        /// </summary>
        public int BatchTimeout { get; set; } = 30;

        /// <summary>
        /// 最大内存队列大小
        /// </summary>
        public int MaxQueueSize { get; set; } = 10000;

        /// <summary>
        /// 是否启用异步处理
        /// </summary>
        public bool EnableAsyncProcessing { get; set; } = true;

        /// <summary>
        /// 异步处理线程数
        /// </summary>
        public int AsyncProcessorThreads { get; set; } = 2;

        /// <summary>
        /// 敏感数据脱敏
        /// </summary>
        public SensitiveDataConfiguration SensitiveData { get; set; } = new();

        /// <summary>
        /// 性能配置
        /// </summary>
        public PerformanceConfiguration Performance { get; set; } = new();

        /// <summary>
        /// 报警配置
        /// </summary>
        public AlertConfiguration Alerts { get; set; } = new();
    }

    /// <summary>
    /// 敏感数据配置
    /// </summary>
    public class SensitiveDataConfiguration
    {
        /// <summary>
        /// 是否启用敏感数据脱敏
        /// </summary>
        public bool EnableMasking { get; set; } = true;

        /// <summary>
        /// 脱敏字符
        /// </summary>
        public string MaskCharacter { get; set; } = "*";

        /// <summary>
        /// 敏感字段列表
        /// </summary>
        public List<string> SensitiveFields { get; set; } = new()
        {
            "password", "token", "secret", "key", "authorization", "cookie",
            "x-api-key", "x-auth-token", "bearer", "refresh_token", "access_token",
            "credit_card", "ssn", "phone", "email"
        };

        /// <summary>
        /// 敏感值正则表达式
        /// </summary>
        public List<string> SensitivePatterns { get; set; } = new()
        {
            @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // 信用卡号
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" // 邮箱
        };
    }

    /// <summary>
    /// 性能配置
    /// </summary>
    public class PerformanceConfiguration
    {
        /// <summary>
        /// 启用数据库连接池
        /// </summary>
        public bool EnableConnectionPooling { get; set; } = true;

        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections { get; set; } = 100;

        /// <summary>
        /// 启用查询缓存
        /// </summary>
        public bool EnableQueryCache { get; set; } = true;

        /// <summary>
        /// 缓存过期时间（分钟）
        /// </summary>
        public int CacheExpirationMinutes { get; set; } = 30;

        /// <summary>
        /// 启用索引优化
        /// </summary>
        public bool EnableIndexOptimization { get; set; } = true;

        /// <summary>
        /// 启用分区
        /// </summary>
        public bool EnablePartitioning { get; set; } = false;

        /// <summary>
        /// 分区策略（Monthly, Yearly）
        /// </summary>
        public string PartitionStrategy { get; set; } = "Monthly";
    }

    /// <summary>
    /// 报警配置
    /// </summary>
    public class AlertConfiguration
    {
        /// <summary>
        /// 启用报警
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 失败率阈值（百分比）
        /// </summary>
        public double FailureRateThreshold { get; set; } = 10.0;

        /// <summary>
        /// 高风险操作阈值（每小时）
        /// </summary>
        public int HighRiskOperationThreshold { get; set; } = 100;

        /// <summary>
        /// 异常IP检测阈值（每小时请求数）
        /// </summary>
        public int SuspiciousIpThreshold { get; set; } = 1000;

        /// <summary>
        /// 邮件通知地址
        /// </summary>
        public List<string> NotificationEmails { get; set; } = new();

        /// <summary>
        /// Webhook通知URL
        /// </summary>
        public string? WebhookUrl { get; set; }
    }

    /// <summary>
    /// 审计配置扩展方法
    /// </summary>
    public static class AuditConfigurationExtensions
    {
        /// <summary>
        /// 添加审计配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddAuditConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // 绑定配置
            var auditConfig = new AuditConfiguration();
            configuration.GetSection("Audit").Bind(auditConfig);

            // 注册为单例
            services.AddSingleton(auditConfig);

            // 验证配置
            ValidateConfiguration(auditConfig);

            return services;
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        /// <param name="config">配置</param>
        private static void ValidateConfiguration(AuditConfiguration config)
        {
            if (config.RetentionDays < 1)
                throw new ArgumentException("审计日志保留天数必须大于0");

            if (config.BatchSize < 1 || config.BatchSize > 10000)
                throw new ArgumentException("批处理大小必须在1-10000之间");

            if (config.BatchTimeout < 1 || config.BatchTimeout > 300)
                throw new ArgumentException("批处理超时时间必须在1-300秒之间");

            if (config.MaxQueueSize < 100 || config.MaxQueueSize > 1000000)
                throw new ArgumentException("最大队列大小必须在100-1000000之间");

            if (config.AsyncProcessorThreads < 1 || config.AsyncProcessorThreads > 10)
                throw new ArgumentException("异步处理线程数必须在1-10之间");
        }
    }
}