using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.ValueObjects;

/// <summary>
/// 评论内容值对象
/// 封装评论内容相关的业务规则和验证逻辑
/// </summary>
public record CommentContent
{
    private const int MaxRawContentLength = 10000;
    private const int MaxProcessedContentLength = 15000;
    private const int MinContentLength = 1;

    /// <summary>
    /// 原始内容（用户输入的内容）
    /// </summary>
    public string RawContent { get; init; }

    /// <summary>
    /// 处理后的内容（经过HTML清理、Markdown渲染等）
    /// </summary>
    public string ProcessedContent { get; init; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; init; }

    /// <summary>
    /// 是否包含敏感内容
    /// </summary>
    public bool ContainsSensitiveContent { get; init; }

    /// <summary>
    /// 内容摘要（用于预览）
    /// </summary>
    public string Summary { get; init; }

    /// <summary>
    /// 私有构造函数，强制使用工厂方法创建
    /// </summary>
    private CommentContent(string rawContent, string processedContent, string contentType,
        bool containsSensitiveContent, string summary)
    {
        RawContent = rawContent;
        ProcessedContent = processedContent;
        ContentType = contentType;
        ContainsSensitiveContent = containsSensitiveContent;
        Summary = summary;
    }

    /// <summary>
    /// 创建评论内容值对象
    /// </summary>
    /// <param name="rawContent">原始内容</param>
    /// <param name="contentType">内容类型（默认为markdown）</param>
    /// <returns>评论内容值对象</returns>
    /// <exception cref="ArgumentException">当内容无效时抛出</exception>
    public static CommentContent Create(string rawContent, string contentType = "markdown")
    {
        ValidateRawContent(rawContent);
        ValidateContentType(contentType);

        var processedContent = ProcessContent(rawContent, contentType);
        var containsSensitiveContent = CheckForSensitiveContent(rawContent);
        var summary = CreateSummary(processedContent);

        return new CommentContent(
            rawContent: rawContent.Trim(),
            processedContent: processedContent,
            contentType: contentType.ToLowerInvariant(),
            containsSensitiveContent: containsSensitiveContent,
            summary: summary
        );
    }

    /// <summary>
    /// 更新内容并重新处理
    /// </summary>
    /// <param name="newRawContent">新的原始内容</param>
    /// <param name="newContentType">新的内容类型（可选）</param>
    /// <returns>新的评论内容值对象</returns>
    public CommentContent UpdateContent(string newRawContent, string? newContentType = null)
    {
        return Create(newRawContent, newContentType ?? ContentType);
    }

    /// <summary>
    /// 验证原始内容
    /// </summary>
    /// <param name="rawContent">原始内容</param>
    /// <exception cref="ArgumentException">当内容无效时抛出</exception>
    private static void ValidateRawContent(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            throw new ArgumentException("评论内容不能为空", nameof(rawContent));

        if (rawContent.Trim().Length < MinContentLength)
            throw new ArgumentException($"评论内容至少需要{MinContentLength}个字符", nameof(rawContent));

        if (rawContent.Length > MaxRawContentLength)
            throw new ArgumentException($"评论内容不能超过{MaxRawContentLength}个字符", nameof(rawContent));

        // 检查是否只包含空白字符或特殊字符
        if (string.IsNullOrWhiteSpace(rawContent.Replace("\n", "").Replace("\r", "").Replace("\t", "")))
            throw new ArgumentException("评论内容不能只包含空白字符", nameof(rawContent));
    }

    /// <summary>
    /// 验证内容类型
    /// </summary>
    /// <param name="contentType">内容类型</param>
    /// <exception cref="ArgumentException">当内容类型无效时抛出</exception>
    private static void ValidateContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("内容类型不能为空", nameof(contentType));

