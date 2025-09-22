using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Serilog;
using MapleBlog.Infrastructure.Data;
using MapleBlog.Admin.Extensions;
using MapleBlog.Admin.Middleware;
using MapleBlog.Admin.Filters;
using MapleBlog.Admin.Documentation;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.Infrastructure.Services;
using MapleBlog.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/admin-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 添加数据库上下文
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 添加内存缓存和Redis缓存
builder.Services.AddMemoryCache();
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

// 配置JWT认证
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // 配置SignalR的JWT认证
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/admin-hub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// 添加授权
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));

    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole("SuperAdmin"));
});

// 注册服务
builder.Services.AddPermissionServices(); // 权限服务
builder.Services.AddAuditServices(); // 审计服务
// builder.Services.AddAdminServices(); // 暂时注释掉以避免依赖项错误

// 注册分析和报表服务
builder.Services.AddScoped<MapleBlog.Admin.Services.AnalyticsService>();
builder.Services.AddScoped<MapleBlog.Admin.Services.ReportService>();
builder.Services.AddScoped<MapleBlog.Admin.Services.DashboardService>();

// 注册系统监控服务
builder.Services.Configure<MapleBlog.Admin.Services.SystemMonitorOptions>(
    builder.Configuration.GetSection("SystemMonitor"));
builder.Services.AddScoped<MapleBlog.Admin.Services.IHealthCheckService, MapleBlog.Admin.Services.HealthCheckService>();
builder.Services.AddScoped<MapleBlog.Admin.Services.ISystemMonitorService, MapleBlog.Admin.Services.SystemMonitorService>();
builder.Services.AddHostedService<MapleBlog.Admin.Services.SystemMonitorService>();

// 添加AutoMapper
builder.Services.AddAdminAutoMapper();

// 配置控制器
builder.Services.AddControllers(options =>
{
    // 添加全局审计过滤器
    options.AddGlobalAuditFilter();
    // 添加管理员安全过滤器
    // options.Filters.Add<AdminSecurityFilter>(); // 暂时注释掉
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = false;
});

// 添加API Explorer
builder.Services.AddEndpointsApiExplorer();

// 配置Swagger
builder.Services.ConfigureAdminSwagger(builder.Configuration);

// 添加CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 添加SignalR
builder.Services.AddSignalR();

// 添加Admin数据库健康检查系统
builder.Services.AddAdminDatabaseHealthChecks(builder.Configuration);

var app = builder.Build();

// 配置请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Maple Blog Admin API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Maple Blog Admin API";
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// 使用HTTPS重定向
app.UseHttpsRedirection();

// 使用CORS
app.UseCors("AdminPolicy");

// 使用认证和授权
app.UseAuthentication();
app.UseAuthorization();

// 使用管理员安全中间件
app.UseMiddleware<AdminSecurityMiddleware>();

// 使用审计服务
app.UseAuditServices();

// 配置路由
app.MapControllers();

// 配置Admin健康检查端点
app.MapAdminHealthCheckEndpoints();

// 配置SignalR Hub
// app.MapHub<AdminDashboardHub>("/admin-hub"); // 暂时注释掉

// 初始化权限系统
try
{
    await app.InitializePermissionSystemAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "权限系统初始化失败");
    return -1;
}

try
{
    Log.Information("Maple Blog Admin API 启动中...");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
    return -1;
}
finally
{
    Log.CloseAndFlush();
}