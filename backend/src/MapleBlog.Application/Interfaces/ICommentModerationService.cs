using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Moderation;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Interfaces;

/// <summary>
/// 评论审核服务接口
/// </summary>
public interface ICommentModerationService
{
    #region 审核队列管理

    /// <summary>
    /// 获取审核队列
    /// </summary>
    /// <param name="query">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核队列分页结果</returns>
    Task<CommentPagedResultDto<CommentModerationDto>> GetModerationQueueAsync(CommentModerationQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个待审核评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核评论详情</returns>
    Task<CommentModerationDto?> GetModerationCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审核统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核统计</returns>
    Task<CommentModerationStatsDto> GetModerationStatsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 审核操作

    /// <summary>
    /// 执行审核操作
    /// </summary>
    /// <param name="request">审核操作请求</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<ModerationOperationResult> ModerateCommentsAsync(CommentModerationActionDto request, Guid moderatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批准评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> ApproveCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 拒绝评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="reason">拒绝原因</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RejectCommentAsync(Guid commentId, Guid moderatorId, ModerationAction reason, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 隐藏评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HideCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复评论
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> RestoreCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记为垃圾信息
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> MarkAsSpamAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default);

    #endregion

    #region 举报管理

    /// <summary>
    /// 获取举报列表
    /// </summary>
    /// <param name="status">举报状态过滤</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报分页结果</returns>
    Task<CommentPagedResultDto<CommentReportDto>> GetReportsAsync(CommentReportStatus[] status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个举报详情
    /// </summary>
    /// <param name="reportId">举报ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>举报详情</returns>
    Task<CommentReportDto?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理举报
    /// </summary>
    /// <param name="request">处理举报请求</param>
    /// <param name="processorId">处理者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理结果</returns>
    Task<ReportProcessResult> ProcessReportsAsync(CommentReportProcessDto request, Guid processorId, CancellationToken cancellationToken = default);

    #endregion

    #region AI审核

    /// <summary>
    /// 重新进行AI审核
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI审核结果</returns>
    Task<AIContentModerationResult> RerunAIModerationAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量AI审核
    /// </summary>
    /// <param name="commentIds">评论ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量审核结果</returns>
    Task<BatchModerationResult> BatchAIModerationAsync(IList<Guid> commentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置AI审核阈值
    /// </summary>
    /// <param name="settings">审核设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateModerationSettingsAsync(ModerationSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取AI审核设置
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核设置</returns>
    Task<ModerationSettings> GetModerationSettingsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region 审核历史

    /// <summary>
    /// 获取评论审核历史
    /// </summary>
    /// <param name="commentId">评论ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审核历史</returns>
    Task<IList<CommentModerationHistoryDto>> GetModerationHistoryAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审核者操作历史
    /// </summary>
    /// <param name="moderatorId">审核者ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作历史</returns>
    Task<CommentPagedResultDto<CommentModerationHistoryDto>> GetModeratorHistoryAsync(Guid moderatorId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    #endregion

    #region 用户管理

    /// <summary>
    /// 获取用户审核统计
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户审核统计</returns>
    Task<UserModerationStatsDto> GetUserModerationStatsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置用户信任度
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="trustScore">信任度评分</param>
    /// <param name="reason">设置原因</param>
    /// <param name="operatorId">操作者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetUserTrustScoreAsync(Guid userId, double trustScore, string reason, Guid operatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户信任度历史
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>信任度历史</returns>
    Task<IList<UserTrustScoreHistoryDto>> GetUserTrustScoreHistoryAsync(Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region 自动化规则

    /// <summary>
    /// 创建自动审核规则
    /// </summary>
    /// <param name="rule">审核规则</param>
    /// <param name="creatorId">创建者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的规则ID</returns>
    Task<Guid> CreateAutoModerationRuleAsync(AutoModerationRuleDto rule, Guid creatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新自动审核规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="rule">更新后的规则</param>
    /// <param name="updaterId">更新者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateAutoModerationRuleAsync(Guid ruleId, AutoModerationRuleDto rule, Guid updaterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除自动审核规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="deleterId">删除者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAutoModerationRuleAsync(Guid ruleId, Guid deleterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取自动审核规则列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>规则列表</returns>
    Task<IList<AutoModerationRuleDto>> GetAutoModerationRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试自动审核规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="testContent">测试内容</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>测试结果</returns>
    Task<RuleTestResult> TestAutoModerationRuleAsync(Guid ruleId, string testContent, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// 审核操作结果
/// </summary>
public record ModerationOperationResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 处理的评论数量
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// 成功处理的评论数量
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败的评论数量
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 详细结果
    /// </summary>
    public IDictionary<Guid, string> Details { get; init; } = new Dictionary<Guid, string>();
}

/// <summary>
/// 举报处理结果
/// </summary>
public record ReportProcessResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 处理的举报数量
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// 成功处理的举报数量
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败的举报数量
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 详细结果
    /// </summary>
    public IDictionary<Guid, string> Details { get; init; } = new Dictionary<Guid, string>();
}

/// <summary>
/// 批量审核结果
/// </summary>
public record BatchModerationResult
{
    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// 审核结果统计
    /// </summary>
    public IDictionary<ModerationResult, int> ResultStats { get; init; } = new Dictionary<ModerationResult, int>();

    /// <summary>
    /// 详细结果
    /// </summary>
    public IDictionary<Guid, AIContentModerationResult> Details { get; init; } = new Dictionary<Guid, AIContentModerationResult>();
}

/// <summary>
/// 规则测试结果
/// </summary>
public record RuleTestResult
{
    /// <summary>
    /// 是否匹配规则
    /// </summary>
    public bool Matched { get; init; }

    /// <summary>
    /// 匹配的条件
    /// </summary>
    public IList<string> MatchedConditions { get; init; } = new List<string>();

    /// <summary>
    /// 建议的动作
    /// </summary>
    public ModerationAction SuggestedAction { get; init; }

    /// <summary>
    /// 置信度评分
    /// </summary>
    public double ConfidenceScore { get; init; }

    /// <summary>
    /// 解释
    /// </summary>
    public string? Explanation { get; init; }
}