        var validTypes = new[] { "markdown", "html", "plaintext" };
        if (!validTypes.Contains(contentType.ToLowerInvariant()))
            throw new ArgumentException($"不支持的内容类型：{contentType}。支持的类型：{string.Join(", ", validTypes)}", nameof(contentType));
    }

    /// <summary>
    /// 处理内容（HTML清理、Markdown渲染等）
    /// </summary>
    /// <param name="rawContent">原始内容</param>
    /// <param name="contentType">内容类型</param>
    /// <returns>处理后的内容</returns>
    private static string ProcessContent(string rawContent, string contentType)
    {
        var processed = rawContent.Trim();

        // 根据内容类型进行相应处理
        switch (contentType.ToLowerInvariant())
        {
            case "markdown":
                processed = ProcessMarkdownContent(processed);
                break;
            case "html":
                processed = SanitizeHtmlContent(processed);
                break;
            case "plaintext":
                processed = EscapeHtmlContent(processed);
                break;
        }

        // 确保处理后的内容不超过最大长度
        if (processed.Length > MaxProcessedContentLength)
        {
            processed = processed.Substring(0, MaxProcessedContentLength - 3) + "...";
        }

        return processed;
    }

    /// <summary>
    /// 处理Markdown内容
    /// </summary>
    /// <param name="content">Markdown内容</param>
    /// <returns>处理后的内容</returns>
    private static string ProcessMarkdownContent(string content)
    {
        // 这里应该使用实际的Markdown处理器
        // 暂时返回原始内容，实际实现中会集成Markdig等库

        // 基本的Markdown安全处理
        content = content.Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase);
        content = content.Replace("javascript:", "javascript&#58;", StringComparison.OrdinalIgnoreCase);

        return content;
    }

    /// <summary>
    /// 清理HTML内容
    /// </summary>
    /// <param name="content">HTML内容</param>
    /// <returns>清理后的内容</returns>
    private static string SanitizeHtmlContent(string content)
    {
        // 这里应该使用HTML清理库（如HtmlSanitizer）
        // 暂时进行基本的危险标签移除

        var dangerousTags = new[] { "script", "iframe", "object", "embed", "form", "input" };
        foreach (var tag in dangerousTags)
        {
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                $@"<\s*{tag}[^>]*>.*?<\s*/\s*{tag}\s*>",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline
            );
        }

        return content;
    }

    /// <summary>
    /// 转义HTML内容
    /// </summary>
    /// <param name="content">纯文本内容</param>
    /// <returns>转义后的内容</returns>
    private static string EscapeHtmlContent(string content)
    {
        return System.Net.WebUtility.HtmlEncode(content);
    }

    /// <summary>
    /// 检查是否包含敏感内容
    /// </summary>
    /// <param name="content">内容</param>
    /// <returns>是否包含敏感内容</returns>
    private static bool CheckForSensitiveContent(string content)
    {
        // 这里应该集成敏感词过滤服务
        // 暂时进行基本检查

        var sensitiveWords = new[] { "spam", "advertisement", "广告", "垃圾" };
        var lowerContent = content.ToLowerInvariant();

        return sensitiveWords.Any(word => lowerContent.Contains(word.ToLowerInvariant()));
    }

    /// <summary>
    /// 创建内容摘要
    /// </summary>
    /// <param name="content">内容</param>
    /// <returns>摘要</returns>
    private static string CreateSummary(string content)
    {
        const int maxSummaryLength = 150;

        // 移除HTML标签
        var summary = System.Text.RegularExpressions.Regex.Replace(content, @"<[^>]*>", "");

        // 移除多余空白
        summary = System.Text.RegularExpressions.Regex.Replace(summary, @"\s+", " ").Trim();

        if (summary.Length <= maxSummaryLength)
            return summary;

        // 找到最后一个完整的词
        var truncated = summary.Substring(0, maxSummaryLength);
        var lastSpace = truncated.LastIndexOf(' ');

        if (lastSpace > maxSummaryLength / 2)
            truncated = truncated.Substring(0, lastSpace);

        return truncated + "...";
    }

    /// <summary>
    /// 获取纯文本内容（去除所有标记）
    /// </summary>
    /// <returns>纯文本内容</returns>
    public string GetPlainText()
    {
        var plainText = System.Text.RegularExpressions.Regex.Replace(ProcessedContent, @"<[^>]*>", "");
        return System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();
    }

    /// <summary>
    /// 获取内容字数
    /// </summary>
    /// <returns>字数</returns>
    public int GetWordCount()
    {
        var plainText = GetPlainText();
        if (string.IsNullOrWhiteSpace(plainText))
            return 0;

        // 简单的字数统计（按空格分割）
        return plainText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// 检查内容是否需要审核
    /// </summary>
    /// <returns>是否需要审核</returns>
    public bool RequiresModeration()
    {
        return ContainsSensitiveContent ||
               GetWordCount() > 500 ||
               ProcessedContent.Contains("http://", StringComparison.OrdinalIgnoreCase) ||
               ProcessedContent.Contains("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检查内容是否为空
    /// </summary>
    /// <returns>是否为空</returns>
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(GetPlainText());
    }
}