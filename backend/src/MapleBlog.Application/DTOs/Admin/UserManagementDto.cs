using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 用户管理概览DTO
    /// </summary>
    public class UserManagementOverviewDto
    {
        /// <summary>
        /// 总用户数
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// 活跃用户数
        /// </summary>
        public int ActiveUsers { get; set; }

        /// <summary>
        /// 今日新增用户数
        /// </summary>
        public int TodayNewUsers { get; set; }

        /// <summary>
        /// 本周新增用户数
        /// </summary>
        public int WeekNewUsers { get; set; }

        /// <summary>
        /// 在线用户数
        /// </summary>
        public int OnlineUsers { get; set; }

        /// <summary>
        /// 锁定用户数
        /// </summary>
        public int LockedUsers { get; set; }

        /// <summary>
        /// 已删除用户数
        /// </summary>
        public int DeletedUsers { get; set; }

        /// <summary>
        /// 用户状态分布
        /// </summary>
        public IEnumerable<UserStatusDistributionDto> StatusDistribution { get; set; } = new List<UserStatusDistributionDto>();

        /// <summary>
        /// 角色分布
        /// </summary>
        public IEnumerable<UserRoleDistributionDto> RoleDistribution { get; set; } = new List<UserRoleDistributionDto>();

        /// <summary>
        /// 用户增长趋势
        /// </summary>
        public IEnumerable<UserGrowthTrendDto> GrowthTrends { get; set; } = new List<UserGrowthTrendDto>();

        /// <summary>
        /// 登录活跃度统计
        /// </summary>
        public UserActivityOverviewDto ActivityOverview { get; set; } = new();
    }

    /// <summary>
    /// 用户管理DTO
    /// </summary>
    public class UserManagementDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 头像URL
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 账户状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 是否邮箱已验证
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// 是否手机已验证
        /// </summary>
        public bool PhoneVerified { get; set; }

        /// <summary>
        /// 是否启用双因子认证
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// 最后登录时间
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// 最后登录IP
        /// </summary>
        public string? LastLoginIp { get; set; }

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 锁定到期时间
        /// </summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// 失败登录次数
        /// </summary>
        public int AccessFailedCount { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        public IEnumerable<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// 用户角色（强类型枚举）
        /// </summary>
        public UserRole UserRole { get; set; } = UserRole.User;

        /// <summary>
        /// 用户标签
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 用户统计信息
        /// </summary>
        public UserStatsDto Stats { get; set; } = new();

        /// <summary>
        /// 风险评级
        /// </summary>
        public string RiskLevel { get; set; } = "Low";

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// 地理位置信息
        /// </summary>
        public UserLocationDto? Location { get; set; }
    }

    /// <summary>
    /// 用户详细信息DTO
    /// </summary>
    public class UserDetailDto
    {
        /// <summary>
        /// 基本信息
        /// </summary>
        public UserManagementDto BasicInfo { get; set; } = new();

        /// <summary>
        /// 个人资料
        /// </summary>
        public UserProfileDto Profile { get; set; } = new();

        /// <summary>
        /// 安全信息
        /// </summary>
        public UserSecurityInfoDto SecurityInfo { get; set; } = new();

        /// <summary>
        /// 活动统计
        /// </summary>
        public UserActivityStatsDto ActivityStats { get; set; } = new();

        /// <summary>
        /// 权限信息
        /// </summary>
        public UserPermissionInfoDto PermissionInfo { get; set; } = new();

        /// <summary>
        /// 社交账号绑定
        /// </summary>
        public IEnumerable<UserSocialAccountDto> SocialAccounts { get; set; } = new List<UserSocialAccountDto>();

        /// <summary>
        /// 设备信息
        /// </summary>
        public IEnumerable<UserDeviceDto> Devices { get; set; } = new List<UserDeviceDto>();

        /// <summary>
        /// 最近活动记录
        /// </summary>
        public IEnumerable<RecentUserActivityDto> RecentActivities { get; set; } = new List<RecentUserActivityDto>();

        /// <summary>
        /// 用户偏好设置
        /// </summary>
        public UserPreferencesDto Preferences { get; set; } = new();
    }

    /// <summary>
    /// 创建用户请求DTO
    /// </summary>
    public class CreateUserRequestDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 角色IDs
        /// </summary>
        public IEnumerable<Guid> RoleIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 初始状态
        /// </summary>
        public string InitialStatus { get; set; } = "Active";

        /// <summary>
        /// 是否需要邮箱验证
        /// </summary>
        public bool RequireEmailVerification { get; set; } = true;

        /// <summary>
        /// 是否发送欢迎邮件
        /// </summary>
        public bool SendWelcomeEmail { get; set; } = true;

        /// <summary>
        /// 用户标签
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 个人资料信息
        /// </summary>
        public CreateUserProfileDto? Profile { get; set; }

        /// <summary>
        /// 偏好设置
        /// </summary>
        public Dictionary<string, object> Preferences { get; set; } = new();
    }

    /// <summary>
    /// 创建用户结果DTO
    /// </summary>
    public class UserCreateResultDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告信息
        /// </summary>
        public IEnumerable<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 生成的临时密码（如果适用）
        /// </summary>
        public string? TemporaryPassword { get; set; }

        /// <summary>
        /// 邮箱验证链接（如果适用）
        /// </summary>
        public string? EmailVerificationLink { get; set; }

        /// <summary>
        /// 创建的用户信息
        /// </summary>
        public UserManagementDto? UserInfo { get; set; }
    }

    /// <summary>
    /// 更新用户请求DTO
    /// </summary>
    public class UpdateUserRequestDto
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// 账户状态
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// 头像URL
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 用户标签
        /// </summary>
        public IEnumerable<string>? Tags { get; set; }

        /// <summary>
        /// 个人资料更新
        /// </summary>
        public UpdateUserProfileDto? Profile { get; set; }

        /// <summary>
        /// 偏好设置更新
        /// </summary>
        public Dictionary<string, object>? Preferences { get; set; }

        /// <summary>
        /// 是否强制邮箱验证
        /// </summary>
        public bool? ForceEmailVerification { get; set; }

        /// <summary>
        /// 是否强制密码重置
        /// </summary>
        public bool? ForcePasswordReset { get; set; }
    }

    /// <summary>
    /// 用户角色DTO
    /// </summary>
    public class UserRoleDto
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 角色显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否系统角色
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// 分配时间
        /// </summary>
        public DateTime AssignedAt { get; set; }

        /// <summary>
        /// 分配人
        /// </summary>
        public string AssignedBy { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 用户权限DTO
    /// </summary>
    public class UserPermissionDto
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 权限显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 权限分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 权限来源
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 来源角色名称
        /// </summary>
        public string? SourceRoleName { get; set; }

        /// <summary>
        /// 是否直接分配
        /// </summary>
        public bool IsDirectlyAssigned { get; set; }

        /// <summary>
        /// 权限范围
        /// </summary>
        public string? Scope { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 用户活动日志DTO
    /// </summary>
    public class UserActivityLogDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 活动类型
        /// </summary>
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>
        /// 活动描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 设备信息
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// 地理位置
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 活动时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public string? ResourceType { get; set; }

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 结果状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 风险级别
        /// </summary>
        public string RiskLevel { get; set; } = "Low";

        /// <summary>
        /// 附加数据
        /// </summary>
        public object? Metadata { get; set; }
    }

    /// <summary>
    /// 用户登录历史DTO
    /// </summary>
    public class UserLoginHistoryDto
    {
        /// <summary>
        /// 登录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 登录时间
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// 登出时间
        /// </summary>
        public DateTime? LogoutTime { get; set; }

        /// <summary>
        /// 会话时长
        /// </summary>
        public TimeSpan? SessionDuration { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 用户代理
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// 设备信息
        /// </summary>
        public UserLoginDeviceDto DeviceInfo { get; set; } = new();

        /// <summary>
        /// 地理位置
        /// </summary>
        public UserLocationDto? Location { get; set; }

        /// <summary>
        /// 登录方式
        /// </summary>
        public string LoginMethod { get; set; } = string.Empty;

        /// <summary>
        /// 登录状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 失败原因
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// 是否可疑登录
        /// </summary>
        public bool IsSuspicious { get; set; }

        /// <summary>
        /// 风险评分
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// 会话令牌ID
        /// </summary>
        public string? SessionTokenId { get; set; }
    }

    /// <summary>
    /// 用户统计DTO
    /// </summary>
    public class UserStatisticsDto
    {
        /// <summary>
        /// 总登录次数
        /// </summary>
        public int TotalLogins { get; set; }

        /// <summary>
        /// 本月登录次数
        /// </summary>
        public int MonthlyLogins { get; set; }

        /// <summary>
        /// 平均会话时长
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// 最长会话时长
        /// </summary>
        public TimeSpan MaxSessionDuration { get; set; }

        /// <summary>
        /// 内容创建数量
        /// </summary>
        public ContentCreationStatsDto ContentStats { get; set; } = new();

        /// <summary>
        /// 互动统计
        /// </summary>
        public UserInteractionStatsDto InteractionStats { get; set; } = new();

        /// <summary>
        /// 登录模式分析
        /// </summary>
        public UserLoginPatternDto LoginPattern { get; set; } = new();

        /// <summary>
        /// 设备使用统计
        /// </summary>
        public IEnumerable<UserDeviceUsageDto> DeviceUsage { get; set; } = new List<UserDeviceUsageDto>();

        /// <summary>
        /// 地理分布
        /// </summary>
        public IEnumerable<UserLocationStatsDto> LocationStats { get; set; } = new List<UserLocationStatsDto>();

        /// <summary>
        /// 活跃度评分
        /// </summary>
        public double ActivityScore { get; set; }

        /// <summary>
        /// 忠诚度评分
        /// </summary>
        public double LoyaltyScore { get; set; }
    }

    /// <summary>
    /// 用户导入请求DTO
    /// </summary>
    public class UserImportRequestDto
    {
        /// <summary>
        /// 导入格式
        /// </summary>
        public string ImportFormat { get; set; } = "Excel";

        /// <summary>
        /// 文件数据
        /// </summary>
        public string FileData { get; set; } = string.Empty;

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 导入选项
        /// </summary>
        public UserImportOptionsDto Options { get; set; } = new();

        /// <summary>
        /// 字段映射
        /// </summary>
        public Dictionary<string, string> FieldMapping { get; set; } = new();

        /// <summary>
        /// 默认角色IDs
        /// </summary>
        public IEnumerable<Guid> DefaultRoleIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 默认状态
        /// </summary>
        public string DefaultStatus { get; set; } = "Active";

        /// <summary>
        /// 是否发送欢迎邮件
        /// </summary>
        public bool SendWelcomeEmail { get; set; }

        /// <summary>
        /// 密码策略
        /// </summary>
        public string PasswordPolicy { get; set; } = "Generate";
    }

    /// <summary>
    /// 用户导入结果DTO
    /// </summary>
    public class UserImportResultDto
    {
        /// <summary>
        /// 导入ID
        /// </summary>
        public Guid ImportId { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 成功导入数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 跳过数
        /// </summary>
        public int SkippedCount { get; set; }

        /// <summary>
        /// 导入详情
        /// </summary>
        public IEnumerable<UserImportDetailDto> ImportDetails { get; set; } = new List<UserImportDetailDto>();

        /// <summary>
        /// 错误摘要
        /// </summary>
        public IEnumerable<string> ErrorSummary { get; set; } = new List<string>();

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// 导入摘要报告
        /// </summary>
        public string SummaryReport { get; set; } = string.Empty;

        /// <summary>
        /// 生成的密码列表（如果适用）
        /// </summary>
        public IEnumerable<UserPasswordInfoDto> GeneratedPasswords { get; set; } = new List<UserPasswordInfoDto>();
    }

    /// <summary>
    /// 用户导出请求DTO
    /// </summary>
    public class UserExportRequestDto
    {
        /// <summary>
        /// 导出格式
        /// </summary>
        public string ExportFormat { get; set; } = "Excel";

        /// <summary>
        /// 筛选条件
        /// </summary>
        public UserExportFilterDto Filter { get; set; } = new();

        /// <summary>
        /// 导出字段
        /// </summary>
        public IEnumerable<string> ExportFields { get; set; } = new List<string>();

        /// <summary>
        /// 是否包含敏感信息
        /// </summary>
        public bool IncludeSensitiveInfo { get; set; }

        /// <summary>
        /// 是否包含统计数据
        /// </summary>
        public bool IncludeStatistics { get; set; } = true;

        /// <summary>
        /// 是否包含活动历史
        /// </summary>
        public bool IncludeActivityHistory { get; set; }

        /// <summary>
        /// 文件名前缀
        /// </summary>
        public string? FileNamePrefix { get; set; }

        /// <summary>
        /// 是否分组导出
        /// </summary>
        public bool GroupByRole { get; set; }
    }

    /// <summary>
    /// 用户导出结果DTO
    /// </summary>
    public class UserExportResultDto
    {
        /// <summary>
        /// 导出ID
        /// </summary>
        public Guid ExportId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 导出用户数量
        /// </summary>
        public int ExportedUserCount { get; set; }

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// 导出状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 导出摘要
        /// </summary>
        public UserExportSummaryDto Summary { get; set; } = new();
    }

    /// <summary>
    /// 在线用户DTO
    /// </summary>
    public class OnlineUserDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 头像
        /// </summary>
        public string? Avatar { get; set; }

        /// <summary>
        /// 在线状态
        /// </summary>
        public string OnlineStatus { get; set; } = string.Empty;

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// 在线时长
        /// </summary>
        public TimeSpan OnlineDuration { get; set; }

        /// <summary>
        /// 当前页面
        /// </summary>
        public string? CurrentPage { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 设备信息
        /// </summary>
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// 地理位置
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// 连接数量
        /// </summary>
        public int ConnectionCount { get; set; }
    }

    /// <summary>
    /// 消息发送结果DTO
    /// </summary>
    public class MessageSendResultDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 总发送数量
        /// </summary>
        public int TotalSent { get; set; }

        /// <summary>
        /// 成功发送数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败发送数量
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// 发送详情
        /// </summary>
        public IEnumerable<MessageSendDetailDto> SendDetails { get; set; } = new List<MessageSendDetailDto>();

        /// <summary>
        /// 错误信息
        /// </summary>
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 消息ID
        /// </summary>
        public Guid? MessageId { get; set; }

        /// <summary>
        /// 发送时间
        /// </summary>
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// 用户行为分析DTO
    /// </summary>
    public class UserBehaviorAnalysisDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 分析时间范围
        /// </summary>
        public DateRangeDto AnalysisPeriod { get; set; } = new();

        /// <summary>
        /// 活跃度分析
        /// </summary>
        public UserActivityAnalysisDto ActivityAnalysis { get; set; } = new();

        /// <summary>
        /// 内容偏好分析
        /// </summary>
        public UserContentPreferenceDto ContentPreferences { get; set; } = new();

        /// <summary>
        /// 使用模式分析
        /// </summary>
        public UserUsagePatternDto UsagePatterns { get; set; } = new();

        /// <summary>
        /// 互动行为分析
        /// </summary>
        public UserInteractionBehaviorDto InteractionBehavior { get; set; } = new();

        /// <summary>
        /// 风险评估
        /// </summary>
        public UserRiskAssessmentDto RiskAssessment { get; set; } = new();

        /// <summary>
        /// 推荐建议
        /// </summary>
        public IEnumerable<UserRecommendationDto> Recommendations { get; set; } = new List<UserRecommendationDto>();

        /// <summary>
        /// 预测指标
        /// </summary>
        public UserPredictionDto Predictions { get; set; } = new();
    }

    #region 辅助DTO类

    /// <summary>
    /// 用户状态分布DTO
    /// </summary>
    public class UserStatusDistributionDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 用户角色分布DTO
    /// </summary>
    public class UserRoleDistributionDto
    {
        public string RoleName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 用户增长趋势DTO
    /// </summary>
    public class UserGrowthTrendDto
    {
        public DateTime Date { get; set; }
        public int NewUsers { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public double GrowthRate { get; set; }
    }

    /// <summary>
    /// 用户活跃度概览DTO
    /// </summary>
    public class UserActivityOverviewDto
    {
        public int DailyActiveUsers { get; set; }
        public int WeeklyActiveUsers { get; set; }
        public int MonthlyActiveUsers { get; set; }
        public double ActivityRate { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
    }

    /// <summary>
    /// 用户统计DTO
    /// </summary>
    public class UserStatsDto
    {
        public int LoginCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public int PostCount { get; set; }
        public int CommentCount { get; set; }
        public long TotalViews { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
    }

    /// <summary>
    /// 用户位置DTO
    /// </summary>
    public class UserLocationDto
    {
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Timezone { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>
    /// 用户资料DTO
    /// </summary>
    public class UserProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public UserLocationDto? Address { get; set; }
        public IEnumerable<string> Interests { get; set; } = new List<string>();
        public IEnumerable<string> Skills { get; set; } = new List<string>();
        public string? Language { get; set; }
        public string? Timezone { get; set; }
    }

    /// <summary>
    /// 用户安全信息DTO
    /// </summary>
    public class UserSecurityInfoDto
    {
        public DateTime? LastPasswordChange { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? TwoFactorEnabledAt { get; set; }
        public IEnumerable<string> RecoveryCodes { get; set; } = new List<string>();
        public DateTime? LastSecurityAudit { get; set; }
        public string SecurityLevel { get; set; } = string.Empty;
        public IEnumerable<SecurityEventDto> RecentSecurityEvents { get; set; } = new List<SecurityEventDto>();
    }

    /// <summary>
    /// 用户活动统计DTO
    /// </summary>
    public class UserActivityStatsDto
    {
        public DateTime FirstLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public int TotalSessions { get; set; }
        public TimeSpan TotalTimeSpent { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public int PagesVisited { get; set; }
        public IEnumerable<string> MostVisitedPages { get; set; } = new List<string>();
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public IEnumerable<HourlyActivityDto> ActivityByHour { get; set; } = new List<HourlyActivityDto>();
    }

    /// <summary>
    /// 用户权限信息DTO
    /// </summary>
    public class UserPermissionInfoDto
    {
        public IEnumerable<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();
        public IEnumerable<UserPermissionDto> DirectPermissions { get; set; } = new List<UserPermissionDto>();
        public IEnumerable<UserPermissionDto> InheritedPermissions { get; set; } = new List<UserPermissionDto>();
        public IEnumerable<string> PermissionGroups { get; set; } = new List<string>();
        public DateTime? LastPermissionUpdate { get; set; }
        public string PermissionLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户社交账号DTO
    /// </summary>
    public class UserSocialAccountDto
    {
        public string Provider { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? ProfileUrl { get; set; }
        public DateTime LinkedAt { get; set; }
        public DateTime? LastSyncAt { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// 用户设备DTO
    /// </summary>
    public class UserDeviceDto
    {
        public Guid Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsTrusted { get; set; }
        public bool IsActive { get; set; }
        public string? PushToken { get; set; }
        public UserLocationDto? Location { get; set; }
    }

    /// <summary>
    /// 最近用户活动DTO
    /// </summary>
    public class RecentUserActivityDto
    {
        public Guid Id { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = string.Empty;
        public object? Details { get; set; }
    }

    /// <summary>
    /// 用户偏好设置DTO
    /// </summary>
    public class UserPreferencesDto
    {
        public string Language { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public Dictionary<string, bool> NotificationSettings { get; set; } = new();
        public Dictionary<string, string> PrivacySettings { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 创建用户资料DTO
    /// </summary>
    public class CreateUserProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public string? Language { get; set; }
        public string? Timezone { get; set; }
    }

    /// <summary>
    /// 更新用户资料DTO
    /// </summary>
    public class UpdateUserProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Bio { get; set; }
        public string? Website { get; set; }
        public string? Company { get; set; }
        public string? JobTitle { get; set; }
        public string? Language { get; set; }
        public string? Timezone { get; set; }
    }

    /// <summary>
    /// 用户登录设备DTO
    /// </summary>
    public class UserLoginDeviceDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string? DeviceModel { get; set; }
        public string? AppVersion { get; set; }
        public bool IsMobile { get; set; }
        public string? ScreenResolution { get; set; }
    }

    /// <summary>
    /// 内容创建统计DTO
    /// </summary>
    public class ContentCreationStatsDto
    {
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int DraftPosts { get; set; }
        public int PublishedPosts { get; set; }
        public long TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }
        public DateTime? LastContentCreated { get; set; }
        public double AveragePostLength { get; set; }
        public IEnumerable<string> PreferredCategories { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户互动统计DTO
    /// </summary>
    public class UserInteractionStatsDto
    {
        public int LikesGiven { get; set; }
        public int LikesReceived { get; set; }
        public int CommentsGiven { get; set; }
        public int CommentsReceived { get; set; }
        public int SharesGiven { get; set; }
        public int SharesReceived { get; set; }
        public int FollowersGained { get; set; }
        public int FollowingCount { get; set; }
        public double EngagementRate { get; set; }
        public double ResponseRate { get; set; }
    }

    /// <summary>
    /// 用户登录模式DTO
    /// </summary>
    public class UserLoginPatternDto
    {
        public IEnumerable<int> PreferredHours { get; set; } = new List<int>();
        public IEnumerable<string> PreferredDays { get; set; } = new List<string>();
        public TimeSpan AverageSessionLength { get; set; }
        public int LoginFrequency { get; set; }
        public string LoginRegularity { get; set; } = string.Empty;
        public IEnumerable<string> CommonLocations { get; set; } = new List<string>();
        public IEnumerable<string> CommonDevices { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户设备使用DTO
    /// </summary>
    public class UserDeviceUsageDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public TimeSpan TotalTime { get; set; }
        public double UsagePercentage { get; set; }
        public DateTime LastUsed { get; set; }
    }

    /// <summary>
    /// 用户位置统计DTO
    /// </summary>
    public class UserLocationStatsDto
    {
        public string Location { get; set; } = string.Empty;
        public int SessionCount { get; set; }
        public TimeSpan TotalTime { get; set; }
        public double UsagePercentage { get; set; }
        public DateTime LastAccess { get; set; }
    }

    /// <summary>
    /// 用户导入选项DTO
    /// </summary>
    public class UserImportOptionsDto
    {
        public bool ValidateEmails { get; set; } = true;
        public bool CheckDuplicates { get; set; } = true;
        public bool GeneratePasswords { get; set; } = true;
        public bool RequireEmailVerification { get; set; } = true;
        public bool CreateProfiles { get; set; } = true;
        public bool AssignDefaultRoles { get; set; } = true;
        public bool SendWelcomeEmails { get; set; }
        public bool LogImportActivity { get; set; } = true;
    }

    /// <summary>
    /// 用户导入详情DTO
    /// </summary>
    public class UserImportDetailDto
    {
        public int RowNumber { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool Success { get; set; }
        public Guid? UserId { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
        public string? GeneratedPassword { get; set; }
    }

    /// <summary>
    /// 用户密码信息DTO
    /// </summary>
    public class UserPasswordInfoDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string GeneratedPassword { get; set; } = string.Empty;
        public bool PasswordSent { get; set; }
    }

    /// <summary>
    /// 用户导出筛选DTO
    /// </summary>
    public class UserExportFilterDto
    {
        public IEnumerable<string> Status { get; set; } = new List<string>();
        public IEnumerable<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// 角色过滤（强类型枚举）
        /// </summary>
        public IEnumerable<UserRole> RoleEnums { get; set; } = new List<UserRole>();
        public DateRangeDto? RegistrationDateRange { get; set; }
        public DateRangeDto? LastLoginDateRange { get; set; }
        public string? SearchTerm { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
        public bool IncludeDeleted { get; set; }
    }

    /// <summary>
    /// 用户导出摘要DTO
    /// </summary>
    public class UserExportSummaryDto
    {
        public int TotalUsers { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
        public DateTime? OldestRegistration { get; set; }
        public DateTime? NewestRegistration { get; set; }
        public double AverageSessionCount { get; set; }
    }

    /// <summary>
    /// 消息发送详情DTO
    /// </summary>
    public class MessageSendDetailDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// 用户活动分析DTO
    /// </summary>
    public class UserActivityAnalysisDto
    {
        public int TotalSessions { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
        public double ActivityScore { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public IEnumerable<DailyActivityDto> DailyActivity { get; set; } = new List<DailyActivityDto>();
        public IEnumerable<string> MostActiveHours { get; set; } = new List<string>();
        public IEnumerable<string> MostActiveDays { get; set; } = new List<string>();
        public double TrendDirection { get; set; }
    }

    /// <summary>
    /// 用户内容偏好DTO
    /// </summary>
    public class UserContentPreferenceDto
    {
        public IEnumerable<string> PreferredCategories { get; set; } = new List<string>();
        public IEnumerable<string> PreferredTags { get; set; } = new List<string>();
        public IEnumerable<string> PreferredAuthors { get; set; } = new List<string>();
        public string PreferredContentType { get; set; } = string.Empty;
        public double ReadingSpeed { get; set; }
        public TimeSpan AverageReadingTime { get; set; }
        public IEnumerable<string> EngagementPatterns { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户使用模式DTO
    /// </summary>
    public class UserUsagePatternDto
    {
        public string PrimaryUsageTime { get; set; } = string.Empty;
        public IEnumerable<string> PreferredDevices { get; set; } = new List<string>();
        public string NavigationPattern { get; set; } = string.Empty;
        public double SessionDurationPattern { get; set; }
        public string EngagementStyle { get; set; } = string.Empty;
        public IEnumerable<string> FeatureUsage { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户互动行为DTO
    /// </summary>
    public class UserInteractionBehaviorDto
    {
        public double CommentRatio { get; set; }
        public double ShareRatio { get; set; }
        public double LikeRatio { get; set; }
        public string InteractionStyle { get; set; } = string.Empty;
        public double ResponseTime { get; set; }
        public string SocialBehavior { get; set; } = string.Empty;
        public double InfluenceScore { get; set; }
    }

    /// <summary>
    /// 用户风险评估DTO
    /// </summary>
    public class UserRiskAssessmentDto
    {
        public double OverallRiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public IEnumerable<string> RiskFactors { get; set; } = new List<string>();
        public IEnumerable<string> SecurityConcerns { get; set; } = new List<string>();
        public double FraudProbability { get; set; }
        public double AccountCompromiseRisk { get; set; }
        public IEnumerable<string> RecommendedActions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户推荐DTO
    /// </summary>
    public class UserRecommendationDto
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Priority { get; set; }
        public double Confidence { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 用户预测DTO
    /// </summary>
    public class UserPredictionDto
    {
        public double ChurnProbability { get; set; }
        public double EngagementTrend { get; set; }
        public double LoyaltyScore { get; set; }
        public string PredictedBehavior { get; set; } = string.Empty;
        public double ValueScore { get; set; }
        public IEnumerable<string> FutureInterests { get; set; } = new List<string>();
        public DateTime? NextExpectedActivity { get; set; }
    }

    /// <summary>
    /// 安全事件DTO
    /// </summary>
    public class SecurityEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 小时活动DTO
    /// </summary>
    public class HourlyActivityDto
    {
        public int Hour { get; set; }
        public int ActivityCount { get; set; }
        public double ActivityPercentage { get; set; }
    }

    /// <summary>
    /// 日活动DTO
    /// </summary>
    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
        public int SessionCount { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public int ActionsPerformed { get; set; }
    }

    #endregion
}