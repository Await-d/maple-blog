using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Swashbuckle.AspNetCore.SwaggerUI;
using MapleBlog.Application.Services;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Mappings;
using MapleBlog.Application.Validators;
using MapleBlog.Infrastructure.Data;
using BlogDbContext = MapleBlog.Infrastructure.Data.BlogDbContext;
using MapleBlog.Infrastructure.Repositories;
using MapleBlog.Infrastructure.Services;
using static MapleBlog.Infrastructure.Services.CacheInvalidationServiceExtensions;
using MapleBlog.Domain.Interfaces;
using MapleBlog.API.Middleware;
using MapleBlog.API.Filters;
using MapleBlog.API.Hubs;
using MapleBlog.API.Extensions;
using MapleBlog.Infrastructure.Extensions;
using MapleBlog.Infrastructure.Data.Seeders.Extensions;
using FluentValidation;
using Serilog;
using System.Text;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - simplified configuration without Elasticsearch
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day);
});

// Add services to the container.
builder.Services.AddControllers();

// Configure OpenAPI and Swagger documentation - .NET 10 native support
builder.Services.AddSwaggerDocumentation(builder.Configuration);

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Fallback to SQLite for development
    connectionString = "Data Source=data/maple-blog.db";
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Determine database provider
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider", "SQLite");

// Register both DbContexts (they are used by different parts of the system)
builder.Services.AddDbContext<MapleBlog.Infrastructure.Data.ApplicationDbContext>(options =>
{
    // For now, just use SQLite
    options.UseSqlite(connectionString);
});

builder.Services.AddDbContext<MapleBlog.Infrastructure.Data.BlogDbContext>(options =>
{
    // For now, just use SQLite
    options.UseSqlite(connectionString);
});

// 注册缺少的服务
builder.Services.AddScoped<MapleBlog.Application.Validators.IRateLimitService, MapleBlog.Application.Validators.InMemoryRateLimitService>();

// 添加HttpClient支持
builder.Services.AddHttpClient();

// Add Memory Cache (always needed for many services)
builder.Services.AddMemoryCache();

// 注册Email服务
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IEmailService, MapleBlog.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.INotificationRepository>(provider => new NoOpNotificationRepository());

// 为需要通用DbContext的服务添加别名
builder.Services.AddScoped<Microsoft.EntityFrameworkCore.DbContext>(provider => provider.GetRequiredService<MapleBlog.Infrastructure.Data.ApplicationDbContext>());

// Redis Configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var useRedis = !string.IsNullOrEmpty(redisConnectionString);

