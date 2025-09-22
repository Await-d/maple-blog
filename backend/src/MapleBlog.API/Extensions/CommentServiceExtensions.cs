using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.API.Hubs;
using MapleBlog.Infrastructure.Services;

namespace MapleBlog.API.Extensions;

/// <summary>
/// 评论系统服务配置扩展
/// </summary>
public static class CommentServiceExtensions
{
    /// <summary>
    /// 添加评论系统服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCommentServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册应用服务
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<ICommentModerationService, CommentModerationService>();
        services.AddScoped<ICommentNotificationService, CommentNotificationService>();

        // 注册基础设施服务
        services.AddScoped<MapleBlog.Application.Interfaces.ICommentCacheService, CommentCacheService>();
        services.AddScoped<MapleBlog.Application.Interfaces.IAIContentModerationService, AIContentModerationService>();
        services.AddScoped<MapleBlog.Application.Interfaces.ISensitiveWordFilter, SensitiveWordFilter>();

        // 配置HTTP客户端（用于AI审核服务）
        services.AddHttpClient<AIContentModerationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "MapleBlog-CommentSystem/1.0");
        });

        return services;
    }

    /// <summary>
    /// 添加SignalR评论中心
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCommentSignalR(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
        })
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.WriteIndented = false;
        });

        return services;
    }

    /// <summary>
    /// 配置评论系统相关选项
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigureCommentOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // 配置内容审核选项
        services.Configure<ContentModerationOptions>(options =>
        {
            configuration.GetSection("ContentModeration").Bind(options);
        });

        // 配置缓存选项
        services.Configure<CommentCacheOptions>(options =>
        {
            configuration.GetSection("CommentCache").Bind(options);
        });

        // 配置通知选项
        services.Configure<NotificationOptions>(options =>
        {
            configuration.GetSection("Notifications").Bind(options);
        });

        return services;
    }

    /// <summary>
    /// 配置评论系统CORS策略
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCommentCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                           ?? new[] { "http://localhost:3000", "https://localhost:3000" };

        services.AddCors(options =>
        {
            options.AddPolicy("CommentPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // SignalR需要启用凭证
            });
        });

        return services;
    }

    /// <summary>
    /// 添加评论系统中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <param name="env">环境</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseCommentSystem(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // 使用CORS
        app.UseCors("CommentPolicy");

        // 映射SignalR中心
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<CommentHub>("/hubs/comment");
        });

        return app;
    }
}

/// <summary>
/// 内容审核配置选项
/// </summary>
public class ContentModerationOptions
{
    /// <summary>
    /// 是否启用AI审核
    /// </summary>
    public bool EnableAI { get; set; } = false;

    /// <summary>
    /// AI审核API端点
    /// </summary>
    public string? AIEndpoint { get; set; }

    /// <summary>
    /// AI审核API密钥
    /// </summary>
    public string? AIApiKey { get; set; }

    /// <summary>
    /// 垃圾信息阈值
    /// </summary>
    public double SpamThreshold { get; set; } = 0.7;

    /// <summary>
    /// 毒性内容阈值
    /// </summary>
    public double ToxicityThreshold { get; set; } = 0.8;

    /// <summary>
    /// 仇恨言论阈值
    /// </summary>
    public double HateSpeechThreshold { get; set; } = 0.9;

    /// <summary>
    /// 是否启用敏感词过滤
    /// </summary>
    public bool EnableSensitiveWordFilter { get; set; } = true;

    /// <summary>
    /// 敏感词文件路径
    /// </summary>
    public string SensitiveWordsFilePath { get; set; } = "Data/sensitive-words.txt";
}

/// <summary>
/// 评论缓存配置选项
/// </summary>
public class CommentCacheOptions
{
    /// <summary>
    /// 是否启用缓存
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 默认缓存时间（分钟）
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// 评论列表缓存时间（分钟）
    /// </summary>
    public int CommentListExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// 统计信息缓存时间（分钟）
    /// </summary>
    public int StatsExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// 缓存键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "comment:";

    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string? RedisConnectionString { get; set; }
}

/// <summary>
/// 通知配置选项
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// 是否启用实时通知
    /// </summary>
    public bool EnableRealtime { get; set; } = true;

    /// <summary>
    /// 是否启用邮件通知
    /// </summary>
    public bool EnableEmail { get; set; } = false;

    /// <summary>
    /// 是否启用移动推送通知
    /// </summary>
    public bool EnableMobilePush { get; set; } = false;

    /// <summary>
    /// 默认通知频率限制（分钟）
    /// </summary>
    public int DefaultFrequencyLimitMinutes { get; set; } = 5;

    /// <summary>
    /// 通知过期时间（天）
    /// </summary>
    public int ExpirationDays { get; set; } = 30;

    /// <summary>
    /// 邮件服务配置
    /// </summary>
    public EmailServiceOptions Email { get; set; } = new();

    /// <summary>
    /// 移动推送服务配置
    /// </summary>
    public MobilePushOptions MobilePush { get; set; } = new();
}

/// <summary>
/// 邮件服务配置选项
/// </summary>
public class EmailServiceOptions
{
    /// <summary>
    /// SMTP服务器
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP端口
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 是否启用SSL
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 发件人邮箱
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人姓名
    /// </summary>
    public string FromName { get; set; } = "Maple Blog";
}

/// <summary>
/// 移动推送配置选项
/// </summary>
public class MobilePushOptions
{
    /// <summary>
    /// Firebase服务器密钥
    /// </summary>
    public string? FirebaseServerKey { get; set; }

    /// <summary>
    /// Firebase项目ID
    /// </summary>
    public string? FirebaseProjectId { get; set; }

    /// <summary>
    /// Apple推送证书路径
    /// </summary>
    public string? ApnsCertificatePath { get; set; }

    /// <summary>
    /// Apple推送证书密码
    /// </summary>
    public string? ApnsCertificatePassword { get; set; }

    /// <summary>
    /// 是否为生产环境
    /// </summary>
    public bool IsProduction { get; set; } = false;
}