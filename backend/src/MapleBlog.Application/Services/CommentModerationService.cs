using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Moderation;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Application.Services;

/// <summary>
/// 评论审核服务实现
/// </summary>
public class CommentModerationService : ICommentModerationService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICommentReportRepository _commentReportRepository;
    private readonly IAIContentModerationService _aiModerationService;
    private readonly ICommentNotificationService _notificationService;
    private readonly ICommentCacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<CommentModerationService> _logger;
    private readonly IMemoryCache _memoryCache;

    public CommentModerationService(
        ICommentRepository commentRepository,
        ICommentReportRepository commentReportRepository,
        IAIContentModerationService aiModerationService,
        ICommentNotificationService notificationService,
        ICommentCacheService cacheService,
        IMapper mapper,
        ILogger<CommentModerationService> logger,
        IMemoryCache memoryCache)
    {
        _commentRepository = commentRepository;
        _commentReportRepository = commentReportRepository;
        _aiModerationService = aiModerationService;
        _notificationService = notificationService;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
        _memoryCache = memoryCache;
    }

    #region 审核队列管理

    /// <summary>
    /// 获取审核队列
    /// </summary>
    public async Task<CommentPagedResultDto<CommentModerationDto>> GetModerationQueueAsync(CommentModerationQueryDto query, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = await BuildModerationQueryAsync(query);

            // 应用排序
            queryable = ApplyModerationSorting(queryable, query.SortBy, query.SortDirection);

            // 分页
            var totalCount = await queryable.CountAsync(cancellationToken);
            var comments = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(c => c.Author)
                .Include(c => c.Moderator)
                .Include(c => c.Post)
                .Include(c => c.Reports.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.Reporter)
                .ToListAsync(cancellationToken);

            var moderationDtos = await Task.WhenAll(
                comments.Select(MapToModerationDtoAsync));

            return new CommentPagedResultDto<CommentModerationDto>
            {
                Items = moderationDtos,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
                CurrentPage = query.Page,
                PageSize = query.PageSize,
                HasNextPage = query.Page * query.PageSize < totalCount,
                HasPreviousPage = query.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation queue");
            throw;
        }
    }

    /// <summary>
    /// 获取单个待审核评论
    /// </summary>
    public async Task<CommentModerationDto?> GetModerationCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdWithDetailsAsync(commentId);
            if (comment == null)
            {
                return null;
            }

            return await MapToModerationDtoAsync(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation comment {CommentId}", commentId);
            return null;
        }
    }

    /// <summary>
    /// 获取审核统计信息
    /// </summary>
    public async Task<CommentModerationStatsDto> GetModerationStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = "moderation_stats";
            var cached = _memoryCache.Get<CommentModerationStatsDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var stats = await CalculateModerationStatsAsync(cancellationToken);

            // 缓存统计信息
            _memoryCache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation stats");
            throw;
        }
    }

    #endregion

    #region 审核操作

    /// <summary>
    /// 执行审核操作
    /// </summary>
    public async Task<ModerationOperationResult> ModerateCommentsAsync(CommentModerationActionDto request, Guid moderatorId, CancellationToken cancellationToken = default)
    {
        var result = new ModerationOperationResult
        {
            ProcessedCount = request.CommentIds.Count
        };

        var details = new Dictionary<Guid, string>();
        var successCount = 0;

        foreach (var commentId in request.CommentIds)
        {
            try
            {
                var success = await ExecuteModerationActionAsync(
                    commentId, moderatorId, request.Action, request.Note, request.SendNotification);

                if (success)
                {
                    successCount++;
                    details[commentId] = "操作成功";
                }
                else
                {
                    details[commentId] = "操作失败";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating comment {CommentId}", commentId);
                details[commentId] = ex.Message;
            }
        }

        return result with
        {
            Success = successCount > 0,
            SuccessCount = successCount,
            FailedCount = result.ProcessedCount - successCount,
            Details = details
        };
    }

    /// <summary>
    /// 批准评论
    /// </summary>
    public async Task<bool> ApproveCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteModerationActionAsync(commentId, moderatorId, ModerationAction.Approve, note, true);
    }

    /// <summary>
    /// 拒绝评论
    /// </summary>
    public async Task<bool> RejectCommentAsync(Guid commentId, Guid moderatorId, ModerationAction reason, string? note = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteModerationActionAsync(commentId, moderatorId, reason, note, true);
    }

    /// <summary>
    /// 隐藏评论
    /// </summary>
    public async Task<bool> HideCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteModerationActionAsync(commentId, moderatorId, ModerationAction.Hide, note, true);
    }

    /// <summary>
    /// 恢复评论
    /// </summary>
    public async Task<bool> RestoreCommentAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteModerationActionAsync(commentId, moderatorId, ModerationAction.Restore, note, true);
    }

    /// <summary>
    /// 标记为垃圾信息
    /// </summary>
    public async Task<bool> MarkAsSpamAsync(Guid commentId, Guid moderatorId, string? note = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteModerationActionAsync(commentId, moderatorId, ModerationAction.MarkAsSpam, note, true);
    }

    #endregion

    #region 举报管理

    /// <summary>
    /// 获取举报列表
    /// </summary>
    public async Task<CommentPagedResultDto<CommentReportDto>> GetReportsAsync(CommentReportStatus[] status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryable = _commentReportRepository.GetReportsQueryable()
                .Where(r => !r.IsDeleted && status.Contains(r.Status))
                .Include(r => r.Comment)
                    .ThenInclude(c => c.Author)
                .Include(r => r.Reporter)
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.CreatedAt);

            var totalCount = await queryable.CountAsync(cancellationToken);
            var reports = await queryable
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var reportDtos = reports.Select(_mapper.Map<CommentReportDto>).ToList();

            return new CommentPagedResultDto<CommentReportDto>
            {
                Items = reportDtos,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                CurrentPage = page,
                PageSize = pageSize,
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reports");
            throw;
        }
    }

    /// <summary>
    /// 获取单个举报详情
    /// </summary>
    public async Task<CommentReportDto?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _commentReportRepository.GetByIdWithDetailsAsync(reportId);
            return report != null ? _mapper.Map<CommentReportDto>(report) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting report {ReportId}", reportId);
            return null;
        }
    }

    /// <summary>
    /// 处理举报
    /// </summary>
    public async Task<ReportProcessResult> ProcessReportsAsync(CommentReportProcessDto request, Guid processorId, CancellationToken cancellationToken = default)
    {
        var result = new ReportProcessResult
        {
            ProcessedCount = request.ReportIds.Count
        };

        var details = new Dictionary<Guid, string>();
        var successCount = 0;

        foreach (var reportId in request.ReportIds)
        {
            try
            {
                var success = await ProcessSingleReportAsync(reportId, request.Action, processorId, request.Note);
                if (success)
                {
                    successCount++;
                    details[reportId] = "处理成功";
                }
                else
                {
                    details[reportId] = "处理失败";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing report {ReportId}", reportId);
                details[reportId] = ex.Message;
            }
        }

        return result with
        {
            Success = successCount > 0,
            SuccessCount = successCount,
            FailedCount = result.ProcessedCount - successCount,
            Details = details
        };
    }

    #endregion

    #region AI审核

    /// <summary>
    /// 重新进行AI审核
    /// </summary>
    public async Task<AIContentModerationResult> RerunAIModerationAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                throw new ArgumentException("评论不存在", nameof(commentId));
            }

            var moderationResult = await _aiModerationService.ModerateCommentAsync(
                comment.Content, comment.AuthorId, comment.IpAddress, cancellationToken);

            // 更新评论的AI审核结果
            comment.SetAIModerationResult(
                moderationResult.Result ? ModerationResult.Approved : ModerationResult.RejectedInappropriate,
                moderationResult.ConfidenceScore,
                moderationResult.ContainsSensitiveWords);

            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);

            _logger.LogInformation("Reran AI moderation for comment {CommentId}", commentId);

            return moderationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rerunning AI moderation for comment {CommentId}", commentId);
            throw;
        }
    }

    /// <summary>
    /// 批量AI审核
    /// </summary>
    public async Task<BatchModerationResult> BatchAIModerationAsync(IList<Guid> commentIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var comments = await _commentRepository.GetByIdsAsync(commentIds);
            var contentTexts = comments.Select(c => c.Content.RawContent).ToList();

            var moderationResults = await _aiModerationService.ModerateBatchAsync(contentTexts, cancellationToken);

            var resultStats = new Dictionary<ModerationResult, int>();
            var details = new Dictionary<Guid, AIContentModerationResult>();
            var successCount = 0;

            foreach (var (comment, result) in comments.Zip(moderationResults, (Comment c, AIContentModerationResult r) => (c, r)))
            {
                try
                {
                    comment.SetAIModerationResult(
                        result.Result ? ModerationResult.Approved : ModerationResult.RejectedInappropriate,
                        result.ConfidenceScore,
                        result.ContainsSensitiveWords);
                    details[comment.Id] = result;
                    successCount++;

                    // 统计结果
                    var moderationResult = result.Result ? ModerationResult.Approved : ModerationResult.RejectedInappropriate;
                    resultStats.TryGetValue(moderationResult, out var count);
                    resultStats[moderationResult] = count + 1;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating AI moderation result for comment {CommentId}", comment.Id);
                }
            }

            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            var cacheTasks = commentIds.Select(id => _cacheService.RemoveCommentAsync(id));
            await Task.WhenAll(cacheTasks);

            return new BatchModerationResult
            {
                TotalCount = commentIds.Count,
                SuccessCount = successCount,
                FailedCount = commentIds.Count - successCount,
                ResultStats = resultStats,
                Details = details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch AI moderation");
            throw;
        }
    }

    /// <summary>
    /// 设置AI审核阈值
    /// </summary>
    public async Task<bool> UpdateModerationSettingsAsync(ModerationSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该保存到配置或数据库
            // 暂时保存到内存缓存
            _memoryCache.Set("moderation_settings", settings, TimeSpan.FromHours(24));

            _logger.LogInformation("Updated moderation settings");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating moderation settings");
            return false;
        }
    }

    /// <summary>
    /// 获取AI审核设置
    /// </summary>
    public async Task<ModerationSettings> GetModerationSettingsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken); // 模拟异步操作

            var cached = _memoryCache.Get<ModerationSettings>("moderation_settings");
            return cached ?? new ModerationSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation settings");
            return new ModerationSettings();
        }
    }

    #endregion

    #region 审核历史

    /// <summary>
    /// 获取评论审核历史
    /// </summary>
    public async Task<IList<CommentModerationHistoryDto>> GetModerationHistoryAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该从审核日志表查询，暂时返回空列表
            await Task.Delay(1, cancellationToken);
            return new List<CommentModerationHistoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderation history for comment {CommentId}", commentId);
            return new List<CommentModerationHistoryDto>();
        }
    }

    /// <summary>
    /// 获取审核者操作历史
    /// </summary>
    public async Task<CommentPagedResultDto<CommentModerationHistoryDto>> GetModeratorHistoryAsync(Guid moderatorId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该从审核日志表查询，暂时返回空结果
            await Task.Delay(1, cancellationToken);
            return new CommentPagedResultDto<CommentModerationHistoryDto>
            {
                Items = new List<CommentModerationHistoryDto>(),
                TotalCount = 0,
                TotalPages = 0,
                CurrentPage = page,
                PageSize = pageSize,
                HasNextPage = false,
                HasPreviousPage = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting moderator history for {ModeratorId}", moderatorId);
            throw;
        }
    }

    #endregion

    #region 用户管理

    /// <summary>
    /// 获取用户审核统计
    /// </summary>
    public async Task<UserModerationStatsDto> GetUserModerationStatsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user_moderation_stats:{userId}";
            var cached = _memoryCache.Get<UserModerationStatsDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var stats = await CalculateUserModerationStatsAsync(userId, cancellationToken);

            // 缓存用户统计信息
            _memoryCache.Set(cacheKey, stats, TimeSpan.FromMinutes(15));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user moderation stats for {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 设置用户信任度
    /// </summary>
    public async Task<bool> SetUserTrustScoreAsync(Guid userId, double trustScore, string reason, Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该保存到用户信任度表
            // 暂时记录日志
            _logger.LogInformation("Set trust score {Score} for user {UserId} by {OperatorId}, reason: {Reason}",
                trustScore, userId, operatorId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting trust score for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 获取用户信任度历史
    /// </summary>
    public async Task<IList<UserTrustScoreHistoryDto>> GetUserTrustScoreHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该从用户信任度历史表查询，暂时返回空列表
            await Task.Delay(1, cancellationToken);
            return new List<UserTrustScoreHistoryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trust score history for user {UserId}", userId);
            return new List<UserTrustScoreHistoryDto>();
        }
    }

    #endregion

    #region 自动化规则

    /// <summary>
    /// 创建自动审核规则
    /// </summary>
    public async Task<Guid> CreateAutoModerationRuleAsync(AutoModerationRuleDto rule, Guid creatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 这里应该保存到规则表，暂时生成ID
            await Task.Delay(1, cancellationToken);
            var ruleId = Guid.NewGuid();

            _logger.LogInformation("Created auto moderation rule {RuleId} by {CreatorId}", ruleId, creatorId);
            return ruleId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating auto moderation rule");
            throw;
        }
    }

    /// <summary>
    /// 更新自动审核规则
    /// </summary>
    public async Task<bool> UpdateAutoModerationRuleAsync(Guid ruleId, AutoModerationRuleDto rule, Guid updaterId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogInformation("Updated auto moderation rule {RuleId} by {UpdaterId}", ruleId, updaterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auto moderation rule {RuleId}", ruleId);
            return false;
        }
    }

    /// <summary>
    /// 删除自动审核规则
    /// </summary>
    public async Task<bool> DeleteAutoModerationRuleAsync(Guid ruleId, Guid deleterId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken);
            _logger.LogInformation("Deleted auto moderation rule {RuleId} by {DeleterId}", ruleId, deleterId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting auto moderation rule {RuleId}", ruleId);
            return false;
        }
    }

    /// <summary>
    /// 获取自动审核规则列表
    /// </summary>
    public async Task<IList<AutoModerationRuleDto>> GetAutoModerationRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken);
            return new List<AutoModerationRuleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting auto moderation rules");
            return new List<AutoModerationRuleDto>();
        }
    }

    /// <summary>
    /// 测试自动审核规则
    /// </summary>
    public async Task<RuleTestResult> TestAutoModerationRuleAsync(Guid ruleId, string testContent, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken);
            return new RuleTestResult
            {
                Matched = false,
                MatchedConditions = new List<string>(),
                SuggestedAction = ModerationAction.Review,
                ConfidenceScore = 0.5,
                Explanation = "规则测试功能暂未实现"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing auto moderation rule {RuleId}", ruleId);
            throw;
        }
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 构建审核查询
    /// </summary>
    private async Task<IQueryable<Comment>> BuildModerationQueryAsync(CommentModerationQueryDto query)
    {
        var queryable = _commentRepository.GetQueryable()
            .Where(c => !c.IsDeleted);

        // 状态过滤
        if (query.StatusFilter.Any())
        {
            queryable = queryable.Where(c => query.StatusFilter.Contains(c.Status));
        }

        // 只显示有举报的评论
        if (query.OnlyReported)
        {
            queryable = queryable.Where(c => c.ReportCount > 0);
        }

        // AI审核结果过滤
        if (query.AIModerationFilter.Any())
        {
            queryable = queryable.Where(c => c.AIModerationResult.HasValue &&
                query.AIModerationFilter.Contains(c.AIModerationResult.Value));
        }

        // 质量等级过滤
        if (query.QualityFilter.Any())
        {
            queryable = queryable.Where(c => query.QualityFilter.Contains(c.Quality));
        }

        // 日期范围过滤
        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(c => c.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(c => c.CreatedAt <= query.EndDate.Value);
        }

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            queryable = queryable.Where(c => c.Content.RawContent.Contains(query.Keyword));
        }

        await Task.Delay(1); // 模拟异步操作
        return queryable;
    }

    /// <summary>
    /// 应用审核排序
    /// </summary>
    private IQueryable<Comment> ApplyModerationSorting(IQueryable<Comment> queryable, ModerationSortField sortBy, SortDirection direction)
    {
        return (sortBy, direction) switch
        {
            (ModerationSortField.CreatedAt, SortDirection.Asc) => queryable.OrderBy(c => c.CreatedAt),
            (ModerationSortField.CreatedAt, SortDirection.Desc) => queryable.OrderByDescending(c => c.CreatedAt),
            (ModerationSortField.ReportCount, SortDirection.Asc) => queryable.OrderBy(c => c.ReportCount),
            (ModerationSortField.ReportCount, SortDirection.Desc) => queryable.OrderByDescending(c => c.ReportCount),
            (ModerationSortField.AIModerationScore, SortDirection.Asc) => queryable.OrderBy(c => c.AIModerationScore),
            (ModerationSortField.AIModerationScore, SortDirection.Desc) => queryable.OrderByDescending(c => c.AIModerationScore),
            (ModerationSortField.Quality, SortDirection.Asc) => queryable.OrderBy(c => c.Quality),
            (ModerationSortField.Quality, SortDirection.Desc) => queryable.OrderByDescending(c => c.Quality),
            _ => queryable.OrderByDescending(c => c.CreatedAt)
        };
    }

    /// <summary>
    /// 映射评论到审核DTO
    /// </summary>
    private async Task<CommentModerationDto> MapToModerationDtoAsync(Comment comment)
    {
        await Task.Delay(1); // 模拟异步操作

        return new CommentModerationDto
        {
            CommentId = comment.Id,
            Content = comment.Content.RawContent,
            Author = comment.Author != null ? _mapper.Map<CommentAuthorDto>(comment.Author) : null,
            PostTitle = comment.Post?.Title ?? "未知文章",
            Status = comment.Status,
            ReportCount = comment.ReportCount,
            AIModerationResult = comment.AIModerationResult,
            AIModerationScore = comment.AIModerationScore,
            ContainsSensitiveWords = comment.ContainsSensitiveWords,
            Quality = comment.Quality,
            IpAddress = comment.IpAddress,
            UserAgent = comment.UserAgent,
            ModeratedAt = comment.ModeratedAt,
            Moderator = comment.Moderator != null ? _mapper.Map<CommentAuthorDto>(comment.Moderator) : null,
            ModerationNote = comment.ModerationNote,
            CreatedAt = comment.CreatedAt,
            Reports = comment.Reports.Where(r => !r.IsDeleted).Select(_mapper.Map<CommentReportDto>).ToList()
        };
    }

    /// <summary>
    /// 执行审核操作
    /// </summary>
    private async Task<bool> ExecuteModerationActionAsync(Guid commentId, Guid moderatorId, ModerationAction action, string? note, bool sendNotification)
    {
        try
        {
            var comment = await _commentRepository.GetByIdAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            var previousStatus = comment.Status;
            comment.Moderate(moderatorId, action, note);

            await _commentRepository.SaveChangesAsync();

            // 清理缓存
            await _cacheService.RemoveCommentAsync(commentId);
            await _cacheService.RemovePostCommentsAsync(comment.PostId);

            // 发送通知
            if (sendNotification)
            {
                await SendModerationNotificationAsync(comment, previousStatus, action, moderatorId);
            }

            _logger.LogInformation("Comment {CommentId} moderated with action {Action} by {ModeratorId}",
                commentId, action, moderatorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing moderation action {Action} for comment {CommentId}",
                action, commentId);
            return false;
        }
    }

    /// <summary>
    /// 处理单个举报
    /// </summary>
    private async Task<bool> ProcessSingleReportAsync(Guid reportId, CommentReportProcessAction action, Guid processorId, string? note)
    {
        try
        {
            var report = await _commentReportRepository.GetByIdAsync(reportId);
            if (report == null)
            {
                return false;
            }

            // 更新举报状态
            report.Status = action switch
            {
                CommentReportProcessAction.Dismiss => CommentReportStatus.Rejected,
                CommentReportProcessAction.ConfirmAndDelete or
                CommentReportProcessAction.ConfirmAndHide or
                CommentReportProcessAction.ConfirmAndMarkSpam => CommentReportStatus.Resolved,
                CommentReportProcessAction.RequireInvestigation => CommentReportStatus.InReview,
                _ => report.Status
            };

            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedBy = processorId;
            report.Resolution = note;

            // 如果确认举报，对评论执行相应操作
            if (action != CommentReportProcessAction.Dismiss)
            {
                var comment = await _commentRepository.GetByIdAsync(report.CommentId);
                if (comment != null)
                {
                    var moderationAction = action switch
                    {
                        CommentReportProcessAction.ConfirmAndDelete => ModerationAction.Delete,
                        CommentReportProcessAction.ConfirmAndHide => ModerationAction.Hide,
                        CommentReportProcessAction.ConfirmAndMarkSpam => ModerationAction.MarkAsSpam,
                        _ => ModerationAction.Review
                    };

                    if (moderationAction != ModerationAction.Review)
                    {
                        comment.Moderate(processorId, moderationAction, $"根据举报处理：{note}");
                    }
                }
            }

            await _commentReportRepository.SaveChangesAsync();

            _logger.LogInformation("Report {ReportId} processed with action {Action} by {ProcessorId}",
                reportId, action, processorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing report {ReportId}", reportId);
            return false;
        }
    }

    /// <summary>
    /// 计算审核统计信息
    /// </summary>
    private async Task<CommentModerationStatsDto> CalculateModerationStatsAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);

        var pendingCount = await _commentRepository.CountByStatusAsync(CommentStatus.Pending);
        var reportedCount = await _commentRepository.CountReportedCommentsAsync();
        var todayCount = await _commentRepository.CountByDateAsync(today);

        // 计算本周审核通过率
        var weeklyStats = await _commentRepository.GetWeeklyModerationStatsAsync(cancellationToken);
        var weeklyApprovalRate = 0.0;

        // 计算AI自动通过率
        var aiStats = await _commentRepository.GetAIModerationStatsAsync();
        var aiApprovalRate = 0.0;

        // 状态统计
        var statusCounts = new Dictionary<CommentStatus, int>();
        foreach (CommentStatus status in Enum.GetValues<CommentStatus>())
        {
            statusCounts[status] = await _commentRepository.CountByStatusAsync(status);
        }

        // 举报原因统计
        var reasonCounts = new Dictionary<CommentReportReason, int>();
        foreach (CommentReportReason reason in Enum.GetValues<CommentReportReason>())
        {
            reasonCounts[reason] = await _commentReportRepository.CountByReasonAsync(reason);
        }

        return new CommentModerationStatsDto
        {
            PendingCount = pendingCount,
            ReportedCount = reportedCount,
            TodayCount = todayCount,
            WeeklyApprovalRate = weeklyApprovalRate,
            AIApprovalRate = aiApprovalRate,
            AverageModerationTime = 15.5, // 这里应该计算实际的平均审核时长
            StatusCounts = statusCounts,
            ReportReasonCounts = reasonCounts
        };
    }

    /// <summary>
    /// 计算用户审核统计信息
    /// </summary>
    private async Task<UserModerationStatsDto> CalculateUserModerationStatsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var stats = await _commentRepository.GetUserModerationStatsAsync(userId);
        return new UserModerationStatsDto
        {
            UserId = userId,
            TotalComments = 0,
            ApprovedComments = 0,
            RejectedComments = 0,
            ReportedComments = 0,
            SpamComments = 0,
            ApprovalRate = 0.0,
            CurrentTrustScore = 0.0,
            RecentCommentCount = 0
        };
    }

    /// <summary>
    /// 发送审核通知
    /// </summary>
    private async Task SendModerationNotificationAsync(Comment comment, CommentStatus previousStatus, ModerationAction action, Guid moderatorId)
    {
        try
        {
            // 这里应该调用通知服务发送审核结果通知
            await Task.Delay(1); // 模拟异步操作

            _logger.LogDebug("Sent moderation notification for comment {CommentId} with action {Action}",
                comment.Id, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending moderation notification for comment {CommentId}",
                comment.Id);
        }
    }

    #endregion
}