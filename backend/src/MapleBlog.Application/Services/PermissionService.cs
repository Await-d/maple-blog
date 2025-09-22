using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Enums;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 权限服务实现 - 提供企业级RBAC权限管理
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly Domain.Interfaces.IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<PermissionService> _logger;

        // 缓存配置
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
        private const string UserPermissionsCacheKeyPrefix = "user_permissions_";
        private const string RolePermissionsCacheKeyPrefix = "role_permissions_";

        public PermissionService(
            Domain.Interfaces.IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUserRoleRepository userRoleRepository,
            IMemoryCache memoryCache,
            ILogger<PermissionService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="permission">权限名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permission))
                    return false;

                _logger.LogDebug("Checking permission {Permission} for user {UserId}", permission, userId);

                // 首先检查缓存
                var cacheKey = $"{UserPermissionsCacheKeyPrefix}{userId}";
                if (_memoryCache.TryGetValue(cacheKey, out ISet<string> cachedPermissions))
                {
                    var hasPermission = cachedPermissions.Contains(permission);
                    _logger.LogDebug("Permission check from cache: {HasPermission}", hasPermission);
                    return hasPermission;
                }

                // 从数据库获取用户权限
                var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);

                // 缓存用户权限
                _memoryCache.Set(cacheKey, userPermissions, CacheDuration);

                var result = userPermissions.Contains(permission);
                _logger.LogDebug("Permission check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
                return false;
            }
        }

        /// <summary>
        /// 检查用户是否有资源操作权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        public async Task<bool> HasResourcePermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default)
        {
            var permission = $"{resource}.{action}";
            return await HasPermissionAsync(userId, permission, cancellationToken);
        }

        /// <summary>
        /// 检查用户是否有指定作用域的资源操作权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="requiredScope">所需的权限作用域</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        public async Task<bool> HasResourcePermissionWithScopeAsync(Guid userId, string resource, string action, PermissionScope requiredScope, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                    return false;

                var userPermissions = await GetUserPermissionsWithDetailsAsync(userId, cancellationToken);

                return userPermissions.Any(p => p.Covers(resource, action, requiredScope));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查用户资源权限作用域时发生错误: UserId={UserId}, Resource={Resource}, Action={Action}, Scope={Scope}",
                    userId, resource, action, requiredScope);
                return false;
            }
        }

        /// <summary>
        /// 获取用户权限详细信息（包含作用域）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限详细信息</returns>
        private async Task<IEnumerable<Permission>> GetUserPermissionsWithDetailsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var permissions = new List<Permission>();
            var userRoles = await _userRoleRepository.GetActiveUserRolesAsync(userId, cancellationToken);

            foreach (var userRole in userRoles)
            {
                var role = await _roleRepository.GetRoleWithPermissionsAsync(userRole.RoleId, cancellationToken);
                if (role != null)
                {
                    foreach (var rolePermission in role.RolePermissions)
                    {
                        if (rolePermission.IsValid() && rolePermission.Permission != null)
                        {
                            permissions.Add(rolePermission.Permission);
                        }
                    }
                }
            }

            return permissions;
        }

        /// <summary>
        /// 获取用户的所有权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限集合</returns>
        public async Task<ISet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting all permissions for user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new HashSet<string>();
                }

                var permissions = new HashSet<string>();

                // 获取用户的角色权限
                var userRoles = await _userRoleRepository.GetActiveUserRolesAsync(userId, cancellationToken);
                foreach (var userRole in userRoles)
                {
                    var rolePermissions = await GetRolePermissionsAsync(userRole.RoleId, cancellationToken);
                    foreach (var permission in rolePermissions)
                    {
                        permissions.Add(permission);
                    }
                }

                // 如果是超级管理员，添加所有权限
                if (user.HasRole("Admin"))
                {
                    var allPermissions = await _permissionRepository.GetAllPermissionsAsync(cancellationToken);
                    foreach (var permission in allPermissions)
                    {
                        permissions.Add(permission.Name);
                    }
                }

                _logger.LogDebug("Found {Count} permissions for user {UserId}", permissions.Count, userId);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", userId);
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// 获取角色的所有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限集合</returns>
        public async Task<ISet<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            try
            {
                var cacheKey = $"{RolePermissionsCacheKeyPrefix}{roleId}";
                if (_memoryCache.TryGetValue(cacheKey, out ISet<string> cachedPermissions))
                {
                    return cachedPermissions;
                }

                var role = await _roleRepository.GetRoleWithPermissionsAsync(roleId, cancellationToken);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleId} not found", roleId);
                    return new HashSet<string>();
                }

                var permissions = new HashSet<string>();
                foreach (var rolePermission in role.RolePermissions)
                {
                    if (rolePermission.IsValid() && rolePermission.Permission != null)
                    {
                        permissions.Add(rolePermission.Permission.Name);
                    }
                }

                // 缓存角色权限
                _memoryCache.Set(cacheKey, permissions, CacheDuration);

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {RoleId}", roleId);
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionIds">权限ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Assigning permissions to role {RoleId}", roleId);

                var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleId} not found", roleId);
                    return false;
                }

                // 检查是否为系统角色
                if (role.IsSystemRole)
                {
                    _logger.LogWarning("Cannot modify permissions for system role {RoleId}", roleId);
                    return false;
                }

                // 清除现有权限
                await _roleRepository.ClearRolePermissionsAsync(roleId, cancellationToken);

                // 添加新权限
                foreach (var permissionId in permissionIds)
                {
                    await _roleRepository.AddRolePermissionAsync(roleId, permissionId, null, cancellationToken);
                }

                // 清除缓存
                ClearRolePermissionsCache(roleId);
                await ClearUserPermissionsCacheAsync(roleId, cancellationToken);

                _logger.LogInformation("Successfully assigned permissions to role {RoleId}", roleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permissions to role {RoleId}", roleId);
                return false;
            }
        }

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="expiresAt">过期时间（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Assigning role {RoleId} to user {UserId}", roleId, userId);

                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                if (role == null)
                {
                    _logger.LogWarning("Role {RoleId} not found", roleId);
                    return false;
                }

                if (!role.IsActive)
                {
                    _logger.LogWarning("Cannot assign inactive role {RoleId} to user {UserId}", roleId, userId);
                    return false;
                }

                // 使用UserRoleRepository分配角色
                var userRole = new Domain.Entities.UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    ExpiresAt = expiresAt
                };

                await _userRoleRepository.AddAsync(userRole, cancellationToken);
                _logger.LogInformation("成功为用户 {UserId} 分配角色 {RoleId}", userId, roleId);

                // 清除用户权限缓存
                ClearUserPermissionsCache(userId);

                _logger.LogInformation("Successfully assigned role {RoleId} to user {UserId}", roleId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return false;
            }
        }

        /// <summary>
        /// 从用户移除角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Removing role {RoleId} from user {UserId}", roleId, userId);

                var result = await _userRoleRepository.RemoveAsync(userId, roleId, cancellationToken);
                if (!result)
                {
                    _logger.LogWarning("Failed to remove role {RoleId} from user {UserId}", roleId, userId);
                    return false;
                }

                // 清除用户权限缓存
                ClearUserPermissionsCache(userId);

                _logger.LogInformation("Successfully removed role {RoleId} from user {UserId}", roleId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return false;
            }
        }

        /// <summary>
        /// 初始化默认权限和角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public async Task<bool> InitializeDefaultPermissionsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Initializing default permissions and roles");

                // 1. 创建系统权限
                await CreateSystemPermissionsAsync(cancellationToken);

                // 2. 创建系统角色
                await CreateSystemRolesAsync(cancellationToken);

                // 3. 分配角色权限
                await AssignDefaultPermissionsToRolesAsync(cancellationToken);

                _logger.LogInformation("Default permissions and roles initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default permissions and roles");
                return false;
            }
        }

        /// <summary>
        /// 清除用户权限缓存
        /// </summary>
        /// <param name="userId">用户ID</param>
        private void ClearUserPermissionsCache(Guid userId)
        {
            var cacheKey = $"{UserPermissionsCacheKeyPrefix}{userId}";
            _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// 清除角色权限缓存
        /// </summary>
        /// <param name="roleId">角色ID</param>
        private void ClearRolePermissionsCache(Guid roleId)
        {
            var cacheKey = $"{RolePermissionsCacheKeyPrefix}{roleId}";
            _memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// 清除所有拥有指定角色的用户的权限缓存
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ClearUserPermissionsCacheAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            try
            {
                var roleUsers = await _userRoleRepository.GetActiveRoleUsersAsync(roleId, cancellationToken);
                foreach (var userRole in roleUsers)
                {
                    ClearUserPermissionsCache(userRole.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user permissions cache for role {RoleId}", roleId);
            }
        }

        /// <summary>
        /// 创建系统权限
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task CreateSystemPermissionsAsync(CancellationToken cancellationToken)
        {
            var systemPermissions = SystemPermission.GetAllPermissions();
            var existingPermissions = await _permissionRepository.GetSystemPermissionsAsync(cancellationToken);
            var existingPermissionNames = existingPermissions.Select(p => p.Name).ToHashSet();

            var permissionsToCreate = new List<Permission>();

            foreach (var permissionName in systemPermissions)
            {
                if (!existingPermissionNames.Contains(permissionName))
                {
                    var parts = permissionName.Split('.');
                    if (parts.Length == 2)
                    {
                        var permission = new Permission();
                        permission.SetResourceAction(parts[0], parts[1], PermissionScope.Global);
                        permission.Description = $"系统权限: {permissionName}";
                        permission.IsSystemPermission = true;
                        permissionsToCreate.Add(permission);
                    }
                }
            }

            if (permissionsToCreate.Any())
            {
                await _permissionRepository.CreateBatchAsync(permissionsToCreate, cancellationToken);
                _logger.LogInformation("创建了 {Count} 个系统权限", permissionsToCreate.Count);
            }
        }

        /// <summary>
        /// 创建系统角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task CreateSystemRolesAsync(CancellationToken cancellationToken)
        {
            var systemRoles = SystemRole.GetAllRoles();

            foreach (var roleName in systemRoles)
            {
                var existingRole = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
                if (existingRole == null)
                {
                    var role = new Role();
                    role.SetName(roleName);
                    role.Description = $"系统角色: {roleName}";
                    role.IsSystemRole = true;

                    await _roleRepository.CreateAsync(role, cancellationToken);
                    _logger.LogInformation("创建系统角色: {RoleName}", roleName);
                }
            }
        }

        /// <summary>
        /// 分配默认权限给角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task AssignDefaultPermissionsToRolesAsync(CancellationToken cancellationToken)
        {
            var systemRoles = SystemRole.GetAllRoles();

            foreach (var roleName in systemRoles)
            {
                var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
                if (role == null) continue;

                var defaultPermissions = SystemRole.GetDefaultPermissions(roleName);
                var permissionIds = new List<Guid>();

                foreach (var permissionName in defaultPermissions)
                {
                    var permission = await _permissionRepository.GetByNameAsync(permissionName, cancellationToken);
                    if (permission != null)
                    {
                        permissionIds.Add(permission.Id);
                    }
                }

                if (permissionIds.Any())
                {
                    await AssignPermissionsToRoleAsync(role.Id, permissionIds, cancellationToken);
                    _logger.LogInformation("为角色 {RoleName} 分配了 {Count} 个权限", roleName, permissionIds.Count);
                }
            }
        }

        /// <summary>
        /// 获取默认权限列表（已过时，保留用于兼容性）
        /// </summary>
        /// <returns>默认权限列表</returns>
        [Obsolete("使用SystemPermission.GetAllPermissions()替代")]
        private static List<Permission> GetDefaultPermissions()
        {
            return new List<Permission>
            {
                // 文章管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "Posts", Action = "Create", Name = "Posts.Create", Description = "创建文章", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Posts", Action = "Read", Name = "Posts.Read", Description = "查看文章", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Posts", Action = "Update", Name = "Posts.Update", Description = "更新文章", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Posts", Action = "Delete", Name = "Posts.Delete", Description = "删除文章", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Posts", Action = "Publish", Name = "Posts.Publish", Description = "发布文章", IsSystemPermission = true },

                // 用户管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "Users", Action = "Create", Name = "Users.Create", Description = "创建用户", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Users", Action = "Read", Name = "Users.Read", Description = "查看用户", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Users", Action = "Update", Name = "Users.Update", Description = "更新用户", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Users", Action = "Delete", Name = "Users.Delete", Description = "删除用户", IsSystemPermission = true },

                // 角色管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "Roles", Action = "Create", Name = "Roles.Create", Description = "创建角色", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Roles", Action = "Read", Name = "Roles.Read", Description = "查看角色", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Roles", Action = "Update", Name = "Roles.Update", Description = "更新角色", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Roles", Action = "Delete", Name = "Roles.Delete", Description = "删除角色", IsSystemPermission = true },

                // 评论管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "Comments", Action = "Create", Name = "Comments.Create", Description = "创建评论", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Comments", Action = "Read", Name = "Comments.Read", Description = "查看评论", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Comments", Action = "Update", Name = "Comments.Update", Description = "更新评论", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Comments", Action = "Delete", Name = "Comments.Delete", Description = "删除评论", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Comments", Action = "Moderate", Name = "Comments.Moderate", Description = "审核评论", IsSystemPermission = true },

                // 分类管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "Categories", Action = "Create", Name = "Categories.Create", Description = "创建分类", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Categories", Action = "Read", Name = "Categories.Read", Description = "查看分类", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Categories", Action = "Update", Name = "Categories.Update", Description = "更新分类", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Categories", Action = "Delete", Name = "Categories.Delete", Description = "删除分类", IsSystemPermission = true },

                // 系统管理权限
                new Permission { Id = Guid.NewGuid(), Resource = "System", Action = "Admin", Name = "System.Admin", Description = "系统管理", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "System", Action = "Monitor", Name = "System.Monitor", Description = "系统监控", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "System", Action = "Config", Name = "System.Config", Description = "系统配置", IsSystemPermission = true },

                // 仪表盘权限
                new Permission { Id = Guid.NewGuid(), Resource = "Dashboard", Action = "View", Name = "Dashboard.View", Description = "查看仪表盘", IsSystemPermission = true },
                new Permission { Id = Guid.NewGuid(), Resource = "Analytics", Action = "View", Name = "Analytics.View", Description = "查看分析数据", IsSystemPermission = true }
            };
        }

        /// <summary>
        /// 获取默认角色列表
        /// </summary>
        /// <returns>默认角色列表</returns>
        private static List<Role> GetDefaultRoles()
        {
            return new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "SuperAdmin",
                    NormalizedName = "SUPERADMIN",
                    Description = "超级管理员，拥有所有权限",
                    IsSystemRole = true,
                    IsActive = true
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "管理员，拥有大部分管理权限",
                    IsSystemRole = true,
                    IsActive = true
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Editor",
                    NormalizedName = "EDITOR",
                    Description = "编辑者，可以管理内容",
                    IsSystemRole = true,
                    IsActive = true
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Author",
                    NormalizedName = "AUTHOR",
                    Description = "作者，可以创建和编辑自己的文章",
                    IsSystemRole = true,
                    IsActive = true
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "普通用户",
                    IsSystemRole = true,
                    IsActive = true
                }
            };
        }
    }
}