using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Text;
using System.Security.Cryptography;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 用户管理服务实现
    /// </summary>
    public partial class UserManagementService : IUserManagementService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly MapleBlog.Application.Interfaces.IEmailService _emailService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly MapleBlog.Domain.Interfaces.ILoginHistoryRepository _loginHistoryRepository;

        public UserManagementService(
            IMapper mapper,
            ILogger<UserManagementService> logger,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IAuditLogService auditLogService,
            MapleBlog.Application.Interfaces.IEmailService emailService,
            IAuditLogRepository auditLogRepository,
            MapleBlog.Domain.Interfaces.ILoginHistoryRepository loginHistoryRepository)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _loginHistoryRepository = loginHistoryRepository ?? throw new ArgumentNullException(nameof(loginHistoryRepository));
        }

        public async Task<UserManagementOverviewDto> GetOverviewAsync()
        {
            try
            {
                _logger.LogInformation("开始获取用户管理概览数据");

                var totalUsers = await _userRepository.CountAsync();
                var today = DateTime.UtcNow.Date;
                var weekAgo = today.AddDays(-7);

                // 获取活跃用户数（最近30天登录）
                var activeUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-30));

                // 获取今日新增用户
                var todayNewUsers = await _userRepository.CountAsync(u => u.CreatedAt >= today);

                // 获取本周新增用户
                var weekNewUsers = await _userRepository.CountAsync(u => u.CreatedAt >= weekAgo);

                // 获取在线用户数（最近5分钟活跃）
                var onlineUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddMinutes(-5));

                // 获取锁定用户数
                var lockedUsers = await _userRepository.CountAsync(u => u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc > DateTime.UtcNow);

                // 获取已删除用户数
                var deletedUsers = await _userRepository.CountAsync(u => u.IsDeleted);

                // 获取状态分布
                var statusDistribution = await GetUserStatusDistributionAsync();

                // 获取角色分布
                var roleDistribution = await GetUserRoleDistributionAsync();

                // 获取增长趋势（最近30天）
                var growthTrends = await GetUserGrowthTrendsAsync(30);

                // 获取活跃度概览
                var activityOverview = await GetUserActivityOverviewAsync();

                var overview = new UserManagementOverviewDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TodayNewUsers = todayNewUsers,
                    WeekNewUsers = weekNewUsers,
                    OnlineUsers = onlineUsers,
                    LockedUsers = lockedUsers,
                    DeletedUsers = deletedUsers,
                    StatusDistribution = statusDistribution,
                    RoleDistribution = roleDistribution,
                    GrowthTrends = growthTrends,
                    ActivityOverview = activityOverview
                };

                _logger.LogInformation("成功获取用户管理概览数据，总用户数: {TotalUsers}", totalUsers);
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户管理概览时发生错误");
                throw;
            }
        }

        public async Task<PagedResultDto<UserManagementDto>> GetUsersAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, string? status = null, string? role = null, string sortBy = "CreatedAt", string sortDirection = "desc")
        {
            try
            {
                _logger.LogInformation("开始获取用户列表，页码: {PageNumber}, 页大小: {PageSize}, 搜索词: {SearchTerm}", pageNumber, pageSize, searchTerm);

                // 构建查询条件
                var query = _userRepository.GetQueryable();

                // 应用搜索条件
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.UserName.ToLower().Contains(searchLower) ||
                        u.Email.Value.ToLower().Contains(searchLower) ||
                        (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchLower)) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
                }

                // 应用状态筛选
                if (!string.IsNullOrWhiteSpace(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(u => u.IsActive && !u.IsDeleted);
                            break;
                        case "inactive":
                            query = query.Where(u => !u.IsActive && !u.IsDeleted);
                            break;
                        case "locked":
                            query = query.Where(u => u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc > DateTime.UtcNow);
                            break;
                        case "deleted":
                            query = query.Where(u => u.IsDeleted);
                            break;
                        case "verified":
                            query = query.Where(u => u.EmailConfirmed);
                            break;
                        case "unverified":
                            query = query.Where(u => !u.EmailConfirmed);
                            break;
                    }
                }

                // 应用角色筛选
                if (!string.IsNullOrWhiteSpace(role))
                {
                    if (Enum.TryParse<Domain.Enums.UserRole>(role, true, out var roleEnum))
                    {
                        query = query.Where(u => u.Role == roleEnum);
                    }
                }

                // 应用排序
                query = ApplyUserSorting(query, sortBy, sortDirection);

                // 获取总数
                var totalCount = await query.CountAsync();

                // 应用分页
                var users = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 转换为DTO
                var userDtos = new List<UserManagementDto>();
                foreach (var user in users)
                {
                    var userDto = _mapper.Map<UserManagementDto>(user);

                    // 添加额外信息
                    userDto.Roles = new[] { user.Role.ToString() };
                    userDto.IsOnline = await IsUserOnlineAsync(user.Id);
                    userDto.Stats = await GetUserStatsAsync(user.Id);
                    userDto.RiskLevel = await CalculateUserRiskLevelAsync(user.Id);

                    userDtos.Add(userDto);
                }

                var result = new PagedResultDto<UserManagementDto>
                {
                    Items = userDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("成功获取用户列表，返回 {Count} 个用户，总计 {TotalCount}", userDtos.Count, totalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表时发生错误");
                throw;
            }
        }

        public async Task<UserDetailDto?> GetUserDetailAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("开始获取用户详细信息，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return null;
                }

                var basicInfo = _mapper.Map<UserManagementDto>(user);
                basicInfo.Roles = new[] { user.Role.ToString() };
                basicInfo.IsOnline = await IsUserOnlineAsync(userId);
                basicInfo.Stats = await GetUserStatsAsync(userId);
                basicInfo.RiskLevel = await CalculateUserRiskLevelAsync(userId);

                var profile = _mapper.Map<UserProfileDto>(user);

                var securityInfo = new UserSecurityInfoDto
                {
                    LastPasswordChange = user.UpdatedAt, // 假设密码更改时会更新UpdatedAt
                    FailedLoginAttempts = user.AccessFailedCount,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    SecurityLevel = await CalculateSecurityLevelAsync(user),
                    RecentSecurityEvents = await GetRecentSecurityEventsAsync(userId)
                };

                var activityStats = new UserActivityStatsDto
                {
                    FirstLogin = user.CreatedAt,
                    LastLogin = user.LastLoginAt,
                    TotalSessions = await GetUserSessionCountAsync(userId),
                    TotalTimeSpent = await GetUserTotalTimeSpentAsync(userId),
                    AverageSessionDuration = await GetUserAverageSessionDurationAsync(userId),
                    PagesVisited = await GetUserPageVisitCountAsync(userId),
                    MostVisitedPages = await GetUserMostVisitedPagesAsync(userId),
                    ActionCounts = await GetUserActionCountsAsync(userId),
                    ActivityByHour = await GetUserActivityByHourAsync(userId)
                };

                var permissionInfo = new UserPermissionInfoDto
                {
                    Roles = await GetUserRolesDetailAsync(userId),
                    DirectPermissions = await GetUserDirectPermissionsAsync(userId),
                    InheritedPermissions = await GetUserInheritedPermissionsAsync(userId),
                    PermissionLevel = user.Role.ToString()
                };

                var recentActivities = await GetUserRecentActivitiesAsync(userId, 20);
                var preferences = await GetUserPreferencesAsync(userId);
                var devices = await GetUserDevicesAsync(userId);
                var socialAccounts = await GetUserSocialAccountsAsync(userId);

                var detail = new UserDetailDto
                {
                    BasicInfo = basicInfo,
                    Profile = profile,
                    SecurityInfo = securityInfo,
                    ActivityStats = activityStats,
                    PermissionInfo = permissionInfo,
                    RecentActivities = recentActivities,
                    Preferences = preferences,
                    Devices = devices,
                    SocialAccounts = socialAccounts
                };

                _logger.LogInformation("成功获取用户详细信息，用户ID: {UserId}", userId);
                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户详细信息时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserCreateResultDto> CreateUserAsync(CreateUserRequestDto createRequest, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始创建用户，用户名: {Username}, 邮箱: {Email}", createRequest.Username, createRequest.Email);

                var result = new UserCreateResultDto();
                var errors = new List<string>();
                var warnings = new List<string>();

                // 验证用户名是否已存在
                if (await _userRepository.UserNameExistsAsync(createRequest.Username))
                {
                    errors.Add("用户名已存在");
                }

                // 验证邮箱是否已存在
                if (await _userRepository.EmailExistsAsync(createRequest.Email))
                {
                    errors.Add("邮箱地址已存在");
                }

                // 验证角色是否存在
                var roles = new List<Role>();
                foreach (var roleId in createRequest.RoleIds)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId);
                    if (role == null)
                    {
                        errors.Add($"角色ID {roleId} 不存在");
                    }
                    else
                    {
                        roles.Add(role);
                    }
                }

                if (errors.Any())
                {
                    result.Success = false;
                    result.Errors = errors;
                    return result;
                }

                // 生成密码哈希
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(createRequest.Password);

                // 创建用户实体
                var user = new User(createRequest.Username, createRequest.Email, passwordHash)
                {
                    DisplayName = createRequest.DisplayName,
                    PhoneNumber = createRequest.PhoneNumber,
                    IsActive = createRequest.InitialStatus == "Active",
                    EmailConfirmed = !createRequest.RequireEmailVerification
                };

                // 设置用户角色（如果只有一个角色）
                if (roles.Count == 1 && Enum.TryParse<Domain.Enums.UserRole>(roles.First().Name, true, out var roleEnum))
                {
                    user.Role = roleEnum;
                }

                // 设置个人资料
                if (createRequest.Profile != null)
                {
                    user.FirstName = createRequest.Profile.FirstName;
                    user.LastName = createRequest.Profile.LastName;
                    user.DateOfBirth = createRequest.Profile.DateOfBirth;
                    user.Gender = createRequest.Profile.Gender;
                    user.Bio = createRequest.Profile.Bio;
                    user.Website = createRequest.Profile.Website;
                    user.Location = createRequest.Profile.Company; // 使用Location字段存储公司信息
                }

                // 设置邮箱验证令牌
                if (createRequest.RequireEmailVerification)
                {
                    var verificationToken = GenerateSecureToken();
                    user.SetEmailVerificationToken(verificationToken);
                }

                // 保存用户
                await _userRepository.AddAsync(user);

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "CreateUser",
                    "User",
                    user.Id.ToString(),
                    $"创建用户: {user.UserName}",
                    null,
                    new { user.UserName, user.Email.Value, user.Role }
                );

                // 发送欢迎邮件
                if (createRequest.SendWelcomeEmail)
                {
                    try
                    {
                        await SendWelcomeEmailAsync(user, createRequest.RequireEmailVerification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "发送欢迎邮件失败，用户ID: {UserId}", user.Id);
                        warnings.Add("用户创建成功，但欢迎邮件发送失败");
                    }
                }

                result.Success = true;
                result.UserId = user.Id;
                result.Warnings = warnings;
                result.UserInfo = _mapper.Map<UserManagementDto>(user);
                result.UserInfo.Roles = new[] { user.Role.ToString() };

                if (createRequest.RequireEmailVerification && user.EmailVerificationToken != null)
                {
                    result.EmailVerificationLink = GenerateEmailVerificationLink(user.EmailVerificationToken);
                }

                _logger.LogInformation("成功创建用户，用户ID: {UserId}, 用户名: {Username}", user.Id, user.UserName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户时发生错误，用户名: {Username}", createRequest.Username);
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequestDto updateRequest, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始更新用户信息，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                var oldValues = new
                {
                    user.UserName,
                    Email = user.Email.Value,
                    user.DisplayName,
                    user.PhoneNumber,
                    user.IsActive,
                    user.Role
                };

                var hasChanges = false;

                // 更新用户名
                if (!string.IsNullOrWhiteSpace(updateRequest.Username) && updateRequest.Username != user.UserName)
                {
                    if (await _userRepository.IsUserNameInUseAsync(updateRequest.Username, userId))
                    {
                        throw new InvalidOperationException("用户名已存在");
                    }
                    user.UserName = updateRequest.Username;
                    hasChanges = true;
                }

                // 更新邮箱
                if (!string.IsNullOrWhiteSpace(updateRequest.Email) && updateRequest.Email != user.Email.Value)
                {
                    if (await _userRepository.IsEmailInUseAsync(updateRequest.Email, userId))
                    {
                        throw new InvalidOperationException("邮箱地址已存在");
                    }
                    user.ChangeEmail(updateRequest.Email);
                    hasChanges = true;
                }

                // 更新显示名称
                if (updateRequest.DisplayName != null && updateRequest.DisplayName != user.DisplayName)
                {
                    user.DisplayName = updateRequest.DisplayName;
                    hasChanges = true;
                }

                // 更新手机号
                if (updateRequest.PhoneNumber != null && updateRequest.PhoneNumber != user.PhoneNumber)
                {
                    user.PhoneNumber = updateRequest.PhoneNumber;
                    hasChanges = true;
                }

                // 更新状态
                if (!string.IsNullOrWhiteSpace(updateRequest.Status))
                {
                    var isActive = updateRequest.Status.ToLower() == "active";
                    if (user.IsActive != isActive)
                    {
                        user.IsActive = isActive;
                        hasChanges = true;
                    }
                }

                // 更新头像
                if (updateRequest.Avatar != null && updateRequest.Avatar != user.AvatarUrl)
                {
                    user.AvatarUrl = updateRequest.Avatar;
                    hasChanges = true;
                }

                // 更新个人资料
                if (updateRequest.Profile != null)
                {
                    if (updateRequest.Profile.FirstName != null && updateRequest.Profile.FirstName != user.FirstName)
                    {
                        user.FirstName = updateRequest.Profile.FirstName;
                        hasChanges = true;
                    }
                    if (updateRequest.Profile.LastName != null && updateRequest.Profile.LastName != user.LastName)
                    {
                        user.LastName = updateRequest.Profile.LastName;
                        hasChanges = true;
                    }
                    if (updateRequest.Profile.DateOfBirth.HasValue && updateRequest.Profile.DateOfBirth != user.DateOfBirth)
                    {
                        user.DateOfBirth = updateRequest.Profile.DateOfBirth;
                        hasChanges = true;
                    }
                    if (updateRequest.Profile.Gender != null && updateRequest.Profile.Gender != user.Gender)
                    {
                        user.Gender = updateRequest.Profile.Gender;
                        hasChanges = true;
                    }
                    if (updateRequest.Profile.Bio != null && updateRequest.Profile.Bio != user.Bio)
                    {
                        user.Bio = updateRequest.Profile.Bio;
                        hasChanges = true;
                    }
                    if (updateRequest.Profile.Website != null && updateRequest.Profile.Website != user.Website)
                    {
                        user.Website = updateRequest.Profile.Website;
                        hasChanges = true;
                    }
                }

                if (!hasChanges)
                {
                    _logger.LogInformation("用户信息未发生变化，用户ID: {UserId}", userId);
                    return true;
                }

                await _userRepository.UpdateAsync(user);

                var newValues = new
                {
                    user.UserName,
                    Email = user.Email.Value,
                    user.DisplayName,
                    user.PhoneNumber,
                    user.IsActive,
                    user.Role
                };

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "UpdateUser",
                    "User",
                    userId.ToString(),
                    $"更新用户信息: {user.UserName}",
                    oldValues,
                    newValues
                );

                // 如果邮箱发生变化且需要强制验证
                if (updateRequest.ForceEmailVerification.HasValue && updateRequest.ForceEmailVerification.Value && !user.EmailConfirmed)
                {
                    var verificationToken = GenerateSecureToken();
                    user.SetEmailVerificationToken(verificationToken);
                    await _userRepository.UpdateAsync(user);

                    try
                    {
                        await SendEmailVerificationAsync(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "发送邮箱验证邮件失败，用户ID: {UserId}", userId);
                    }
                }

                _logger.LogInformation("成功更新用户信息，用户ID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId, bool softDelete, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始删除用户，用户ID: {UserId}, 软删除: {SoftDelete}", userId, softDelete);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                // 检查是否为系统管理员（系统管理员不能被删除）
                if (user.Role == Domain.Enums.UserRole.Admin)
                {
                    var adminCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && !u.IsDeleted);
                    if (adminCount <= 1)
                    {
                        throw new InvalidOperationException("不能删除最后一个管理员");
                    }
                }

                var oldValues = new
                {
                    user.UserName,
                    Email = user.Email.Value,
                    user.IsActive,
                    user.IsDeleted
                };

                if (softDelete)
                {
                    // 软删除
                    user.IsDeleted = true;
                    user.IsActive = false;
                    user.DeletedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    // 硬删除（需要先清理相关数据）
                    await CleanupUserRelatedDataAsync(userId);
                    await _userRepository.DeleteAsync(user.Id);
                }

                var newValues = softDelete ? new { user.IsDeleted, user.IsActive, user.DeletedAt } : null;

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    softDelete ? "SoftDeleteUser" : "HardDeleteUser",
                    "User",
                    userId.ToString(),
                    $"删除用户: {user.UserName} ({(softDelete ? "软删除" : "硬删除")})",
                    oldValues,
                    newValues
                );

                _logger.LogInformation("成功删除用户，用户ID: {UserId}, 软删除: {SoftDelete}", userId, softDelete);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<BatchOperationResultDto> BatchDeleteUsersAsync(IEnumerable<Guid> userIds, bool softDelete, Guid operatorId)
        {
            var userIdList = userIds.ToList();
            var result = new BatchOperationResultDto
            {
                TotalCount = userIdList.Count,
                SuccessCount = 0,
                FailCount = 0,
                ItemResults = new List<BatchItemResultDto>()
            };

            try
            {
                _logger.LogInformation("开始批量删除用户，数量: {Count}, 软删除: {SoftDelete}", userIdList.Count, softDelete);

                // 检查管理员数量
                var adminCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && !u.IsDeleted);
                var adminIdsToDelete = new List<Guid>();

                foreach (var userId in userIdList)
                {
                    try
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user == null)
                        {
                            ((List<BatchItemResultDto>)result.ItemResults).Add(new BatchItemResultDto
                            {
                                ItemId = userId,
                                ItemTitle = $"User {userId}",
                                Success = false,
                                ErrorMessage = "用户不存在"
                            });
                            result.FailCount++;
                            continue;
                        }

                        // 检查是否为管理员
                        if (user.Role == Domain.Enums.UserRole.Admin)
                        {
                            adminIdsToDelete.Add(userId);
                        }

                        // 执行删除操作
                        var deleteSuccess = await DeleteUserAsync(userId, softDelete, operatorId);
                        if (deleteSuccess)
                        {
                            ((List<BatchItemResultDto>)result.ItemResults).Add(new BatchItemResultDto
                            {
                                ItemId = userId,
                                ItemTitle = user.UserName ?? $"User {userId}",
                                Success = true,
                                ResultData = $"成功{(softDelete ? "软" : "硬")}删除用户"
                            });
                            result.SuccessCount++;
                        }
                        else
                        {
                            ((List<BatchItemResultDto>)result.ItemResults).Add(new BatchItemResultDto
                            {
                                ItemId = userId,
                                ItemTitle = user.UserName ?? $"User {userId}",
                                Success = false,
                                ErrorMessage = "删除操作失败"
                            });
                            result.FailCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "批量删除用户时发生错误，用户ID: {UserId}", userId);
                        ((List<BatchItemResultDto>)result.ItemResults).Add(new BatchItemResultDto
                        {
                            ItemId = userId,
                            ItemTitle = $"User {userId}",
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        result.FailCount++;
                    }
                }

                // 检查是否删除了所有管理员
                if (adminIdsToDelete.Count >= adminCount)
                {
                    result.Errors = new[] { "警告：已删除所有管理员，系统可能无法正常管理" };
                }

                result.Success = result.FailCount == 0;

                // 记录批量操作审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "BatchDeleteUsers",
                    "User",
                    string.Join(",", userIdList),
                    $"批量{(softDelete ? "软" : "硬")}删除用户，成功: {result.SuccessCount}, 失败: {result.FailCount}",
                    null,
                    new { userIdList, softDelete, result.SuccessCount, result.FailCount }
                );

                _logger.LogInformation("批量删除用户完成，成功: {SuccessCount}, 失败: {FailCount}", result.SuccessCount, result.FailCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除用户时发生错误");
                result.Success = false;
                result.FailCount = userIdList.Count;
                result.Errors = new[] { ex.Message };
                return result;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始重置用户密码，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                // 验证密码强度
                if (!IsPasswordValid(newPassword))
                {
                    throw new ArgumentException("密码不符合安全要求");
                }

                var oldPasswordHash = user.PasswordHash;
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                // 更新密码
                user.UpdatePassword(newPasswordHash);

                // 重置登录失败次数
                user.ResetAccessFailedCount();

                // 解除账户锁定
                if (user.IsLockedOut())
                {
                    user.UnlockUser();
                }

                await _userRepository.UpdateAsync(user);

                // 记录审计日志（不记录密码内容）
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "ResetPassword",
                    "User",
                    userId.ToString(),
                    $"重置用户密码: {user.UserName}",
                    new { PasswordChanged = true },
                    new { PasswordChanged = true, ResetAt = DateTime.UtcNow }
                );

                // 发送密码重置通知邮件
                try
                {
                    await SendPasswordResetNotificationAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送密码重置通知邮件失败，用户ID: {UserId}", userId);
                }

                _logger.LogInformation("成功重置用户密码，用户ID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户密码时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> LockUserAccountAsync(Guid userId, string lockReason, int? lockDuration, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始锁定用户账户，用户ID: {UserId}, 原因: {LockReason}", userId, lockReason);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                // 检查是否为系统管理员
                if (user.Role == Domain.Enums.UserRole.Admin)
                {
                    var adminCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && !u.IsDeleted && !u.IsLockedOut());
                    if (adminCount <= 1)
                    {
                        throw new InvalidOperationException("不能锁定最后一个活跃管理员");
                    }
                }

                var oldValues = new
                {
                    user.LockoutEndDateUtc,
                    user.LockoutEnabled,
                    user.IsActive
                };

                // 计算锁定结束时间
                DateTime lockoutEndDate;
                if (lockDuration.HasValue && lockDuration.Value > 0)
                {
                    lockoutEndDate = DateTime.UtcNow.AddMinutes(lockDuration.Value);
                }
                else
                {
                    // 默认锁定24小时
                    lockoutEndDate = DateTime.UtcNow.AddHours(24);
                }

                // 锁定用户
                user.LockUser(lockoutEndDate);
                user.IsActive = false;

                await _userRepository.UpdateAsync(user);

                var newValues = new
                {
                    user.LockoutEndDateUtc,
                    user.LockoutEnabled,
                    user.IsActive,
                    LockReason = lockReason,
                    LockDuration = lockDuration
                };

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "LockUserAccount",
                    "User",
                    userId.ToString(),
                    $"锁定用户账户: {user.UserName}, 原因: {lockReason}",
                    oldValues,
                    newValues
                );

                // 发送账户锁定通知邮件
                try
                {
                    await SendAccountLockNotificationAsync(user, lockReason, lockoutEndDate);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送账户锁定通知邮件失败，用户ID: {UserId}", userId);
                }

                _logger.LogInformation("成功锁定用户账户，用户ID: {UserId}, 锁定至: {LockoutEnd}", userId, lockoutEndDate);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "锁定用户账户时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UnlockUserAccountAsync(Guid userId, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始解除用户账户锁定，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                if (!user.IsLockedOut())
                {
                    _logger.LogInformation("用户账户未被锁定，用户ID: {UserId}", userId);
                    return true;
                }

                var oldValues = new
                {
                    user.LockoutEndDateUtc,
                    user.AccessFailedCount,
                    user.IsActive
                };

                // 解除锁定
                user.UnlockUser();
                user.IsActive = true;

                await _userRepository.UpdateAsync(user);

                var newValues = new
                {
                    user.LockoutEndDateUtc,
                    user.AccessFailedCount,
                    user.IsActive
                };

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "UnlockUserAccount",
                    "User",
                    userId.ToString(),
                    $"解除用户账户锁定: {user.UserName}",
                    oldValues,
                    newValues
                );

                // 发送账户解除通知邮件
                try
                {
                    await SendAccountUnlockNotificationAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送账户解除通知邮件失败，用户ID: {UserId}", userId);
                }

                _logger.LogInformation("成功解除用户账户锁定，用户ID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解除用户账户锁定时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AssignRolesToUserAsync(Guid userId, IEnumerable<Guid> roleIds, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始为用户分配角色，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                var roleIdList = roleIds.ToList();
                if (!roleIdList.Any())
                {
                    _logger.LogWarning("未提供角色ID列表");
                    return false;
                }

                // 验证角色是否存在并获取角色信息
                var roles = new List<Role>();
                foreach (var roleId in roleIdList)
                {
                    var role = await _roleRepository.GetByIdAsync(roleId);
                    if (role == null)
                    {
                        throw new ArgumentException($"角色ID {roleId} 不存在");
                    }
                    roles.Add(role);
                }

                var oldValues = new { user.Role };

                // 如果只有一个角色且为系统预定义角色，直接设置
                if (roles.Count == 1 && Enum.TryParse<Domain.Enums.UserRole>(roles.First().Name, true, out var roleEnum))
                {
                    user.Role = roleEnum;
                }
                else
                {
                    // 如果有多个角色，选择权限最高的角色
                    var highestRole = roles.OrderByDescending(r => GetRolePriority(r.Name)).First();
                    if (Enum.TryParse<Domain.Enums.UserRole>(highestRole.Name, true, out var highestRoleEnum))
                    {
                        user.Role = highestRoleEnum;
                    }
                }

                await _userRepository.UpdateAsync(user);

                var newValues = new { user.Role };

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "AssignRoles",
                    "User",
                    userId.ToString(),
                    $"为用户分配角色: {user.UserName}, 角色: {string.Join(", ", roles.Select(r => r.Name))}",
                    oldValues,
                    newValues
                );

                _logger.LogInformation("成功为用户分配角色，用户ID: {UserId}, 最终角色: {Role}", userId, user.Role);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为用户分配角色时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> RemoveRolesFromUserAsync(Guid userId, IEnumerable<Guid> roleIds, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始移除用户角色，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                var roleIdList = roleIds.ToList();
                if (!roleIdList.Any())
                {
                    _logger.LogWarning("未提供角色ID列表");
                    return false;
                }

                // 检查是否试图移除管理员角色
                if (user.Role == Domain.Enums.UserRole.Admin)
                {
                    var adminCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && !u.IsDeleted);
                    if (adminCount <= 1)
                    {
                        throw new InvalidOperationException("不能移除最后一个管理员的管理员角色");
                    }
                }

                var oldValues = new { user.Role };

                // 简化实现：将用户角色重置为普通用户
                user.Role = Domain.Enums.UserRole.User;

                await _userRepository.UpdateAsync(user);

                var newValues = new { user.Role };

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "RemoveRoles",
                    "User",
                    userId.ToString(),
                    $"移除用户角色: {user.UserName}",
                    oldValues,
                    newValues
                );

                _logger.LogInformation("成功移除用户角色，用户ID: {UserId}, 当前角色: {Role}", userId, user.Role);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除用户角色时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("开始获取用户角色列表，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return Enumerable.Empty<UserRoleDto>();
                }

                var userRoles = new List<UserRoleDto>
                {
                    new UserRoleDto
                    {
                        Id = Guid.NewGuid(),
                        Name = user.Role.ToString(),
                        DisplayName = user.Role.ToString(),
                        Description = GetRoleDescription(user.Role),
                        IsSystem = true,
                        AssignedAt = user.CreatedAt,
                        AssignedBy = "System",
                        IsActive = true
                    }
                };

                _logger.LogInformation("成功获取用户角色列表，用户ID: {UserId}, 角色数量: {Count}", userId, userRoles.Count);
                return userRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户角色列表时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("开始获取用户权限列表，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return Enumerable.Empty<UserPermissionDto>();
                }

                // 获取用户的所有权限（直接权限 + 角色继承权限）
                var directPermissions = await GetUserDirectPermissionsAsync(userId);
                var inheritedPermissions = await GetUserInheritedPermissionsAsync(userId);

                var allPermissions = directPermissions.Concat(inheritedPermissions)
                    .GroupBy(p => p.Name)
                    .Select(g => g.First()) // 去重
                    .ToList();

                _logger.LogInformation("成功获取用户权限列表，用户ID: {UserId}, 权限数量: {Count}", userId, allPermissions.Count);
                return allPermissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户权限列表时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        // 注意：其余方法的实现在 UserManagementServicePart2.cs 文件中

        #region Helper Methods

        private async Task<IEnumerable<UserStatusDistributionDto>> GetUserStatusDistributionAsync()
        {
            var totalUsers = await _userRepository.CountAsync();
            if (totalUsers == 0) return new List<UserStatusDistributionDto>();

            var activeUsers = await _userRepository.CountAsync(u => u.IsActive && !u.IsDeleted);
            var inactiveUsers = await _userRepository.CountAsync(u => !u.IsActive && !u.IsDeleted);
            var lockedUsers = await _userRepository.CountAsync(u => u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc > DateTime.UtcNow);
            var deletedUsers = await _userRepository.CountAsync(u => u.IsDeleted);

            return new[]
            {
                new UserStatusDistributionDto { Status = "Active", Count = activeUsers, Percentage = (double)activeUsers / totalUsers * 100 },
                new UserStatusDistributionDto { Status = "Inactive", Count = inactiveUsers, Percentage = (double)inactiveUsers / totalUsers * 100 },
                new UserStatusDistributionDto { Status = "Locked", Count = lockedUsers, Percentage = (double)lockedUsers / totalUsers * 100 },
                new UserStatusDistributionDto { Status = "Deleted", Count = deletedUsers, Percentage = (double)deletedUsers / totalUsers * 100 }
            };
        }

        private async Task<IEnumerable<UserRoleDistributionDto>> GetUserRoleDistributionAsync()
        {
            var totalUsers = await _userRepository.CountAsync(u => !u.IsDeleted);
            if (totalUsers == 0) return new List<UserRoleDistributionDto>();

            var adminCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Admin && !u.IsDeleted);
            var authorCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.Author && !u.IsDeleted);
            var userCount = await _userRepository.CountAsync(u => u.Role == Domain.Enums.UserRole.User && !u.IsDeleted);

            return new[]
            {
                new UserRoleDistributionDto { RoleName = "Admin", UserCount = adminCount, Percentage = (double)adminCount / totalUsers * 100 },
                new UserRoleDistributionDto { RoleName = "Author", UserCount = authorCount, Percentage = (double)authorCount / totalUsers * 100 },
                new UserRoleDistributionDto { RoleName = "User", UserCount = userCount, Percentage = (double)userCount / totalUsers * 100 }
            };
        }

        private async Task<IEnumerable<UserGrowthTrendDto>> GetUserGrowthTrendsAsync(int days)
        {
            var trends = new List<UserGrowthTrendDto>();
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i);
                var endDate = date.AddDays(1);

                var newUsers = await _userRepository.CountAsync(u => u.CreatedAt >= date && u.CreatedAt < endDate);
                var totalUsers = await _userRepository.CountAsync(u => u.CreatedAt <= endDate && !u.IsDeleted);
                var activeUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= date && u.LastLoginAt < endDate);

                var previousDayTotal = await _userRepository.CountAsync(u => u.CreatedAt <= date && !u.IsDeleted);
                var growthRate = previousDayTotal > 0 ? (double)newUsers / previousDayTotal * 100 : 0;

                trends.Add(new UserGrowthTrendDto
                {
                    Date = date,
                    NewUsers = newUsers,
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    GrowthRate = growthRate
                });
            }

            return trends;
        }

        private async Task<UserActivityOverviewDto> GetUserActivityOverviewAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddDays(-30);

            var dailyActiveUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= today);
            var weeklyActiveUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= weekAgo);
            var monthlyActiveUsers = await _userRepository.CountAsync(u => u.LastLoginAt >= monthAgo);
            var totalUsers = await _userRepository.CountAsync(u => !u.IsDeleted);

            var activityRate = totalUsers > 0 ? (double)monthlyActiveUsers / totalUsers * 100 : 0;

            return new UserActivityOverviewDto
            {
                DailyActiveUsers = dailyActiveUsers,
                WeeklyActiveUsers = weeklyActiveUsers,
                MonthlyActiveUsers = monthlyActiveUsers,
                ActivityRate = activityRate,
                AverageSessionDuration = TimeSpan.FromMinutes(15) // 假设平均会话时长
            };
        }

        private IQueryable<User> ApplyUserSorting(IQueryable<User> query, string sortBy, string sortDirection)
        {
            var isDescending = sortDirection.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "username" => isDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),
                "email" => isDescending ? query.OrderByDescending(u => u.Email.Value) : query.OrderBy(u => u.Email.Value),
                "displayname" => isDescending ? query.OrderByDescending(u => u.DisplayName) : query.OrderBy(u => u.DisplayName),
                "createdat" => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
                "updatedat" => isDescending ? query.OrderByDescending(u => u.UpdatedAt) : query.OrderBy(u => u.UpdatedAt),
                "lastloginat" => isDescending ? query.OrderByDescending(u => u.LastLoginAt) : query.OrderBy(u => u.LastLoginAt),
                "role" => isDescending ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role),
                _ => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt)
            };
        }

        private async Task<bool> IsUserOnlineAsync(Guid userId)
        {
            // 简单实现：检查最后登录时间是否在5分钟内
            var user = await _userRepository.GetByIdAsync(userId);
            return user?.LastLoginAt >= DateTime.UtcNow.AddMinutes(-5);
        }

        private async Task<UserStatsDto> GetUserStatsAsync(Guid userId)
        {
            // 简化实现，实际项目中需要从相关表获取真实数据
            return new UserStatsDto
            {
                LoginCount = 0, // 需要从登录历史表获取
                PostCount = 0, // 需要从文章表获取
                CommentCount = 0, // 需要从评论表获取
                TotalViews = 0, // 需要从统计表获取
                FollowersCount = 0,
                FollowingCount = 0
            };
        }

        private async Task<string> CalculateUserRiskLevelAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return "Unknown";

            var riskScore = 0;

            // 基于多个因素计算风险评分
            if (user.AccessFailedCount > 5) riskScore += 20;
            if (!user.EmailConfirmed) riskScore += 10;
            if (user.CreatedAt > DateTime.UtcNow.AddDays(-7)) riskScore += 15; // 新用户
            if (user.LastLoginAt == null || user.LastLoginAt < DateTime.UtcNow.AddDays(-30)) riskScore += 10; // 长期不活跃

            return riskScore switch
            {
                >= 40 => "High",
                >= 20 => "Medium",
                _ => "Low"
            };
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private string GenerateEmailVerificationLink(string token)
        {
            // 实际实现应该使用配置的基础URL
            return $"https://your-domain.com/verify-email?token={token}";
        }

        private bool IsPasswordValid(string password)
        {
            // 简单的密码验证逻辑
            return !string.IsNullOrWhiteSpace(password) &&
                   password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }

        private async Task SendWelcomeEmailAsync(User user, bool includeVerification)
        {
            var subject = "欢迎加入我们的平台";
            var body = $"亲爱的 {user.GetDisplayName()}，欢迎加入我们的平台！";

            if (includeVerification && user.EmailVerificationToken != null)
            {
                var verificationLink = GenerateEmailVerificationLink(user.EmailVerificationToken);
                body += $"\n\n请点击以下链接验证您的邮箱：{verificationLink}";
            }

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        private async Task SendEmailVerificationAsync(User user)
        {
            if (user.EmailVerificationToken == null) return;

            var verificationLink = GenerateEmailVerificationLink(user.EmailVerificationToken);
            var subject = "邮箱验证";
            var body = $"请点击以下链接验证您的邮箱：{verificationLink}";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        private async Task SendPasswordResetNotificationAsync(User user)
        {
            var subject = "密码重置通知";
            var body = $"您的密码已被管理员重置。如果这不是您本人操作，请立即联系我们。";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        private async Task SendAccountLockNotificationAsync(User user, string reason, DateTime lockoutEnd)
        {
            var subject = "账户锁定通知";
            var body = $"您的账户已被锁定。\n原因：{reason}\n锁定至：{lockoutEnd:yyyy-MM-dd HH:mm:ss}";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        private async Task SendAccountUnlockNotificationAsync(User user)
        {
            var subject = "账户解锁通知";
            var body = "您的账户锁定已被解除，您现在可以正常使用我们的服务。";

            await _emailService.SendEmailAsync(user.Email.Value, subject, body);
        }

        private async Task CleanupUserRelatedDataAsync(Guid userId)
        {
            // 在实际项目中，这里需要清理与用户相关的所有数据
            // 例如：用户的文章、评论、收藏、关注关系等
            _logger.LogInformation("清理用户相关数据，用户ID: {UserId}", userId);
        }

        #endregion
    }
}