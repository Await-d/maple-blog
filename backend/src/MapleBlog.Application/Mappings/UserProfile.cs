using AutoMapper;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Mappings.TypeConverters;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Application.Mappings
{
    /// <summary>
    /// 增强的用户映射配置 - 支持值对象、枚举转换和复杂场景
    /// </summary>
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            ConfigureTypeConverters();
            CreateUserMappings();
            CreateAuthenticationMappings();
            CreateAdminUserMappings();
        }

        /// <summary>
        /// 配置类型转换器
        /// </summary>
        private void ConfigureTypeConverters()
        {
            // Email值对象转换
            CreateMap<Email, string>().ConvertUsing<EmailToStringConverter>();
            CreateMap<string, Email>().ConvertUsing<StringToEmailConverter>();

            // 枚举转换
            CreateMap<UserRoleEnum, string>().ConvertUsing<UserRoleToStringConverter>();
            CreateMap<string, UserRoleEnum>().ConvertUsing<StringToUserRoleConverter>();

            // 日期时间转换
            CreateMap<DateTime, DateTime>().ConvertUsing<DateTimeToUtcConverter>();
            CreateMap<DateTime, string>().ConvertUsing<DateTimeToFormattedStringConverter>();
            CreateMap<DateTime?, string>().ConvertUsing<NullableDateTimeToFormattedStringConverter>();

            // 文件路径转URL
            CreateMap<string, string>().ConvertUsing(new FilePathToUrlConverter("/api/files/"));
        }

        /// <summary>
        /// 创建用户基础映射
        /// </summary>
        private void CreateUserMappings()
        {
            // User -> UserDto (增强版本，支持值对象和计算属性)
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.GetFullName()))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetDisplayName()))
                .ForMember(dest => dest.Avatar, opt => opt.MapFrom(src => src.AvatarUrl))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.EmailConfirmed))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive && !src.IsLockedOut()))
                .AfterMap((src, dest, context) =>
                {
                    // 后处理逻辑 - 可以添加权限检查、数据脱敏等
                    if (context.Items.ContainsKey("MaskSensitiveData") && (bool)context.Items["MaskSensitiveData"])
                    {
                        dest.Email = MaskEmail(dest.Email);
                    }
                });

            // UserDto -> User (反向映射，用于更新场景)
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => Email.Create(src.Email)))
                .ForMember(dest => dest.FirstName, opt => opt.Ignore())
                .ForMember(dest => dest.LastName, opt => opt.Ignore())
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationToken, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationTokenExpiry, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEndDateUtc, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.IsEmailVerified))
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
        }

        /// <summary>
        /// 创建身份验证相关映射
        /// </summary>
        private void CreateAuthenticationMappings()
        {
            // RegisterRequest -> User (增强版本，支持角色设置)
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => Email.Create(src.Email)))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}".Trim()))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // 在服务中处理
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore()) // 在服务中生成
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => UserRoleEnum.User)) // 默认角色
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.AvatarUrl, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationToken, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationTokenExpiry, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.LockoutEndDateUtc, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Bio, opt => opt.Ignore())
                .ForMember(dest => dest.Website, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.Gender, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.PasswordResetToken, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // UpdateUserProfileRequest -> User (部分更新映射)
            CreateMap<UpdateUserProfileRequest, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Avatar))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
                .ForMember(dest => dest.EmailConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationToken, opt => opt.Ignore())
                .ForMember(dest => dest.EmailVerificationTokenExpiry, opt => opt.Ignore())
                .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEndDateUtc, opt => opt.Ignore())
                .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.TwoFactorEnabled, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumber, opt => opt.Ignore())
                .ForMember(dest => dest.PhoneNumberConfirmed, opt => opt.Ignore())
                .ForMember(dest => dest.Bio, opt => opt.Ignore())
                .ForMember(dest => dest.Website, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.Gender, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerified, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetToken, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordResetTokenExpiresAt, opt => opt.Ignore())
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DisplayName, opt => opt.Ignore()); // 可能在服务中重新计算
        }

        /// <summary>
        /// 创建管理员相关映射
        /// </summary>
        private void CreateAdminUserMappings()
        {
            // 这些映射已在AdminProfile中定义，这里只做引用声明
            // 避免重复定义，保持映射的一致性
        }

        #region 辅助方法

        /// <summary>
        /// 邮箱脱敏处理
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

        #endregion
    }

    /// <summary>
    /// 用户映射扩展方法
    /// </summary>
    public static class UserMappingExtensions
    {
        /// <summary>
        /// 根据权限映射用户信息
        /// </summary>
        public static UserDto MapWithPermissions(this IMapper mapper, User user, UserRoleEnum currentUserRole, bool includeSensitiveData = false)
        {
            var options = new Dictionary<string, object>();

            // 根据权限决定是否脱敏
            if (!includeSensitiveData && !currentUserRole.HasAnyRole(UserRoleEnum.Admin, UserRoleEnum.SuperAdmin))
            {
                options["MaskSensitiveData"] = true;
            }

            return mapper.Map<UserDto>(user, opt =>
            {
                foreach (var item in options)
                {
                    opt.Items[item.Key] = item.Value;
                }
            });
        }

        /// <summary>
        /// 批量映射用户信息（带权限控制）
        /// </summary>
        public static IEnumerable<UserDto> MapUsersWithPermissions(this IMapper mapper, IEnumerable<User> users, UserRoleEnum currentUserRole, bool includeSensitiveData = false)
        {
            return users.Select(user => mapper.MapWithPermissions(user, currentUserRole, includeSensitiveData));
        }

        /// <summary>
        /// 安全映射用户列表（自动脱敏）
        /// </summary>
        public static IEnumerable<UserDto> MapUsersSecurely(this IMapper mapper, IEnumerable<User> users)
        {
            return mapper.Map<IEnumerable<UserDto>>(users, opt =>
            {
                opt.Items["MaskSensitiveData"] = true;
            });
        }
    }
}