if (useRedis)
{
    // Configure Redis connection
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var configuration = ConfigurationOptions.Parse(redisConnectionString!);
        configuration.AbortOnConnectFail = false; // 不要在连接失败时中止
        configuration.ConnectRetry = 3;
        configuration.ConnectTimeout = 5000;
        configuration.SyncTimeout = 5000;
        configuration.AsyncTimeout = 5000;

        return ConnectionMultiplexer.Connect(configuration);
    });

    // Add Redis distributed cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName", "MapleBlog");
    });

    // Redis health check options
    builder.Services.Configure<MapleBlog.Infrastructure.Services.RedisHealthCheckOptions>(options =>
    {
        options.ResponseTimeThresholdMs = builder.Configuration.GetValue<long>("Redis:HealthCheck:ResponseTimeThresholdMs", 1000);
        options.EnableDetailedChecks = builder.Configuration.GetValue<bool>("Redis:HealthCheck:EnableDetailedChecks", true);
        options.TimeoutMs = builder.Configuration.GetValue<int>("Redis:HealthCheck:TimeoutMs", 5000);
    });

    // Redis monitoring options
    builder.Services.Configure<MapleBlog.Infrastructure.Services.RedisMonitoringOptions>(options =>
    {
        options.HealthCheckIntervalSeconds = builder.Configuration.GetValue<int>("Redis:Monitoring:HealthCheckIntervalSeconds", 30);
        options.MetricsCollectionIntervalSeconds = builder.Configuration.GetValue<int>("Redis:Monitoring:MetricsCollectionIntervalSeconds", 60);
        options.ResponseTimeWarningThresholdMs = builder.Configuration.GetValue<long>("Redis:Monitoring:ResponseTimeWarningThresholdMs", 1000);
    });

    // Redis Prometheus options
    builder.Services.Configure<MapleBlog.Infrastructure.Services.RedisPrometheusOptions>(options =>
    {
        options.MetricsCollectionIntervalSeconds = builder.Configuration.GetValue<int>("Redis:Prometheus:MetricsCollectionIntervalSeconds", 30);
        options.HealthCheckIntervalSeconds = builder.Configuration.GetValue<int>("Redis:Prometheus:HealthCheckIntervalSeconds", 15);
        options.ResponseTimeThresholdMs = builder.Configuration.GetValue<long>("Redis:Prometheus:ResponseTimeThresholdMs", 1000);
        options.EnableDetailedMetrics = builder.Configuration.GetValue<bool>("Redis:Prometheus:EnableDetailedMetrics", true);
        options.MetricLabelPrefix = builder.Configuration.GetValue<string>("Redis:Prometheus:MetricLabelPrefix", "maple_blog");
    });

    // Register Redis services
    builder.Services.AddSingleton<MapleBlog.Infrastructure.Services.IRedisMonitoringService, MapleBlog.Infrastructure.Services.RedisMonitoringService>();
    builder.Services.AddHostedService<MapleBlog.Infrastructure.Services.RedisMonitoringService>(provider =>
        (MapleBlog.Infrastructure.Services.RedisMonitoringService)provider.GetRequiredService<MapleBlog.Infrastructure.Services.IRedisMonitoringService>());
}
else
{
    // Fallback to in-memory cache
    builder.Services.AddMemoryCache();
    builder.Services.AddDistributedMemoryCache();
}

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey");

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured. Please set JwtSettings:SecretKey in appsettings.json");
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = builder.Environment.IsProduction();
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR JWT configuration
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("AuthorOrAdmin", policy =>
        policy.RequireRole("Author", "Admin"));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("MapleBlogCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(UserProfile));

// Validation - Add validators from Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<MapleBlog.Application.DTOs.UserDto>();

// Application Services
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IAuthService, MapleBlog.Application.Services.AuthService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IBlogService, MapleBlog.Application.Services.BlogService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ICategoryService, MapleBlog.Application.Services.CategoryService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ITagService, MapleBlog.Application.Services.TagService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IFileService, MapleBlog.Application.Services.FileService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IStorageQuotaService, MapleBlog.Application.Services.StorageQuotaService>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IMarkdownService, MapleBlog.Infrastructure.Services.MarkdownService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IApplicationMarkdownService, MapleBlog.Infrastructure.Services.ApplicationMarkdownService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IImageProcessingService, MapleBlog.Infrastructure.Services.ImageProcessingService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ICommentService, MapleBlog.Application.Services.CommentService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ICommentModerationService, MapleBlog.Application.Services.CommentModerationService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ICommentNotificationService, MapleBlog.Application.Services.CommentNotificationService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ISearchService, MapleBlog.Application.Services.SimpleSearchService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IArchiveService, MapleBlog.Application.Services.SimpleArchiveService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ISearchAnalyticsService, MapleBlog.Infrastructure.Services.SearchAnalyticsService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IStatsService, MapleBlog.Application.Services.StatsService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IRecommendationService, MapleBlog.Application.Services.RecommendationService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IPermissionService, MapleBlog.Application.Services.PermissionService>();

// Infrastructure Services
builder.Services.AddHttpContextAccessor(); // Required for UserContextService

// Add missing services
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IUserManagementService, MapleBlog.Application.Services.UserManagementService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ILoginTrackingService, MapleBlog.Infrastructure.Services.LoginTrackingService>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IUserRoleRepository, MapleBlog.Infrastructure.Repositories.UserRoleRepository>();
// Add LoginHistory repository
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ILoginHistoryRepository, MapleBlog.Infrastructure.Repositories.LoginHistoryRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IFileRepository, MapleBlog.Infrastructure.Repositories.FileRepository>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IUserContextService, MapleBlog.Infrastructure.Services.UserContextService>();
builder.Services.AddSingleton<MapleBlog.Application.Interfaces.IJwtService, MapleBlog.Infrastructure.Services.JwtService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ICommentCacheService, MapleBlog.Infrastructure.Services.CommentCacheService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IAIContentModerationService, MapleBlog.Infrastructure.Services.AIContentModerationService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ISensitiveWordFilter, MapleBlog.Infrastructure.Services.SensitiveWordFilter>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.ISearchIndexManager, MapleBlog.Infrastructure.Services.SearchIndexManager>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IElasticsearchService, MapleBlog.Infrastructure.Services.ElasticsearchService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IDatabaseSearchService, MapleBlog.Infrastructure.Services.DatabaseSearchService>();
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IHomepageCacheService, MapleBlog.Infrastructure.Services.HomepageCacheService>();

// Audit Services
builder.Services.AddScoped<MapleBlog.Application.Interfaces.IAuditLogService, MapleBlog.Infrastructure.Services.AuditLogService>();
builder.Services.AddAuditServices(builder.Configuration);

// Repositories
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IUserRepository, MapleBlog.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IPostRepository, MapleBlog.Infrastructure.Repositories.PostRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ICategoryRepository, MapleBlog.Infrastructure.Repositories.CategoryRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ITagRepository, MapleBlog.Infrastructure.Repositories.TagRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ICommentRepository, MapleBlog.Infrastructure.Repositories.CommentRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ICommentLikeRepository, MapleBlog.Infrastructure.Repositories.CommentLikeRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.ICommentReportRepository, MapleBlog.Infrastructure.Repositories.CommentReportRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IUserInteractionRepository, MapleBlog.Infrastructure.Repositories.UserInteractionRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IRoleRepository, MapleBlog.Infrastructure.Repositories.RoleRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IPermissionRepository, MapleBlog.Infrastructure.Repositories.PermissionRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IAuditLogRepository, MapleBlog.Infrastructure.Repositories.AuditLogRepository>();
builder.Services.AddScoped<MapleBlog.Domain.Interfaces.IStorageQuotaConfigurationRepository, MapleBlog.Infrastructure.Repositories.StorageQuotaConfigurationRepository>();

// Generic repository registration for other entities - 使用BlogBaseRepository
builder.Services.AddScoped(typeof(MapleBlog.Domain.Interfaces.IRepository<>), typeof(MapleBlog.Infrastructure.Repositories.BlogBaseRepository<>));

// Modern Seed Data System
builder.Services.AddSeedDataServices(builder.Configuration);

// Health Checks - use centralized configuration
builder.Services.AddApplicationHealthChecks(builder.Configuration);

// Add Health Check UI for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHealthCheckUI(builder.Configuration);
}

