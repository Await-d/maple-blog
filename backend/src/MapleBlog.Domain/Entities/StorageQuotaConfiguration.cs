using System.ComponentModel.DataAnnotations;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 基于角色的存储配额配置
/// </summary>
public class StorageQuotaConfiguration : BaseEntity
{
    /// <summary>
    /// 用户角色
    /// </summary>
    [Required]
    public UserRoleEnum Role { get; set; }

    /// <summary>
    /// 最大存储配额（字节）
    /// </summary>
    public long MaxQuotaBytes { get; set; }

    /// <summary>
    /// 最大文件数量限制
    /// </summary>
    public int? MaxFileCount { get; set; }

    /// <summary>
    /// 单个文件最大大小（字节）
    /// </summary>
    public long? MaxFileSize { get; set; }

    /// <summary>
    /// 允许的文件类型（MIME类型，逗号分隔）
    /// </summary>
    public string? AllowedFileTypes { get; set; }

    /// <summary>
    /// 禁止的文件类型（MIME类型，逗号分隔）
    /// </summary>
    public string? ForbiddenFileTypes { get; set; }

    /// <summary>
    /// 是否允许公共文件访问
    /// </summary>
    public bool AllowPublicFiles { get; set; } = true;

    /// <summary>
    /// 配额预警阈值（百分比：0-100）
    /// </summary>
    public int WarningThreshold { get; set; } = 80;

    /// <summary>
    /// 配额严重警告阈值（百分比：0-100）
    /// </summary>
    public int CriticalThreshold { get; set; } = 95;

    /// <summary>
    /// 是否启用自动清理
    /// </summary>
    public bool AutoCleanupEnabled { get; set; } = false;

    /// <summary>
    /// 自动清理的天数阈值（未使用文件）
    /// </summary>
    public int? AutoCleanupDays { get; set; }

    /// <summary>
    /// 配置是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 配置优先级（数值越高优先级越高）
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 配置描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 失效时间（可选）
    /// </summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>
    /// 获取格式化的配额大小
    /// </summary>
    public string FormattedMaxQuota => FormatBytes(MaxQuotaBytes);

    /// <summary>
    /// 获取格式化的单文件大小限制
    /// </summary>
    public string FormattedMaxFileSize => MaxFileSize.HasValue ? FormatBytes(MaxFileSize.Value) : "无限制";

    /// <summary>
    /// 检查文件类型是否被允许
    /// </summary>
    /// <param name="mimeType">MIME类型</param>
    /// <returns>是否允许</returns>
    public bool IsFileTypeAllowed(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return false;

        // 检查禁止的文件类型
        if (!string.IsNullOrWhiteSpace(ForbiddenFileTypes))
        {
            var forbiddenTypes = ForbiddenFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
            if (forbiddenTypes.Contains(mimeType.ToLowerInvariant()))
                return false;
        }

        // 检查允许的文件类型
        if (!string.IsNullOrWhiteSpace(AllowedFileTypes))
        {
            var allowedTypes = AllowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim().ToLowerInvariant());
            return allowedTypes.Contains(mimeType.ToLowerInvariant()) ||
                   allowedTypes.Contains("*/*") ||
                   allowedTypes.Any(t => t.EndsWith("/*") && mimeType.ToLowerInvariant().StartsWith(t[..^1]));
        }

        return true; // 如果没有设置允许类型，默认允许
    }

    /// <summary>
    /// 检查文件大小是否在限制内
    /// </summary>
    /// <param name="fileSize">文件大小</param>
    /// <returns>是否在限制内</returns>
    public bool IsFileSizeAllowed(long fileSize)
    {
        return !MaxFileSize.HasValue || fileSize <= MaxFileSize.Value;
    }

    /// <summary>
    /// 检查配置是否在有效期内
    /// </summary>
    /// <returns>是否有效</returns>
    public bool IsEffective()
    {
        var now = DateTime.UtcNow;
        return IsActive &&
               now >= EffectiveFrom &&
               (!EffectiveTo.HasValue || now <= EffectiveTo.Value);
    }

    /// <summary>
    /// 获取允许的文件类型列表
    /// </summary>
    /// <returns>文件类型列表</returns>
    public string[] GetAllowedFileTypes()
    {
        if (string.IsNullOrWhiteSpace(AllowedFileTypes))
            return Array.Empty<string>();

        return AllowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToArray();
    }

    /// <summary>
    /// 获取禁止的文件类型列表
    /// </summary>
    /// <returns>文件类型列表</returns>
    public string[] GetForbiddenFileTypes()
    {
        if (string.IsNullOrWhiteSpace(ForbiddenFileTypes))
            return Array.Empty<string>();

        return ForbiddenFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToArray();
    }

    /// <summary>
    /// 格式化字节大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化字符串</returns>
    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "0 B";
        if (bytes < 0) return "无限制";

        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 创建默认角色配额配置
    /// </summary>
    /// <param name="role">用户角色</param>
    /// <returns>配额配置</returns>
    public static StorageQuotaConfiguration CreateDefault(UserRoleEnum role)
    {
        return role switch
        {
            UserRoleEnum.Guest => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 0, // 0MB - 不允许上传
                MaxFileCount = 0,
                MaxFileSize = 0,
                AllowedFileTypes = "",
                AllowPublicFiles = false,
                Description = "访客用户 - 无上传权限"
            },
            UserRoleEnum.User => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 100 * 1024 * 1024, // 100MB
                MaxFileCount = 100,
                MaxFileSize = 10 * 1024 * 1024, // 10MB
                AllowedFileTypes = "image/jpeg,image/png,image/gif,image/webp,text/plain,application/pdf",
                AllowPublicFiles = true,
                Description = "普通用户 - 基础文件上传权限"
            },
            UserRoleEnum.Author => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 500 * 1024 * 1024, // 500MB
                MaxFileCount = 500,
                MaxFileSize = 50 * 1024 * 1024, // 50MB
                AllowedFileTypes = "image/*,video/mp4,video/webm,audio/mpeg,audio/wav,text/*,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                AllowPublicFiles = true,
                Description = "作者用户 - 扩展媒体文件权限"
            },
            UserRoleEnum.Moderator => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 2L * 1024 * 1024 * 1024, // 2GB
                MaxFileCount = 1000,
                MaxFileSize = 100 * 1024 * 1024, // 100MB
                AllowedFileTypes = "*/*", // 允许所有类型
                ForbiddenFileTypes = "application/x-executable,application/x-msdownload,application/vnd.microsoft.portable-executable",
                AllowPublicFiles = true,
                Description = "版主用户 - 管理权限文件上传"
            },
            UserRoleEnum.Admin => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 10L * 1024 * 1024 * 1024, // 10GB
                MaxFileCount = 5000,
                MaxFileSize = 500 * 1024 * 1024, // 500MB
                AllowedFileTypes = "*/*",
                AllowPublicFiles = true,
                Description = "管理员用户 - 高级管理权限"
            },
            UserRoleEnum.SuperAdmin => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = -1, // 无限制
                MaxFileCount = null, // 无限制
                MaxFileSize = null, // 无限制
                AllowedFileTypes = "*/*",
                AllowPublicFiles = true,
                Description = "超级管理员 - 无限制访问"
            },
            _ => new StorageQuotaConfiguration
            {
                Role = role,
                MaxQuotaBytes = 0,
                MaxFileCount = 0,
                Description = "未知角色 - 默认无权限"
            }
        };
    }
}