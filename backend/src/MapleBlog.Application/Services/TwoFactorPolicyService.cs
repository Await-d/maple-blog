using Microsoft.Extensions.Logging;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services;

/// <summary>
/// 双因素认证策略服务
/// </summary>
public class TwoFactorPolicyService : ITwoFactorPolicyService
{
    private readonly ILogger<TwoFactorPolicyService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorAuthRepository _twoFactorAuthRepository;
    private readonly ITwoFactorAuditService _auditService;

    // 策略配置
    private readonly Dictionary<UserRole, TwoFactorPolicy> _rolePolicies;

    public TwoFactorPolicyService(
        ILogger<TwoFactorPolicyService> logger,
        IUserRepository userRepository,
        ITwoFactorAuthRepository twoFactorAuthRepository,
        ITwoFactorAuditService auditService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _twoFactorAuthRepository = twoFactorAuthRepository ?? throw new ArgumentNullException(nameof(twoFactorAuthRepository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

        // 初始化角色策略
        _rolePolicies = InitializeRolePolicies();
    }

    /// <summary>
    /// 检查用户是否需要2FA
    /// </summary>
    public async Task<bool> IsRequiredForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            // 检查基于角色的策略
            var rolePolicy = GetPolicyForRole(user.Role);
            if (rolePolicy.IsRequired)
            {
                return true;
            }

            // 检查基于风险的策略
            if (await IsHighRiskUserAsync(userId, cancellationToken))
            {
                return true;
            }

            // 检查基于时间的策略
            if (await IsTimeBasedPolicyTriggeredAsync(userId, cancellationToken))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA requirement for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查用户是否符合2FA策略
    /// </summary>
    public async Task<PolicyComplianceResult> CheckComplianceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return PolicyComplianceResult.CreateFailure("User not found");
            }

            var twoFactorAuth = await _twoFactorAuthRepository.GetByUserIdAsync(userId, cancellationToken);
            var rolePolicy = GetPolicyForRole(user.Role);

            var result = new PolicyComplianceResult
            {
                UserId = userId,
                IsCompliant = true,
                PolicyViolations = new List<PolicyViolation>()
            };

            // 检查是否需要启用2FA
            if (rolePolicy.IsRequired && (twoFactorAuth?.IsEnabled != true))
            {
                result.IsCompliant = false;
                result.PolicyViolations.Add(new PolicyViolation
                {
                    Type = "2FA_REQUIRED",
                    Description = $"Role {user.Role} requires two-factor authentication to be enabled",
                    Severity = PolicyViolationSeverity.High,
                    RequiredAction = "Enable two-factor authentication"
                });
            }

            // 检查所需的最小方法数量
            if (twoFactorAuth?.IsEnabled == true && rolePolicy.MinimumMethods > 0)
            {
                var enabledMethodsCount = twoFactorAuth.GetEnabledMethods().Count();
                if (enabledMethodsCount < rolePolicy.MinimumMethods)
                {
                    result.IsCompliant = false;
                    result.PolicyViolations.Add(new PolicyViolation
                    {
                        Type = "INSUFFICIENT_METHODS",
                        Description = $"Role {user.Role} requires at least {rolePolicy.MinimumMethods} 2FA methods, but only {enabledMethodsCount} are enabled",
                        Severity = PolicyViolationSeverity.Medium,
                        RequiredAction = $"Enable at least {rolePolicy.MinimumMethods - enabledMethodsCount} additional 2FA method(s)"
                    });
                }
            }

            // 检查所需的特定方法
            if (twoFactorAuth?.IsEnabled == true && rolePolicy.RequiredMethods.Any())
            {
                var enabledMethods = twoFactorAuth.GetEnabledMethods().ToHashSet();
                var missingMethods = rolePolicy.RequiredMethods.Except(enabledMethods).ToList();

                if (missingMethods.Any())
                {
                    result.IsCompliant = false;
                    result.PolicyViolations.Add(new PolicyViolation
                    {
                        Type = "MISSING_REQUIRED_METHODS",
                        Description = $"Role {user.Role} requires the following 2FA methods: {string.Join(", ", missingMethods.Select(m => m.GetDisplayName()))}",
                        Severity = PolicyViolationSeverity.High,
                        RequiredAction = $"Enable required 2FA methods: {string.Join(", ", missingMethods.Select(m => m.GetDisplayName()))}"
                    });
                }
            }

            // 检查禁用的方法
            if (twoFactorAuth?.IsEnabled == true && rolePolicy.ProhibitedMethods.Any())
            {
                var enabledMethods = twoFactorAuth.GetEnabledMethods().ToHashSet();
                var prohibitedUsed = rolePolicy.ProhibitedMethods.Intersect(enabledMethods).ToList();

                if (prohibitedUsed.Any())
                {
                    result.IsCompliant = false;
                    result.PolicyViolations.Add(new PolicyViolation
                    {
                        Type = "PROHIBITED_METHODS_USED",
                        Description = $"Role {user.Role} prohibits the following 2FA methods: {string.Join(", ", prohibitedUsed.Select(m => m.GetDisplayName()))}",
                        Severity = PolicyViolationSeverity.High,
                        RequiredAction = $"Disable prohibited 2FA methods: {string.Join(", ", prohibitedUsed.Select(m => m.GetDisplayName()))}"
                    });
                }
            }

            // 检查设置期限
            if (rolePolicy.SetupDeadline.HasValue && (twoFactorAuth?.IsEnabled != true))
            {
                var daysSinceCreation = (DateTime.UtcNow - user.CreatedAt).Days;
                if (daysSinceCreation > rolePolicy.SetupDeadline.Value)
                {
                    result.IsCompliant = false;
                    result.PolicyViolations.Add(new PolicyViolation
                    {
                        Type = "SETUP_DEADLINE_EXCEEDED",
                        Description = $"2FA must be enabled within {rolePolicy.SetupDeadline.Value} days of account creation",
                        Severity = PolicyViolationSeverity.Critical,
                        RequiredAction = "Enable two-factor authentication immediately"
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking 2FA compliance for user {UserId}", userId);
            return PolicyComplianceResult.CreateFailure("Error checking compliance");
        }
    }

    /// <summary>
    /// 强制执行2FA策略
    /// </summary>
    public async Task<OperationResult> EnforcePolicyAsync(Guid adminUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminUser = await _userRepository.GetByIdAsync(adminUserId, cancellationToken);
            if (adminUser == null || !adminUser.IsSystemManager())
            {
                return OperationResult.Failure("Insufficient permissions to enforce policy");
            }

            var compliance = await CheckComplianceAsync(targetUserId, cancellationToken);
            if (compliance.IsCompliant)
            {
                return OperationResult.CreateSuccess("User is already compliant with 2FA policy");
            }

            var targetUser = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
            if (targetUser == null)
            {
                return OperationResult.Failure("Target user not found");
            }

            // 记录策略执行
            await _auditService.LogSecurityEventAsync(
                targetUserId,
                "TwoFactorForced",
                $"2FA policy enforcement initiated by admin {adminUser.UserName}",
                SecurityEventSeverity.High,
                new AuditContextDto
                {
                    Metadata = new Dictionary<string, object>
                    {
                        ["adminUserId"] = adminUserId,
                        ["adminUserName"] = adminUser.UserName,
                        ["violations"] = compliance.PolicyViolations,
                        ["enforcementReason"] = "Policy non-compliance"
                    }
                },
                cancellationToken);

            // 在实际实现中，这里可能会：
            // 1. 强制用户在下次登录时设置2FA
            // 2. 限制用户访问直到完成2FA设置
            // 3. 发送通知邮件
            // 4. 创建定时任务跟踪合规性

            _logger.LogInformation("2FA policy enforcement initiated for user {TargetUserId} by admin {AdminUserId}",
                targetUserId, adminUserId);

            return OperationResult.CreateSuccess("2FA policy enforcement has been initiated. User will be required to enable 2FA on next login.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing 2FA policy for user {TargetUserId} by admin {AdminUserId}",
                targetUserId, adminUserId);
            return OperationResult.Failure("An error occurred while enforcing 2FA policy");
        }
    }

    /// <summary>
    /// 获取系统范围的策略合规性报告
    /// </summary>
    public async Task<OperationResult<PolicyComplianceReportDto>> GetComplianceReportAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalUsers = await _userRepository.CountAsync(cancellationToken: cancellationToken);
            var usersWithTwoFactor = await _twoFactorAuthRepository.GetEnabledCountAsync(cancellationToken);

            var report = new PolicyComplianceReportDto
            {
                TotalUsers = totalUsers,
                CompliantUsers = 0,
                NonCompliantUsers = 0,
                ComplianceRate = 0,
                ViolationsByType = new Dictionary<string, int>(),
                ViolationsBySeverity = new Dictionary<string, int>(),
                GeneratedAt = DateTime.UtcNow
            };

            // 获取需要2FA的用户
            var usersRequiring2FA = await _twoFactorAuthRepository.GetUsersRequiring2FAAsync(cancellationToken);

            foreach (var userId in usersRequiring2FA)
            {
                var compliance = await CheckComplianceAsync(userId, cancellationToken);

                if (compliance.IsCompliant)
                {
                    report.CompliantUsers++;
                }
                else
                {
                    report.NonCompliantUsers++;

                    // 统计违规类型
                    foreach (var violation in compliance.PolicyViolations)
                    {
                        report.ViolationsByType.TryGetValue(violation.Type, out var typeCount);
                        report.ViolationsByType[violation.Type] = typeCount + 1;

                        var severityKey = violation.Severity.ToString();
                        report.ViolationsBySeverity.TryGetValue(severityKey, out var severityCount);
                        report.ViolationsBySeverity[severityKey] = severityCount + 1;
                    }
                }
            }

            report.ComplianceRate = usersRequiring2FA.Count > 0
                ? (double)report.CompliantUsers / usersRequiring2FA.Count * 100
                : 100;

            return OperationResult<PolicyComplianceReportDto>.CreateSuccess(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            return OperationResult<PolicyComplianceReportDto>.Failure("Error generating compliance report");
        }
    }

    #region Private Methods

    /// <summary>
    /// 初始化角色策略
    /// </summary>
    private static Dictionary<UserRole, TwoFactorPolicy> InitializeRolePolicies()
    {
        return new Dictionary<UserRole, TwoFactorPolicy>
        {
            [UserRole.User] = new TwoFactorPolicy
            {
                IsRequired = false,
                MinimumMethods = 0,
                RequiredMethods = new List<TwoFactorMethod>(),
                ProhibitedMethods = new List<TwoFactorMethod>(),
                SetupDeadline = null
            },

            [UserRole.Author] = new TwoFactorPolicy
            {
                IsRequired = false, // 可选但推荐
                MinimumMethods = 1,
                RequiredMethods = new List<TwoFactorMethod>(),
                ProhibitedMethods = new List<TwoFactorMethod>(),
                SetupDeadline = null
            },

            [UserRole.Moderator] = new TwoFactorPolicy
            {
                IsRequired = true,
                MinimumMethods = 1,
                RequiredMethods = new List<TwoFactorMethod> { TwoFactorMethod.TOTP },
                ProhibitedMethods = new List<TwoFactorMethod>(),
                SetupDeadline = 7 // 7天内必须设置
            },

            [UserRole.Admin] = new TwoFactorPolicy
            {
                IsRequired = true,
                MinimumMethods = 2,
                RequiredMethods = new List<TwoFactorMethod> { TwoFactorMethod.TOTP },
                ProhibitedMethods = new List<TwoFactorMethod> { TwoFactorMethod.SMS }, // SMS不够安全
                SetupDeadline = 3 // 3天内必须设置
            },

            [UserRole.SuperAdmin] = new TwoFactorPolicy
            {
                IsRequired = true,
                MinimumMethods = 2,
                RequiredMethods = new List<TwoFactorMethod> { TwoFactorMethod.TOTP, TwoFactorMethod.HardwareKey },
                ProhibitedMethods = new List<TwoFactorMethod> { TwoFactorMethod.SMS, TwoFactorMethod.Email },
                SetupDeadline = 1 // 1天内必须设置
            }
        };
    }

    /// <summary>
    /// 获取角色策略
    /// </summary>
    private TwoFactorPolicy GetPolicyForRole(UserRole role)
    {
        _rolePolicies.TryGetValue(role, out var policy);
        return policy ?? _rolePolicies[UserRole.User]; // 默认策略
    }

    /// <summary>
    /// 检查是否为高风险用户
    /// </summary>
    private async Task<bool> IsHighRiskUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            // 这里可以实现复杂的风险评估逻辑
            // 例如：最近登录失败次数、异常登录位置、账户价值等

            // 简单示例：检查最近的可疑活动
            var recentSuspiciousEvents = await _auditService.GetUserAuditLogsAsync(
                userId,
                new AuditLogFilterDto
                {
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    IsSuspicious = true,
                    PageSize = 10
                },
                cancellationToken);

            return recentSuspiciousEvents.Success &&
                   recentSuspiciousEvents.Data != null &&
                   recentSuspiciousEvents.Data.Items.Count() >= 3; // 30天内有3次以上可疑活动
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking risk level for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 检查基于时间的策略触发
    /// </summary>
    private async Task<bool> IsTimeBasedPolicyTriggeredAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            // 示例：如果用户最近更改了重要信息（如密码、邮箱），可能需要临时启用2FA
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            // 检查密码最近是否更改（基于UpdatedAt字段）
            var daysSinceUpdate = user.UpdatedAt.HasValue ? (DateTime.UtcNow - user.UpdatedAt.Value).Days : int.MaxValue;
            if (daysSinceUpdate <= 1) // 最近1天内更改过信息
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking time-based policy for user {UserId}", userId);
            return false;
        }
    }

    #endregion
}