// Enhanced Caching Services
builder.Services.AddEnhancedCacheServices(builder.Configuration);

// Cache Invalidation Services
builder.Services.AddCacheInvalidationServices();

// Built-in Response Caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64MB
    options.UseCaseSensitivePaths = true;
    options.SizeLimit = 100 * 1024 * 1024; // 100MB
});

// Custom Response Caching with intelligent rules
builder.Services.AddResponseCaching(builder.Configuration);

// Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Prometheus Metrics (only in non-development environments or when explicitly enabled)
var enableMetrics = builder.Configuration.GetValue<bool>("Metrics:Enabled", !builder.Environment.IsDevelopment());
if (enableMetrics)
{
    // Prometheus metrics are handled by prometheus-net.AspNetCore
    // No separate health checks prometheus registration needed
}

var app = builder.Build();

// Get metrics configuration for use in pipeline
var enableMetricsInPipeline = app.Configuration.GetValue<bool>("Metrics:Enabled", !app.Environment.IsDevelopment());

// Configure the HTTP request pipeline.
// OpenAPI和Swagger配置 - .NET 10原生支持
var enableSwaggerInProduction = app.Configuration.GetValue<bool>("Swagger:EnableInProduction", false);
var swaggerBasicAuth = app.Configuration.GetValue<bool>("Swagger:RequireAuthentication", true);

if (app.Environment.IsDevelopment() || enableSwaggerInProduction)
{
    // 在生产环境中启用基本身份验证保护Swagger
    if (app.Environment.IsProduction() && swaggerBasicAuth)
    {
        app.UseMiddleware<BasicAuthMiddleware>();
    }

    // 映射原生OpenAPI端点
    app.MapOpenApi();

    // 配置Swagger UI指向新的OpenAPI端点
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Maple Blog API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Maple Blog API Documentation";

        // 基本功能
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.EnableDeepLinking();
        options.EnableFilter();

        // 自定义CSS和JavaScript
        options.InjectStylesheet("/swagger-ui/custom.css");
        options.InjectJavascript("/swagger-ui/custom.js");

        // 生产环境警告
        if (app.Environment.IsProduction())
        {
            options.HeadContent = """
                <style>
                    .swagger-ui .topbar { background-color: #ff4444; }
                    .swagger-ui .topbar::after {
                        content: "生产环境 - 请谨慎操作";
                        color: white;
                        font-weight: bold;
                        margin-left: 20px;
                    }
                </style>
                """;
        }
    });
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable response compression
app.UseResponseCompression();

// Enable built-in response caching
app.UseResponseCaching();

// Enable CORS
app.UseCors("MapleBlogCors");

