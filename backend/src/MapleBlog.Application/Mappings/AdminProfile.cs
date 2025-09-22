using AutoMapper;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;
using UserRoleEntity = MapleBlog.Domain.Entities.UserRole;

namespace MapleBlog.Application.Mappings
{
    /// <summary>
    /// Admin系统AutoMapper配置 - 用于管理员界面的复杂映射
    /// </summary>
    public class AdminProfile : Profile
    {
        public AdminProfile()
        {
            CreateUserManagementMappings();
            CreateDashboardMappings();
            CreateAnalyticsMappings();
            CreateAuditMappings();
            CreatePermissionMappings();
            CreateContentManagementMappings();
        }

        /// <summary>
        /// 用户管理相关映射
        /// </summary>
        private void CreateUserManagementMappings()
        {
            // User -> UserManagementDto (带权限处理和数据脱敏)
            CreateMap<User, UserManagementDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetDisplayName()))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.AvatarUrl))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GetUserStatus(src)))
                .ForMember(dest => dest.EmailVerified, opt => opt.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.PhoneVerified, opt => opt.MapFrom(src => src.PhoneNumberConfirmed))
                .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEndDateUtc))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => GetUserRoles(src.Role)))
                .ForMember(dest => dest.UserRole, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Tags, opt => opt.Ignore()) // 需要从其他地方获取
                .ForMember(dest => dest.Stats, opt => opt.Ignore()) // 需要聚合查询
                .ForMember(dest => dest.RiskLevel, opt => opt.Ignore()) // 需要计算
                .ForMember(dest => dest.IsOnline, opt => opt.Ignore()) // 需要从缓存获取
                .ForMember(dest => dest.Location, opt => opt.Ignore()); // 需要从登录历史获取

            // User -> UserDetailDto (完整的用户详情，包含所有关联数据)
            CreateMap<User, UserDetailDto>()
                .ForMember(dest => dest.BasicInfo, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Profile, opt => opt.MapFrom(src => MapToUserProfile(src)))
                .ForMember(dest => dest.SecurityInfo, opt => opt.Ignore()) // 需要额外查询
                .ForMember(dest => dest.ActivityStats, opt => opt.Ignore()) // 需要聚合查询
                .ForMember(dest => dest.PermissionInfo, opt => opt.Ignore()) // 需要权限服务
                .ForMember(dest => dest.SocialAccounts, opt => opt.Ignore()) // 需要额外查询
                .ForMember(dest => dest.Devices, opt => opt.Ignore()) // 需要额外查询
                .ForMember(dest => dest.RecentActivities, opt => opt.Ignore()) // 需要活动日志
                .ForMember(dest => dest.Preferences, opt => opt.Ignore()); // 需要用户偏好服务

            // CreateUserRequestDto -> User
            CreateMap<CreateUserRequestDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => Email.Create(src.Email)))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.InitialStatus == "Active"))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => !src.RequireEmailVerification))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 在服务中处理
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore()) // 在服务中生成
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

            // User -> UserProfileDto
            CreateMap<User, UserProfileDto>()
                .ForMember(dest => dest.Address, opt => opt.Ignore()) // 需要地址信息
                .ForMember(dest => dest.Interests, opt => opt.Ignore()) // 需要用户兴趣数据
                .ForMember(dest => dest.Skills, opt => opt.Ignore()) // 需要用户技能数据
                .ForMember(dest => dest.Language, opt => opt.Ignore()) // 需要用户偏好
                .ForMember(dest => dest.Timezone, opt => opt.Ignore()); // 需要用户偏好

            // LoginHistory -> UserLoginHistoryDto
            CreateMap<LoginHistory, UserLoginHistoryDto>()
                .ForMember(dest => dest.LoginTime, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LogoutTime, opt => opt.MapFrom(src => src.LogoutAt))
                .ForMember(dest => dest.SessionDuration, opt => opt.MapFrom(src =>
                    src.LogoutAt.HasValue ? src.LogoutAt.Value - src.CreatedAt : (TimeSpan?)null))
                .ForMember(dest => dest.DeviceInfo, opt => opt.MapFrom(src => MapToLoginDevice(src)))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => MapToUserLocation(src)))
                .ForMember(dest => dest.LoginMethod, opt => opt.MapFrom(src => src.LoginType.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsSuccessful ? "Success" : "Failed"))
                .ForMember(dest => dest.FailureReason, opt => opt.MapFrom(src => src.FailureReason))
                .ForMember(dest => dest.IsSuspicious, opt => opt.MapFrom(src => src.RiskScore > 0.7))
                .ForMember(dest => dest.SessionTokenId, opt => opt.MapFrom(src => src.SessionId));
        }

        /// <summary>
        /// 仪表板相关映射
        /// </summary>
        private void CreateDashboardMappings()
        {
            // 这些映射通常涉及聚合数据，在服务层手动构建
            // 但可以定义一些辅助映射
        }

        /// <summary>
        /// 分析报告相关映射
        /// </summary>
        private void CreateAnalyticsMappings()
        {
            // 分析数据通常是计算得出的，映射较少
            // 主要用于格式化展示
        }

        /// <summary>
        /// 审计日志相关映射
        /// </summary>
        private void CreateAuditMappings()
        {
            // AuditLog -> AuditLogDto
            CreateMap<AuditLog, AuditLogDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.UserEmail))
                .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action))
                .ForMember(dest => dest.ResourceType, opt => opt.MapFrom(src => src.ResourceType))
                .ForMember(dest => dest.ResourceId, opt => opt.MapFrom(src => src.ResourceId))
                .ForMember(dest => dest.ResourceName, opt => opt.MapFrom(src => src.ResourceName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.RequestPath, opt => opt.MapFrom(src => src.RequestPath))
                .ForMember(dest => dest.RequestMethod, opt => opt.MapFrom(src => src.HttpMethod))
                .ForMember(dest => dest.ResponseStatusCode, opt => opt.MapFrom(src => src.StatusCode))
                .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.OldValues, opt => opt.MapFrom(src => src.OldValues))
                .ForMember(dest => dest.NewValues, opt => opt.MapFrom(src => src.NewValues))
                .ForMember(dest => dest.Changes, opt => opt.MapFrom(src => src.NewValues))
                .ForMember(dest => dest.Exception, opt => opt.MapFrom(src => src.ErrorMessage))
                .ForMember(dest => dest.AdditionalInfo, opt => opt.MapFrom(src => src.AdditionalData))
                .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => src.RiskLevel))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => new string[0]))
                .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.Environment, opt => opt.MapFrom(src => "Production"))
                .ForMember(dest => dest.ApplicationName, opt => opt.MapFrom(src => "MapleBlog"))
                .ForMember(dest => dest.ApplicationVersion, opt => opt.MapFrom(src => "1.0.0"))
                .ForMember(dest => dest.CorrelationId, opt => opt.MapFrom(src => src.CorrelationId))
                .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionId))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => (Guid?)null))
                .ForMember(dest => dest.ModuleName, opt => opt.MapFrom(src => src.ResourceType))
                .ForMember(dest => dest.FeatureName, opt => opt.MapFrom(src => src.Action))
                .ForMember(dest => dest.BusinessProcess, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.RiskLevel, opt => opt.MapFrom(src => src.RiskLevel.ToString()))
                .ForMember(dest => dest.ComplianceFlags, opt => opt.MapFrom(src => new string[0]))
                .ForMember(dest => dest.DataClassification, opt => opt.MapFrom(src => "Public"))
                .ForMember(dest => dest.RetentionPeriod, opt => opt.MapFrom(src => src.RetentionPeriod))
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived))
                .ForMember(dest => dest.ArchivedAt, opt => opt.MapFrom(src => src.ArchivedAt));
        }

        /// <summary>
        /// 权限管理相关映射
        /// </summary>
        private void CreatePermissionMappings()
        {
            // Role -> RoleDto
            CreateMap<Role, RoleDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsSystem, opt => opt.MapFrom(src => src.IsSystemRole))
                .ForMember(dest => dest.Permissions, opt => opt.Ignore()) // 需要从关联表获取
                .ForMember(dest => dest.UserCount, opt => opt.Ignore()); // 需要聚合查询

            // Permission -> PermissionDto
            CreateMap<Permission, PermissionDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Scope, opt => opt.MapFrom(src => src.Scope.ToString()))
                .ForMember(dest => dest.IsSystemPermission, opt => opt.MapFrom(src => src.IsSystemPermission));

            // UserRoleEntity -> UserRoleDto
            CreateMap<UserRoleEntity, UserRoleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.RoleId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : "Unknown"))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.Role != null ? src.Role.DisplayName : "Unknown"))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Role != null ? src.Role.Description : null))
                .ForMember(dest => dest.IsSystem, opt => opt.MapFrom(src => src.Role != null && src.Role.IsSystemRole))
                .ForMember(dest => dest.AssignedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.AssignedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore()) // 如果有过期机制
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsDeleted));
        }

        /// <summary>
        /// 内容管理相关映射
        /// </summary>
        private void CreateContentManagementMappings()
        {
            // Post -> PostManagementDto (管理员视图的文章)
            CreateMap<Post, PostManagementDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PublishedAt, opt => opt.MapFrom(src => src.PublishedAt))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => src.ViewCount))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentCount))
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikeCount))
                .ForMember(dest => dest.ShareCount, opt => opt.MapFrom(src => src.ShareCount))
                .ForMember(dest => dest.WordCount, opt => opt.MapFrom(src => src.WordCount))
                .ForMember(dest => dest.ReadingTime, opt => opt.MapFrom(src => src.ReadingTime))
                .ForMember(dest => dest.IsFeatured, opt => opt.MapFrom(src => src.IsFeatured))
                .ForMember(dest => dest.IsSticky, opt => opt.MapFrom(src => src.IsSticky))
                .ForMember(dest => dest.AllowComments, opt => opt.MapFrom(src => src.AllowComments))
                .ForMember(dest => dest.ContentWarnings, opt => opt.Ignore()) // 需要内容分析
                .ForMember(dest => dest.ModerationFlags, opt => opt.Ignore()) // 需要审核记录
                .ForMember(dest => dest.SeoScore, opt => opt.Ignore()) // 需要SEO分析
                .ForMember(dest => dest.LastEditedBy, opt => opt.MapFrom(src => src.UpdatedBy));

            // Comment -> CommentManagementDto (管理员视图的评论)
            CreateMap<Comment, CommentManagementDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content.RawContent))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
                .ForMember(dest => dest.Post, opt => opt.MapFrom(src => src.Post))
                .ForMember(dest => dest.ParentComment, opt => opt.MapFrom(src => src.Parent))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
                .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.LikeCount))
                .ForMember(dest => dest.ReplyCount, opt => opt.MapFrom(src => src.ReplyCount))
                .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
                .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
                .ForMember(dest => dest.ReportCount, opt => opt.MapFrom(src => src.Reports.Count(r => !r.IsDeleted)))
                .ForMember(dest => dest.ModerationNotes, opt => opt.MapFrom(src => src.ModerationNotes))
                .ForMember(dest => dest.ModeratedAt, opt => opt.MapFrom(src => src.ModeratedAt))
                .ForMember(dest => dest.ModeratedBy, opt => opt.MapFrom(src => src.Moderator))
                .ForMember(dest => dest.RiskScore, opt => opt.Ignore()) // 需要计算
                .ForMember(dest => dest.AutoModerationFlags, opt => opt.Ignore()); // 需要AI分析结果
        }

        #region 辅助方法

        /// <summary>
        /// 获取用户状态
        /// </summary>
        private static string GetUserStatus(User user)
        {
            if (!user.IsActive) return "Inactive";
            if (user.IsLockedOut()) return "Locked";
            if (!user.EmailConfirmed) return "Pending";
            return "Active";
        }

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        private static IEnumerable<string> GetUserRoles(UserRoleEnum role)
        {
            return role.GetIndividualRoles().Select(r => r.ToString());
        }

        /// <summary>
        /// 映射用户资料
        /// </summary>
        private static UserProfileDto MapToUserProfile(User user)
        {
            return new UserProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Bio = user.Bio,
                Website = user.Website,
                // 其他字段需要从用户偏好或扩展信息获取
            };
        }

        /// <summary>
        /// 映射登录设备信息
        /// </summary>
        private static UserLoginDeviceDto MapToLoginDevice(LoginHistory login)
        {
            return new UserLoginDeviceDto
            {
                DeviceType = login.DeviceType,
                OperatingSystem = login.OperatingSystem,
                Browser = login.Browser,
                DeviceModel = login.DeviceModel,
                IsMobile = login.IsMobile,
                ScreenResolution = login.ScreenResolution
            };
        }

        /// <summary>
        /// 映射用户位置信息
        /// </summary>
        private static UserLocationDto MapToUserLocation(LoginHistory login)
        {
            return new UserLocationDto
            {
                Country = login.Country,
                Region = login.Region,
                City = login.City,
                PostalCode = login.PostalCode,
                Latitude = login.Latitude,
                Longitude = login.Longitude,
                Timezone = login.Timezone,
                IpAddress = login.IpAddress
            };
        }

        #endregion
    }

    /// <summary>
    /// Admin映射扩展方法
    /// </summary>
    public static class AdminMappingExtensions
    {
        /// <summary>
        /// 映射并脱敏敏感信息
        /// </summary>
        public static TDestination MapWithDataMasking<TSource, TDestination>(
            this IMapper mapper,
            TSource source,
            bool includeSensitiveData = false)
        {
            var result = mapper.Map<TDestination>(source);

            if (!includeSensitiveData && result is UserManagementDto userDto)
            {
                // 脱敏处理
                userDto.Email = MaskEmail(userDto.Email);
                userDto.PhoneNumber = MaskPhoneNumber(userDto.PhoneNumber);
            }

            return result;
        }

        /// <summary>
        /// 邮箱脱敏
        /// </summary>
        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return email;

            var parts = email.Split('@');
            var localPart = parts[0];
            var domainPart = parts[1];

            if (localPart.Length <= 2)
                return $"{localPart[0]}***@{domainPart}";

            return $"{localPart.Substring(0, 2)}***@{domainPart}";
        }

        /// <summary>
        /// 手机号脱敏
        /// </summary>
        private static string? MaskPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
                return phoneNumber;

            return $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(phoneNumber.Length - 4)}";
        }
    }
}