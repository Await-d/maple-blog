using MapleBlog.Application.DTOs.File;
using MapleBlog.Domain.Entities;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 基于角色的存储配额管理服务接口
    /// </summary>
    public interface IStorageQuotaService
    {
        /// <summary>
        /// 获取用户的存储配额信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户存储配额信息</returns>
        Task<UserStorageQuotaDto> GetUserStorageQuotaAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据角色获取存储配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>存储配额配置</returns>
        Task<StorageQuotaConfiguration> GetRoleQuotaConfigurationAsync(UserRoleEnum role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户上传权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有上传权限</returns>
        Task<bool> CheckUploadPermissionAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有足够的存储空间上传文件
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有足够空间</returns>
        Task<bool> CheckStorageAvailabilityAsync(Guid userId, long fileSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查文件是否符合用户的配额限制
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="fileSize">文件大小</param>
        /// <param name="mimeType">文件MIME类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>检查结果</returns>
        Task<QuotaValidationResultDto> ValidateFileUploadAsync(Guid userId, long fileSize, string mimeType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 计算用户当前存储使用量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>存储使用量（字节）</returns>
        Task<long> CalculateUserStorageUsageAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户文件数量
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件数量</returns>
        Task<int> GetUserFileCountAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有角色的配额配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色配额配置列表</returns>
        Task<IEnumerable<StorageQuotaConfiguration>> GetAllRoleQuotaConfigurationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新角色的配额配置
        /// </summary>
        /// <param name="role">用户角色</param>
        /// <param name="configuration">新的配额配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateRoleQuotaConfigurationAsync(UserRoleEnum role, StorageQuotaConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 初始化默认角色配额配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否初始化成功</returns>
        Task<bool> InitializeDefaultQuotaConfigurationsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取接近配额限制的用户列表
        /// </summary>
        /// <param name="thresholdPercentage">阈值百分比（默认80%）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>接近限制的用户列表</returns>
        Task<IEnumerable<UserQuotaWarningDto>> GetUsersNearQuotaLimitAsync(double thresholdPercentage = 0.8, CancellationToken cancellationToken = default);

        /// <summary>
        /// 发送配额警告通知
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="warningType">警告类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SendQuotaWarningNotificationAsync(Guid userId, QuotaWarningType warningType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理过期的配额历史记录
        /// </summary>
        /// <param name="retentionDays">保留天数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理的记录数</returns>
        Task<int> CleanupExpiredQuotaHistoryAsync(int retentionDays = 90, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统存储使用统计
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>系统存储统计</returns>
        Task<SystemStorageStatsDto> GetSystemStorageStatsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 配额验证结果
    /// </summary>
    public class QuotaValidationResultDto
    {
        /// <summary>
        /// 是否通过验证
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证失败的原因
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// 剩余存储空间
        /// </summary>
        public long RemainingSpace { get; set; }

        /// <summary>
        /// 当前使用量
        /// </summary>
        public long CurrentUsage { get; set; }

        /// <summary>
        /// 总配额
        /// </summary>
        public long TotalQuota { get; set; }

        /// <summary>
        /// 是否超出文件数量限制
        /// </summary>
        public bool ExceedsFileCountLimit { get; set; }

        /// <summary>
        /// 是否超出单文件大小限制
        /// </summary>
        public bool ExceedsFileSizeLimit { get; set; }

        /// <summary>
        /// 文件类型是否被允许
        /// </summary>
        public bool IsFileTypeAllowed { get; set; }
    }

    /// <summary>
    /// 配额警告类型
    /// </summary>
    public enum QuotaWarningType
    {
        /// <summary>
        /// 接近配额限制（80%）
        /// </summary>
        Approaching = 1,

        /// <summary>
        /// 严重警告（95%）
        /// </summary>
        Critical = 2,

        /// <summary>
        /// 已超出配额
        /// </summary>
        Exceeded = 3
    }

    /// <summary>
    /// 系统存储统计
    /// </summary>
    public class SystemStorageStatsDto
    {
        /// <summary>
        /// 总存储使用量
        /// </summary>
        public long TotalStorageUsed { get; set; }

        /// <summary>
        /// 总分配配额
        /// </summary>
        public long TotalQuotaAllocated { get; set; }

        /// <summary>
        /// 活跃用户数
        /// </summary>
        public int ActiveUserCount { get; set; }

        /// <summary>
        /// 按角色分组的使用统计
        /// </summary>
        public Dictionary<UserRoleEnum, RoleStorageStatsDto> UsageByRole { get; set; } = new();

        /// <summary>
        /// 存储使用率百分比
        /// </summary>
        public double UsagePercentage => TotalQuotaAllocated > 0 ? (double)TotalStorageUsed / TotalQuotaAllocated * 100 : 0;

        /// <summary>
        /// 格式化的总使用量
        /// </summary>
        public string FormattedTotalUsage => FormatBytes(TotalStorageUsed);

        /// <summary>
        /// 格式化的总配额
        /// </summary>
        public string FormattedTotalQuota => FormatBytes(TotalQuotaAllocated);

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
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
    }

    /// <summary>
    /// 角色存储统计
    /// </summary>
    public class RoleStorageStatsDto
    {
        /// <summary>
        /// 角色
        /// </summary>
        public UserRoleEnum Role { get; set; }

        /// <summary>
        /// 用户数量
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// 总使用量
        /// </summary>
        public long TotalUsage { get; set; }

        /// <summary>
        /// 平均使用量
        /// </summary>
        public long AverageUsage => UserCount > 0 ? TotalUsage / UserCount : 0;

        /// <summary>
        /// 总配额
        /// </summary>
        public long TotalQuota { get; set; }

        /// <summary>
        /// 使用率百分比
        /// </summary>
        public double UsagePercentage => TotalQuota > 0 ? (double)TotalUsage / TotalQuota * 100 : 0;
    }
}