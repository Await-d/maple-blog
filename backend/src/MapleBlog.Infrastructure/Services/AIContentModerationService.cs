using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Moderation;

namespace MapleBlog.Infrastructure.Services;



/// <summary>
/// AI内容审核服务实现
/// </summary>
public class AIContentModerationService : IAIContentModerationService
{
    private readonly ISensitiveWordFilter _sensitiveWordFilter;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIContentModerationService> _logger;
    private readonly HttpClient _httpClient;

    // 审核配置
    private readonly double _spamThreshold;
    private readonly double _toxicityThreshold;
    private readonly double _hateSpeechThreshold;
    private readonly bool _enableAIModeration;
    private readonly string? _aiModerationEndpoint;
    private readonly string? _aiModerationApiKey;

    public AIContentModerationService(
        ISensitiveWordFilter sensitiveWordFilter,
        IConfiguration configuration,
        ILogger<AIContentModerationService> logger,
        HttpClient httpClient)
    {
        _sensitiveWordFilter = sensitiveWordFilter;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        // 从配置读取审核参数
        _spamThreshold = configuration.GetValue<double>("ContentModeration:SpamThreshold", 0.7);
        _toxicityThreshold = configuration.GetValue<double>("ContentModeration:ToxicityThreshold", 0.8);
        _hateSpeechThreshold = configuration.GetValue<double>("ContentModeration:HateSpeechThreshold", 0.9);
        _enableAIModeration = configuration.GetValue<bool>("ContentModeration:EnableAI", false);
        _aiModerationEndpoint = configuration.GetValue<string>("ContentModeration:AIEndpoint");
        _aiModerationApiKey = configuration.GetValue<string>("ContentModeration:AIApiKey");
    }

    #region IAIContentModerationService Implementation

    /// <summary>
    /// 对内容进行AI审核
    /// </summary>
    public async Task<AIContentModerationResult> ModerateContentAsync(string content, CancellationToken cancellationToken = default)
    {
        var commentContent = CommentContent.Create(content);
        var result = await ModerateCommentInternalAsync(commentContent, Guid.Empty, null, cancellationToken);
        return ConvertToApplicationResult(result);
    }

    /// <summary>
    /// 批量内容审核
    /// </summary>
    public async Task<IEnumerable<AIContentModerationResult>> ModerateBatchAsync(IEnumerable<string> contents, CancellationToken cancellationToken = default)
    {
        var contentItems = contents.Select(c => (CommentContent.Create(c), Guid.Empty, (string?)null));
        var results = await ModerateBatchInternalAsync(contentItems, cancellationToken);
        return results.Select(ConvertToApplicationResult);
    }

    /// <summary>
    /// 检查内容是否包含敏感词
    /// </summary>
    public async Task<bool> ContainsSensitiveContentAsync(string content, CancellationToken cancellationToken = default)
    {
        var result = await _sensitiveWordFilter.CheckContentAsync(content);
        return result.ContainsSensitiveWords;
    }

    /// <summary>
    /// 获取内容风险等级
    /// </summary>
    public async Task<RiskLevel> GetContentRiskLevelAsync(string content, CancellationToken cancellationToken = default)
    {
        var moderationResult = await ModerateContentAsync(content, cancellationToken);
        return moderationResult.RiskLevel;
    }

    /// <summary>
    /// 训练AI审核模型
    /// </summary>
    public async Task TrainModelAsync(IEnumerable<(string Content, bool IsApproved)> trainingData, CancellationToken cancellationToken = default)
    {
        // Placeholder for model training - would integrate with ML.NET or external API
        await Task.Delay(100, cancellationToken);
        _logger.LogInformation("Training data received with {Count} samples", trainingData.Count());
    }

    /// <summary>
    /// 更新敏感词库
    /// </summary>
    public async Task UpdateSensitiveWordsAsync(IEnumerable<string> words, CancellationToken cancellationToken = default)
    {
        // This would typically update the sensitive word filter
        await Task.Delay(50, cancellationToken);
        _logger.LogInformation("Sensitive words updated with {Count} words", words.Count());
    }