// Enable intelligent response cache middleware
// app.UseMiddleware<ResponseCacheMiddleware>(); // TODO: Fix IResponseCacheConfigurationService dependency

// Authentication middleware for JWT token processing
app.UseAuthMiddleware();

app.UseAuthentication();
app.UseStorageQuotaCheck();
app.UseAuthorization();

// Custom authorization middleware
app.UseMiddleware<PermissionMiddleware>();

// Audit middleware for enterprise logging
app.UseAuditSystem();

app.MapControllers();

// SignalR Hubs
app.MapHub<CommentHub>("/hubs/comments");

// Configure health check endpoints using extension method
app.MapHealthCheckEndpoints();

// Prometheus metrics endpoint (only if metrics are enabled)
if (enableMetricsInPipeline)
{
    app.UseHttpMetrics();
    app.MapMetrics("/metrics");
}

// DI Test endpoint (only in development)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/test-di", (IServiceProvider serviceProvider) =>
    {
        return "✅ DI配置测试端点可用";
    });
}

// Static files for uploads
app.UseStaticFiles();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MapleBlog.Infrastructure.Data.ApplicationDbContext>();
    try
    {
        // Apply database migrations
        // await context.Database.MigrateAsync(); // TODO: Create and apply migrations

        // Initialize storage quota configurations
        // var storageQuotaService = scope.ServiceProvider.GetRequiredService<MapleBlog.Application.Interfaces.IStorageQuotaService>();
        // await storageQuotaService.InitializeDefaultQuotaConfigurationsAsync(); // TODO: Fix EF Core relationships

        app.Logger.LogInformation("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating/migrating the database.");

        // Don't crash the application in development
        if (!app.Environment.IsDevelopment())
            throw;
    }
}

// Initialize modern seed data system
await app.SeedDataAsync();

app.Logger.LogInformation("Maple Blog API is starting up...");

app.Run();

/// <summary>
/// 临时的NoOp邮件服务实现
/// </summary>
public class NoOpEmailService : MapleBlog.Application.Interfaces.IEmailService
{
    public Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendEmailVerificationAsync(string email, string userName, string verificationToken, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendPasswordResetAsync(string email, string userName, string resetToken, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendWelcomeEmailAsync(string email, string userName, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendNotificationEmailAsync(string email, string userName, string title, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendCommentNotificationAsync(string email, string userName, string postTitle, string commentContent, string postUrl, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendSystemNotificationAsync(string email, string userName, string subject, string message, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> SendQuotaWarningEmailAsync(string email, string warningType, object quotaInfo, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<MapleBlog.Application.Interfaces.EmailServiceStatus> GetServiceStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new MapleBlog.Application.Interfaces.EmailServiceStatus
        {
            IsAvailable = true,
            StatusMessage = "NoOp service - always available",
            LastChecked = DateTime.UtcNow,
            QueuedEmails = 0,
            SentToday = 0,
            FailedToday = 0
        });
}

/// <summary>
/// 临时的NoOp通知仓储实现
/// </summary>
public class NoOpNotificationRepository : MapleBlog.Application.Interfaces.INotificationRepository
{
    public Task<MapleBlog.Domain.Entities.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<MapleBlog.Domain.Entities.Notification?>(null);

    public Task<IEnumerable<MapleBlog.Domain.Entities.Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<MapleBlog.Domain.Entities.Notification>());

    public Task<IEnumerable<MapleBlog.Domain.Entities.Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<MapleBlog.Domain.Entities.Notification>());

    public Task<MapleBlog.Domain.Entities.Notification> CreateAsync(MapleBlog.Domain.Entities.Notification notification, CancellationToken cancellationToken = default)
        => Task.FromResult(notification);

    public Task<IEnumerable<MapleBlog.Domain.Entities.Notification>> CreateManyAsync(IEnumerable<MapleBlog.Domain.Entities.Notification> notifications, CancellationToken cancellationToken = default)
        => Task.FromResult(notifications);

    public Task AddRangeAsync(IEnumerable<MapleBlog.Domain.Entities.Notification> notifications, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<MapleBlog.Domain.Entities.Notification> UpdateAsync(MapleBlog.Domain.Entities.Notification notification, CancellationToken cancellationToken = default)
        => Task.FromResult(notification);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MarkManyAsReadAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<int> CleanupOldNotificationsAsync(int retentionDays = 30, CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public IQueryable<MapleBlog.Domain.Entities.Notification> GetQueryable()
        => Enumerable.Empty<MapleBlog.Domain.Entities.Notification>().AsQueryable();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}