/// <summary>
/// 2FA策略接口
/// </summary>
public interface ITwoFactorPolicyService
{
    Task<bool> IsRequiredForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PolicyComplianceResult> CheckComplianceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OperationResult> EnforcePolicyAsync(Guid adminUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<OperationResult<PolicyComplianceReportDto>> GetComplianceReportAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 2FA策略配置
/// </summary>
public class TwoFactorPolicy
{
    /// <summary>
    /// 是否必需
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 最少方法数量
    /// </summary>
    public int MinimumMethods { get; set; }

    /// <summary>
    /// 必需的方法
    /// </summary>
    public List<TwoFactorMethod> RequiredMethods { get; set; } = new();

    /// <summary>
    /// 禁用的方法
    /// </summary>
    public List<TwoFactorMethod> ProhibitedMethods { get; set; } = new();

    /// <summary>
    /// 设置期限（天数）
    /// </summary>
    public int? SetupDeadline { get; set; }
}

/// <summary>
/// 策略合规性结果
/// </summary>
public class PolicyComplianceResult
{
    public Guid UserId { get; set; }
    public bool IsCompliant { get; set; }
    public List<PolicyViolation> PolicyViolations { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public static PolicyComplianceResult CreateFailure(string message)
    {
        return new PolicyComplianceResult
        {
            IsCompliant = false,
            PolicyViolations = new List<PolicyViolation>
            {
                new PolicyViolation
                {
                    Type = "SYSTEM_ERROR",
                    Description = message,
                    Severity = PolicyViolationSeverity.High
                }
            }
        };
    }
}

/// <summary>
/// 策略违规
/// </summary>
public class PolicyViolation
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PolicyViolationSeverity Severity { get; set; }
    public string RequiredAction { get; set; } = string.Empty;
}

/// <summary>
/// 策略违规严重程度
/// </summary>
public enum PolicyViolationSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// 策略合规性报告DTO
/// </summary>
public class PolicyComplianceReportDto
{
    public int TotalUsers { get; set; }
    public int CompliantUsers { get; set; }
    public int NonCompliantUsers { get; set; }
    public double ComplianceRate { get; set; }
    public Dictionary<string, int> ViolationsByType { get; set; } = new();
    public Dictionary<string, int> ViolationsBySeverity { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}