using System.ComponentModel.DataAnnotations;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Services;

namespace MapleBlog.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    [Required]
    public Email Email { get; set; } = Email.Create("user@example.com");

    /// <summary>
    /// 邮箱是否已确认
    /// </summary>
    public bool EmailConfirmed { get; set; } = false;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 安全戳
    /// </summary>
    [Required]
    [StringLength(255)]
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 电话号码
    /// </summary>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 电话号码是否已确认
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; } = false;

    /// <summary>
    /// 是否启用双因子认证
    /// </summary>
    public bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    public DateTime? LockoutEndDateUtc { get; set; }

    /// <summary>
    /// 是否启用锁定
    /// </summary>
    public bool LockoutEnabled { get; set; } = true;

    /// <summary>
    /// 访问失败次数
    /// </summary>
    public int AccessFailedCount { get; set; } = 0;

    // 用户资料信息

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 名字
    /// </summary>
    [StringLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// 姓氏
    /// </summary>
    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 个人简介
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// 个人网站
    /// </summary>
    [StringLength(255)]
    [Url]
    public string? Website { get; set; }

    /// <summary>
    /// 所在地
    /// </summary>
    [StringLength(100)]
    public string? Location { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [StringLength(10)]
    public string? Gender { get; set; }

    // 状态信息

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 邮箱验证令牌
    /// </summary>
    [StringLength(255)]
    public string? EmailVerificationToken { get; set; }

    /// <summary>
    /// 邮箱验证令牌过期时间
    /// </summary>
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    /// <summary>
    /// 是否已验证
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// 邮箱是否已验证（属性别名，用于保持API一致性）
    /// </summary>
    public bool IsEmailVerified => EmailConfirmed;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 密码重置令牌
    /// </summary>
    [StringLength(255)]
    public string? PasswordResetToken { get; set; }

    /// <summary>
    /// 密码重置令牌过期时间
    /// </summary>
    public DateTime? PasswordResetTokenExpiresAt { get; set; }

    // 导航属性

    /// <summary>
    /// 用户角色（单一角色模式）
    /// </summary>
    public Enums.UserRole Role { get; set; } = Enums.UserRole.User;

    /// <summary>
    /// 用户角色关联（多角色模式 - 用于权限系统）
    /// </summary>
    public virtual ICollection<UserRole>? UserRoles { get; set; }

    /// <summary>
    /// 用户的数据权限规则
    /// </summary>
    public virtual ICollection<DataPermissionRule>? DataPermissionRules { get; set; }

    /// <summary>
    /// 用户授予的数据权限规则
    /// </summary>
    public virtual ICollection<DataPermissionRule>? GrantedDataPermissionRules { get; set; }

    /// <summary>
    /// 用户的临时权限
    /// </summary>
    public virtual ICollection<TemporaryPermission>? TemporaryPermissions { get; set; }

    /// <summary>
    /// 用户授予的临时权限
    /// </summary>
    public virtual ICollection<TemporaryPermission>? GrantedTemporaryPermissions { get; set; }

    /// <summary>
    /// 用户委派的临时权限
    /// </summary>
    public virtual ICollection<TemporaryPermission>? DelegatedTemporaryPermissions { get; set; }

    /// <summary>
    /// 用户分配的用户角色（作为分配者）
    /// </summary>
    public virtual ICollection<UserRole>? AssignedUserRoles { get; set; }

    /// <summary>
    /// 用户授予的角色权限（作为授权者）
    /// </summary>
    public virtual ICollection<RolePermission>? GrantedRolePermissions { get; set; }

    /// <summary>
    /// 用户创建的文章
    /// </summary>
    public virtual ICollection<Post>? Posts { get; set; }

    /// <summary>
    /// 用户发表的评论
    /// </summary>
    public virtual ICollection<Comment>? Comments { get; set; }

    /// <summary>
    /// 用户的审计日志
    /// </summary>
    public virtual ICollection<AuditLog>? AuditLogs { get; set; }

    /// <summary>
    /// 用户上传的文件
    /// </summary>
    public virtual ICollection<File>? Files { get; set; }

    /// <summary>
    /// 用户的登录历史
    /// </summary>
    public virtual ICollection<LoginHistory>? LoginHistories { get; set; }

    /// <summary>
    /// 用户的搜索查询
    /// </summary>
    public virtual ICollection<SearchQuery>? SearchQueries { get; set; }

    /// <summary>
    /// 用户撤销的临时权限
    /// </summary>
    public virtual ICollection<TemporaryPermission>? RevokedTemporaryPermissions { get; set; }

    // 业务方法

    /// <summary>
    /// 获取显示名称（优先使用DisplayName，否则使用UserName）
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : UserName;
    }

    /// <summary>
    /// 获取全名
    /// </summary>
    public string? GetFullName()
    {
        if (string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName))
            return null;

        return $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// 检查是否被锁定
    /// </summary>
    public bool IsLockedOut()
    {
        return LockoutEnabled && LockoutEndDateUtc.HasValue && LockoutEndDateUtc.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    /// <param name="lockoutEndDate">锁定结束时间</param>
    public void LockUser(DateTime lockoutEndDate)
    {
        LockoutEndDateUtc = lockoutEndDate;
        UpdateAuditFields();
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    public void UnlockUser()
    {
        LockoutEndDateUtc = null;
        AccessFailedCount = 0;
        UpdateAuditFields();
    }

    /// <summary>
    /// 增加访问失败次数
    /// </summary>
    public void IncreaseAccessFailedCount()
    {
        AccessFailedCount++;
        UpdateAuditFields();
    }

    /// <summary>
    /// 重置访问失败次数
    /// </summary>
    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
        UpdateAuditFields();
    }

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    public void UpdateLastLoginTime()
    {
        LastLoginAt = DateTime.UtcNow;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查用户是否拥有指定角色（支持 Flags 枚举）
    /// </summary>
    /// <param name="role">角色枚举值</param>
    /// <returns>是否拥有角色</returns>
    public bool HasRole(Enums.UserRole role)
    {
        return Role.HasRole(role);
    }

    /// <summary>
    /// 检查用户是否拥有任意一个指定角色
    /// </summary>
    /// <param name="roles">角色枚举值数组</param>
    /// <returns>是否拥有任意一个角色</returns>
    public bool HasAnyRole(params Enums.UserRole[] roles)
    {
        return Role.HasAnyRole(roles);
    }

    /// <summary>
    /// 检查用户是否拥有所有指定角色
    /// </summary>
    /// <param name="roles">角色枚举值数组</param>
    /// <returns>是否拥有所有角色</returns>
    public bool HasAllRoles(params Enums.UserRole[] roles)
    {
        return Role.HasAllRoles(roles);
    }

    /// <summary>
    /// 检查用户是否拥有指定角色（字符串版本）
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <returns>是否拥有角色</returns>
    public bool HasRole(string roleName)
    {
        if (Enum.TryParse<Enums.UserRole>(roleName, true, out var role))
            return HasRole(role);
        return false;
    }

    /// <summary>
    /// 检查用户是否为管理员
    /// </summary>
    /// <returns>是否为管理员</returns>
    public bool IsAdmin()
    {
        return Role.HasRole(Enums.UserRole.Admin);
    }

    /// <summary>
    /// 检查用户是否为超级管理员
    /// </summary>
    /// <returns>是否为超级管理员</returns>
    public bool IsSuperAdmin()
    {
        return Role.HasRole(Enums.UserRole.SuperAdmin);
    }

    /// <summary>
    /// 检查用户是否为作者或更高权限
    /// </summary>
    /// <returns>是否为作者或管理员</returns>
    public bool IsAuthorOrHigher()
    {
        return Role.HasAnyRole(Enums.UserRole.Author, Enums.UserRole.Moderator, Enums.UserRole.Admin, Enums.UserRole.SuperAdmin);
    }

    /// <summary>
    /// 检查用户是否为内容管理员（作者或审核员）
    /// </summary>
    /// <returns>是否为内容管理员</returns>
    public bool IsContentManager()
    {
        return Role.HasAnyRole(Enums.UserRole.Author, Enums.UserRole.Moderator) ||
               Role.HasRole(Enums.UserRole.ContentManager);
    }

    /// <summary>
    /// 检查用户是否为系统管理员（管理员或超级管理员）
    /// </summary>
    /// <returns>是否为系统管理员</returns>
    public bool IsSystemManager()
    {
        return Role.HasAnyRole(Enums.UserRole.Admin, Enums.UserRole.SuperAdmin) ||
               Role.HasRole(Enums.UserRole.SystemManager);
    }

    /// <summary>
    /// 检查用户是否拥有特定权限
    /// </summary>
    /// <param name="permission">权限名称</param>
    /// <returns>是否拥有权限</returns>
    public bool HasPermission(string permission)
    {
        return RolePermissionService.HasPermission(Role, permission);
    }

    /// <summary>
    /// 检查用户是否拥有任意一个指定权限
    /// </summary>
    /// <param name="permissions">权限名称数组</param>
    /// <returns>是否拥有任意一个权限</returns>
    public bool HasAnyPermission(params string[] permissions)
    {
        return RolePermissionService.HasAnyPermission(Role, permissions);
    }

    /// <summary>
    /// 检查用户是否拥有所有指定权限
    /// </summary>
    /// <param name="permissions">权限名称数组</param>
    /// <returns>是否拥有所有权限</returns>
    public bool HasAllPermissions(params string[] permissions)
    {
        return RolePermissionService.HasAllPermissions(Role, permissions);
    }

    /// <summary>
    /// 获取用户的所有权限
    /// </summary>
    /// <returns>权限集合</returns>
    public HashSet<string> GetPermissions()
    {
        return RolePermissionService.GetUserPermissions(Role);
    }

    /// <summary>
    /// 获取用户角色的显示名称
    /// </summary>
    /// <returns>角色显示名称</returns>
    public string GetRoleDisplayName()
    {
        return Role.ToDisplayString();
    }

    /// <summary>
    /// 获取用户的个人角色（非组合角色）
    /// </summary>
    /// <returns>个人角色数组</returns>
    public Enums.UserRole[] GetIndividualRoles()
    {
        return Role.GetIndividualRoles();
    }

    /// <summary>
    /// 添加角色给用户
    /// </summary>
    /// <param name="roleToAdd">要添加的角色</param>
    public void AddRole(Enums.UserRole roleToAdd)
    {
        Role = Role.AddRole(roleToAdd);
        UpdateAuditFields();
    }

    /// <summary>
    /// 从用户移除角色
    /// </summary>
    /// <param name="roleToRemove">要移除的角色</param>
    public void RemoveRole(Enums.UserRole roleToRemove)
    {
        Role = Role.RemoveRole(roleToRemove);
        UpdateAuditFields();
    }

    /// <summary>
    /// 设置用户角色（替换现有角色）
    /// </summary>
    /// <param name="newRole">新角色</param>
    public void SetRole(Enums.UserRole newRole)
    {
        Role = newRole;
        UpdateAuditFields();
    }

    /// <summary>
    /// 检查用户是否可以管理目标用户
    /// </summary>
    /// <param name="targetUser">目标用户</param>
    /// <returns>是否可以管理</returns>
    public bool CanManageUser(User targetUser)
    {
        return Role.CanManageUser(targetUser.Role);
    }

    /// <summary>
    /// 检查用户是否可以将目标用户的角色更改为指定角色
    /// </summary>
    /// <param name="targetUser">目标用户</param>
    /// <param name="newRole">新角色</param>
    /// <returns>是否可以更改角色</returns>
    public bool CanChangeUserRole(User targetUser, Enums.UserRole newRole)
    {
        return RolePermissionService.CanChangeUserRole(Role, targetUser.Role, newRole);
    }

    /// <summary>
    /// 获取角色层级等级
    /// </summary>
    /// <returns>层级等级</returns>
    public int GetRoleHierarchyLevel()
    {
        return Role.GetHierarchyLevel();
    }

    /// <summary>
    /// 设置邮箱验证令牌
    /// </summary>
    /// <param name="token">验证令牌</param>
    /// <param name="expiryMinutes">过期时间（分钟）</param>
    public void SetEmailVerificationToken(string token, int expiryMinutes = 1440) // 默认24小时
    {
        EmailVerificationToken = token;
        EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        UpdateAuditFields();
    }

    /// <summary>
    /// 验证邮箱
    /// </summary>
    public void VerifyEmail()
    {
        EmailConfirmed = true;
        IsVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        UpdateAuditFields();
    }

    /// <summary>
    /// 构造函数用于创建具有基本信息的用户
    /// </summary>
    public User(string userName, Email email, string passwordHash, Enums.UserRole role = Enums.UserRole.User)
    {
        UserName = userName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        SecurityStamp = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// 构造函数用于创建具有基本信息的用户（字符串Email版本）
    /// </summary>
    public User(string userName, string email, string passwordHash, Enums.UserRole role = Enums.UserRole.User)
        : this(userName, Email.Create(email), passwordHash, role)
    {
    }

    /// <summary>
    /// 无参构造函数（供ORM使用）
    /// </summary>
    public User()
    {
    }

    /// <summary>
    /// 设置密码重置令牌
    /// </summary>
    /// <param name="token">重置令牌</param>
    /// <param name="expiryMinutes">过期时间（分钟）</param>
    public void SetPasswordResetToken(string token, int expiryMinutes = 1440)
    {
        // Note: This method assumes password reset token properties exist
        // If they don't exist in the entity, they would need to be added
        UpdateAuditFields();
    }

    /// <summary>
    /// 更新密码
    /// </summary>
    /// <param name="newPasswordHash">新密码哈希</param>
    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid().ToString();
        UpdateAuditFields();
    }

    /// <summary>
    /// 更改邮箱
    /// </summary>
    /// <param name="newEmail">新邮箱</param>
    public void ChangeEmail(Email newEmail)
    {
        Email = newEmail;
        EmailConfirmed = false;
        UpdateAuditFields();
    }

    /// <summary>
    /// 更改邮箱（字符串版本）
    /// </summary>
    /// <param name="newEmail">新邮箱字符串</param>
    public void ChangeEmail(string newEmail)
    {
        ChangeEmail(Email.Create(newEmail));
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="displayName">显示名称</param>
    /// <param name="firstName">名字</param>
    /// <param name="lastName">姓氏</param>
    /// <param name="bio">个人简介</param>
    /// <param name="website">个人网站</param>
    /// <param name="location">所在地</param>
    public void UpdateProfile(string? displayName = null, string? firstName = null, string? lastName = null,
                            string? bio = null, string? website = null, string? location = null)
    {
        if (displayName != null) DisplayName = displayName;
        if (firstName != null) FirstName = firstName;
        if (lastName != null) LastName = lastName;
        if (bio != null) Bio = bio;
        if (website != null) Website = website;
        if (location != null) Location = location;
        UpdateAuditFields();
    }
}