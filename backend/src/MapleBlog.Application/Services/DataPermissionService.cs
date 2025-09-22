using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Models;
using DataPermissionScopeClass = MapleBlog.Domain.Interfaces.DataPermissionScope;
using DataPermissionScopeEnum = MapleBlog.Domain.Enums.DataPermissionScope;
using MapleBlog.Application.Services.Permissions;
using System.Security.Claims;
using System.Diagnostics;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Application.Services;

/// <summary>
/// 数据权限控制服务
/// 实现基于角色和用户的数据过滤和权限控制
/// 支持缓存、审计日志、临时权限、权限委派等企业级功能
/// </summary>
public class DataPermissionService : IDataPermissionService
{
    private readonly ILogger<DataPermissionService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IDataPermissionRuleRepository _dataPermissionRuleRepository;
    private readonly ITemporaryPermissionRepository _temporaryPermissionRepository;
    private readonly IMemoryCache _cache;
    // private readonly PermissionRuleEngine _ruleEngine;
    private readonly PermissionStatistics _statistics;

    private const string CACHE_PREFIX = "DataPermission";
    private const int CACHE_DURATION_MINUTES = 15;

    public DataPermissionService(
        ILogger<DataPermissionService> logger,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IDataPermissionRuleRepository dataPermissionRuleRepository,
        ITemporaryPermissionRepository temporaryPermissionRepository,
        IMemoryCache cache)
    {
        _logger = logger;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _dataPermissionRuleRepository = dataPermissionRuleRepository;
        _temporaryPermissionRepository = temporaryPermissionRepository;
        _cache = cache;
        // _ruleEngine = ruleEngine;
        _statistics = new PermissionStatistics();
    }

