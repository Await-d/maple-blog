using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.API.Middleware;
using MapleBlog.API.Filters;
using MapleBlog.Infrastructure.Services;
using MapleBlog.Infrastructure.Repositories;

namespace MapleBlog.API.Extensions
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加认证服务
        /// </summary>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册应用服务
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IUserProfileService, UserProfileService>();

            // 注册基础设施服务
            services.AddScoped<IJwtService, JwtService>();

            // 注册登录跟踪和安全监控服务
            services.AddScoped<MapleBlog.Domain.Interfaces.ILoginHistoryRepository, LoginHistoryRepository>();
            services.AddScoped<ILoginTrackingService, LoginTrackingService>();

            // 注册文件共享服务
            services.AddScoped<MapleBlog.Domain.Interfaces.IFileShareRepository, FileShareRepository>();
            services.AddScoped<MapleBlog.Domain.Interfaces.IFileAccessLogRepository, FileAccessLogRepository>();

            // 注册后台清理服务
            var cleanupSettings = configuration.GetSection("LoginHistoryCleanupSettings").Get<LoginHistoryCleanupSettings>()
                ?? new LoginHistoryCleanupSettings();

            if (cleanupSettings.Enabled)
            {
                services.AddHostedService<LoginHistoryCleanupService>();
                services.Configure<LoginHistoryCleanupSettings>(configuration.GetSection("LoginHistoryCleanupSettings"));
            }

            // 配置JWT设置
            var jwtSettings = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettings);

            // 配置其他设置
            services.Configure<EmailVerificationSettings>(configuration.GetSection("EmailVerificationSettings"));
            services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordResetSettings"));
            services.Configure<UserProfileSettings>(configuration.GetSection("UserProfileSettings"));
            services.Configure<SecuritySettings>(configuration.GetSection("SecuritySettings"));

            return services;
        }

        /// <summary>
        /// 添加JWT认证
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT settings not configured");

            // 从配置或生成RSA密钥
            var rsa = System.Security.Cryptography.RSA.Create();
            if (!string.IsNullOrEmpty(jwtSettings.PrivateKey))
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(jwtSettings.PrivateKey), out _);
            }
            else
            {
                // 在开发环境中生成临时密钥
                rsa.KeySize = 2048;
            }

            var securityKey = new RsaSecurityKey(rsa);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // 在生产环境中应设为true
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireExpirationTime = true
                };

                // 配置事件
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError("Authentication failed: {Message}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("JWT Challenge: {Error}", context.Error);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        /// <summary>
        /// 添加授权策略
        /// </summary>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // 默认策略
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // 管理员策略
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("role", "Admin"));

                // 作者或管理员策略
                options.AddPolicy("AuthorOrAdmin", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(context =>
                              context.User.HasClaim("role", "Author") ||
                              context.User.HasClaim("role", "Admin")));

                // 邮箱已验证策略
                options.AddPolicy("EmailVerified", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("email_verified", "true"));

                // 自己的资源策略
                options.AddPolicy("OwnResource", policy =>
                    policy.RequireAuthenticatedUser()
                          .RequireAssertion(context =>
                          {
                              var userIdClaim = context.User.FindFirst("sub")?.Value ??
                                              context.User.FindFirst("userId")?.Value;
                              var resourceUserId = context.Resource?.ToString();
                              return userIdClaim == resourceUserId;
                          }));
            });

            return services;
        }

        /// <summary>
        /// 添加CORS策略
        /// </summary>
        public static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration configuration)
        {
            var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();

            services.AddCors(options =>
            {
                options.AddPolicy("Default", builder =>
                {
                    if (corsSettings.AllowAnyOrigin)
                    {
                        builder.AllowAnyOrigin();
                    }
                    else
                    {
                        builder.WithOrigins(corsSettings.AllowedOrigins.ToArray());
                    }

                    if (corsSettings.AllowAnyMethod)
                    {
                        builder.AllowAnyMethod();
                    }
                    else
                    {
                        builder.WithMethods(corsSettings.AllowedMethods.ToArray());
                    }

                    if (corsSettings.AllowAnyHeader)
                    {
                        builder.AllowAnyHeader();
                    }
                    else
                    {
                        builder.WithHeaders(corsSettings.AllowedHeaders.ToArray());
                    }

                    if (corsSettings.AllowCredentials)
                    {
                        builder.AllowCredentials();
                    }

                    builder.SetPreflightMaxAge(TimeSpan.FromMinutes(corsSettings.PreflightMaxAgeMinutes));
                });

                // 严格的CORS策略（用于生产环境）
                options.AddPolicy("Strict", builder =>
                {
                    builder.WithOrigins(corsSettings.AllowedOrigins.ToArray())
                           .WithMethods("GET", "POST", "PUT", "DELETE")
                           .WithHeaders("Authorization", "Content-Type", "Accept")
                           .AllowCredentials()
                           .SetPreflightMaxAge(TimeSpan.FromMinutes(5));
                });
            });

            return services;
        }

        /// <summary>
        /// 添加OpenAPI文档 - .NET 10原生支持
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
        {
            // .NET 10原生OpenAPI支持
            services.AddOpenApi("v1", options =>
            {
                // API基本信息
                options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Maple Blog API",
                        Description = "一个现代化的AI驱动博客系统API - 基于.NET 10构建",
                        Contact = new OpenApiContact
                        {
                            Name = "Maple Blog Team",
                            Email = "support@mapleblog.com",
                            Url = new Uri("https://github.com/mapleblog/mapleblog")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT License",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        },
                        TermsOfService = new Uri("https://mapleblog.com/terms")
                    };

                    // 添加JWT Bearer认证配置
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                                      "Example: \"Bearer 12345abcdef\""
                    };

                    // 服务器配置
                    var serverUrls = configuration.GetSection("Swagger:Servers").Get<string[]>();
                    if (serverUrls?.Any() == true)
                    {
                        document.Servers = serverUrls.Select(url => new OpenApiServer { Url = url }).ToList();
                    }
                    else
                    {
                        // 默认服务器
                        document.Servers = new List<OpenApiServer>
                        {
                            new OpenApiServer
                            {
                                Url = "https://localhost:5001",
                                Description = "Development HTTPS"
                            },
                            new OpenApiServer
                            {
                                Url = "http://localhost:5000",
                                Description = "Development HTTP"
                            }
                        };
                    }

                    return Task.CompletedTask;
                });

                // 添加操作变换器来处理认证
                options.AddOperationTransformer((operation, context, cancellationToken) =>
                {
                    // 检查是否需要认证
                    var authAttributes = context.Description.ActionDescriptor.EndpointMetadata
                        .OfType<AuthorizeAttribute>();

                    var allowAnonymousAttributes = context.Description.ActionDescriptor.EndpointMetadata
                        .OfType<AllowAnonymousAttribute>();

                    if (allowAnonymousAttributes.Any())
                    {
                        return Task.CompletedTask;
                    }

                    if (authAttributes.Any())
                    {
                        // 添加认证相关的响应
                        operation.Responses.TryAdd("401", new OpenApiResponse
                        {
                            Description = "Unauthorized - JWT token is missing or invalid"
                        });
                        operation.Responses.TryAdd("403", new OpenApiResponse
                        {
                            Description = "Forbidden - Insufficient permissions"
                        });

                        // 添加安全要求
                        operation.Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {
                                {
                                    new OpenApiSecurityScheme
                                    {
                                        Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        }
                                    },
                                    Array.Empty<string>()
                                }
                            }
                        };

                        // 添加角色和策略信息到描述中
                        var roles = authAttributes.SelectMany(a => a.Roles?.Split(',') ?? Array.Empty<string>()).Distinct();
                        var policies = authAttributes.Where(a => !string.IsNullOrEmpty(a.Policy)).Select(a => a.Policy).Distinct();

                        var authInfo = new List<string>();
                        if (roles.Any())
                        {
                            authInfo.Add($"Required roles: {string.Join(", ", roles)}");
                        }
                        if (policies.Any())
                        {
                            authInfo.Add($"Required policies: {string.Join(", ", policies)}");
                        }

                        if (authInfo.Any())
                        {
                            operation.Description = $"{operation.Description}\n\n**Authorization Requirements:**\n- {string.Join("\n- ", authInfo)}";
                        }
                    }

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        /// <summary>
        /// 添加速率限制
        /// </summary>
        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var rateLimitingSettings = configuration.GetSection("RateLimitingSettings").Get<RateLimitingSettings>()
                ?? new RateLimitingSettings();

            services.AddSingleton(rateLimitingSettings);

            return services;
        }

        /// <summary>
        /// 添加安全头
        /// </summary>
        public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
        {
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            return services;
        }

        /// <summary>
        /// 添加健康检查
        /// </summary>
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // 数据库健康检查
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecksBuilder.AddDbContextCheck<Infrastructure.Data.BlogDbContext>();
            }

            // Redis健康检查（如果配置了Redis）
            // TODO: Install Microsoft.Extensions.Diagnostics.HealthChecks.Redis package to enable Redis health checks
            // var redisConnectionString = configuration.GetConnectionString("Redis");
            // if (!string.IsNullOrEmpty(redisConnectionString))
            // {
            //     healthChecksBuilder.AddRedis(redisConnectionString);
            // }

            return services;
        }
    }

    #region Configuration Classes

    /// <summary>
    /// JWT设置
    /// </summary>
    public class JwtSettings
    {
        public string Issuer { get; set; } = "MapleBlog";
        public string Audience { get; set; } = "MapleBlog.Users";
        public int AccessTokenExpirationMinutes { get; set; } = 15;
        public int RefreshTokenExpirationDays { get; set; } = 7;
        public string? PrivateKey { get; set; }
        public string? PublicKey { get; set; }
    }

    /// <summary>
    /// CORS设置
    /// </summary>
    public class CorsSettings
    {
        public bool AllowAnyOrigin { get; set; } = false;
        public List<string> AllowedOrigins { get; set; } = new() { "http://localhost:3000" };
        public bool AllowAnyMethod { get; set; } = false;
        public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        public bool AllowAnyHeader { get; set; } = false;
        public List<string> AllowedHeaders { get; set; } = new() { "Authorization", "Content-Type", "Accept" };
        public bool AllowCredentials { get; set; } = true;
        public int PreflightMaxAgeMinutes { get; set; } = 10;
    }

    /// <summary>
    /// 安全设置
    /// </summary>
    public class SecuritySettings
    {
        public int MaxFailedAttemptsPerEmail { get; set; } = 5;
        public int MaxFailedAttemptsPerIp { get; set; } = 10;
        public int FailedAttemptWindowMinutes { get; set; } = 15;
        public int BlockThresholdScore { get; set; } = 80;
        public int FlagThresholdScore { get; set; } = 60;
    }

    /// <summary>
    /// 登录历史清理设置
    /// </summary>
    public class LoginHistoryCleanupSettings
    {
        /// <summary>
        /// 保留登录历史记录的天数
        /// </summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// 清理操作之间的间隔小时数
        /// </summary>
        public double CleanupIntervalHours { get; set; } = 24.0;

        /// <summary>
        /// 是否启用清理服务
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    #endregion
}