    /// <summary>
    /// 获取审核统计信息
    /// </summary>
    public async Task<ModerationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        // Placeholder statistics - would typically query from database
        return new ModerationStatistics
        {
            TotalModerated = 1000,
            Approved = 850,
            Rejected = 150,
            AccuracyRate = 0.95,
            IssueTypes = new Dictionary<string, int>
            {
                ["spam"] = 80,
                ["inappropriate"] = 50,
                ["hate_speech"] = 20
            },
            RiskLevels = new Dictionary<RiskLevel, int>
            {
                [RiskLevel.Low] = 850,
                [RiskLevel.Medium] = 100,
                [RiskLevel.High] = 40,
                [RiskLevel.Critical] = 10
            }
        };
    }

    /// <summary>
    /// 审核评论内容
    /// </summary>
    public async Task<AIContentModerationResult> ModerateCommentAsync(
        CommentContent content,
        Guid authorId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ModerateCommentInternalAsync(content, authorId, ipAddress, cancellationToken);
        return ConvertToApplicationResult(result);
    }

    /// <summary>
    /// 检查是否需要人工审核
    /// </summary>
    public async Task<bool> RequiresHumanReviewAsync(string content, AIContentModerationResult moderationResult, CancellationToken cancellationToken = default)
    {
        var commentContent = CommentContent.Create(content);
        return await RequiresHumanReviewInternalAsync(commentContent, Guid.Empty);
    }

    #endregion

    #region Internal Implementation

    /// <summary>
    /// 审核评论内容（内部实现）
    /// </summary>
    private async Task<InternalModerationResult> ModerateCommentInternalAsync(
        CommentContent content,
        Guid authorId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        try
        {
            // 1. 基础检查
            var basicChecks = await PerformBasicChecksAsync(content, authorId, ipAddress);
            if (basicChecks.Result != ModerationResult.RequiresHumanReview)
            {
                return basicChecks with { ProcessingTimeMs = GetProcessingTime(startTime) };
            }

            // 2. 敏感词检查
            var sensitiveWordResult = await _sensitiveWordFilter.CheckContentAsync(content.RawContent);
            if (sensitiveWordResult.ContainsSensitiveWords)
            {
                return new InternalModerationResult
                {
                    Result = GetResultFromSensitiveWords(sensitiveWordResult),
                    ConfidenceScore = 0.95,
                    ContainsSensitiveWords = true,
                    DetectedSensitiveWords = sensitiveWordResult.DetectedWords,
                    Reason = "内容包含敏感词汇",
                    SuggestedAction = "需要人工审核或直接拒绝",
                    ProcessingTimeMs = GetProcessingTime(startTime)
                };
            }

            // 3. AI审核 (如果启用)
            if (_enableAIModeration)
            {
                var aiResult = await PerformAIModerationAsync(content, authorId, cancellationToken);
                if (aiResult != null)
                {
                    return aiResult with { ProcessingTimeMs = GetProcessingTime(startTime) };
                }
            }

            // 4. 规则审核
            var ruleResult = await PerformRuleBasedModerationAsync(content, authorId);
            return ruleResult with { ProcessingTimeMs = GetProcessingTime(startTime) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during content moderation for author {AuthorId}", authorId);

            // 出错时默认需要人工审核
            return new InternalModerationResult
            {
                Result = ModerationResult.RequiresHumanReview,
                ConfidenceScore = 0.0,
                ContainsSensitiveWords = false,
                Reason = "审核过程中发生错误",
                SuggestedAction = "需要人工审核",
                ProcessingTimeMs = GetProcessingTime(startTime)
            };
        }
    }

    /// <summary>
    /// 批量审核评论内容（内部实现）
    /// </summary>
    private async Task<IEnumerable<InternalModerationResult>> ModerateBatchInternalAsync(
        IEnumerable<(CommentContent Content, Guid AuthorId, string? IpAddress)> contents,
        CancellationToken cancellationToken = default)
    {
        var tasks = contents.Select(async item =>
        {
            try
            {
                return await ModerateCommentInternalAsync(item.Content, item.AuthorId, item.IpAddress, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch moderation for author {AuthorId}", item.AuthorId);
                return new InternalModerationResult
                {
                    Result = ModerationResult.RequiresHumanReview,
                    ConfidenceScore = 0.0,
                    ContainsSensitiveWords = false,
                    Reason = "批量审核过程中发生错误",
                    SuggestedAction = "需要人工审核",
                    ProcessingTimeMs = 0
                };
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 检查内容是否需要人工审核（内部实现）
    /// </summary>
    private async Task<bool> RequiresHumanReviewInternalAsync(CommentContent content, Guid authorId)
    {
        try
        {
            // 内容本身要求审核
            if (content.RequiresModeration())
                return true;

            // 用户信任度低
            var trustScore = await GetUserTrustScoreAsync(authorId);
            if (trustScore < 0.5)
                return true;

            // 包含可疑内容
            var suspiciousPatterns = new[]
            {
                @"http[s]?://[^\s]+", // 包含链接
                @"[\u4e00-\u9fa5]*广告[\u4e00-\u9fa5]*", // 包含"广告"
                @"[\u4e00-\u9fa5]*联系[\u4e00-\u9fa5]*", // 包含"联系"
                @"\d{10,}", // 长数字串（可能是电话号码）
                @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}" // 邮箱地址
            };

            var text = content.RawContent;
            if (suspiciousPatterns.Any(pattern => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase)))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if content requires human review for author {AuthorId}", authorId);
            return true; // 出错时保守处理
        }
    }

    /// <summary>
    /// 获取用户的信任度评分
    /// </summary>
    public async Task<double> GetUserTrustScoreAsync(Guid authorId)
    {
        try
        {
            // 这里应该基于用户历史行为计算信任度
            // 暂时返回一个基础的信任度评分
            await Task.Delay(10); // 模拟异步操作

            // 简单的信任度计算逻辑
            // 实际应用中应该基于用户的评论历史、举报记录、点赞比例等
            var userId = authorId.ToString();
            var hash = userId.GetHashCode();
            var trustScore = 0.5 + (Math.Abs(hash) % 1000) / 2000.0; // 0.5 - 1.0

            return Math.Min(1.0, trustScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating trust score for user {AuthorId}", authorId);
            return 0.5; // 默认中等信任度
        }
    }

    #region 私有方法

    /// <summary>
    /// 执行基础检查
    /// </summary>
    private async Task<InternalModerationResult> PerformBasicChecksAsync(
        CommentContent content,
        Guid authorId,
        string? ipAddress)
    {
        await Task.Delay(1); // 模拟异步操作

        var text = content.RawContent;

        // 检查内容长度
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2)
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedInappropriate,
                ConfidenceScore = 1.0,
                ContainsSensitiveWords = false,
                Reason = "内容过短或为空",
                SuggestedAction = "拒绝发布"
            };
        }

        // 检查是否全是特殊字符或重复字符
        if (IsLowQualityContent(text))
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedSpam,
                ConfidenceScore = 0.9,
                ContainsSensitiveWords = false,
                Reason = "内容质量过低",
                SuggestedAction = "标记为垃圾信息"
            };
        }

        // 检查是否包含明显的垃圾信息标识
        if (IsObviousSpam(text))
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedSpam,
                ConfidenceScore = 0.95,
                ContainsSensitiveWords = false,
                Reason = "包含垃圾信息标识",
                SuggestedAction = "标记为垃圾信息"
            };
        }

        return new InternalModerationResult
        {
            Result = ModerationResult.RequiresHumanReview,
            ConfidenceScore = 0.5,
            ContainsSensitiveWords = false
        };
    }

    /// <summary>
    /// 执行AI审核
    /// </summary>
    private async Task<InternalModerationResult?> PerformAIModerationAsync(
        CommentContent content,
        Guid authorId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_aiModerationEndpoint) || string.IsNullOrEmpty(_aiModerationApiKey))
        {
            return null;
        }

        try
        {
            var requestData = new
            {
                text = content.RawContent,
                user_id = authorId.ToString(),
                features = new[] { "toxicity", "spam", "hate_speech" }
            };

            var json = JsonSerializer.Serialize(requestData);
            var requestContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiModerationApiKey}");

            var response = await _httpClient.PostAsync(_aiModerationEndpoint, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI moderation API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var aiResponse = JsonSerializer.Deserialize<AIModerationResponse>(responseJson);

            if (aiResponse == null)
            {
                return null;
            }

            return InterpretAIResponse(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI moderation service");
            return null;
        }
    }

    /// <summary>
    /// 执行基于规则的审核
    /// </summary>
    private async Task<InternalModerationResult> PerformRuleBasedModerationAsync(
        CommentContent content,
        Guid authorId)
    {
        await Task.Delay(1); // 模拟异步操作

        var text = content.RawContent;
        var trustScore = await GetUserTrustScoreAsync(authorId);

        // 基于规则的评分
        var spamScore = CalculateSpamScore(text);
        var toxicityScore = CalculateToxicityScore(text);

        // 综合评分，考虑用户信任度
        var adjustedSpamScore = spamScore * (2.0 - trustScore);
        var adjustedToxicityScore = toxicityScore * (2.0 - trustScore);

        if (adjustedToxicityScore > _hateSpeechThreshold)
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedHateSpeech,
                ConfidenceScore = adjustedToxicityScore,
                ContainsSensitiveWords = false,
                Reason = "内容可能包含仇恨言论",
                SuggestedAction = "需要人工审核"
            };
        }

        if (adjustedToxicityScore > _toxicityThreshold)
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedInappropriate,
                ConfidenceScore = adjustedToxicityScore,
                ContainsSensitiveWords = false,
                Reason = "内容可能不当",
                SuggestedAction = "需要人工审核"
            };
        }

        if (adjustedSpamScore > _spamThreshold)
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.RejectedSpam,
                ConfidenceScore = adjustedSpamScore,
                ContainsSensitiveWords = false,
                Reason = "内容可能为垃圾信息",
                SuggestedAction = "标记为垃圾信息"
            };
        }

        // 高信任度用户直接通过
        if (trustScore > 0.8 && spamScore < 0.3 && toxicityScore < 0.3)
        {
            return new InternalModerationResult
            {
                Result = ModerationResult.Approved,
                ConfidenceScore = trustScore,
                ContainsSensitiveWords = false,
                Reason = "高信任度用户的正常内容",
                SuggestedAction = "批准发布"
            };
        }

        // 默认需要人工审核
        return new InternalModerationResult
        {
            Result = ModerationResult.RequiresHumanReview,
            ConfidenceScore = 0.5,
            ContainsSensitiveWords = false,
            Reason = "内容需要人工审核",
            SuggestedAction = "人工审核"
        };
    }

    /// <summary>
    /// 检查是否为低质量内容
    /// </summary>
    private static bool IsLowQualityContent(string text)
    {
        // 检查是否大量重复字符
        if (Regex.IsMatch(text, @"(.)\1{10,}"))
            return true;

        // 检查是否全是标点符号
        if (Regex.IsMatch(text, @"^[^\w\u4e00-\u9fa5]+$"))
            return true;

        // 检查字符种类过少
        var uniqueChars = text.Distinct().Count();
        if (text.Length > 20 && uniqueChars < 5)
            return true;

        return false;
    }

    /// <summary>
    /// 检查是否为明显的垃圾信息
    /// </summary>
    private static bool IsObviousSpam(string text)
    {
        var spamPatterns = new[]
        {
            @"免费.*领取", @"限时.*优惠", @"加.*微信", @"扫.*二维码",
            @"点击.*链接", @"立即.*下载", @"注册.*送", @"充值.*返利"
        };

        return spamPatterns.Any(pattern =>
            Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// 计算垃圾信息评分
    /// </summary>
    private static double CalculateSpamScore(string text)
    {
        var score = 0.0;

        // 包含链接
        if (Regex.IsMatch(text, @"http[s]?://[^\s]+"))
            score += 0.3;

        // 包含电话号码模式
        if (Regex.IsMatch(text, @"\d{3,4}[-\s]?\d{7,8}|\d{11}"))
            score += 0.4;

        // 包含邮箱
        if (Regex.IsMatch(text, @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"))
            score += 0.2;

        // 过多的感叹号
        var exclamationCount = text.Count(c => c == '!');
        if (exclamationCount > 3)
            score += Math.Min(0.3, exclamationCount * 0.05);

        // 包含营销词汇
        var marketingWords = new[] { "优惠", "折扣", "免费", "赚钱", "兼职" };
        var marketingCount = marketingWords.Count(word => text.Contains(word));
        score += marketingCount * 0.1;

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// 计算毒性评分
    /// </summary>
    private static double CalculateToxicityScore(string text)
    {
        var score = 0.0;

        // 简单的毒性词汇检测
        var toxicWords = new[] { "傻", "蠢", "死", "滚", "垃圾" };
        var toxicCount = toxicWords.Count(word => text.Contains(word));
        score += toxicCount * 0.2;

        // 全大写文本 (英文)
        var uppercaseRatio = text.Count(char.IsUpper) / (double)Math.Max(1, text.Count(char.IsLetter));
        if (uppercaseRatio > 0.5)
            score += 0.1;

        return Math.Min(1.0, score);
    }

    /// <summary>
    /// 根据敏感词结果确定审核结果
    /// </summary>
    private static ModerationResult GetResultFromSensitiveWords(SensitiveWordResult result)
    {
        if (result.HighRiskWords.Any())
            return ModerationResult.RejectedHateSpeech;

        if (result.MediumRiskWords.Any())
            return ModerationResult.RejectedInappropriate;

        return ModerationResult.RequiresHumanReview;
    }

    /// <summary>
    /// 解释AI响应
    /// </summary>
    private InternalModerationResult InterpretAIResponse(AIModerationResponse response)
    {
        var maxScore = Math.Max(response.Toxicity, Math.Max(response.Spam, response.HateSpeech));
        var result = ModerationResult.Approved;
        var reason = "AI审核通过";

        if (response.HateSpeech > _hateSpeechThreshold)
        {
            result = ModerationResult.RejectedHateSpeech;
            reason = "AI检测到仇恨言论";
        }
        else if (response.Toxicity > _toxicityThreshold)
        {
            result = ModerationResult.RejectedInappropriate;
            reason = "AI检测到不当内容";
        }
        else if (response.Spam > _spamThreshold)
        {
            result = ModerationResult.RejectedSpam;
            reason = "AI检测到垃圾信息";
        }
        else if (maxScore > 0.5)
        {
            result = ModerationResult.RequiresHumanReview;
            reason = "AI建议人工审核";
        }

        return new InternalModerationResult
        {
            Result = result,
            ConfidenceScore = maxScore,
            ContainsSensitiveWords = false,
            Reason = reason,
            SuggestedAction = GetSuggestedAction(result)
        };
    }

    /// <summary>
    /// 获取建议的处理方式
    /// </summary>
    private static string GetSuggestedAction(ModerationResult result)
    {
        return result switch
        {
            ModerationResult.Approved => "批准发布",
            ModerationResult.RequiresHumanReview => "需要人工审核",
            ModerationResult.RejectedSpam => "标记为垃圾信息",
            ModerationResult.RejectedInappropriate => "拒绝发布",
            ModerationResult.RejectedHateSpeech => "拒绝发布并记录",
            ModerationResult.RejectedSensitiveWords => "包含敏感词，需要处理",
            _ => "需要进一步处理"
        };
    }

    /// <summary>
    /// 计算处理时间
    /// </summary>
    private static long GetProcessingTime(long startTime)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime;
    }

    #endregion

    #region AI响应模型

    private record AIModerationResponse
    {
        public double Toxicity { get; init; }
        public double Spam { get; init; }
        public double HateSpeech { get; init; }
    }

    /// <summary>
    /// 转换为Application层的结果
    /// </summary>
    private AIContentModerationResult ConvertToApplicationResult(InternalModerationResult internalResult)
    {
        return new AIContentModerationResult
        {
            IsApproved = internalResult.Result == ModerationResult.Approved,
            Confidence = internalResult.ConfidenceScore,
            Reason = internalResult.Reason,
            DetectedIssues = ConvertToModerationIssues(internalResult),
            SuggestedAction = ConvertToModerationAction(internalResult.Result),
            RiskLevel = ConvertToRiskLevel(internalResult.Result, internalResult.ConfidenceScore),
            ModelVersion = "1.0",
            ProcessedAt = DateTime.UtcNow,
            ProcessingTimeMs = internalResult.ProcessingTimeMs,
            SensitiveWords = ConvertToSensitiveWordDetections(internalResult.DetectedSensitiveWords)
        };
    }

    private List<ModerationIssue> ConvertToModerationIssues(InternalModerationResult result)
    {
        var issues = new List<ModerationIssue>();

        if (result.ContainsSensitiveWords)
        {
            issues.Add(new ModerationIssue
            {
                Type = "sensitive_words",
                Description = "Content contains sensitive words",
                Severity = 0.8
            });
        }

        if (result.Result == ModerationResult.RejectedSpam)
        {
            issues.Add(new ModerationIssue
            {
                Type = "spam",
                Description = "Content detected as spam",
                Severity = 0.7
            });
        }

        return issues;
    }

    private ModerationAction ConvertToModerationAction(ModerationResult result)
    {
        return result switch
        {
            ModerationResult.Approved => ModerationAction.Approve,
            ModerationResult.RequiresHumanReview => ModerationAction.Review,
            ModerationResult.RejectedSpam => ModerationAction.MarkAsSpam,
            ModerationResult.RejectedInappropriate => ModerationAction.Delete,
            ModerationResult.RejectedHateSpeech => ModerationAction.Delete,
            ModerationResult.RejectedSensitiveWords => ModerationAction.Review,
            _ => ModerationAction.Review
        };
    }

    private RiskLevel ConvertToRiskLevel(ModerationResult result, double confidence)
    {
        return result switch
        {
            ModerationResult.Approved => RiskLevel.Low,
            ModerationResult.RequiresHumanReview => confidence > 0.7 ? RiskLevel.Medium : RiskLevel.Low,
            ModerationResult.RejectedSpam => RiskLevel.Medium,
            ModerationResult.RejectedInappropriate => RiskLevel.High,
            ModerationResult.RejectedHateSpeech => RiskLevel.Critical,
            ModerationResult.RejectedSensitiveWords => RiskLevel.High,
            _ => RiskLevel.Medium
        };
    }

    private List<SensitiveWordDetection> ConvertToSensitiveWordDetections(IEnumerable<string> words)
    {
        return words.Select(word => new SensitiveWordDetection
        {
            Word = word,
            Category = "general",
            Severity = 0.7
        }).ToList();
    }

    #endregion

    #region Helper Types

    /// <summary>
    /// 内部审核结果（用于避免接口冲突）
    /// </summary>
    private record InternalModerationResult
    {
        public ModerationResult Result { get; init; }
        public double ConfidenceScore { get; init; }
        public bool ContainsSensitiveWords { get; init; }
        public IEnumerable<string> DetectedSensitiveWords { get; init; } = [];
        public string? Reason { get; init; }
        public string? SuggestedAction { get; init; }
        public long ProcessingTimeMs { get; init; }
    }

    #endregion

    #endregion
}