    /// <summary>
    /// 应用用户数据权限过滤
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="query">查询</param>
    /// <param name="currentUserId">当前用户ID</param>
    /// <param name="userRole">用户角色</param>
    /// <returns>过滤后的查询</returns>
    public IQueryable<T> ApplyUserDataFilter<T>(IQueryable<T> query, Guid currentUserId, UserRoleEnum userRole) where T : BaseEntity
    {
        try
        {
            // 管理员可以访问所有数据
            if (userRole == UserRoleEnum.Admin)
            {
                _logger.LogDebug("Admin user {UserId} has access to all data of type {EntityType}",
                    currentUserId, typeof(T).Name);
                return query;
            }

            // 根据实体类型应用不同的过滤规则
            if (typeof(T) == typeof(Post))
            {
                return ApplyPostDataFilter(query as IQueryable<Post>, currentUserId, userRole) as IQueryable<T>;
            }

            if (typeof(T) == typeof(Comment))
            {
                return ApplyCommentDataFilter(query as IQueryable<Comment>, currentUserId, userRole) as IQueryable<T>;
            }

            if (typeof(T) == typeof(User))
            {
                return ApplyUserDataFilter(query as IQueryable<User>, currentUserId, userRole) as IQueryable<T>;
            }

            if (typeof(T) == typeof(Category))
            {
                return ApplyCategoryDataFilter(query as IQueryable<Category>, currentUserId, userRole) as IQueryable<T>;
            }

            // 默认情况：普通用户只能访问自己创建的数据
            if (HasCreatedByProperty<T>())
            {
                return query.Where(CreateOwnershipFilter<T>(currentUserId));
            }

            _logger.LogWarning("No specific data filter found for entity type {EntityType}, returning empty result",
                typeof(T).Name);
            return query.Where(x => false); // 返回空结果作为安全默认值
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying data filter for entity type {EntityType} and user {UserId}",
                typeof(T).Name, currentUserId);
            throw;
        }
    }

    /// <summary>
    /// 应用文章数据权限过滤
    /// </summary>
    private IQueryable<Post> ApplyPostDataFilter(IQueryable<Post> query, Guid currentUserId, UserRoleEnum userRole)
    {
        switch (userRole)
        {
            case UserRoleEnum.Admin:
                return query; // 管理员可以访问所有文章

            case UserRoleEnum.Author:
                // 作者可以访问所有已发布的文章和自己的文章
                return query.Where(p => p.IsPublished || p.CreatedBy == currentUserId);

            case UserRoleEnum.User:
            default:
                // 普通用户只能访问已发布的文章
                return query.Where(p => p.IsPublished);
        }
    }

    /// <summary>
    /// 应用评论数据权限过滤
    /// </summary>
    private IQueryable<Comment> ApplyCommentDataFilter(IQueryable<Comment> query, Guid currentUserId, UserRoleEnum userRole)
    {
        switch (userRole)
        {
            case UserRoleEnum.Admin:
                return query; // 管理员可以访问所有评论

            case UserRoleEnum.Author:
                // 作者可以访问自己文章的评论和自己的评论
                return query.Where(c => c.Post.CreatedBy == currentUserId || c.CreatedBy == currentUserId);

            case UserRoleEnum.User:
            default:
                // 普通用户可以访问已审核的评论和自己的评论
                return query.Where(c => c.IsApproved || c.CreatedBy == currentUserId);
        }
    }

    /// <summary>
    /// 应用用户数据权限过滤
    /// </summary>
    private IQueryable<User> ApplyUserDataFilter(IQueryable<User> query, Guid currentUserId, UserRoleEnum userRole)
    {
        switch (userRole)
        {
            case UserRoleEnum.Admin:
                return query; // 管理员可以访问所有用户

            case UserRoleEnum.Author:
                // 作者可以访问公开用户信息和自己的信息
                return query.Where(u => u.IsActive || u.Id == currentUserId);

            case UserRoleEnum.User:
            default:
                // 普通用户只能访问自己的信息
                return query.Where(u => u.Id == currentUserId);
        }
    }

    /// <summary>
    /// 应用分类数据权限过滤
    /// </summary>
    private IQueryable<Category> ApplyCategoryDataFilter(IQueryable<Category> query, Guid currentUserId, UserRoleEnum userRole)
    {
        switch (userRole)
        {
            case UserRoleEnum.Admin:
                return query; // 管理员可以访问所有分类

            case UserRoleEnum.Author:
            case UserRoleEnum.User:
            default:
                // 作者和普通用户只能访问激活的分类
                return query.Where(c => c.IsActive);
        }
    }

    /// <summary>
    /// 检查用户是否有权限访问特定资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resource">资源名称</param>
    /// <param name="action">操作名称</param>
    /// <param name="resourceId">资源ID（可选）</param>
    /// <returns>是否有权限</returns>
    public async Task<bool> HasPermissionAsync(Guid userId, string resource, string action, Guid? resourceId = null)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Permission check failed: User {UserId} not found or inactive", userId);
                return false;
            }

            // 管理员拥有所有权限
            if (user.Role == UserRoleEnum.Admin)
            {
                _logger.LogDebug("Admin user {UserId} granted permission for {Resource}.{Action}",
                    userId, resource, action);
                return true;
            }

            // 检查是否有资源的所有权
            if (resourceId.HasValue && await IsResourceOwnerAsync(userId, resource, resourceId.Value))
            {
                _logger.LogDebug("Resource owner {UserId} granted permission for {Resource}.{Action}",
                    userId, resource, action);
                return true;
            }

            // 检查角色权限
            var hasPermission = await CheckRolePermissionAsync(user.Role, resource, action);

            _logger.LogDebug("Permission check for user {UserId}, resource {Resource}, action {Action}: {Result}",
                userId, resource, action, hasPermission);

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}, resource {Resource}, action {Action}",
                userId, resource, action);
            return false; // 安全默认值
        }
    }

    /// <summary>
    /// 检查角色是否有指定权限
    /// </summary>
    private async Task<bool> CheckRolePermissionAsync(UserRoleEnum userRole, string resource, string action)
    {
        // 基于角色的基本权限检查
        switch (userRole)
        {
            case UserRoleEnum.Admin:
                return true; // 管理员拥有所有权限

            case UserRoleEnum.Author:
                return await CheckAuthorPermissionAsync(resource, action);

            case UserRoleEnum.User:
            default:
                return await CheckUserPermissionAsync(resource, action);
        }
    }

    /// <summary>
    /// 检查作者权限
    /// </summary>
    private async Task<bool> CheckAuthorPermissionAsync(string resource, string action)
    {
        // 作者的权限规则
        var allowedPermissions = new Dictionary<string, string[]>
        {
            { "Posts", new[] { "Create", "Read", "Update", "Delete" } },
            { "Comments", new[] { "Read", "Update", "Delete", "Moderate" } },
            { "Categories", new[] { "Read" } },
            { "Tags", new[] { "Read", "Create" } },
            { "Users", new[] { "Read" } } // 限制为只读其他用户
        };

        if (allowedPermissions.TryGetValue(resource, out var actions))
        {
            return actions.Contains(action, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// 检查普通用户权限
    /// </summary>
    private async Task<bool> CheckUserPermissionAsync(string resource, string action)
    {
        // 普通用户的权限规则
        var allowedPermissions = new Dictionary<string, string[]>
        {
            { "Posts", new[] { "Read" } },
            { "Comments", new[] { "Create", "Read", "Update", "Delete" } }, // 只能操作自己的评论
            { "Categories", new[] { "Read" } },
            { "Tags", new[] { "Read" } },
            { "Users", new[] { "Read" } } // 只能查看自己的信息
        };

        if (allowedPermissions.TryGetValue(resource, out var actions))
        {
            return actions.Contains(action, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// 检查用户是否为资源所有者
    /// </summary>
    private async Task<bool> IsResourceOwnerAsync(Guid userId, string resource, Guid resourceId)
    {
        try
        {
            switch (resource.ToLowerInvariant())
            {
                case "posts":
                    var post = await _userRepository.GetByIdAsync(resourceId);
                    return post?.CreatedBy == userId;

                case "comments":
                    // 需要实现评论仓储的获取方法
                    // var comment = await _commentRepository.GetByIdAsync(resourceId);
                    // return comment?.CreatedBy == userId;
                    return false; // 临时返回false，等待评论仓储实现

                case "users":
                    return resourceId == userId; // 用户只能操作自己的资料

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource ownership for user {UserId}, resource {Resource}, resourceId {ResourceId}",
                userId, resource, resourceId);
            return false;
        }
    }

    /// <summary>
    /// 应用敏感数据脱敏
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <param name="userRole">用户角色</param>
    /// <returns>脱敏后的实体</returns>
    public T ApplyDataMasking<T>(T entity, UserRoleEnum userRole) where T : class
    {
        if (entity == null) return entity;

        try
        {
            // 管理员不需要脱敏
            if (userRole == UserRoleEnum.Admin)
            {
                return entity;
            }

            // 对User实体进行脱敏
            if (entity is User user)
            {
                return MaskUserData(user, userRole) as T;
            }

            // 对于其他实体类型，可以在这里添加脱敏逻辑
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying data masking for entity type {EntityType}", typeof(T).Name);
            return entity;
        }
    }

    /// <summary>
    /// 对用户数据进行脱敏
    /// </summary>
    private User MaskUserData(User user, UserRoleEnum viewerRole)
    {
        // 创建副本以避免修改原始数据
        var maskedUser = new User
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            // 只显示基本公开信息
        };

        // 根据查看者角色决定显示哪些信息
        switch (viewerRole)
        {
            case UserRoleEnum.Author:
                // 作者可以看到更多信息
                maskedUser.Email = user.Email;
                maskedUser.Role = user.Role;
                maskedUser.LastLoginAt = user.LastLoginAt;
                break;

            case UserRoleEnum.User:
            default:
                // 普通用户只能看到最基本的信息
                // 邮箱进行部分脱敏
                if (!string.IsNullOrEmpty(user.Email?.Value))
                {
                    maskedUser.Email = MaskEmail(user.Email.Value);
                }
                break;
        }

        return maskedUser;
    }

    /// <summary>
    /// 邮箱脱敏
    /// </summary>
    private Domain.ValueObjects.Email MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
        {
            return Domain.ValueObjects.Email.Create("***@***.***");
        }

        var parts = email.Split('@');
        var localPart = parts[0];
        var domainPart = parts[1];

        // 保留前2位和后1位字符
        if (localPart.Length <= 3)
        {
            localPart = new string('*', localPart.Length);
        }
        else
        {
            localPart = localPart.Substring(0, 2) + new string('*', localPart.Length - 3) + localPart.Substring(localPart.Length - 1);
        }

        return Domain.ValueObjects.Email.Create($"{localPart}@{domainPart}");
    }

    /// <summary>
    /// 创建所有权过滤表达式
    /// </summary>
    private Expression<Func<T, bool>> CreateOwnershipFilter<T>(Guid userId) where T : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, nameof(BaseEntity.CreatedBy));
        var userIdConstant = Expression.Constant(userId);
        var equality = Expression.Equal(property, userIdConstant);
        return Expression.Lambda<Func<T, bool>>(equality, parameter);
    }

    /// <summary>
    /// 检查实体是否有CreatedBy属性
    /// </summary>
    private bool HasCreatedByProperty<T>()
    {
        return typeof(T).GetProperty(nameof(BaseEntity.CreatedBy)) != null;
    }

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceType">资源类型</param>
    /// <returns>数据权限范围信息</returns>
    public async Task<DataPermissionScopeClass> GetUserDataScopeAsync(Guid userId, string resourceType = null)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var cacheKey = $"{CACHE_PREFIX}:Scope:{userId}:{resourceType ?? "all"}";
            if (_cache.TryGetValue(cacheKey, out DataPermissionScopeClass cachedScope))
            {
                _statistics.CacheHits++;
                return cachedScope;
            }
            _statistics.CacheMisses++;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return new DataPermissionScopeClass { HasAccess = false };
            }

            var scope = new DataPermissionScopeClass
            {
                HasAccess = user.IsActive,
                UserId = userId,
                UserRole = user.Role,
                CanAccessAllData = user.Role == UserRoleEnum.Admin,
                CanAccessOwnData = true
            };

            // 应用自定义权限规则
            await ApplyCustomPermissionRules(scope, userId, resourceType);

            // 根据角色设置基础权限范围
            switch (user.Role)
            {
                case UserRoleEnum.Admin:
                    scope.CanAccessAllUsers = true;
                    scope.CanAccessAllPosts = true;
                    scope.CanAccessAllComments = true;
                    scope.CanManageSystem = true;
                    break;

                case UserRoleEnum.Author:
                    scope.CanAccessPublicUsers = true;
                    scope.CanAccessAllPosts = true;
                    scope.CanAccessOwnPosts = true;
                    scope.CanAccessRelatedComments = true;
                    break;

                case UserRoleEnum.User:
                default:
                    scope.CanAccessPublicUsers = false;
                    scope.CanAccessPublishedPosts = true;
                    scope.CanAccessOwnComments = true;
                    break;
            }

            // 缓存结果
            _cache.Set(cacheKey, scope, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            return scope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting data scope for user {UserId}", userId);
            return new DataPermissionScopeClass { HasAccess = false };
        }
        finally
        {
            stopwatch.Stop();
            UpdateStatistics(stopwatch.ElapsedMilliseconds);
        }
    }

    #region 核心权限检查方法

    /// <summary>
    /// 检查用户是否有权限对实体执行指定操作
    /// </summary>
    public async Task<bool> HasDataAccessAsync<T>(Guid userId, T entity, DataOperation operation) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _statistics.TotalPermissionChecks++;

            if (entity == null)
            {
                _statistics.FailedChecks++;
                return false;
            }

            var resourceType = typeof(T).Name;
            var resourceId = GetEntityId(entity);

            var result = await CanAccessResourceAsync(userId, resourceType, resourceId, operation);

            if (result)
                _statistics.SuccessfulChecks++;
            else
                _statistics.FailedChecks++;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking data access for user {UserId}, entity {EntityType}, operation {Operation}",
                userId, typeof(T).Name, operation);
            _statistics.FailedChecks++;
            return false;
        }
        finally
        {
            stopwatch.Stop();
            UpdateStatistics(stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 检查用户是否可以访问指定资源
    /// </summary>
    public async Task<bool> CanAccessResourceAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var cacheKey = $"{CACHE_PREFIX}:Access:{userId}:{resourceType}:{resourceId}:{operation}";
            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
            {
                _statistics.CacheHits++;
                return cachedResult;
            }
            _statistics.CacheMisses++;

            // 检查用户是否存在且激活
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            // 管理员拥有所有权限
            if (user.Role == UserRoleEnum.Admin)
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return true;
            }

            // 检查临时权限
            var hasTemporaryPermission = await CheckTemporaryPermissionAsync(userId, resourceType, resourceId, operation);
            if (hasTemporaryPermission)
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(5)); // 临时权限缓存时间较短
                return true;
            }

            // 检查数据权限规则
            var hasRulePermission = await CheckDataPermissionRulesAsync(userId, resourceType, resourceId, operation);
            if (hasRulePermission)
            {
                _cache.Set(cacheKey, true, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return true;
            }

            // 检查基础角色权限
            var hasBasicPermission = await CheckBasicRolePermissionAsync(user.Role, resourceType, operation, resourceId);

            _cache.Set(cacheKey, hasBasicPermission, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return hasBasicPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking resource access for user {UserId}, resource {ResourceType}:{ResourceId}, operation {Operation}",
                userId, resourceType, resourceId, operation);
            return false;
        }
        finally
        {
            stopwatch.Stop();
            UpdateStatistics(stopwatch.ElapsedMilliseconds);
        }
    }

    #endregion

    #region 批量权限检查

    /// <summary>
    /// 批量检查数据访问权限
    /// </summary>
    public async Task<Dictionary<Guid, bool>> CheckBatchDataAccessAsync<T>(Guid userId, IEnumerable<T> entities, DataOperation operation) where T : BaseEntity
    {
        var results = new Dictionary<Guid, bool>();
        var entityList = entities.ToList();

        if (!entityList.Any())
            return results;

        try
        {
            var resourceType = typeof(T).Name;

            // 首先检查用户权限范围
            var scope = await GetUserDataScopeAsync(userId, resourceType);
            if (!scope.HasAccess)
            {
                // 如果用户没有访问权限，所有实体都返回false
                foreach (var entity in entityList)
                {
                    results[entity.Id] = false;
                }
                return results;
            }

            // 如果是管理员，所有实体都有权限
            if (scope.CanAccessAllData)
            {
                foreach (var entity in entityList)
                {
                    results[entity.Id] = true;
                }
                return results;
            }

            // 批量检查权限规则
            var resourceIds = entityList.Select(e => e.Id).ToList();
            var rules = await _dataPermissionRuleRepository.GetUserResourcePermissionsAsync(userId, resourceType, operation);

            foreach (var entity in entityList)
            {
                var hasAccess = await EvaluateEntityAccess(userId, entity, operation, rules, scope);
                results[entity.Id] = hasAccess;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch permission check for user {UserId}, entity type {EntityType}",
                userId, typeof(T).Name);

            // 安全默认：所有实体都返回false
            foreach (var entity in entityList)
            {
                results[entity.Id] = false;
            }
            return results;
        }
    }

    /// <summary>
    /// 过滤用户有权限访问的实体
    /// </summary>
    public async Task<IEnumerable<T>> FilterAccessibleEntitiesAsync<T>(Guid userId, IEnumerable<T> entities, DataOperation operation) where T : BaseEntity
    {
        var accessResults = await CheckBatchDataAccessAsync(userId, entities, operation);
        return entities.Where(e => accessResults.TryGetValue(e.Id, out var hasAccess) && hasAccess);
    }

    #endregion

    #region 查询过滤

    /// <summary>
    /// 根据数据权限过滤查询
    /// </summary>
    public async Task<IQueryable<T>> FilterByDataPermissionsAsync<T>(Guid userId, IQueryable<T> query, DataOperation operation) where T : BaseEntity
    {
        try
        {
            var scope = await GetUserDataScopeAsync(userId, typeof(T).Name);
            if (!scope.HasAccess)
            {
                return query.Where(x => false); // 返回空结果
            }

            if (scope.CanAccessAllData)
            {
                return query; // 管理员可以访问所有数据
            }

            // 应用数据权限过滤
            return ApplyUserDataFilter(query, userId, scope.UserRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering query by data permissions for user {UserId}, entity type {EntityType}",
                userId, typeof(T).Name);
            return query.Where(x => false); // 安全默认
        }
    }

    #endregion

    #region 权限范围和规则

    /// <summary>
    /// 获取用户的数据权限规则
    /// </summary>
    public async Task<IEnumerable<DataPermissionRule>> GetUserPermissionRulesAsync(Guid userId, string resourceType = null)
    {
        try
        {
            var cacheKey = $"{CACHE_PREFIX}:Rules:{userId}:{resourceType ?? "all"}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<DataPermissionRule> cachedRules))
            {
                _statistics.CacheHits++;
                return cachedRules;
            }
            _statistics.CacheMisses++;

            var rules = await _dataPermissionRuleRepository.GetByUserIdAsync(userId, resourceType);
            var effectiveRules = rules.Where(r => r.IsEffective()).ToList();

            _cache.Set(cacheKey, effectiveRules, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            return effectiveRules;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission rules for user {UserId}, resource type {ResourceType}",
                userId, resourceType);
            return Enumerable.Empty<DataPermissionRule>();
        }
    }

    #endregion

    #region 临时权限和委派

    /// <summary>
    /// 授予临时权限
    /// </summary>
    public async Task<bool> GrantTemporaryPermissionAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation, DateTime expiresAt, Guid grantedBy)
    {
        try
        {
            var temporaryPermission = new TemporaryPermission
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Operation = operation,
                ExpiresAt = expiresAt,
                GrantedBy = grantedBy,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = grantedBy
            };

            await _temporaryPermissionRepository.AddAsync(temporaryPermission);

            // 清除相关缓存
            await ClearUserPermissionCacheAsync(userId);

            _logger.LogInformation("Temporary permission granted to user {UserId} for {ResourceType}:{ResourceId} operation {Operation} by {GrantedBy}",
                userId, resourceType, resourceId, operation, grantedBy);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting temporary permission to user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 撤销临时权限
    /// </summary>
    public async Task<bool> RevokeTemporaryPermissionAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation)
    {
        try
        {
            var permissions = await _temporaryPermissionRepository.GetByUserAndResourceAsync(userId, resourceType, resourceId, operation);

            foreach (var permission in permissions.Where(p => p.IsActive))
            {
                permission.IsActive = false;
                permission.UpdatedAt = DateTime.UtcNow;
                _temporaryPermissionRepository.Update(permission);
            }

            if (permissions.Any(p => p.IsActive))
            {
                await _temporaryPermissionRepository.SaveChangesAsync();
            }

            // 清除相关缓存
            await ClearUserPermissionCacheAsync(userId);

            _logger.LogInformation("Temporary permissions revoked for user {UserId} for {ResourceType}:{ResourceId} operation {Operation}",
                userId, resourceType, resourceId, operation);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking temporary permission for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 委派权限给其他用户
    /// </summary>
    public async Task<bool> DelegatePermissionAsync(Guid fromUserId, Guid toUserId, string resourceType, Guid resourceId, DataOperation operation, DateTime expiresAt)
    {
        try
        {
            // 检查委派人是否有权限
            var canDelegate = await CanAccessResourceAsync(fromUserId, resourceType, resourceId, operation);
            if (!canDelegate)
            {
                _logger.LogWarning("User {FromUserId} attempted to delegate permission they don't have to user {ToUserId}",
                    fromUserId, toUserId);
                return false;
            }

            // 创建委派权限规则
            var delegationRule = new DataPermissionRule
            {
                Id = Guid.NewGuid(),
                UserId = toUserId,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Operation = operation,
                IsAllowed = true,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveTo = expiresAt,
                GrantedBy = fromUserId,
                Source = PermissionSource.Delegated,
                Remarks = $"Delegated from user {fromUserId}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = fromUserId
            };

            await _dataPermissionRuleRepository.AddAsync(delegationRule);

            // 清除相关缓存
            await ClearUserPermissionCacheAsync(toUserId);

            _logger.LogInformation("Permission delegated from user {FromUserId} to user {ToUserId} for {ResourceType}:{ResourceId} operation {Operation}",
                fromUserId, toUserId, resourceType, resourceId, operation);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delegating permission from user {FromUserId} to user {ToUserId}",
                fromUserId, toUserId);
            return false;
        }
    }

    #endregion

    #region 数据脱敏和安全

    /// <summary>
    /// 批量应用数据脱敏
    /// </summary>
    public async Task<IEnumerable<T>> ApplyBatchDataMaskingAsync<T>(IEnumerable<T> entities, Guid userId) where T : class
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return entities;
            }

            return entities.Select(entity => ApplyDataMasking(entity, user.Role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying batch data masking for user {UserId}", userId);
            return entities;
        }
    }

    #endregion

    #region 权限缓存和性能

    /// <summary>
    /// 清除用户权限缓存
    /// </summary>
    public async Task<bool> ClearUserPermissionCacheAsync(Guid userId)
    {
        try
        {
            var keys = new List<string>();

            // 生成所有可能的缓存键
            var resourceTypes = new[] { "Posts", "Comments", "Users", "Categories", "Tags", "all" };
            var operations = Enum.GetValues<DataOperation>();

            foreach (var resourceType in resourceTypes)
            {
                keys.Add($"{CACHE_PREFIX}:Scope:{userId}:{resourceType}");
                keys.Add($"{CACHE_PREFIX}:Rules:{userId}:{resourceType}");

                foreach (var operation in operations)
                {
                    keys.Add($"{CACHE_PREFIX}:Access:{userId}:{resourceType}:*:{operation}");
                }
            }

            foreach (var key in keys)
            {
                _cache.Remove(key);
            }

            _logger.LogDebug("Cleared permission cache for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing permission cache for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 预热用户权限缓存
    /// </summary>
    public async Task<bool> WarmupUserPermissionCacheAsync(Guid userId)
    {
        try
        {
            var resourceTypes = new[] { "Posts", "Comments", "Users", "Categories", "Tags" };
            var operations = new[] { DataOperation.Read, DataOperation.Create, DataOperation.Update, DataOperation.Delete };

            // 预加载权限范围
            foreach (var resourceType in resourceTypes)
            {
                await GetUserDataScopeAsync(userId, resourceType);
                await GetUserPermissionRulesAsync(userId, resourceType);
            }

            _logger.LogDebug("Warmed up permission cache for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error warming up permission cache for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取权限检查统计信息
    /// </summary>
    public async Task<PermissionStatistics> GetPermissionStatisticsAsync()
    {
        try
        {
            var ruleStats = await _dataPermissionRuleRepository.GetStatisticsAsync();

            _statistics.ActiveRulesCount = ruleStats.ActiveRules;
            _statistics.TemporaryPermissionsCount = ruleStats.TemporaryRules;

            return _statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission statistics");
            return _statistics;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 应用自定义权限规则到权限范围
    /// </summary>
    private async Task ApplyCustomPermissionRules(DataPermissionScopeClass scope, Guid userId, string resourceType)
    {
        try
        {
            var rules = await GetUserPermissionRulesAsync(userId, resourceType);

            foreach (var rule in rules.Where(r => r.IsAllowed))
            {
                // 根据规则类型应用特定权限
                ApplyRuleToScope(scope, rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying custom permission rules for user {UserId}", userId);
        }
    }

    /// <summary>
    /// 应用规则到权限范围
    /// </summary>
    private void ApplyRuleToScope(DataPermissionScopeClass scope, DataPermissionRule rule)
    {
        switch (rule.ResourceType.ToLowerInvariant())
        {
            case "users":
                if (rule.Operation == DataOperation.Read)
                {
                    scope.CanAccessAllUsers = true;
                }
                break;
            case "posts":
                if (rule.Operation == DataOperation.Read)
                {
                    scope.CanAccessAllPosts = true;
                }
                break;
            case "comments":
                if (rule.Operation == DataOperation.Read)
                {
                    scope.CanAccessAllComments = true;
                }
                break;
        }
    }

    /// <summary>
    /// 检查临时权限
    /// </summary>
    private async Task<bool> CheckTemporaryPermissionAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation)
    {
        try
        {
            var permissions = await _temporaryPermissionRepository.GetActiveByUserAndResourceAsync(userId, resourceType, resourceId, operation);
            return permissions.Any(p => p.IsActive && p.ExpiresAt > DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking temporary permission for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查数据权限规则
    /// </summary>
    private async Task<bool> CheckDataPermissionRulesAsync(Guid userId, string resourceType, Guid resourceId, DataOperation operation)
    {
        try
        {
            var rules = await _dataPermissionRuleRepository.GetUserResourcePermissionsAsync(userId, resourceType, operation, resourceId);

            // 按优先级排序，优先级高的规则优先
            var sortedRules = rules.Where(r => r.IsEffective()).OrderByDescending(r => r.Priority);

            foreach (var rule in sortedRules)
            {
                if (rule.Matches(resourceType, operation, resourceId))
                {
                    // 如果有条件表达式，需要评估
                    if (!string.IsNullOrEmpty(rule.Conditions))
                    {
                        var conditionResult = await EvaluateConditionsAsync(rule.Conditions, userId, resourceId);
                        return rule.IsAllowed && conditionResult;
                    }

                    return rule.IsAllowed;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking data permission rules for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查基础角色权限
    /// </summary>
    private async Task<bool> CheckBasicRolePermissionAsync(UserRoleEnum userRole, string resourceType, DataOperation operation, Guid resourceId)
    {
        // 使用现有的角色权限检查逻辑
        return await CheckRolePermissionAsync(userRole, resourceType, operation.ToString());
    }

    /// <summary>
    /// 评估实体访问权限
    /// </summary>
    private async Task<bool> EvaluateEntityAccess<T>(Guid userId, T entity, DataOperation operation, IEnumerable<DataPermissionRule> rules, DataPermissionScopeClass scope) where T : BaseEntity
    {
        try
        {
            // 检查是否是实体所有者
            if (entity.CreatedBy == userId)
            {
                return scope.CanAccessOwnData;
            }

            // 检查特定规则
            var applicableRules = rules.Where(r => r.Matches(typeof(T).Name, operation, entity.Id));

            foreach (var rule in applicableRules.OrderByDescending(r => r.Priority))
            {
                if (!string.IsNullOrEmpty(rule.Conditions))
                {
                    var conditionResult = await EvaluateConditionsAsync(rule.Conditions, userId, entity.Id);
                    if (conditionResult)
                    {
                        return rule.IsAllowed;
                    }
                }
                else
                {
                    return rule.IsAllowed;
                }
            }

            // 使用默认权限
            return await CheckBasicRolePermissionAsync(scope.UserRole, typeof(T).Name, operation, entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating entity access for user {UserId}, entity {EntityId}", userId, entity.Id);
            return false;
        }
    }

    /// <summary>
    /// 获取实体ID
    /// </summary>
    private Guid GetEntityId<T>(T entity) where T : class
    {
        if (entity is BaseEntity baseEntity)
        {
            return baseEntity.Id;
        }

        // 尝试通过反射获取Id属性
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            return (Guid)idProperty.GetValue(entity);
        }

        return Guid.Empty;
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics(double elapsedMilliseconds)
    {
        if (_statistics.TotalPermissionChecks == 1)
        {
            _statistics.MinCheckTime = elapsedMilliseconds;
            _statistics.MaxCheckTime = elapsedMilliseconds;
            _statistics.AverageCheckTime = elapsedMilliseconds;
        }
        else
        {
            _statistics.MinCheckTime = Math.Min(_statistics.MinCheckTime, elapsedMilliseconds);
            _statistics.MaxCheckTime = Math.Max(_statistics.MaxCheckTime, elapsedMilliseconds);
            _statistics.AverageCheckTime = (_statistics.AverageCheckTime * (_statistics.TotalPermissionChecks - 1) + elapsedMilliseconds) / _statistics.TotalPermissionChecks;
        }
    }

    #endregion

    #region IDataPermissionService 扩展方法实现

    /// <summary>
    /// 评估条件表达式（兼容权限规则引擎）
    /// </summary>
    /// <param name="conditions">条件JSON字符串</param>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceId">资源ID</param>
    /// <returns>评估结果</returns>
    public async Task<bool> EvaluateConditionsAsync(string conditions, Guid userId, Guid resourceId)
    {
        try
        {
            if (string.IsNullOrEmpty(conditions))
            {
                return true;
            }

            var conditionDict = JsonSerializer.Deserialize<Dictionary<string, object>>(conditions);
            if (conditionDict == null)
            {
                return true;
            }

            // 替换变量
            var processedConditions = new Dictionary<string, object>();
            foreach (var kvp in conditionDict)
            {
                var value = kvp.Value?.ToString();
                if (value == "{UserId}")
                {
                    processedConditions[kvp.Key] = userId;
                }
                else if (value == "{ResourceId}")
                {
                    processedConditions[kvp.Key] = resourceId;
                }
                else
                {
                    processedConditions[kvp.Key] = kvp.Value;
                }
            }

            // 简化的条件评估逻辑
            // 在实际项目中，这里应该使用更复杂的表达式解析器
            return await EvaluateSimpleConditions(processedConditions, userId, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating conditions: {Conditions}", conditions);
            return false;
        }
    }

    /// <summary>
    /// 简化的条件评估
    /// </summary>
    private async Task<bool> EvaluateSimpleConditions(Dictionary<string, object> conditions, Guid userId, Guid resourceId)
    {
        try
        {
            // 检查用户相关条件
            if (conditions.ContainsKey("CreatedBy"))
            {
                var createdBy = conditions["CreatedBy"];
                if (createdBy is Guid createdById && createdById != userId)
                {
                    return false;
                }
            }

            // 检查时间相关条件
            if (conditions.ContainsKey("StartDate") || conditions.ContainsKey("EndDate"))
            {
                var now = DateTime.UtcNow;

                if (conditions.ContainsKey("StartDate") &&
                    DateTime.TryParse(conditions["StartDate"]?.ToString(), out var startDate) &&
                    now < startDate)
                {
                    return false;
                }

                if (conditions.ContainsKey("EndDate") &&
                    DateTime.TryParse(conditions["EndDate"]?.ToString(), out var endDate) &&
                    now > endDate)
                {
                    return false;
                }
            }

            // 检查状态相关条件
            if (conditions.ContainsKey("IsPublished"))
            {
                // 这里需要根据具体的资源类型来检查
                // 暂时返回true，实际应用中需要查询数据库
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in simple condition evaluation");
            return false;
        }
    }

    #endregion
}

