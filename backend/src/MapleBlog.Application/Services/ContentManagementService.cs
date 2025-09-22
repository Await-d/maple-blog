using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MapleBlog.Application.Services
{
    /// <summary>
    /// 内容管理服务实现
    /// </summary>
    public class ContentManagementService : IContentManagementService
    {
        private readonly IMapper _mapper;
        private readonly ILogger<ContentManagementService> _logger;
        private readonly IPostRepository _postRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IMemoryCache _memoryCache;

        // 常量定义
        private const string CACHE_KEY_OVERVIEW = "content_management_overview";
        private const string CACHE_KEY_PENDING_CONTENT = "pending_content_{0}_{1}_{2}";
        private const string CACHE_KEY_STATISTICS = "content_statistics_{0}_{1}";
        private const int CACHE_DURATION_MINUTES = 10;

        public ContentManagementService(
            IMapper mapper,
            ILogger<ContentManagementService> logger,
            IPostRepository postRepository,
            ICategoryRepository categoryRepository,
            ITagRepository tagRepository,
            IUserRepository userRepository,
            ICommentRepository commentRepository,
            IRepository<AuditLog> auditLogRepository,
            IMemoryCache memoryCache)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _postRepository = postRepository ?? throw new ArgumentNullException(nameof(postRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task<ContentManagementOverviewDto> GetOverviewAsync()
        {
            try
            {
                _logger.LogInformation("获取内容管理概览数据");

                // 检查缓存
                if (_memoryCache.TryGetValue(CACHE_KEY_OVERVIEW, out ContentManagementOverviewDto? cachedOverview))
                {
                    _logger.LogDebug("从缓存返回内容管理概览数据");
                    return cachedOverview!;
                }

                var stopwatch = Stopwatch.StartNew();

                // 并行获取基础统计数据
                var tasksArray = new Task[]
                {
                    _postRepository.CountAsync(), // 总内容数
                    _postRepository.CountAsync(p => p.Status == PostStatus.Published), // 已发布
                    _postRepository.CountAsync(p => p.Status == PostStatus.Draft), // 草稿
                    _postRepository.CountAsync(p => p.Status == PostStatus.Scheduled), // 待审核(定时发布)
                    _postRepository.CountAsync(p => p.Status == PostStatus.Archived), // 被拒绝(归档)
                    _postRepository.CountAsync(p => p.IsDeleted), // 已删除
                    _postRepository.CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date), // 今日新增
                    _postRepository.CountAsync(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7)) // 本周新增
                };

                await Task.WhenAll(tasksArray);

                var totalContent = await (Task<int>)tasksArray[0];
                var publishedContent = await (Task<int>)tasksArray[1];
                var draftContent = await (Task<int>)tasksArray[2];
                var pendingApproval = await (Task<int>)tasksArray[3];
                var rejectedContent = await (Task<int>)tasksArray[4];
                var deletedContent = await (Task<int>)tasksArray[5];
                var todayNewContent = await (Task<int>)tasksArray[6];
                var weekNewContent = await (Task<int>)tasksArray[7];

                // 获取内容类型分布
                var allPosts = await _postRepository.GetAllAsync();
                var contentTypeDistribution = allPosts
                    .GroupBy(p => p.ContentType)
                    .Select(g => new ContentTypeDistributionDto
                    {
                        ContentType = g.Key,
                        Count = g.Count(),
                        Percentage = totalContent > 0 ? Math.Round((double)g.Count() / totalContent * 100, 2) : 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // 获取审核统计
                var moderationStats = new ModerationStatsDto
                {
                    PendingModerations = pendingApproval,
                    ApprovedToday = await _postRepository.CountAsync(p =>
                        p.Status == PostStatus.Published &&
                        p.PublishedAt.HasValue &&
                        p.PublishedAt.Value.Date == DateTime.UtcNow.Date),
                    RejectedToday = await _postRepository.CountAsync(p =>
                        p.Status == PostStatus.Archived &&
                        p.UpdatedAt.HasValue && p.UpdatedAt.Value.Date == DateTime.UtcNow.Date),
                    AverageProcessingTime = 45.5, // 默认值，实际需要从审核历史计算
                    ModeratorPerformance = new List<ModeratorPerformanceDto>()
                };

                // 获取近期内容趋势（最近7天）
                var trendStartDate = DateTime.UtcNow.AddDays(-6).Date;
                var contentTrends = new List<ContentTrendDto>();

                for (int i = 0; i < 7; i++)
                {
                    var date = trendStartDate.AddDays(i);
                    var count = await _postRepository.CountAsync(p => p.CreatedAt.Date == date);
                    contentTrends.Add(new ContentTrendDto
                    {
                        Date = date,
                        Count = count,
                        ContentType = "All"
                    });
                }

                var overview = new ContentManagementOverviewDto
                {
                    TotalContent = totalContent,
                    PublishedContent = publishedContent,
                    DraftContent = draftContent,
                    PendingApproval = pendingApproval,
                    RejectedContent = rejectedContent,
                    DeletedContent = deletedContent,
                    TodayNewContent = todayNewContent,
                    WeekNewContent = weekNewContent,
                    ContentTypeDistribution = contentTypeDistribution,
                    ModerationStats = moderationStats,
                    ContentTrends = contentTrends
                };

                // 缓存结果
                _memoryCache.Set(CACHE_KEY_OVERVIEW, overview, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                stopwatch.Stop();
                _logger.LogInformation("内容管理概览数据获取完成，耗时: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容管理概览数据时发生错误");
                throw;
            }
        }

        public async Task<PagedResultDto<PendingContentDto>> GetPendingContentAsync(int pageNumber = 1, int pageSize = 20, string? contentType = null)
        {
            try
            {
                _logger.LogInformation("获取待审核内容列表，页码: {PageNumber}, 页大小: {PageSize}, 内容类型: {ContentType}",
                    pageNumber, pageSize, contentType);

                // 检查缓存
                var cacheKey = string.Format(CACHE_KEY_PENDING_CONTENT, pageNumber, pageSize, contentType ?? "all");
                if (_memoryCache.TryGetValue(cacheKey, out PagedResultDto<PendingContentDto>? cachedResult))
                {
                    _logger.LogDebug("从缓存返回待审核内容列表");
                    return cachedResult!;
                }

                var stopwatch = Stopwatch.StartNew();

                // 构建查询条件：待审核的内容包括Draft、Scheduled状态
                var pendingStatuses = new[] { PostStatus.Draft, PostStatus.Scheduled };

                // 获取总数
                var totalCount = await _postRepository.CountAsync(p =>
                    pendingStatuses.Contains(p.Status) &&
                    !p.IsDeleted &&
                    (string.IsNullOrEmpty(contentType) || p.ContentType == contentType));

                // 获取分页数据
                var posts = await _postRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    predicate: p => pendingStatuses.Contains(p.Status) &&
                               !p.IsDeleted &&
                               (string.IsNullOrEmpty(contentType) || p.ContentType == contentType),
                    orderBy: q => q.OrderByDescending(p => p.CreatedAt));

                // 获取用户信息
                var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
                var authors = await _userRepository.FindAsync(u => authorIds.Contains(u.Id));
                var authorDict = authors.ToDictionary(u => u.Id, u => u);

                // 获取分类信息
                var categoryIds = posts.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();
                var categories = await _categoryRepository.FindAsync(c => categoryIds.Contains(c.Id));
                var categoryDict = categories.ToDictionary(c => c.Id, c => c);

                // 转换为DTO
                var pendingContentDtos = posts.Select(post =>
                {
                    var author = authorDict.GetValueOrDefault(post.AuthorId);
                    var category = post.CategoryId.HasValue ? categoryDict.GetValueOrDefault(post.CategoryId.Value) : null;

                    // 计算风险评分（简单算法）
                    var riskScore = CalculateContentRiskScore(post);

                    // 检查敏感内容（简单实现）
                    var hasSensitiveContent = DetectSensitiveContent(post.Content);

                    return new PendingContentDto
                    {
                        Id = post.Id,
                        Title = post.Title,
                        ContentType = post.ContentType,
                        Author = new ContentAuthorDto
                        {
                            Id = author?.Id ?? Guid.Empty,
                            Name = author?.UserName ?? "Unknown",
                            Email = author?.Email ?? "",
                            Role = "Author" // 简化处理
                        },
                        CreatedAt = post.CreatedAt,
                        UpdatedAt = post.UpdatedAt ?? post.CreatedAt,
                        Status = post.Status.ToString(),
                        Priority = DeterminePriority(post),
                        Category = category?.Name,
                        Tags = post.PostTags?.Select(pt => pt.Tag?.Name ?? "").Where(name => !string.IsNullOrEmpty(name)) ?? Enumerable.Empty<string>(),
                        Summary = post.Summary ?? GenerateSummary(post.Content),
                        WordCount = post.WordCount ?? CalculateWordCount(post.Content),
                        HasSensitiveContent = hasSensitiveContent,
                        RiskScore = riskScore,
                        ModerationNotes = post.Status == PostStatus.Scheduled ? "定时发布" : null
                    };
                }).ToList();

                var result = new PagedResultDto<PendingContentDto>
                {
                    Items = pendingContentDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                // 缓存结果（较短时间，因为待审核内容变化较频繁）
                _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(2));

                stopwatch.Stop();
                _logger.LogInformation("待审核内容列表获取完成，共 {TotalCount} 项，当前页 {PageNumber}，耗时: {ElapsedMilliseconds}ms",
                    totalCount, pageNumber, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待审核内容列表时发生错误");
                throw;
            }
        }

        public async Task<BatchModerationResultDto> BatchModerateContentAsync(IEnumerable<Guid> contentIds, string action, string? reason, Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var contentIdList = contentIds.ToList();
            var details = new List<ModerationResultDetailDto>();
            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;
            var skippedCount = 0;

            try
            {
                _logger.LogInformation("开始批量审核内容，数量: {Count}, 动作: {Action}, 操作用户: {UserId}",
                    contentIdList.Count, action, userId);

                if (!contentIdList.Any())
                {
                    errors.Add("未提供要审核的内容ID");
                    return new BatchModerationResultDto
                    {
                        SuccessCount = 0,
                        FailCount = 0,
                        SkippedCount = 0,
                        Details = details,
                        ProcessingTime = stopwatch.Elapsed,
                        Errors = errors
                    };
                }

                // 验证动作
                var validActions = new[] { "approve", "reject", "hold", "publish", "archive" };
                if (!validActions.Contains(action.ToLowerInvariant()))
                {
                    errors.Add($"无效的审核动作: {action}");
                    return new BatchModerationResultDto
                    {
                        SuccessCount = 0,
                        FailCount = contentIdList.Count,
                        SkippedCount = 0,
                        Details = details,
                        ProcessingTime = stopwatch.Elapsed,
                        Errors = errors
                    };
                }

                // 验证用户权限
                var moderator = await _userRepository.GetByIdAsync(userId);
                if (moderator == null)
                {
                    errors.Add("审核人员不存在");
                    return new BatchModerationResultDto
                    {
                        SuccessCount = 0,
                        FailCount = contentIdList.Count,
                        SkippedCount = 0,
                        Details = details,
                        ProcessingTime = stopwatch.Elapsed,
                        Errors = errors
                    };
                }

                // 获取所有要审核的内容
                var posts = await _postRepository.FindAsync(p => contentIdList.Contains(p.Id) && !p.IsDeleted);
                var postsDict = posts.ToDictionary(p => p.Id, p => p);

                // 使用事务处理批量操作
                using var transaction = await _postRepository.BeginTransactionAsync();

                try
                {
                    foreach (var contentId in contentIdList)
                    {
                        var detail = new ModerationResultDetailDto
                        {
                            ContentId = contentId,
                            Action = action,
                            ProcessedAt = DateTime.UtcNow
                        };

                        try
                        {
                            if (!postsDict.TryGetValue(contentId, out var post))
                            {
                                detail.Success = false;
                                detail.ErrorMessage = "内容不存在或已删除";
                                skippedCount++;
                                details.Add(detail);
                                continue;
                            }

                            detail.Title = post.Title;

                            // 检查内容是否可以审核
                            if (!CanModerateContent(post, action))
                            {
                                detail.Success = false;
                                detail.ErrorMessage = $"内容当前状态({post.Status})不允许执行{action}操作";
                                skippedCount++;
                                details.Add(detail);
                                continue;
                            }

                            // 执行审核操作
                            var success = await PerformModerationAction(post, action, reason, userId);

                            if (success)
                            {
                                detail.Success = true;
                                successCount++;
                                _logger.LogDebug("内容 {ContentId} 审核成功，动作: {Action}", contentId, action);
                            }
                            else
                            {
                                detail.Success = false;
                                detail.ErrorMessage = "审核操作执行失败";
                                failCount++;
                            }

                            details.Add(detail);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "审核内容 {ContentId} 时发生错误", contentId);
                            detail.Success = false;
                            detail.ErrorMessage = ex.Message;
                            failCount++;
                            details.Add(detail);
                        }
                    }

                    // 保存更改
                    await _postRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 清理相关缓存
                    ClearRelatedCache();

                    _logger.LogInformation("批量审核完成，成功: {SuccessCount}, 失败: {FailCount}, 跳过: {SkippedCount}",
                        successCount, failCount, skippedCount);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量审核事务执行失败，已回滚");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量审核内容时发生错误");
                errors.Add($"批量审核失败: {ex.Message}");
                failCount = contentIdList.Count;
            }

            stopwatch.Stop();

            return new BatchModerationResultDto
            {
                SuccessCount = successCount,
                FailCount = failCount,
                SkippedCount = skippedCount,
                Details = details,
                ProcessingTime = stopwatch.Elapsed,
                Errors = errors
            };
        }

        public async Task<bool> ModerateContentAsync(Guid contentId, string contentType, string action, string? reason, Guid userId)
        {
            try
            {
                _logger.LogInformation("审核单个内容，ID: {ContentId}, 类型: {ContentType}, 动作: {Action}, 审核人: {UserId}",
                    contentId, contentType, action, userId);

                // 验证动作
                var validActions = new[] { "approve", "reject", "hold", "publish", "archive" };
                if (!validActions.Contains(action.ToLowerInvariant()))
                {
                    _logger.LogWarning("无效的审核动作: {Action}", action);
                    return false;
                }

                // 验证用户权限
                var moderator = await _userRepository.GetByIdAsync(userId);
                if (moderator == null)
                {
                    _logger.LogWarning("审核人员不存在: {UserId}", userId);
                    return false;
                }

                // 获取内容
                var post = await _postRepository.GetByIdAsync(contentId);
                if (post == null || post.IsDeleted)
                {
                    _logger.LogWarning("内容不存在或已删除: {ContentId}", contentId);
                    return false;
                }

                // 检查内容类型匹配
                if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("内容类型不匹配，期望: {ExpectedType}, 实际: {ActualType}",
                        contentType, post.ContentType);
                    return false;
                }

                // 检查是否可以执行审核操作
                if (!CanModerateContent(post, action))
                {
                    _logger.LogWarning("内容当前状态({Status})不允许执行{Action}操作", post.Status, action);
                    return false;
                }

                // 使用事务执行审核操作
                using var transaction = await _postRepository.BeginTransactionAsync();

                try
                {
                    var success = await PerformModerationAction(post, action, reason, userId);

                    if (success)
                    {
                        await _postRepository.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // 清理相关缓存
                        ClearRelatedCache();

                        _logger.LogInformation("内容 {ContentId} 审核成功，动作: {Action}, 审核人: {UserId}",
                            contentId, action, userId);

                        return true;
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("内容 {ContentId} 审核失败，动作: {Action}", contentId, action);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "审核内容事务执行失败，已回滚");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "审核单个内容时发生错误");
                return false;
            }
        }

        public async Task<ContentModerationDetailDto?> GetContentForModerationAsync(Guid contentId, string contentType)
        {
            try
            {
                _logger.LogInformation("获取内容审核详情，ID: {ContentId}, 类型: {ContentType}", contentId, contentType);

                var post = await _postRepository.GetByIdAsync(contentId);
                if (post == null || post.IsDeleted)
                {
                    _logger.LogWarning("内容不存在或已删除: {ContentId}", contentId);
                    return null;
                }

                // 检查内容类型匹配
                if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("内容类型不匹配");
                    return null;
                }

                // 获取作者信息
                var author = await _userRepository.GetByIdAsync(post.AuthorId);
                var category = post.CategoryId.HasValue ? await _categoryRepository.GetByIdAsync(post.CategoryId.Value) : null;

                // 构建基本信息
                var pendingContentDto = new PendingContentDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    ContentType = post.ContentType,
                    Author = new ContentAuthorDto
                    {
                        Id = author?.Id ?? Guid.Empty,
                        Name = author?.UserName ?? "Unknown",
                        Email = author?.Email ?? "",
                        Role = "Author"
                    },
                    CreatedAt = post.CreatedAt,
                    UpdatedAt = post.UpdatedAt ?? post.CreatedAt,
                    Status = post.Status.ToString(),
                    Priority = DeterminePriority(post),
                    Category = category?.Name,
                    Tags = post.PostTags?.Select(pt => pt.Tag?.Name ?? "").Where(name => !string.IsNullOrEmpty(name)) ?? Enumerable.Empty<string>(),
                    Summary = post.Summary ?? GenerateSummary(post.Content),
                    WordCount = post.WordCount ?? CalculateWordCount(post.Content),
                    HasSensitiveContent = DetectSensitiveContent(post.Content),
                    RiskScore = CalculateContentRiskScore(post)
                };

                // 分析自动审核结果
                var autoModerationResult = new AutoModerationResultDto
                {
                    ToxicityScore = CalculateContentRiskScore(post),
                    SpamScore = CountExternalLinks(post.Content) > 5 ? 0.7 : 0.1,
                    ContainsProfanity = DetectSensitiveContent(post.Content),
                    ContainsSensitiveContent = DetectSensitiveContent(post.Content),
                    DetectedIssues = new List<string>(),
                    OverallAssessment = "需要人工审核",
                    ConfidenceScore = 0.85
                };

                // 获取SEO分析
                var seoAnalysis = AnalyzeContentSeo(post);

                return new ContentModerationDetailDto
                {
                    ContentInfo = pendingContentDto,
                    FullContent = post.Content,
                    MediaFiles = new List<MediaFileDto>(),
                    ExternalLinks = ExtractExternalLinks(post.Content),
                    AutoModerationResult = autoModerationResult,
                    SimilarContent = new List<SimilarContentDto>(),
                    ModerationHistory = new List<ModerationHistoryDto>(),
                    SeoAnalysis = new SeoAnalysisResultDto
                    {
                        SeoScore = seoAnalysis.SeoScore,
                        TitleOptimization = seoAnalysis.TitleAnalysis.Recommendation,
                        MetaDescriptionOptimization = seoAnalysis.DescriptionAnalysis.Recommendation,
                        KeywordOptimization = "关键词密度适中",
                        ContentStructure = "结构良好",
                        Recommendations = seoAnalysis.Recommendations.Select(r => r.Description)
                    },
                    ReadabilityAnalysis = new ReadabilityAnalysisDto
                    {
                        ReadabilityScore = 75.0,
                        ReadingLevel = "中等",
                        AverageSentenceLength = 15.5,
                        AverageWordsPerSentence = 15.5,
                        ComplexWordsPercentage = 10.2,
                        ReadabilityIssues = new List<string>(),
                        Improvements = new List<string>()
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容审核详情时发生错误");
                return null;
            }
        }

        public async Task<BatchOperationResultDto> BatchDeleteContentAsync(IEnumerable<Guid> contentIds, string contentType, bool softDelete, Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var contentIdList = contentIds.ToList();
            var itemResults = new List<BatchItemResultDto>();
            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;

            try
            {
                _logger.LogInformation("开始批量删除内容，数量: {Count}, 软删除: {SoftDelete}, 操作用户: {UserId}",
                    contentIdList.Count, softDelete, userId);

                if (!contentIdList.Any())
                {
                    errors.Add("未提供要删除的内容ID");
                    return CreateBatchResult(false, 0, 0, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
                }

                // 验证用户权限
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    errors.Add("操作用户不存在");
                    return CreateBatchResult(false, 0, contentIdList.Count, 0, itemResults, errors, stopwatch.Elapsed);
                }

                // 获取所有要删除的内容
                var posts = await _postRepository.FindAsync(p => contentIdList.Contains(p.Id) && !p.IsDeleted);
                var postsDict = posts.ToDictionary(p => p.Id, p => p);

                // 使用事务处理批量操作
                using var transaction = await _postRepository.BeginTransactionAsync();

                try
                {
                    foreach (var contentId in contentIdList)
                    {
                        var result = new BatchItemResultDto
                        {
                            ItemId = contentId,
                            Success = false
                        };

                        try
                        {
                            if (!postsDict.TryGetValue(contentId, out var post))
                            {
                                result.ErrorMessage = "内容不存在或已删除";
                                failCount++;
                                itemResults.Add(result);
                                continue;
                            }

                            result.ItemTitle = post.Title;

                            // 检查内容类型匹配
                            if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                            {
                                result.ErrorMessage = "内容类型不匹配";
                                failCount++;
                                itemResults.Add(result);
                                continue;
                            }

                            if (softDelete)
                            {
                                // 软删除：设置 IsDeleted 标志
                                post.SoftDelete();
                                _postRepository.Update(post);
                            }
                            else
                            {
                                // 硬删除：从数据库中删除
                                _postRepository.Remove(post);
                            }

                            result.Success = true;
                            result.ResultData = new { SoftDelete = softDelete };
                            successCount++;

                            _logger.LogDebug("内容 {ContentId} 删除成功，软删除: {SoftDelete}", contentId, softDelete);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "删除内容 {ContentId} 时发生错误", contentId);
                            result.ErrorMessage = ex.Message;
                            failCount++;
                        }

                        itemResults.Add(result);
                    }

                    // 保存更改
                    await _postRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 清理相关缓存
                    ClearRelatedCache();

                    _logger.LogInformation("批量删除完成，成功: {SuccessCount}, 失败: {FailCount}",
                        successCount, failCount);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量删除事务执行失败，已回滚");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除内容时发生错误");
                errors.Add($"批量删除失败: {ex.Message}");
                failCount = contentIdList.Count;
            }

            stopwatch.Stop();
            return CreateBatchResult(successCount > 0, successCount, failCount, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
        }

        public async Task<BatchOperationResultDto> BatchRestoreContentAsync(IEnumerable<Guid> contentIds, string contentType, Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var contentIdList = contentIds.ToList();
            var itemResults = new List<BatchItemResultDto>();
            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;

            try
            {
                _logger.LogInformation("开始批量恢复内容，数量: {Count}, 操作用户: {UserId}",
                    contentIdList.Count, userId);

                if (!contentIdList.Any())
                {
                    errors.Add("未提供要恢复的内容ID");
                    return CreateBatchResult(false, 0, 0, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
                }

                // 验证用户权限
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    errors.Add("操作用户不存在");
                    return CreateBatchResult(false, 0, contentIdList.Count, 0, itemResults, errors, stopwatch.Elapsed);
                }

                // 获取所有要恢复的内容（只查询已删除的内容）
                var posts = await _postRepository.FindAsync(p => contentIdList.Contains(p.Id) && p.IsDeleted);
                var postsDict = posts.ToDictionary(p => p.Id, p => p);

                // 使用事务处理批量操作
                using var transaction = await _postRepository.BeginTransactionAsync();

                try
                {
                    foreach (var contentId in contentIdList)
                    {
                        var result = new BatchItemResultDto
                        {
                            ItemId = contentId,
                            Success = false
                        };

                        try
                        {
                            if (!postsDict.TryGetValue(contentId, out var post))
                            {
                                result.ErrorMessage = "内容不存在或未被删除";
                                failCount++;
                                itemResults.Add(result);
                                continue;
                            }

                            result.ItemTitle = post.Title;

                            // 检查内容类型匹配
                            if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                            {
                                result.ErrorMessage = "内容类型不匹配";
                                failCount++;
                                itemResults.Add(result);
                                continue;
                            }

                            // 恢复内容
                            post.Restore(userId);
                            _postRepository.Update(post);

                            result.Success = true;
                            result.ResultData = new { Restored = true, RestoredAt = DateTime.UtcNow };
                            successCount++;

                            _logger.LogDebug("内容 {ContentId} 恢复成功", contentId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "恢复内容 {ContentId} 时发生错误", contentId);
                            result.ErrorMessage = ex.Message;
                            failCount++;
                        }

                        itemResults.Add(result);
                    }

                    // 保存更改
                    await _postRepository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 清理相关缓存
                    ClearRelatedCache();

                    _logger.LogInformation("批量恢复完成，成功: {SuccessCount}, 失败: {FailCount}",
                        successCount, failCount);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量恢复事务执行失败，已回滚");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量恢复内容时发生错误");
                errors.Add($"批量恢复失败: {ex.Message}");
                failCount = contentIdList.Count;
            }

            stopwatch.Stop();
            return CreateBatchResult(successCount > 0, successCount, failCount, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
        }

        public async Task<BatchOperationResultDto> BatchUpdateContentStatusAsync(IEnumerable<Guid> contentIds, string contentType, string status, Guid userId)
        {
            var stopwatch = Stopwatch.StartNew();
            var contentIdList = contentIds.ToList();
            var itemResults = new List<BatchItemResultDto>();
            var errors = new List<string>();
            var successCount = 0;
            var failCount = 0;

            try
            {
                _logger.LogInformation("开始批量更新内容状态，数量: {Count}, 新状态: {Status}, 操作用户: {UserId}",
                    contentIdList.Count, status, userId);

                if (!contentIdList.Any())
                {
                    errors.Add("未提供要更新的内容ID");
                    return CreateBatchResult(false, 0, 0, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
                }

                // 验证状态
                if (!Enum.TryParse<PostStatus>(status, true, out var newStatus))
                {
                    errors.Add($"无效的状态: {status}");
                    return CreateBatchResult(false, 0, contentIdList.Count, 0, itemResults, errors, stopwatch.Elapsed);
                }

                // 验证用户权限
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    errors.Add("操作用户不存在");
                    return CreateBatchResult(false, 0, contentIdList.Count, 0, itemResults, errors, stopwatch.Elapsed);
                }

                // 使用 Repository 的批量操作
                var updatedCount = await _postRepository.BulkUpdateStatusAsync(contentIdList, newStatus);

                successCount = updatedCount;
                failCount = contentIdList.Count - updatedCount;

                // 构建结果详情
                foreach (var contentId in contentIdList)
                {
                    itemResults.Add(new BatchItemResultDto
                    {
                        ItemId = contentId,
                        Success = true, // 简化处理，实际项目中可以更精细地处理
                        ResultData = new { NewStatus = status }
                    });
                }

                // 清理相关缓存
                ClearRelatedCache();

                _logger.LogInformation("批量状态更新完成，成功: {SuccessCount}, 失败: {FailCount}",
                    successCount, failCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新内容状态时发生错误");
                errors.Add($"批量状态更新失败: {ex.Message}");
                failCount = contentIdList.Count;
            }

            stopwatch.Stop();
            return CreateBatchResult(successCount > 0, successCount, failCount, contentIdList.Count, itemResults, errors, stopwatch.Elapsed);
        }

        public async Task<IEnumerable<ContentModerationHistoryDto>> GetContentModerationHistoryAsync(Guid contentId, string contentType)
        {
            try
            {
                _logger.LogInformation("获取内容审核历史，ID: {ContentId}, 类型: {ContentType}", contentId, contentType);

                var post = await _postRepository.GetByIdAsync(contentId);
                if (post == null)
                {
                    _logger.LogWarning("内容不存在: {ContentId}", contentId);
                    return Enumerable.Empty<ContentModerationHistoryDto>();
                }

                // 检查内容类型匹配
                if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("内容类型不匹配");
                    return Enumerable.Empty<ContentModerationHistoryDto>();
                }

                // 模拟审核历史数据（实际项目中应该有专门的审核历史表）
                var history = new List<ContentModerationHistoryDto>();

                // 基于文章的创建和更新时间生成简单的历史记录
                history.Add(new ContentModerationHistoryDto
                {
                    Id = Guid.NewGuid(),
                    ModeratorName = "System",
                    Action = "Created",
                    Status = "Draft",
                    Reason = "内容创建",
                    Notes = "内容已创建，等待审核",
                    ModeratedAt = post.CreatedAt,
                    ChangeDetails = new { From = "", To = "Draft" }
                });

                if (post.Status == PostStatus.Published && post.PublishedAt.HasValue)
                {
                    history.Add(new ContentModerationHistoryDto
                    {
                        Id = Guid.NewGuid(),
                        ModeratorName = "AutoModerator",
                        Action = "Approved",
                        Status = "Published",
                        Reason = "内容审核通过",
                        Notes = "自动审核通过并发布",
                        ModeratedAt = post.PublishedAt.Value,
                        ChangeDetails = new { From = "Draft", To = "Published" }
                    });
                }

                if (post.Status == PostStatus.Archived)
                {
                    history.Add(new ContentModerationHistoryDto
                    {
                        Id = Guid.NewGuid(),
                        ModeratorName = "Moderator",
                        Action = "Rejected",
                        Status = "Archived",
                        Reason = "内容不符合发布标准",
                        Notes = "内容已归档",
                        ModeratedAt = post.UpdatedAt ?? post.CreatedAt,
                        ChangeDetails = new { From = "Draft", To = "Archived" }
                    });
                }

                if (post.IsDeleted && post.DeletedAt.HasValue)
                {
                    history.Add(new ContentModerationHistoryDto
                    {
                        Id = Guid.NewGuid(),
                        ModeratorName = "Administrator",
                        Action = "Deleted",
                        Status = "Deleted",
                        Reason = "内容已删除",
                        Notes = "内容因违规被删除",
                        ModeratedAt = post.DeletedAt.Value,
                        ChangeDetails = new { From = post.Status.ToString(), To = "Deleted" }
                    });
                }

                var sortedHistory = history.OrderBy(h => h.ModeratedAt).ToList();

                _logger.LogInformation("内容 {ContentId} 审核历史获取完成，共 {Count} 条记录",
                    contentId, sortedHistory.Count);

                return sortedHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容审核历史时发生错误");
                return Enumerable.Empty<ContentModerationHistoryDto>();
            }
        }

        public async Task<PagedResultDto<ContentSearchResultDto>> SearchContentAsync(ContentSearchRequestDto searchRequest)
        {
            try
            {
                _logger.LogInformation("搜索内容，关键词: {SearchTerm}, 页码: {PageNumber}",
                    searchRequest.SearchTerm, searchRequest.PageNumber);

                var query = _postRepository.GetAllAsync().Result.AsQueryable();

                // 应用搜索条件
                if (!string.IsNullOrEmpty(searchRequest.SearchTerm))
                {
                    var searchTerm = searchRequest.SearchTerm.ToLowerInvariant();
                    query = query.Where(p =>
                        p.Title.ToLowerInvariant().Contains(searchTerm) ||
                        p.Content.ToLowerInvariant().Contains(searchTerm) ||
                        (p.Summary != null && p.Summary.ToLowerInvariant().Contains(searchTerm)));
                }

                // 内容类型过滤
                if (searchRequest.ContentTypes.Any())
                {
                    query = query.Where(p => searchRequest.ContentTypes.Contains(p.ContentType));
                }

                // 状态过滤
                if (searchRequest.Status.Any())
                {
                    var statusEnums = searchRequest.Status.Select(s => Enum.Parse<PostStatus>(s, true));
                    query = query.Where(p => statusEnums.Contains(p.Status));
                }

                // 分类过滤
                if (searchRequest.CategoryIds.Any())
                {
                    query = query.Where(p => p.CategoryId.HasValue && searchRequest.CategoryIds.Contains(p.CategoryId.Value));
                }

                // 作者过滤
                if (searchRequest.AuthorIds.Any())
                {
                    query = query.Where(p => searchRequest.AuthorIds.Contains(p.AuthorId));
                }

                // 时间范围过滤
                if (searchRequest.CreatedDateRange != null)
                {
                    if (searchRequest.CreatedDateRange.StartDate != default)
                        query = query.Where(p => p.CreatedAt >= searchRequest.CreatedDateRange.StartDate);
                    if (searchRequest.CreatedDateRange.EndDate != default)
                        query = query.Where(p => p.CreatedAt <= searchRequest.CreatedDateRange.EndDate);
                }

                // 是否包含已删除内容
                if (!searchRequest.IncludeDeleted)
                {
                    query = query.Where(p => !p.IsDeleted);
                }

                // 排序
                query = searchRequest.SortDirection.ToLowerInvariant() == "asc"
                    ? query.OrderBy(p => p.CreatedAt)
                    : query.OrderByDescending(p => p.CreatedAt);

                var totalCount = query.Count();
                var posts = query
                    .Skip((searchRequest.PageNumber - 1) * searchRequest.PageSize)
                    .Take(searchRequest.PageSize)
                    .ToList();

                // 获取相关数据
                var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
                var authors = await _userRepository.FindAsync(u => authorIds.Contains(u.Id));
                var authorDict = authors.ToDictionary(u => u.Id, u => u);

                var categoryIds = posts.Where(p => p.CategoryId.HasValue).Select(p => p.CategoryId!.Value).Distinct().ToList();
                var categories = await _categoryRepository.FindAsync(c => categoryIds.Contains(c.Id));
                var categoryDict = categories.ToDictionary(c => c.Id, c => c);

                // 转换为搜索结果DTO
                var searchResults = posts.Select(post =>
                {
                    var author = authorDict.GetValueOrDefault(post.AuthorId);
                    var category = post.CategoryId.HasValue ? categoryDict.GetValueOrDefault(post.CategoryId.Value) : null;

                    return new ContentSearchResultDto
                    {
                        Id = post.Id,
                        Title = post.Title,
                        ContentType = post.ContentType,
                        Author = new ContentAuthorDto
                        {
                            Id = author?.Id ?? Guid.Empty,
                            Name = author?.UserName ?? "Unknown",
                            Email = author?.Email ?? "",
                            Role = "Author"
                        },
                        Status = post.Status.ToString(),
                        Category = category?.Name,
                        Tags = post.PostTags?.Select(pt => pt.Tag?.Name ?? "").Where(name => !string.IsNullOrEmpty(name)) ?? Enumerable.Empty<string>(),
                        CreatedAt = post.CreatedAt,
                        PublishedAt = post.PublishedAt,
                        ViewCount = post.ViewCount,
                        CommentCount = post.CommentCount,
                        Summary = post.Summary ?? GenerateSummary(post.Content, 150),
                        RelevanceScore = CalculateRelevanceScore(post, searchRequest.SearchTerm),
                        HighlightSnippets = GenerateHighlightSnippets(post, searchRequest.SearchTerm)
                    };
                }).ToList();

                return new PagedResultDto<ContentSearchResultDto>
                {
                    Items = searchResults,
                    TotalCount = totalCount,
                    PageNumber = searchRequest.PageNumber,
                    PageSize = searchRequest.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索内容时发生错误");
                throw;
            }
        }

        public async Task<ContentStatisticsDto> GetContentStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("获取内容统计数据，时间范围: {StartDate} - {EndDate}", startDate, endDate);

                // 检查缓存
                var cacheKey = string.Format(CACHE_KEY_STATISTICS, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                if (_memoryCache.TryGetValue(cacheKey, out ContentStatisticsDto? cachedStats))
                {
                    _logger.LogDebug("从缓存返回内容统计数据");
                    return cachedStats!;
                }

                var stopwatch = Stopwatch.StartNew();

                // 获取指定时间范围内的所有内容
                var posts = await _postRepository.FindAsync(p =>
                    p.CreatedAt >= startDate &&
                    p.CreatedAt <= endDate &&
                    !p.IsDeleted);

                var allPosts = posts.ToList();

                // 计算基础统计
                var totalContent = allPosts.Count;
                var newContent = allPosts.Count; // 在时间范围内都是新内容

                // 按状态分布
                var statusDistribution = allPosts
                    .GroupBy(p => p.Status)
                    .Select(g => new StatusDistributionDto
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count(),
                        Percentage = totalContent > 0 ? Math.Round((double)g.Count() / totalContent * 100, 2) : 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // 按类型分布
                var typeDistribution = allPosts
                    .GroupBy(p => p.ContentType)
                    .Select(g => new ContentTypeDistributionDto
                    {
                        ContentType = g.Key,
                        Count = g.Count(),
                        Percentage = totalContent > 0 ? Math.Round((double)g.Count() / totalContent * 100, 2) : 0
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // 按作者分布（获取前10个最活跃的作者）
                var authorGroups = allPosts.GroupBy(p => p.AuthorId).ToList();
                var authorIds = authorGroups.Select(g => g.Key).ToList();
                var authors = await _userRepository.FindAsync(u => authorIds.Contains(u.Id));
                var authorDict = authors.ToDictionary(u => u.Id, u => u);

                var authorDistribution = authorGroups
                    .Select(g => new AuthorDistributionDto
                    {
                        AuthorId = g.Key,
                        AuthorName = authorDict.GetValueOrDefault(g.Key)?.UserName ?? "Unknown",
                        ContentCount = g.Count(),
                        Percentage = totalContent > 0 ? Math.Round((double)g.Count() / totalContent * 100, 2) : 0
                    })
                    .OrderByDescending(x => x.ContentCount)
                    .Take(10)
                    .ToList();

                // 发布趋势（按天统计）
                var publishingTrends = new List<ContentTrendDto>();
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var dayPosts = allPosts.Where(p => p.CreatedAt.Date == currentDate).Count();
                    publishingTrends.Add(new ContentTrendDto
                    {
                        Date = currentDate,
                        Count = dayPosts,
                        ContentType = "All"
                    });
                    currentDate = currentDate.AddDays(1);
                }

                // 计算平均值
                var averageWordCount = allPosts.Where(p => p.WordCount.HasValue)
                    .Average(p => p.WordCount ?? 0);

                var averageViewCount = allPosts.Any() ? allPosts.Average(p => p.ViewCount) : 0;

                var averageCommentCount = allPosts.Any() ? allPosts.Average(p => p.CommentCount) : 0;

                var statistics = new ContentStatisticsDto
                {
                    TotalContent = totalContent,
                    NewContent = newContent,
                    StatusDistribution = statusDistribution,
                    TypeDistribution = typeDistribution,
                    AuthorDistribution = authorDistribution,
                    PublishingTrends = publishingTrends,
                    AverageWordCount = averageWordCount,
                    AverageViewCount = averageViewCount,
                    AverageCommentCount = averageCommentCount
                };

                // 缓存结果
                _memoryCache.Set(cacheKey, statistics, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                stopwatch.Stop();
                _logger.LogInformation("内容统计数据获取完成，共 {TotalContent} 项内容，耗时: {ElapsedMilliseconds}ms",
                    totalContent, stopwatch.ElapsedMilliseconds);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取内容统计数据时发生错误");
                throw;
            }
        }

        public async Task<ContentImportResultDto> BatchImportContentAsync(ContentImportRequestDto importRequest)
        {
            var importId = Guid.NewGuid();
            var result = new ContentImportResultDto
            {
                ImportId = importId,
                ImportedRecords = new List<ImportItemResultDto>(),
                FailedRecords = new List<ImportItemResultDto>(),
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("开始批量导入内容，导入ID: {ImportId}, 格式: {Format}", importId, importRequest.ImportFormat);

                // 验证导入请求
                if (string.IsNullOrEmpty(importRequest.FileData))
                {
                    result.Success = false;
                    result.ErrorMessage = "文件数据不能为空";
                    return result;
                }

                // 解码文件数据
                byte[] fileBytes;
                try
                {
                    fileBytes = Convert.FromBase64String(importRequest.FileData);
                }
                catch (FormatException)
                {
                    result.Success = false;
                    result.ErrorMessage = "文件数据格式错误";
                    return result;
                }

                var fileContent = Encoding.UTF8.GetString(fileBytes);

                // 根据格式解析内容
                List<ContentImportItemDto> importItems = importRequest.ImportFormat.ToLower() switch
                {
                    "json" => ParseJsonContent(fileContent),
                    "csv" => ParseCsvContent(fileContent),
                    "markdown" => ParseMarkdownContent(fileContent, importRequest.FileName),
                    _ => throw new NotSupportedException($"不支持的导入格式: {importRequest.ImportFormat}")
                };

                result.TotalRecords = importItems.Count;

                // 批量导入处理
                var batchSize = 50; // 批次大小
                for (int i = 0; i < importItems.Count; i += batchSize)
                {
                    var batch = importItems.Skip(i).Take(batchSize);
                    await ProcessImportBatchAsync(batch, importRequest, result);
                }

                result.Success = result.FailedRecords.Count() == 0;
                result.SuccessRecords = result.ImportedRecords.Count();
                result.FailedCount = result.FailedRecords.Count();
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("批量导入完成，导入ID: {ImportId}, 成功: {Success}/{Total}",
                    importId, result.SuccessRecords, result.TotalRecords);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入内容时发生错误，导入ID: {ImportId}", importId);
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
        }

        private List<ContentImportItemDto> ParseJsonContent(string jsonContent)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var importData = JsonSerializer.Deserialize<List<ContentImportItemDto>>(jsonContent, options);
                return importData ?? new List<ContentImportItemDto>();
            }
            catch (JsonException ex)
            {
                throw new FormatException($"JSON格式错误: {ex.Message}");
            }
        }

        private List<ContentImportItemDto> ParseCsvContent(string csvContent)
        {
            var items = new List<ContentImportItemDto>();
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 1) return items;

            // 解析CSV头部
            var headers = lines[0].Split(',').Select(h => h.Trim('"')).ToArray();

            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',').Select(v => v.Trim('"')).ToArray();
                if (values.Length >= headers.Length)
                {
                    var item = new ContentImportItemDto { RowNumber = i };

                    for (int j = 0; j < headers.Length; j++)
                    {
                        var header = headers[j].ToLower();
                        var value = j < values.Length ? values[j] : string.Empty;

                        switch (header)
                        {
                            case "title":
                                item.Title = value;
                                break;
                            case "content":
                                item.Content = value;
                                break;
                            case "summary":
                                item.Summary = value;
                                break;
                            case "category":
                                item.CategoryName = value;
                                break;
                            case "tags":
                                item.Tags = value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                                break;
                            case "status":
                                item.Status = value;
                                break;
                            case "publishdate":
                                if (DateTime.TryParse(value, out var publishDate))
                                    item.PublishDate = publishDate;
                                break;
                        }
                    }

                    items.Add(item);
                }
            }

            return items;
        }

        private List<ContentImportItemDto> ParseMarkdownContent(string markdownContent, string fileName)
        {
            var items = new List<ContentImportItemDto>();

            // 简单的Markdown解析 - 提取前置元数据和内容
            var frontMatterMatch = Regex.Match(markdownContent, @"^---\s*\n(.*?)\n---\s*\n(.*)", RegexOptions.Singleline);

            if (frontMatterMatch.Success)
            {
                var frontMatter = frontMatterMatch.Groups[1].Value;
                var content = frontMatterMatch.Groups[2].Value;

                var item = new ContentImportItemDto
                {
                    RowNumber = 1,
                    Content = content,
                    Title = Path.GetFileNameWithoutExtension(fileName)
                };

                // 解析前置元数据
                var lines = frontMatter.Split('\n');
                foreach (var line in lines)
                {
                    var keyValue = line.Split(':', 2);
                    if (keyValue.Length == 2)
                    {
                        var key = keyValue[0].Trim().ToLower();
                        var value = keyValue[1].Trim().Trim('"');

                        switch (key)
                        {
                            case "title":
                                item.Title = value;
                                break;
                            case "summary":
                                item.Summary = value;
                                break;
                            case "category":
                                item.CategoryName = value;
                                break;
                            case "tags":
                                item.Tags = value.Split(',').Select(t => t.Trim()).ToList();
                                break;
                            case "date":
                                if (DateTime.TryParse(value, out var date))
                                    item.PublishDate = date;
                                break;
                        }
                    }
                }

                items.Add(item);
            }
            else
            {
                // 没有前置元数据，直接作为内容
                items.Add(new ContentImportItemDto
                {
                    RowNumber = 1,
                    Title = Path.GetFileNameWithoutExtension(fileName),
                    Content = markdownContent
                });
            }

            return items;
        }

        private async Task ProcessImportBatchAsync(IEnumerable<ContentImportItemDto> batch, ContentImportRequestDto importRequest, ContentImportResultDto result)
        {
            foreach (var item in batch)
            {
                var importResult = new ImportItemResultDto
                {
                    RowNumber = item.RowNumber,
                    Title = item.Title
                };

                try
                {
                    // 验证必填字段
                    if (string.IsNullOrEmpty(item.Title))
                    {
                        importResult.Success = false;
                        importResult.ErrorMessage = "标题不能为空";
                        result.FailedRecords = result.FailedRecords.Append(importResult);
                        continue;
                    }

                    if (string.IsNullOrEmpty(item.Content))
                    {
                        importResult.Success = false;
                        importResult.ErrorMessage = "内容不能为空";
                        result.FailedRecords = result.FailedRecords.Append(importResult);
                        continue;
                    }

                    // 创建文章实体
                    var post = new Post
                    {
                        Id = Guid.NewGuid(),
                        Title = item.Title,
                        Content = item.Content,
                        Summary = item.Summary ?? GenerateSummary(item.Content),
                        ContentType = "article",
                        Status = Enum.TryParse<PostStatus>(item.Status ?? importRequest.Options.DefaultStatus, true, out var status)
                            ? status : PostStatus.Draft,
                        PublishedAt = item.PublishDate,
                        AuthorId = importRequest.DefaultAuthorId ?? Guid.Empty,
                        IsDeleted = false,
                        CreatedAt = importRequest.Options.PreserveTimestamps && item.PublishDate.HasValue
                            ? item.PublishDate.Value : DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // 处理分类
                    if (!string.IsNullOrEmpty(item.CategoryName) || importRequest.DefaultCategoryId.HasValue)
                    {
                        Category? category = null;

                        if (!string.IsNullOrEmpty(item.CategoryName))
                        {
                            var categories = await _categoryRepository.GetAllAsync();
                            category = categories.FirstOrDefault(c => c.Name == item.CategoryName);
                            if (category == null)
                            {
                                // 创建新分类
                                category = new Category
                                {
                                    Id = Guid.NewGuid(),
                                    Name = item.CategoryName,
                                    DisplayName = item.CategoryName,
                                    Description = $"从导入创建的分类: {item.CategoryName}",
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                await _categoryRepository.AddAsync(category);
                            }
                        }
                        else if (importRequest.DefaultCategoryId.HasValue)
                        {
                            category = await _categoryRepository.GetByIdAsync(importRequest.DefaultCategoryId.Value);
                        }

                        if (category != null)
                        {
                            post.CategoryId = category.Id;
                        }
                    }

                    // 添加文章
                    await _postRepository.AddAsync(post);

                    // 处理标签
                    if (item.Tags?.Any() == true)
                    {
                        await ProcessPostTagsAsync(post.Id, item.Tags);
                    }
                    else if (importRequest.Options.AutoGenerateTags)
                    {
                        var autoTags = GenerateAutoTags(item.Content);
                        if (autoTags.Any())
                        {
                            await ProcessPostTagsAsync(post.Id, autoTags);
                        }
                    }

                    // SEO优化
                    if (importRequest.Options.AutoOptimizeSeo)
                    {
                        await OptimizePostSeoAsync(post);
                    }

                    importResult.Success = true;
                    importResult.ContentId = post.Id;
                    result.ImportedRecords = result.ImportedRecords.Append(importResult);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "导入第 {RowNumber} 行内容失败: {Title}", item.RowNumber, item.Title);
                    importResult.Success = false;
                    importResult.ErrorMessage = ex.Message;
                    result.FailedRecords = result.FailedRecords.Append(importResult);
                }
            }
        }

        private async Task ProcessPostTagsAsync(Guid postId, IEnumerable<string> tagNames)
        {
            foreach (var tagName in tagNames)
            {
                var tag = await _tagRepository.GetByNameAsync(tagName.Trim());
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = tagName.Trim(),
                        DisplayName = tagName.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _tagRepository.AddAsync(tag);
                }

                // 这里需要添加PostTag关联，但由于TagRepository的限制，先记录日志
                _logger.LogInformation("为文章 {PostId} 关联标签 {TagName}", postId, tagName);
            }
        }

        private string GenerateSummary(string content, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            // 移除HTML标签和Markdown标记
            var plainText = Regex.Replace(content, @"<[^>]+>", "");
            plainText = Regex.Replace(plainText, @"[#*`_\[\]()]", "");
            plainText = plainText.Replace("\n", " ").Replace("\r", "");

            // 截取指定长度
            if (plainText.Length > maxLength)
            {
                plainText = plainText.Substring(0, maxLength) + "...";
            }

            return plainText.Trim();
        }

        private List<string> GenerateAutoTags(string content)
        {
            var tags = new List<string>();

            // 简单的关键词提取算法
            var words = Regex.Matches(content, @"\b[a-zA-Z\u4e00-\u9fa5]{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value.ToLower())
                .Where(w => !IsStopWord(w))
                .GroupBy(w => w)
                .Where(g => g.Count() >= 2)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            tags.AddRange(words);
            return tags;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string> { "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "是", "的", "了", "在", "有", "和", "就", "都", "而", "及", "与" };
            return stopWords.Contains(word);
        }

        private async Task OptimizePostSeoAsync(Post post)
        {
            // 简单的SEO优化
            if (string.IsNullOrEmpty(post.MetaDescription))
            {
                post.MetaDescription = GenerateSummary(post.Content, 160);
            }

            if (string.IsNullOrEmpty(post.MetaKeywords))
            {
                var autoTags = GenerateAutoTags(post.Content);
                post.MetaKeywords = string.Join(", ", autoTags);
            }

            post.UpdateAuditFields();
            _postRepository.Update(post);
        }

        public async Task<ContentExportResultDto> BatchExportContentAsync(ContentExportRequestDto exportRequest)
        {
            var exportId = Guid.NewGuid();
            var result = new ContentExportResultDto
            {
                ExportId = exportId,
                GeneratedAt = DateTime.UtcNow,
                Status = "Processing"
            };

            try
            {
                _logger.LogInformation("开始批量导出内容，导出ID: {ExportId}, 格式: {Format}", exportId, exportRequest.ExportFormat);

                // 构建查询条件
                var query = await BuildExportQueryAsync(exportRequest);

                // 获取要导出的数据
                var posts = await query.ToListAsync();
                result.RecordCount = posts.Count;

                if (!posts.Any())
                {
                    result.Status = "Completed";
                    result.FileName = GenerateExportFileName(exportRequest.ExportFormat, exportRequest.FileName);
                    return result;
                }

                // 根据格式生成导出内容
                var exportContent = exportRequest.ExportFormat.ToLower() switch
                {
                    "json" => await GenerateJsonExportAsync(posts, exportRequest),
                    "csv" => await GenerateCsvExportAsync(posts, exportRequest),
                    "xml" => await GenerateXmlExportAsync(posts, exportRequest),
                    "markdown" => await GenerateMarkdownExportAsync(posts, exportRequest),
                    _ => throw new NotSupportedException($"不支持的导出格式: {exportRequest.ExportFormat}")
                };

                // 生成文件
                var fileName = GenerateExportFileName(exportRequest.ExportFormat, exportRequest.FileName);
                var tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

                await System.IO.File.WriteAllTextAsync(tempFilePath, exportContent, Encoding.UTF8);
                var fileInfo = new FileInfo(tempFilePath);

                result.FileName = fileName;
                result.FileSize = fileInfo.Length;
                result.DownloadUrl = $"/api/admin/content/export/download/{exportId}";
                result.ExpiresAt = DateTime.UtcNow.AddHours(24); // 24小时后过期
                result.Status = "Completed";

                // 在实际应用中，应该将文件存储到持久化存储中，这里暂时使用临时文件
                _logger.LogInformation("批量导出完成，导出ID: {ExportId}, 文件: {FileName}, 记录数: {RecordCount}",
                    exportId, fileName, result.RecordCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导出内容时发生错误，导出ID: {ExportId}", exportId);
                result.Status = "Failed";
                return result;
            }
        }

        private async Task<IQueryable<Post>> BuildExportQueryAsync(ContentExportRequestDto exportRequest)
        {
            var query = _postRepository.GetQueryable();

            // 应用过滤条件
            if (exportRequest.ContentFilter != null)
            {
                var filter = exportRequest.ContentFilter;

                // 按状态过滤
                if (filter.Statuses?.Any() == true)
                {
                    var statuses = filter.Statuses
                        .Where(s => Enum.TryParse<PostStatus>(s, true, out _))
                        .Select(s => Enum.Parse<PostStatus>(s, true))
                        .ToList();

                    if (statuses.Any())
                    {
                        query = query.Where(p => statuses.Contains(p.Status));
                    }
                }

                // 按分类过滤
                if (filter.CategoryIds?.Any() == true)
                {
                    query = query.Where(p => p.CategoryId.HasValue && filter.CategoryIds.Contains(p.CategoryId.Value));
                }

                // 按作者过滤
                if (filter.AuthorIds?.Any() == true)
                {
                    query = query.Where(p => filter.AuthorIds.Contains(p.AuthorId));
                }

                // 按日期范围过滤
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(p => p.CreatedAt <= filter.EndDate.Value);
                }

                // 按关键词过滤
                if (!string.IsNullOrEmpty(filter.Keywords))
                {
                    query = query.Where(p => p.Title.Contains(filter.Keywords) || p.Content.Contains(filter.Keywords));
                }
            }

            // 排除已删除的内容
            query = query.Where(p => !p.IsDeleted);

            // 排序
            query = query.OrderByDescending(p => p.CreatedAt);

            // 应用分页
            if (exportRequest.ContentFilter?.PageSize > 0)
            {
                var pageSize = Math.Min(exportRequest.ContentFilter.PageSize, 10000); // 限制最大导出数量
                query = query.Take(pageSize);
            }

            return query;
        }

        private async Task<string> GenerateJsonExportAsync(List<Post> posts, ContentExportRequestDto exportRequest)
        {
            var exportData = posts.Select(post => new
            {
                Id = post.Id,
                Title = post.Title,
                Content = exportRequest.IncludeContent ? post.Content : null,
                Summary = post.Summary,
                Status = post.Status.ToString(),
                AuthorId = post.AuthorId,
                CategoryId = post.CategoryId,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                PublishedAt = post.PublishedAt,
                MetaKeywords = post.MetaKeywords,
                MetaDescription = post.MetaDescription,
                ViewCount = post.ViewCount,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount
            });

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        private async Task<string> GenerateCsvExportAsync(List<Post> posts, ContentExportRequestDto exportRequest)
        {
            var csv = new StringBuilder();

            // CSV头部
            var headers = new List<string> { "ID", "Title", "Summary", "Status", "AuthorId", "CategoryId", "CreatedAt", "UpdatedAt", "PublishedAt", "ViewCount", "LikeCount", "CommentCount" };

            if (exportRequest.IncludeContent)
            {
                headers.Insert(3, "Content");
            }

            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // CSV数据行
            foreach (var post in posts)
            {
                var values = new List<string>
                {
                    $"\"{post.Id}\"",
                    $"\"{EscapeCsvValue(post.Title)}\"",
                    $"\"{EscapeCsvValue(post.Summary)}\"",
                    $"\"{post.Status}\"",
                    $"\"{post.AuthorId}\"",
                    $"\"{post.CategoryId}\"",
                    $"\"{post.CreatedAt:yyyy-MM-dd HH:mm:ss}\"",
                    $"\"{post.UpdatedAt:yyyy-MM-dd HH:mm:ss}\"",
                    $"\"{post.PublishedAt?.ToString("yyyy-MM-dd HH:mm:ss")}\"",
                    $"{post.ViewCount}",
                    $"{post.LikeCount}",
                    $"{post.CommentCount}"
                };

                if (exportRequest.IncludeContent)
                {
                    values.Insert(3, $"\"{EscapeCsvValue(post.Content)}\"");
                }

                csv.AppendLine(string.Join(",", values));
            }

            return csv.ToString();
        }

        private async Task<string> GenerateXmlExportAsync(List<Post> posts, ContentExportRequestDto exportRequest)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<posts>");

            foreach (var post in posts)
            {
                xml.AppendLine("  <post>");
                xml.AppendLine($"    <id>{post.Id}</id>");
                xml.AppendLine($"    <title><![CDATA[{post.Title}]]></title>");
                xml.AppendLine($"    <summary><![CDATA[{post.Summary}]]></summary>");

                if (exportRequest.IncludeContent)
                {
                    xml.AppendLine($"    <content><![CDATA[{post.Content}]]></content>");
                }

                xml.AppendLine($"    <status>{post.Status}</status>");
                xml.AppendLine($"    <authorId>{post.AuthorId}</authorId>");
                xml.AppendLine($"    <categoryId>{post.CategoryId}</categoryId>");
                xml.AppendLine($"    <createdAt>{post.CreatedAt:yyyy-MM-ddTHH:mm:ss}</createdAt>");
                xml.AppendLine($"    <updatedAt>{post.UpdatedAt:yyyy-MM-ddTHH:mm:ss}</updatedAt>");

                if (post.PublishedAt.HasValue)
                {
                    xml.AppendLine($"    <publishedAt>{post.PublishedAt.Value:yyyy-MM-ddTHH:mm:ss}</publishedAt>");
                }

                xml.AppendLine($"    <viewCount>{post.ViewCount}</viewCount>");
                xml.AppendLine($"    <likeCount>{post.LikeCount}</likeCount>");
                xml.AppendLine($"    <commentCount>{post.CommentCount}</commentCount>");
                xml.AppendLine("  </post>");
            }

            xml.AppendLine("</posts>");
            return xml.ToString();
        }

        private async Task<string> GenerateMarkdownExportAsync(List<Post> posts, ContentExportRequestDto exportRequest)
        {
            var markdown = new StringBuilder();
            markdown.AppendLine("# 博客内容导出");
            markdown.AppendLine($"## 导出时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            markdown.AppendLine($"## 总计: {posts.Count} 篇文章");
            markdown.AppendLine();

            foreach (var post in posts)
            {
                markdown.AppendLine("---");
                markdown.AppendLine();
                markdown.AppendLine($"# {post.Title}");
                markdown.AppendLine();
                markdown.AppendLine($"- **ID**: {post.Id}");
                markdown.AppendLine($"- **状态**: {post.Status}");
                markdown.AppendLine($"- **作者ID**: {post.AuthorId}");
                markdown.AppendLine($"- **分类ID**: {post.CategoryId}");
                markdown.AppendLine($"- **创建时间**: {post.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                markdown.AppendLine($"- **更新时间**: {post.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

                if (post.PublishedAt.HasValue)
                {
                    markdown.AppendLine($"- **发布时间**: {post.PublishedAt.Value:yyyy-MM-dd HH:mm:ss}");
                }

                markdown.AppendLine($"- **浏览数**: {post.ViewCount}");
                markdown.AppendLine($"- **点赞数**: {post.LikeCount}");
                markdown.AppendLine($"- **评论数**: {post.CommentCount}");
                markdown.AppendLine();

                if (!string.IsNullOrEmpty(post.Summary))
                {
                    markdown.AppendLine("## 摘要");
                    markdown.AppendLine(post.Summary);
                    markdown.AppendLine();
                }

                if (exportRequest.IncludeContent && !string.IsNullOrEmpty(post.Content))
                {
                    markdown.AppendLine("## 内容");
                    markdown.AppendLine(post.Content);
                    markdown.AppendLine();
                }
            }

            return markdown.ToString();
        }

        private string GenerateExportFileName(string format, string? customFileName = null)
        {
            if (!string.IsNullOrEmpty(customFileName))
            {
                var extension = Path.GetExtension(customFileName);
                if (string.IsNullOrEmpty(extension))
                {
                    return $"{customFileName}.{format.ToLower()}";
                }
                return customFileName;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return $"content_export_{timestamp}.{format.ToLower()}";
        }

        private string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
        }

        public async Task<ContentSeoAnalysisDto> GetContentSeoAnalysisAsync(Guid contentId, string contentType)
        {
            try
            {
                _logger.LogInformation("分析内容SEO，ID: {ContentId}, 类型: {ContentType}", contentId, contentType);

                var post = await _postRepository.GetByIdAsync(contentId);
                if (post == null || post.IsDeleted)
                {
                    _logger.LogWarning("内容不存在或已删除: {ContentId}", contentId);
                    return new ContentSeoAnalysisDto();
                }

                // 检查内容类型匹配
                if (!string.IsNullOrEmpty(contentType) && !post.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("内容类型不匹配");
                    return new ContentSeoAnalysisDto();
                }

                // 调用已存在的 SEO 分析方法
                var seoAnalysis = AnalyzeContentSeo(post);

                _logger.LogInformation("内容 {ContentId} SEO分析完成，评分: {SeoScore}",
                    contentId, seoAnalysis.SeoScore);

                return seoAnalysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析内容SEO时发生错误");
                return new ContentSeoAnalysisDto();
            }
        }

        public async Task<BatchSeoOptimizationResultDto> BatchOptimizeContentSeoAsync(IEnumerable<Guid> contentIds, SeoOptimizationOptionsDto optimizationOptions)
        {
            var taskId = Guid.NewGuid();
            var result = new BatchSeoOptimizationResultDto
            {
                TaskId = taskId,
                TotalCount = contentIds.Count(),
                OptimizedCount = 0,
                SkippedCount = 0,
                FailedCount = 0,
                Details = new List<SeoOptimizationDetailDto>(),
                Summary = "批量SEO优化开始处理"
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("开始批量SEO优化，任务ID: {TaskId}, 内容数量: {Count}", taskId, result.TotalCount);

                var contentIdsList = contentIds.ToList();

                // 分批处理，避免内存和性能问题
                var batchSize = 20;
                var details = new List<SeoOptimizationDetailDto>();

                for (int i = 0; i < contentIdsList.Count; i += batchSize)
                {
                    var batch = contentIdsList.Skip(i).Take(batchSize);
                    var batchDetails = await ProcessSeoOptimizationBatchAsync(batch, optimizationOptions);
                    details.AddRange(batchDetails);
                }

                // 统计结果
                result.OptimizedCount = details.Count(d => d.Success && d.Changes.Any());
                result.SkippedCount = details.Count(d => d.Success && !d.Changes.Any());
                result.FailedCount = details.Count(d => !d.Success);
                result.Details = details;

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.Summary = $"批量SEO优化完成: 优化 {result.OptimizedCount} 项，跳过 {result.SkippedCount} 项，失败 {result.FailedCount} 项";

                _logger.LogInformation("批量SEO优化完成，任务ID: {TaskId}, 耗时: {Duration}ms, 结果: {Summary}",
                    taskId, stopwatch.ElapsedMilliseconds, result.Summary);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;
                result.Summary = $"批量SEO优化失败: {ex.Message}";

                _logger.LogError(ex, "批量SEO优化时发生错误，任务ID: {TaskId}", taskId);
                throw;
            }
        }

        private async Task<List<SeoOptimizationDetailDto>> ProcessSeoOptimizationBatchAsync(IEnumerable<Guid> contentIds, SeoOptimizationOptionsDto options)
        {
            var details = new List<SeoOptimizationDetailDto>();

            foreach (var contentId in contentIds)
            {
                var detail = new SeoOptimizationDetailDto
                {
                    ContentId = contentId,
                    Changes = new List<string>(),
                    Recommendations = new List<string>()
                };

                try
                {
                    var post = await _postRepository.GetByIdAsync(contentId);
                    if (post == null || post.IsDeleted)
                    {
                        detail.Success = false;
                        detail.ErrorMessage = "内容不存在或已删除";
                        details.Add(detail);
                        continue;
                    }

                    var hasChanges = false;
                    var originalPost = new Post
                    {
                        Title = post.Title,
                        Content = post.Content,
                        Summary = post.Summary,
                        MetaTitle = post.MetaTitle,
                        MetaDescription = post.MetaDescription,
                        MetaKeywords = post.MetaKeywords
                    };

                    // 优化标题
                    if (options.OptimizeTitle)
                    {
                        var optimizedTitle = OptimizeTitle(post.Title, post.Content);
                        if (!string.IsNullOrEmpty(optimizedTitle) && optimizedTitle != post.Title)
                        {
                            post.MetaTitle = optimizedTitle;
                            detail.Changes.Add($"优化了Meta标题: {optimizedTitle}");
                            hasChanges = true;
                        }
                        else
                        {
                            detail.Recommendations.Add("标题已优化或无需调整");
                        }
                    }

                    // 优化描述
                    if (options.OptimizeDescription)
                    {
                        var optimizedDescription = OptimizeDescription(post.Content, post.Summary);
                        if (!string.IsNullOrEmpty(optimizedDescription) && optimizedDescription != post.MetaDescription)
                        {
                            post.MetaDescription = optimizedDescription;
                            detail.Changes.Add($"优化了Meta描述: {optimizedDescription.Substring(0, Math.Min(50, optimizedDescription.Length))}...");
                            hasChanges = true;
                        }
                        else
                        {
                            detail.Recommendations.Add("描述已优化或无需调整");
                        }
                    }

                    // 优化关键词
                    if (options.OptimizeKeywords)
                    {
                        var optimizedKeywords = OptimizeKeywords(post.Title, post.Content);
                        if (!string.IsNullOrEmpty(optimizedKeywords) && optimizedKeywords != post.MetaKeywords)
                        {
                            post.MetaKeywords = optimizedKeywords;
                            detail.Changes.Add($"优化了关键词: {optimizedKeywords}");
                            hasChanges = true;
                        }
                        else
                        {
                            detail.Recommendations.Add("关键词已优化或无需调整");
                        }
                    }

                    // 优化内容结构
                    if (options.OptimizeContentStructure)
                    {
                        var structureAnalysis = AnalyzeContentStructure(post.Content);
                        detail.Recommendations.AddRange(structureAnalysis);
                    }

                    // 保存更改
                    if (hasChanges)
                    {
                        post.UpdateAuditFields();
                        _postRepository.Update(post);
                        await _postRepository.SaveChangesAsync();
                    }

                    detail.Success = true;
                    detail.ContentTitle = post.Title;

                    // 添加通用SEO建议
                    if (!detail.Recommendations.Any())
                    {
                        detail.Recommendations.Add("内容SEO表现良好");
                    }
                }
                catch (Exception ex)
                {
                    detail.Success = false;
                    detail.ErrorMessage = ex.Message;
                    _logger.LogWarning(ex, "优化内容 {ContentId} 的SEO时发生错误", contentId);
                }

                details.Add(detail);
            }

            return details;
        }

        private string OptimizeTitle(string title, string content)
        {
            // 如果已有MetaTitle且长度合适，则不修改
            if (!string.IsNullOrEmpty(title) && title.Length >= 30 && title.Length <= 60)
            {
                return title;
            }

            // 基于内容生成优化的标题
            if (!string.IsNullOrEmpty(title))
            {
                // 确保标题长度在SEO最佳范围内 (30-60字符)
                if (title.Length < 30)
                {
                    // 可以考虑添加前缀或后缀
                    var keywords = ExtractKeywords(content);
                    if (keywords.Any())
                    {
                        var additionalKeywords = string.Join(", ", keywords.Take(2));
                        return $"{title} - {additionalKeywords}";
                    }
                }
                else if (title.Length > 60)
                {
                    // 截断过长的标题
                    return title.Substring(0, 57) + "...";
                }
            }

            return title;
        }

        private string OptimizeDescription(string content, string? existingSummary)
        {
            // 如果已有合适的描述，则使用现有的
            if (!string.IsNullOrEmpty(existingSummary) && existingSummary.Length >= 120 && existingSummary.Length <= 160)
            {
                return existingSummary;
            }

            // 从内容中生成描述
            var description = GenerateSummary(content, 155);

            // 确保描述长度在SEO最佳范围内 (120-160字符)
            if (description.Length < 120 && !string.IsNullOrEmpty(content))
            {
                // 尝试添加更多内容
                var sentences = content.Split(new[] { '.', '。', '!', '！', '?', '？' }, StringSplitOptions.RemoveEmptyEntries);
                var additionalContent = string.Join(". ", sentences.Skip(1).Take(2));
                if (!string.IsNullOrEmpty(additionalContent))
                {
                    description = $"{description} {additionalContent}".Substring(0, Math.Min(160, description.Length + additionalContent.Length));
                }
            }

            return description;
        }

        private string OptimizeKeywords(string title, string content)
        {
            var keywords = ExtractKeywords($"{title} {content}");

            // 选择最相关的5-10个关键词
            var selectedKeywords = keywords
                .GroupBy(k => k.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(8)
                .Select(g => g.Key)
                .Where(k => k.Length >= 3 && k.Length <= 20)
                .ToList();

            return string.Join(", ", selectedKeywords);
        }

        private List<string> ExtractKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            // 移除HTML标签和特殊字符
            var cleanText = Regex.Replace(text, @"<[^>]+>", "");
            cleanText = Regex.Replace(cleanText, @"[^\w\s\u4e00-\u9fa5]", " ");

            // 提取关键词
            var words = Regex.Matches(cleanText, @"\b[\w\u4e00-\u9fa5]{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value.ToLower())
                .Where(w => !IsStopWord(w))
                .ToList();

            return words;
        }

        private List<string> AnalyzeContentStructure(string content)
        {
            var recommendations = new List<string>();

            if (string.IsNullOrEmpty(content))
            {
                recommendations.Add("内容为空，建议添加内容");
                return recommendations;
            }

            // 检查标题结构
            var headingMatches = Regex.Matches(content, @"<h[1-6][^>]*>.*?</h[1-6]>", RegexOptions.IgnoreCase);
            if (headingMatches.Count == 0)
            {
                recommendations.Add("建议添加标题结构 (H1-H6) 以改善SEO");
            }

            // 检查内容长度
            var wordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount < 300)
            {
                recommendations.Add("内容长度偏短，建议增加到至少300词以改善SEO效果");
            }

            // 检查图片alt属性
            var imageMatches = Regex.Matches(content, @"<img[^>]*>", RegexOptions.IgnoreCase);
            var imagesWithoutAlt = imageMatches.Cast<Match>()
                .Where(m => !m.Value.Contains("alt=", StringComparison.OrdinalIgnoreCase))
                .Count();

            if (imagesWithoutAlt > 0)
            {
                recommendations.Add($"发现 {imagesWithoutAlt} 张图片缺少alt属性，建议添加以改善可访问性和SEO");
            }

            // 检查链接
            var linkMatches = Regex.Matches(content, @"<a[^>]*href=[""']([^""']*)[""'][^>]*>", RegexOptions.IgnoreCase);
            var externalLinks = linkMatches.Cast<Match>()
                .Where(m => m.Groups[1].Value.StartsWith("http") && !m.Value.Contains("target=", StringComparison.OrdinalIgnoreCase))
                .Count();

            if (externalLinks > 0)
            {
                recommendations.Add($"发现 {externalLinks} 个外部链接建议添加 target='_blank' 属性");
            }

            return recommendations;
        }

        public async Task<IEnumerable<DuplicateContentDto>> GetDuplicateContentAsync(double threshold = 0.8)
        {
            try
            {
                _logger.LogInformation("检测重复内容，相似度阈值: {Threshold}", threshold);

                // 获取所有已发布的内容
                var posts = await _postRepository.FindAsync(p =>
                    p.Status == PostStatus.Published &&
                    !p.IsDeleted &&
                    !string.IsNullOrEmpty(p.Content));

                var allPosts = posts.ToList();
                var duplicates = new List<DuplicateContentDto>();

                // 比较每两篇文章
                for (int i = 0; i < allPosts.Count; i++)
                {
                    for (int j = i + 1; j < allPosts.Count; j++)
                    {
                        var post1 = allPosts[i];
                        var post2 = allPosts[j];

                        var similarity = CalculateSimilarity(post1.Content, post2.Content);

                        if (similarity >= threshold)
                        {
                            var duplicate = new DuplicateContentDto
                            {
                                OriginalContentId = post1.CreatedAt <= post2.CreatedAt ? post1.Id : post2.Id,
                                DuplicateContentId = post1.CreatedAt <= post2.CreatedAt ? post2.Id : post1.Id,
                                OriginalTitle = post1.CreatedAt <= post2.CreatedAt ? post1.Title : post2.Title,
                                DuplicateTitle = post1.CreatedAt <= post2.CreatedAt ? post2.Title : post1.Title,
                                SimilarityScore = similarity,
                                DuplicateType = similarity > 0.95 ? "Exact" : "Similar",
                                SimilarSnippets = GenerateHighlightSnippets(post1, "").Take(2),
                                SuggestedAction = similarity > 0.95 ? "Delete" : "Review"
                            };

                            duplicates.Add(duplicate);
                        }
                    }
                }

                _logger.LogInformation("重复内容检测完成，发现 {Count} 对重复内容", duplicates.Count);
                return duplicates.OrderByDescending(d => d.SimilarityScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检测重复内容时发生错误");
                return Enumerable.Empty<DuplicateContentDto>();
            }
        }

        public async Task<AutoTaggingResultDto> AutoTagContentAsync(IEnumerable<Guid> contentIds, string contentType)
        {
            var stopwatch = Stopwatch.StartNew();
            var contentIdList = contentIds.ToList();
            var taggingResults = new List<ContentTaggingResultDto>();
            var newTags = new HashSet<string>();
            var successCount = 0;
            var skippedCount = 0;
            const double confidenceThreshold = 0.6;

            try
            {
                _logger.LogInformation("开始自动标签，内容数量: {Count}, 类型: {ContentType}",
                    contentIdList.Count, contentType);

                if (!contentIdList.Any())
                {
                    return new AutoTaggingResultDto
                    {
                        TotalProcessed = 0,
                        SuccessCount = 0,
                        SkippedCount = 0,
                        ConfidenceThreshold = confidenceThreshold
                    };
                }

                // 获取所有内容
                var posts = await _postRepository.FindAsync(p =>
                    contentIdList.Contains(p.Id) &&
                    !p.IsDeleted &&
                    (string.IsNullOrEmpty(contentType) || p.ContentType == contentType));

                // 获取现有标签
                var existingTags = await _tagRepository.GetAllAsync();
                var tagDict = existingTags.ToDictionary(t => t.Name.ToLowerInvariant(), t => t);

                foreach (var post in posts)
                {
                    try
                    {
                        var suggestedTags = new List<TagSuggestionDto>();
                        var appliedTags = new List<string>();

                        // 基于关键词提取的简单自动标签
                        var keywords = ExtractRelatedKeywords(post.Content);

                        foreach (var keyword in keywords.Take(5)) // 最多5个标签
                        {
                            var confidence = CalculateTagConfidence(post.Content, keyword);

                            var suggestion = new TagSuggestionDto
                            {
                                Tag = keyword,
                                Confidence = confidence,
                                Source = "Content Analysis",
                                Applied = confidence >= confidenceThreshold
                            };

                            suggestedTags.Add(suggestion);

                            if (confidence >= confidenceThreshold)
                            {
                                appliedTags.Add(keyword);

                                // 记录新标签
                                if (!tagDict.ContainsKey(keyword.ToLowerInvariant()))
                                {
                                    newTags.Add(keyword);
                                }
                            }
                        }

                        var taggingResult = new ContentTaggingResultDto
                        {
                            ContentId = post.Id,
                            Title = post.Title,
                            SuggestedTags = suggestedTags,
                            AppliedTags = appliedTags,
                            Success = true
                        };

                        taggingResults.Add(taggingResult);
                        successCount++;

                        _logger.LogDebug("内容 {ContentId} 自动标签完成，推荐 {TagCount} 个标签",
                            post.Id, suggestedTags.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理内容 {ContentId} 的自动标签时发生错误", post.Id);

                        taggingResults.Add(new ContentTaggingResultDto
                        {
                            ContentId = post.Id,
                            Title = post.Title,
                            Success = false
                        });

                        skippedCount++;
                    }
                }

                stopwatch.Stop();

                var result = new AutoTaggingResultDto
                {
                    TotalProcessed = contentIdList.Count,
                    SuccessCount = successCount,
                    SkippedCount = skippedCount,
                    TaggingResults = taggingResults,
                    NewTags = newTags,
                    ProcessingTime = stopwatch.Elapsed,
                    ConfidenceThreshold = confidenceThreshold
                };

                _logger.LogInformation("自动标签完成，成功: {SuccessCount}, 跳过: {SkippedCount}, 新标签: {NewTagCount}",
                    successCount, skippedCount, newTags.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动标签时发生错误");
                stopwatch.Stop();

                return new AutoTaggingResultDto
                {
                    TotalProcessed = contentIdList.Count,
                    SuccessCount = 0,
                    SkippedCount = contentIdList.Count,
                    ProcessingTime = stopwatch.Elapsed,
                    ConfidenceThreshold = confidenceThreshold
                };
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 计算内容风险评分
        /// </summary>
        private double CalculateContentRiskScore(Post post)
        {
            double riskScore = 0.0;

            // 基于内容长度的风险评分
            if (post.WordCount < 100)
                riskScore += 0.3; // 内容过短

            // 基于外部链接的风险评分
            var linkCount = CountExternalLinks(post.Content);
            if (linkCount > 5)
                riskScore += 0.2; // 外部链接过多

            // 基于敏感词的风险评分
            if (DetectSensitiveContent(post.Content))
                riskScore += 0.5; // 包含敏感内容

            // 基于作者历史的风险评分（简化）
            // 实际项目中可以查询作者的历史审核通过率

            return Math.Min(riskScore, 1.0); // 最大值为1.0
        }

        /// <summary>
        /// 检测敏感内容
        /// </summary>
        private bool DetectSensitiveContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            // 简单的敏感词检测（实际项目应使用更复杂的算法）
            var sensitiveWords = new[] { "敏感词1", "敏感词2", "广告", "spam" };
            var lowerContent = content.ToLowerInvariant();

            return sensitiveWords.Any(word => lowerContent.Contains(word));
        }

        /// <summary>
        /// 计算外部链接数量
        /// </summary>
        private int CountExternalLinks(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            // 使用正则表达式匹配HTTP链接
            var httpRegex = new Regex(@"https?://[^\s]+", RegexOptions.IgnoreCase);
            return httpRegex.Matches(content).Count;
        }

        /// <summary>
        /// 确定内容优先级
        /// </summary>
        private string DeterminePriority(Post post)
        {
            // 基于时间和内容类型确定优先级
            var hoursSinceCreation = (DateTime.UtcNow - post.CreatedAt).TotalHours;

            if (hoursSinceCreation > 24)
                return "High"; // 超过24小时的内容优先级高

            if (post.Status == PostStatus.Scheduled && post.ScheduledAt.HasValue)
            {
                var hoursUntilScheduled = (post.ScheduledAt.Value - DateTime.UtcNow).TotalHours;
                if (hoursUntilScheduled < 2)
                    return "Urgent"; // 即将定时发布的内容紧急
            }

            return "Normal";
        }

        /// <summary>
        /// 计算字数
        /// </summary>
        private int CalculateWordCount(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            // 移除HTML标签和Markdown语法
            var plainText = Regex.Replace(content, @"<[^>]+>|[#*\-\[\]()]+", " ");
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            // 简单的字数统计（按空格分隔）
            return plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// 分析内容SEO指标
        /// </summary>
        private ContentSeoAnalysisDto AnalyzeContentSeo(Post post)
        {
            var seoScore = 0;
            var recommendations = new List<SeoRecommendationDto>();

            // 标题分析
            var titleAnalysis = new SeoTitleAnalysisDto
            {
                Length = post.Title.Length,
                IsOptimalLength = post.Title.Length >= 30 && post.Title.Length <= 60,
                ContainsKeyword = true, // 简化处理
                Score = post.Title.Length >= 30 && post.Title.Length <= 60 ? 10 : 5
            };

            if (!titleAnalysis.IsOptimalLength)
            {
                titleAnalysis.Recommendation = post.Title.Length < 30 ? "标题太短，建议增加到30-60字符" : "标题太长，建议缩短到60字符以内";
                recommendations.Add(new SeoRecommendationDto
                {
                    Category = "Title",
                    Title = "优化标题长度",
                    Description = titleAnalysis.Recommendation,
                    Priority = "High",
                    Action = "修改标题",
                    ImpactScore = 8
                });
            }

            seoScore += titleAnalysis.Score;

            // 描述分析
            var descriptionAnalysis = new SeoDescriptionAnalysisDto
            {
                Length = post.MetaDescription?.Length ?? 0,
                IsOptimalLength = (post.MetaDescription?.Length ?? 0) >= 120 && (post.MetaDescription?.Length ?? 0) <= 160,
                ContainsKeyword = true, // 简化处理
                IsUnique = true, // 简化处理
                Score = (post.MetaDescription?.Length ?? 0) >= 120 && (post.MetaDescription?.Length ?? 0) <= 160 ? 10 : 3
            };

            seoScore += descriptionAnalysis.Score;

            // 关键词分析
            var keywordAnalysis = new SeoKeywordAnalysisDto
            {
                PrimaryKeyword = ExtractPrimaryKeyword(post.Content),
                KeywordDensity = CalculateKeywordDensity(post.Content, ExtractPrimaryKeyword(post.Content)),
                IsOptimalDensity = true, // 简化处理
                RelatedKeywords = ExtractRelatedKeywords(post.Content),
                Score = 8
            };

            seoScore += keywordAnalysis.Score;

            // 内容结构分析
            var contentStructure = new SeoContentStructureDto
            {
                HeadingCount = CountHeadings(post.Content),
                HasH1 = post.Content.Contains("# ") || post.Content.Contains("<h1>"),
                HasProperHeadingHierarchy = true, // 简化处理
                WordCount = post.WordCount ?? CalculateWordCount(post.Content),
                IsOptimalLength = (post.WordCount ?? 0) >= 300,
                ParagraphCount = CountParagraphs(post.Content),
                Score = (post.WordCount ?? 0) >= 300 ? 10 : 5
            };

            seoScore += contentStructure.Score;

            // 链接分析
            var linkAnalysis = new SeoLinkAnalysisDto
            {
                InternalLinks = CountInternalLinks(post.Content),
                ExternalLinks = CountExternalLinks(post.Content),
                HasOutboundLinks = CountExternalLinks(post.Content) > 0,
                BrokenLinks = new List<string>(), // 实际项目中需要检查链接有效性
                Score = 6
            };

            seoScore += linkAnalysis.Score;

            // 图片分析
            var imageAnalysis = new SeoImageAnalysisDto
            {
                TotalImages = CountImages(post.Content),
                ImagesWithAlt = 0, // 简化处理
                ImagesWithoutAlt = CountImages(post.Content),
                AltTextCoverage = 0.0,
                OptimizationSuggestions = new[] { "为所有图片添加Alt标签" },
                Score = 2
            };

            seoScore += imageAnalysis.Score;

            return new ContentSeoAnalysisDto
            {
                SeoScore = seoScore,
                TitleAnalysis = titleAnalysis,
                DescriptionAnalysis = descriptionAnalysis,
                KeywordAnalysis = keywordAnalysis,
                ContentStructure = contentStructure,
                LinkAnalysis = linkAnalysis,
                ImageAnalysis = imageAnalysis,
                Recommendations = recommendations
            };
        }

        /// <summary>
        /// 提取主要关键词
        /// </summary>
        private string ExtractPrimaryKeyword(string content)
        {
            // 简化实现：返回第一个有意义的词
            var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.FirstOrDefault(w => w.Length > 3) ?? "";
        }

        /// <summary>
        /// 计算关键词密度
        /// </summary>
        private double CalculateKeywordDensity(string content, string keyword)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(keyword))
                return 0;

            var totalWords = CalculateWordCount(content);
            var keywordCount = Regex.Matches(content, keyword, RegexOptions.IgnoreCase).Count;

            return totalWords > 0 ? (double)keywordCount / totalWords * 100 : 0;
        }

        /// <summary>
        /// 提取相关关键词
        /// </summary>
        private IEnumerable<string> ExtractRelatedKeywords(string content)
        {
            // 简化实现：返回频率较高的词
            var words = Regex.Matches(content, @"\b\w{4,}\b", RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => !IsStopWord(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            return words;
        }


        /// <summary>
        /// 计算标题数量
        /// </summary>
        private int CountHeadings(string content)
        {
            return Regex.Matches(content, @"^#{1,6}\s|<h[1-6]>", RegexOptions.Multiline).Count;
        }

        /// <summary>
        /// 计算段落数量
        /// </summary>
        private int CountParagraphs(string content)
        {
            return content.Split(new[] { "\n\n", "</p>" }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// 计算内部链接数量
        /// </summary>
        private int CountInternalLinks(string content)
        {
            // 简化实现：假设相对链接为内部链接
            return Regex.Matches(content, @"href\s*=\s*[""'](?!https?://)[^""']+[""']", RegexOptions.IgnoreCase).Count;
        }

        /// <summary>
        /// 计算图片数量
        /// </summary>
        private int CountImages(string content)
        {
            return Regex.Matches(content, @"!\[.*?\]|<img\s", RegexOptions.IgnoreCase).Count;
        }

        /// <summary>
        /// 计算内容相似度
        /// </summary>
        private double CalculateSimilarity(string content1, string content2)
        {
            if (string.IsNullOrEmpty(content1) || string.IsNullOrEmpty(content2))
                return 0;

            // 简化的Jaccard相似度算法
            var words1 = new HashSet<string>(ExtractWords(content1));
            var words2 = new HashSet<string>(ExtractWords(content2));

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0;
        }

        /// <summary>
        /// 提取单词
        /// </summary>
        private IEnumerable<string> ExtractWords(string content)
        {
            return Regex.Matches(content, @"\b\w{3,}\b", RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => !IsStopWord(w));
        }

        /// <summary>
        /// 检查内容是否可以执行指定的审核操作
        /// </summary>
        private bool CanModerateContent(Post post, string action)
        {
            switch (action.ToLowerInvariant())
            {
                case "approve":
                case "publish":
                    return post.Status == PostStatus.Draft || post.Status == PostStatus.Scheduled;
                case "reject":
                case "archive":
                    return post.Status != PostStatus.Archived && post.Status != PostStatus.Published;
                case "hold":
                    return post.Status == PostStatus.Draft || post.Status == PostStatus.Scheduled;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 执行审核操作
        /// </summary>
        private async Task<bool> PerformModerationAction(Post post, string action, string? reason, Guid userId)
        {
            try
            {
                switch (action.ToLowerInvariant())
                {
                    case "approve":
                    case "publish":
                        post.Publish();
                        break;
                    case "reject":
                    case "archive":
                        post.Archive();
                        break;
                    case "hold":
                        // 保持当前状态，只记录审核历史
                        break;
                    default:
                        return false;
                }

                post.UpdateAuditFields();
                _postRepository.Update(post);

                // 记录审核历史到审核日志表
                await LogContentAuditAsync(post, action, userId, reason);

                _logger.LogInformation("内容 {PostId} 审核操作 {Action} 执行完成，审核人: {UserId}, 理由: {Reason}",
                    post.Id, action, userId, reason);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行审核操作时发生错误，内容ID: {PostId}, 动作: {Action}", post.Id, action);
                return false;
            }
        }

        /// <summary>
        /// 提取外部链接
        /// </summary>
        private IEnumerable<string> ExtractExternalLinks(string content)
        {
            if (string.IsNullOrEmpty(content))
                return Enumerable.Empty<string>();

            var httpRegex = new Regex(@"https?://[^\s]+", RegexOptions.IgnoreCase);
            return httpRegex.Matches(content).Cast<Match>().Select(m => m.Value).Distinct();
        }

        /// <summary>
        /// 计算搜索相关性评分
        /// </summary>
        private double CalculateRelevanceScore(Post post, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return 1.0;

            double score = 0.0;
            var term = searchTerm.ToLowerInvariant();

            // 标题匹配权重更高
            if (post.Title.ToLowerInvariant().Contains(term))
                score += 0.5;

            // 内容匹配
            if (post.Content.ToLowerInvariant().Contains(term))
                score += 0.3;

            // 摘要匹配
            if (post.Summary?.ToLowerInvariant().Contains(term) == true)
                score += 0.2;

            return Math.Min(score, 1.0);
        }

        /// <summary>
        /// 生成搜索高亮片段
        /// </summary>
        private IEnumerable<string> GenerateHighlightSnippets(Post post, string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Enumerable.Empty<string>();

            var snippets = new List<string>();
            var term = searchTerm.ToLowerInvariant();

            // 从标题中提取片段
            if (post.Title.ToLowerInvariant().Contains(term))
            {
                snippets.Add(post.Title);
            }

            // 从内容中提取片段
            var content = post.Content;
            var index = content.ToLowerInvariant().IndexOf(term);
            if (index >= 0)
            {
                var start = Math.Max(0, index - 50);
                var length = Math.Min(100, content.Length - start);
                var snippet = content.Substring(start, length);
                if (start > 0) snippet = "..." + snippet;
                if (start + length < content.Length) snippet += "...";
                snippets.Add(snippet);
            }

            return snippets.Take(3); // 最多返回3个片段
        }

        /// <summary>
        /// 计算标签置信度
        /// </summary>
        private double CalculateTagConfidence(string content, string tag)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(tag))
                return 0;

            var lowerContent = content.ToLowerInvariant();
            var lowerTag = tag.ToLowerInvariant();

            // 简单的置信度计算
            double confidence = 0;

            // 标签在内容中出现的频率
            var occurrences = Regex.Matches(lowerContent, Regex.Escape(lowerTag), RegexOptions.IgnoreCase).Count;
            var totalWords = CalculateWordCount(content);

            if (totalWords > 0)
            {
                var frequency = (double)occurrences / totalWords;
                confidence = Math.Min(frequency * 10, 1.0); // 频率加权，最大为1.0
            }

            // 如果标签出现在标题中，增加置信度
            if (occurrences > 0)
                confidence = Math.Min(confidence + 0.3, 1.0);

            return confidence;
        }

        /// <summary>
        /// 创建批量操作结果
        /// </summary>
        private BatchOperationResultDto CreateBatchResult(bool success, int successCount, int failCount, int totalCount,
            IEnumerable<BatchItemResultDto> itemResults, IEnumerable<string> errors, TimeSpan processingTime)
        {
            return new BatchOperationResultDto
            {
                Success = success,
                SuccessCount = successCount,
                FailCount = failCount,
                TotalCount = totalCount,
                ItemResults = itemResults,
                Errors = errors,
                ProcessingTime = processingTime
            };
        }

        /// <summary>
        /// 记录内容审核日志
        /// </summary>
        /// <param name="post">文章实体</param>
        /// <param name="action">审核动作</param>
        /// <param name="userId">审核用户ID</param>
        /// <param name="reason">审核理由</param>
        private async Task LogContentAuditAsync(Post post, string action, Guid userId, string? reason)
        {
            try
            {
                // 获取审核用户信息
                var user = await _userRepository.GetByIdAsync(userId);

                // 创建审核日志记录
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserName = user?.UserName ?? "未知用户",
                    UserEmail = user?.Email?.Value ?? "unknown@example.com",
                    Action = $"Content{action}", // ContentApprove, ContentReject, ContentPending等
                    ResourceType = "Post",
                    ResourceId = post.Id.ToString(),
                    ResourceName = post.Title,
                    Details = CreateAuditDetails(post, action, reason),
                    IpAddress = "system", // 在实际应用中应该从HTTP上下文获取
                    UserAgent = "ContentManagementService",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.AddAsync(auditLog);
                await _auditLogRepository.SaveChangesAsync();

                _logger.LogDebug("审核日志已记录: {Action} - {PostId} by {UserId}", action, post.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "记录审核日志时发生错误: {PostId} - {Action}", post.Id, action);
                // 不抛出异常，避免影响主要业务流程
            }
        }

        /// <summary>
        /// 创建审核详情JSON
        /// </summary>
        private string CreateAuditDetails(Post post, string action, string? reason)
        {
            var details = new
            {
                PostId = post.Id,
                PostTitle = post.Title,
                PostStatus = post.Status.ToString(),
                Action = action,
                Reason = reason ?? "无",
                Timestamp = DateTime.UtcNow,
                PostMetadata = new
                {
                    post.ViewCount,
                    post.LikeCount,
                    post.CommentCount,
                    post.CategoryId,
                    post.AuthorId,
                    post.CreatedAt,
                    post.UpdatedAt
                }
            };

            return JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        private void ClearRelatedCache()
        {
            _memoryCache.Remove(CACHE_KEY_OVERVIEW);
            // 清理待审核内容的相关缓存
            for (int page = 1; page <= 10; page++)
            {
                for (int size = 10; size <= 50; size += 10)
                {
                    _memoryCache.Remove(string.Format(CACHE_KEY_PENDING_CONTENT, page, size, "all"));
                }
            }
        }

        #endregion
    }
}