using MapleBlog.Application.DTOs.Moderation;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// AI内容审核服务接口
    /// </summary>
    public interface IAIContentModerationService
    {
        /// <summary>
        /// 对内容进行AI审核
        /// </summary>
        Task<AIContentModerationResult> ModerateContentAsync(string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量内容审核
        /// </summary>
        Task<IEnumerable<AIContentModerationResult>> ModerateBatchAsync(IEnumerable<string> contents, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查内容是否包含敏感词
        /// </summary>
        Task<bool> ContainsSensitiveContentAsync(string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取内容风险等级
        /// </summary>
        Task<RiskLevel> GetContentRiskLevelAsync(string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// 训练AI审核模型
        /// </summary>
        Task TrainModelAsync(IEnumerable<(string Content, bool IsApproved)> trainingData, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新敏感词库
        /// </summary>
        Task UpdateSensitiveWordsAsync(IEnumerable<string> words, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取审核统计信息
        /// </summary>
        Task<ModerationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 审核评论内容
        /// </summary>
        /// <param name="content">评论内容</param>
        /// <param name="authorId">作者ID</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>审核结果</returns>
        Task<AIContentModerationResult> ModerateCommentAsync(
            CommentContent content,
            Guid authorId,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查是否需要人工审核
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="moderationResult">AI审核结果</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否需要人工审核</returns>
        Task<bool> RequiresHumanReviewAsync(string content, AIContentModerationResult moderationResult, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 审核统计信息
    /// </summary>
    public class ModerationStatistics
    {
        public int TotalModerated { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public double AccuracyRate { get; set; }
        public Dictionary<string, int> IssueTypes { get; set; } = new();
        public Dictionary<RiskLevel, int> RiskLevels { get; set; } = new();
    }
}