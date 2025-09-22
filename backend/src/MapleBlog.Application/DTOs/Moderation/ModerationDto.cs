using MapleBlog.Domain.Enums;
using ModerationActionEnum = MapleBlog.Domain.Enums.ModerationAction;

namespace MapleBlog.Application.DTOs.Moderation
{
    /// <summary>
    /// AI内容审核结果
    /// </summary>
    public class AIContentModerationResult
    {
        /// <summary>
        /// 是否通过审核
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// 置信度 (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 审核理由
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 检测到的问题类型
        /// </summary>
        public List<ModerationIssue> DetectedIssues { get; set; } = new();

        /// <summary>
        /// 建议的操作
        /// </summary>
        public ModerationActionEnum SuggestedAction { get; set; }

        /// <summary>
        /// 风险等级
        /// </summary>
        public RiskLevel RiskLevel { get; set; }

        /// <summary>
        /// 审核模型版本
        /// </summary>
        public string ModelVersion { get; set; } = string.Empty;

        /// <summary>
        /// 审核时间戳
        /// </summary>
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 处理时长（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 额外的元数据
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// 敏感词汇检测结果
        /// </summary>
        public List<SensitiveWordDetection> SensitiveWords { get; set; } = new();

        /// <summary>
        /// 情感分析结果
        /// </summary>
        public SentimentAnalysis? Sentiment { get; set; }

        /// <summary>
        /// 语言检测结果
        /// </summary>
        public LanguageDetection? Language { get; set; }

        /// <summary>
        /// 垃圾内容检测
        /// </summary>
        public SpamDetection? Spam { get; set; }

        /// <summary>
        /// 审核结果（别名：兼容性属性）
        /// </summary>
        public bool Result => IsApproved;

        /// <summary>
        /// 置信度分数（别名：兼容性属性）
        /// </summary>
        public double ConfidenceScore => Confidence;

        /// <summary>
        /// 是否包含敏感词（别名：兼容性属性）
        /// </summary>
        public bool ContainsSensitiveWords => SensitiveWords.Any();
    }

    /// <summary>
    /// 审核问题
    /// </summary>
    public class ModerationIssue
    {
        /// <summary>
        /// 问题类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 问题描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 严重程度 (0.0 - 1.0)
        /// </summary>
        public double Severity { get; set; }

        /// <summary>
        /// 出现位置（字符范围）
        /// </summary>
        public List<TextRange> Locations { get; set; } = new();
    }

    /// <summary>
    /// 文本范围
    /// </summary>
    public class TextRange
    {
        /// <summary>
        /// 起始位置
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// 结束位置
        /// </summary>
        public int End { get; set; }

        /// <summary>
        /// 匹配的文本
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// 敏感词检测结果
    /// </summary>
    public class SensitiveWordDetection
    {
        /// <summary>
        /// 敏感词
        /// </summary>
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// 分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 严重程度
        /// </summary>
        public double Severity { get; set; }

        /// <summary>
        /// 出现位置
        /// </summary>
        public List<TextRange> Locations { get; set; } = new();
    }

    /// <summary>
    /// 情感分析结果
    /// </summary>
    public class SentimentAnalysis
    {
        /// <summary>
        /// 整体情感倾向 (-1.0 到 1.0，负值表示负面，正值表示正面)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 情感标签
        /// </summary>
        public string Label { get; set; } = string.Empty; // "positive", "negative", "neutral"

        /// <summary>
        /// 检测到的情绪
        /// </summary>
        public List<EmotionDetection> Emotions { get; set; } = new();
    }

    /// <summary>
    /// 情绪检测
    /// </summary>
    public class EmotionDetection
    {
        /// <summary>
        /// 情绪类型
        /// </summary>
        public string Emotion { get; set; } = string.Empty; // "anger", "fear", "joy", "sadness", etc.

        /// <summary>
        /// 强度 (0.0 - 1.0)
        /// </summary>
        public double Intensity { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 语言检测结果
    /// </summary>
    public class LanguageDetection
    {
        /// <summary>
        /// 检测到的语言代码 (如: "en", "zh", "es")
        /// </summary>
        public string LanguageCode { get; set; } = string.Empty;

        /// <summary>
        /// 语言名称
        /// </summary>
        public string LanguageName { get; set; } = string.Empty;

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 是否是支持的语言
        /// </summary>
        public bool IsSupported { get; set; }
    }

    /// <summary>
    /// 垃圾内容检测
    /// </summary>
    public class SpamDetection
    {
        /// <summary>
        /// 是否是垃圾内容
        /// </summary>
        public bool IsSpam { get; set; }

        /// <summary>
        /// 垃圾内容概率 (0.0 - 1.0)
        /// </summary>
        public double SpamProbability { get; set; }

        /// <summary>
        /// 检测到的垃圾特征
        /// </summary>
        public List<string> SpamFeatures { get; set; } = new();

        /// <summary>
        /// 垃圾内容类型
        /// </summary>
        public List<string> SpamTypes { get; set; } = new(); // "promotional", "malicious", "repetitive", etc.
    }

    /// <summary>
    /// 风险等级
    /// </summary>
    public enum RiskLevel
    {
        /// <summary>
        /// 低风险
        /// </summary>
        Low = 0,

        /// <summary>
        /// 中等风险
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 高风险
        /// </summary>
        High = 2,

        /// <summary>
        /// 严重风险
        /// </summary>
        Critical = 3
    }

}