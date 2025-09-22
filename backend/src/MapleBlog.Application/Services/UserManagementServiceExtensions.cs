using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// UserManagementService的扩展方法
    /// </summary>
    public partial class UserManagementService
    {
        #region User Detail Helper Methods

        private async Task<string> CalculateSecurityLevelAsync(User user)
        {
            var score = 0;

            // 基于多个安全因素计算分数
            if (user.EmailConfirmed) score += 20;
            if (user.PhoneNumberConfirmed) score += 15;
            if (user.TwoFactorEnabled) score += 25;
            if (user.AccessFailedCount == 0) score += 10;
            if (!user.IsLockedOut()) score += 15;
            if (user.LastLoginAt.HasValue && user.LastLoginAt > DateTime.UtcNow.AddDays(-7)) score += 15;

            return score switch
            {
                >= 80 => "High",
                >= 50 => "Medium",
                _ => "Low"
            };
        }

        private async Task<IEnumerable<SecurityEventDto>> GetRecentSecurityEventsAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从安全事件表获取
            return new[]
            {
                new SecurityEventDto
                {
                    EventType = "Login",
                    Description = "用户登录",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Severity = "Info",
                    Status = "Success"
                }
            };
        }

        private async Task<int> GetUserSessionCountAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从会话表获取
            return 10;
        }

        private async Task<TimeSpan> GetUserTotalTimeSpentAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从活动统计表获取
            return TimeSpan.FromHours(50);
        }

        private async Task<TimeSpan> GetUserAverageSessionDurationAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从会话统计表获取
            return TimeSpan.FromMinutes(25);
        }

        private async Task<int> GetUserPageVisitCountAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从页面访问表获取
            return 150;
        }

        private async Task<IEnumerable<string>> GetUserMostVisitedPagesAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从页面访问统计表获取
            return new[] { "/dashboard", "/profile", "/posts" };
        }

        private async Task<Dictionary<string, int>> GetUserActionCountsAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从用户操作统计表获取
            return new Dictionary<string, int>
            {
                { "Login", 15 },
                { "Post", 5 },
                { "Comment", 12 },
                { "Like", 25 }
            };
        }

        private async Task<IEnumerable<HourlyActivityDto>> GetUserActivityByHourAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从小时活动统计表获取
            var activities = new List<HourlyActivityDto>();
            for (int hour = 0; hour < 24; hour++)
            {
                activities.Add(new HourlyActivityDto
                {
                    Hour = hour,
                    ActivityCount = new Random().Next(0, 10),
                    ActivityPercentage = new Random().Next(0, 100) / 100.0
                });
            }
            return activities;
        }

        private async Task<IEnumerable<UserRoleDto>> GetUserRolesDetailAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new List<UserRoleDto>();

            return new[]
            {
                new UserRoleDto
                {
                    Id = Guid.NewGuid(),
                    Name = user.Role.ToString(),
                    DisplayName = user.Role.ToString(),
                    Description = $"{user.Role.ToString()} 角色",
                    IsSystem = true,
                    AssignedAt = user.CreatedAt,
                    AssignedBy = "System",
                    IsActive = true
                }
            };
        }

        private async Task<IEnumerable<UserPermissionDto>> GetUserDirectPermissionsAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从用户权限表获取
            return new List<UserPermissionDto>();
        }

        private async Task<IEnumerable<UserPermissionDto>> GetUserInheritedPermissionsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new List<UserPermissionDto>();

            // 基于角色生成权限
            var permissions = new List<UserPermissionDto>();

            switch (user.Role)
            {
                case MapleBlog.Domain.Enums.UserRole.Admin:
                    permissions.AddRange(GetAdminPermissions());
                    break;
                case MapleBlog.Domain.Enums.UserRole.Author:
                    permissions.AddRange(GetAuthorPermissions());
                    break;
                case MapleBlog.Domain.Enums.UserRole.User:
                    permissions.AddRange(GetUserPermissions());
                    break;
            }

            return permissions;
        }

        private IEnumerable<UserPermissionDto> GetAdminPermissions()
        {
            return new[]
            {
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "user.manage", DisplayName = "用户管理", Category = "User", Source = "Role", SourceRoleName = "Admin" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "content.manage", DisplayName = "内容管理", Category = "Content", Source = "Role", SourceRoleName = "Admin" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "system.config", DisplayName = "系统配置", Category = "System", Source = "Role", SourceRoleName = "Admin" }
            };
        }

        private IEnumerable<UserPermissionDto> GetAuthorPermissions()
        {
            return new[]
            {
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "post.create", DisplayName = "创建文章", Category = "Content", Source = "Role", SourceRoleName = "Author" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "post.edit", DisplayName = "编辑文章", Category = "Content", Source = "Role", SourceRoleName = "Author" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "comment.moderate", DisplayName = "评论审核", Category = "Content", Source = "Role", SourceRoleName = "Author" }
            };
        }

        private IEnumerable<UserPermissionDto> GetUserPermissions()
        {
            return new[]
            {
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "post.read", DisplayName = "阅读文章", Category = "Content", Source = "Role", SourceRoleName = "User" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "comment.create", DisplayName = "发表评论", Category = "Content", Source = "Role", SourceRoleName = "User" },
                new UserPermissionDto { Id = Guid.NewGuid(), Name = "profile.edit", DisplayName = "编辑个人资料", Category = "Profile", Source = "Role", SourceRoleName = "User" }
            };
        }

        private async Task<IEnumerable<RecentUserActivityDto>> GetUserRecentActivitiesAsync(Guid userId, int limit)
        {
            // 简化实现，实际项目中应该从活动日志表获取
            var activities = new List<RecentUserActivityDto>();
            for (int i = 0; i < Math.Min(limit, 10); i++)
            {
                activities.Add(new RecentUserActivityDto
                {
                    Id = Guid.NewGuid(),
                    ActivityType = new[] { "Login", "Post", "Comment", "Like" }[i % 4],
                    Description = $"用户执行了{new[] { "登录", "发布文章", "发表评论", "点赞" }[i % 4]}操作",
                    Timestamp = DateTime.UtcNow.AddHours(-i),
                    Status = "Success"
                });
            }
            return activities;
        }

        private async Task<UserPreferencesDto> GetUserPreferencesAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从用户偏好设置表获取
            return new UserPreferencesDto
            {
                Language = "zh-CN",
                Timezone = "Asia/Shanghai",
                Theme = "light",
                NotificationSettings = new Dictionary<string, bool>
                {
                    { "email", true },
                    { "push", false },
                    { "sms", false }
                },
                PrivacySettings = new Dictionary<string, string>
                {
                    { "profile_visibility", "public" },
                    { "activity_visibility", "friends" }
                },
                LastUpdated = DateTime.UtcNow.AddDays(-7)
            };
        }

        private async Task<IEnumerable<UserDeviceDto>> GetUserDevicesAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从用户设备表获取
            return new[]
            {
                new UserDeviceDto
                {
                    Id = Guid.NewGuid(),
                    DeviceId = "device-001",
                    DeviceName = "iPhone 13",
                    DeviceType = "Mobile",
                    OperatingSystem = "iOS 15.0",
                    Browser = "Safari",
                    FirstSeen = DateTime.UtcNow.AddDays(-30),
                    LastSeen = DateTime.UtcNow.AddHours(-2),
                    IsTrusted = true,
                    IsActive = true
                }
            };
        }

        private async Task<IEnumerable<UserSocialAccountDto>> GetUserSocialAccountsAsync(Guid userId)
        {
            // 简化实现，实际项目中应该从社交账号绑定表获取
            return new List<UserSocialAccountDto>();
        }

        #endregion

        #region Batch Operation Detail DTO

        public class BatchOperationDetailDto
        {
            public string ResourceId { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? ErrorMessage { get; set; }
        }

        #endregion

        #region Additional Helper Methods

        private string GetRoleDescription(MapleBlog.Domain.Enums.UserRole role)
        {
            return role switch
            {
                MapleBlog.Domain.Enums.UserRole.Admin => "系统管理员，拥有所有权限",
                MapleBlog.Domain.Enums.UserRole.Author => "作者，可以创建和管理内容",
                MapleBlog.Domain.Enums.UserRole.User => "普通用户，可以浏览和评论",
                _ => "未知角色"
            };
        }

        private int GetRolePriority(string roleName)
        {
            return roleName.ToLower() switch
            {
                "admin" => 100,
                "author" => 50,
                "user" => 10,
                _ => 0
            };
        }

        #endregion
    }
}