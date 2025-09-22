using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Markdown;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Application层Markdown服务实现
/// 提供完整的Markdown处理功能，包括转换、验证、统计等
/// </summary>
public class ApplicationMarkdownService : IApplicationMarkdownService
{
    private readonly ILogger<ApplicationMarkdownService> _logger;

    // 正则表达式模式
    private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)(?:\s+""([^""]*)"")?\)", RegexOptions.Compiled);
    private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^)]+)(?:\s+""([^""]*)"")?\)", RegexOptions.Compiled);
    private static readonly Regex CodeBlockRegex = new(@"```(\w+)?\n(.*?)\n```", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex BoldRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new(@"\b\w+\b", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);

    public ApplicationMarkdownService(ILogger<ApplicationMarkdownService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ConvertToHtmlAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            await Task.Delay(1); // 保持异步模式

            var html = markdown;

            // 转换标题
            html = HeadingRegex.Replace(html, match =>
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value;
                return $"<h{level}>{text}</h{level}>";
            });

            // 转换代码块
            html = CodeBlockRegex.Replace(html, match =>
            {
                var language = match.Groups[1].Value;
                var code = match.Groups[2].Value;
                return $"<pre><code class=\"language-{language}\">{System.Web.HttpUtility.HtmlEncode(code)}</code></pre>";
            });

            // 转换内联代码
            html = InlineCodeRegex.Replace(html, "<code>$1</code>");

            // 转换粗体
            html = BoldRegex.Replace(html, "<strong>$1</strong>");

            // 转换斜体
            html = ItalicRegex.Replace(html, "<em>$1</em>");

            // 转换链接
            html = LinkRegex.Replace(html, match =>
            {
                var text = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var title = match.Groups[3].Value;
                return string.IsNullOrEmpty(title)
                    ? $"<a href=\"{url}\">{text}</a>"
                    : $"<a href=\"{url}\" title=\"{title}\">{text}</a>";
            });

            // 转换图片
            html = ImageRegex.Replace(html, match =>
            {
                var alt = match.Groups[1].Value;
                var src = match.Groups[2].Value;
                var title = match.Groups[3].Value;
                return string.IsNullOrEmpty(title)
                    ? $"<img src=\"{src}\" alt=\"{alt}\" />"
                    : $"<img src=\"{src}\" alt=\"{alt}\" title=\"{title}\" />";
            });

            // 转换段落
            var paragraphs = html.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                var trimmed = paragraph.Trim();
                if (!trimmed.StartsWith("<"))
                {
                    result.AppendLine($"<p>{trimmed}</p>");
                }
                else
                {
                    result.AppendLine(trimmed);
                }
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting markdown to HTML");
            return string.Empty;
        }
    }

    public async Task<MarkdownValidationResult> ValidateMarkdownAsync(string markdown)
    {
        try
        {
            await Task.Delay(1); // 保持异步模式

            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(markdown))
            {
                errors.Add("Markdown内容不能为空");
                return new MarkdownValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            // 检查未闭合的代码块
            var codeBlockMatches = CodeBlockRegex.Matches(markdown);
            var codeBlockCount = markdown.Split("```").Length - 1;
            if (codeBlockCount % 2 != 0)
            {
                warnings.Add("检测到未闭合的代码块");
            }

            // 检查链接格式
            var linkMatches = LinkRegex.Matches(markdown);
            foreach (Match match in linkMatches)
            {
                var url = match.Groups[2].Value;
                if (string.IsNullOrWhiteSpace(url))
                {
                    warnings.Add($"链接缺少URL: {match.Value}");
                }
            }

            var result = new MarkdownValidationResult
            {
                IsValid = true,
                Errors = errors,
                Warnings = warnings
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating markdown");
            return new MarkdownValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "验证过程中发生错误" }
            };
        }
    }

    public async Task<string> SanitizeHtmlAsync(string html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            await Task.Delay(1); // 保持异步模式

            // 基本的HTML清理，移除脚本标签
            var sanitized = html;
            sanitized = Regex.Replace(sanitized, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"<iframe\b[^<]*(?:(?!<\/iframe>)<[^<]*)*<\/iframe>", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"javascript:", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=\s*[""'][^""']*[""']", "", RegexOptions.IgnoreCase);

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML");
            return string.Empty;
        }
    }

    public async Task<MarkdownStatsDto> GetMarkdownStatsAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new MarkdownStatsDto();
            }

            await Task.Delay(1); // 保持异步模式

            var wordCount = await CountWordsAsync(markdown);
            var charCount = markdown.Length;
            var lineCount = markdown.Split('\n').Length;
            var headingCount = HeadingRegex.Matches(markdown).Count;
            var linkCount = LinkRegex.Matches(markdown).Count;
            var imageCount = ImageRegex.Matches(markdown).Count;
            var codeBlockCount = CodeBlockRegex.Matches(markdown).Count;
            var readingTime = await EstimateReadingTimeAsync(markdown);

            return new MarkdownStatsDto
            {
                WordCount = wordCount,
                CharacterCount = charCount,
                ParagraphCount = lineCount, // 使用ParagraphCount而不是LineCount
                HeadingCount = headingCount,
                LinkCount = linkCount,
                ImageCount = imageCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting markdown stats");
            return new MarkdownStatsDto();
        }
    }

    public async Task<string> ExtractPlainTextAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            await Task.Delay(1); // 保持异步模式

            var text = markdown;

            // 移除代码块
            text = CodeBlockRegex.Replace(text, "");

            // 移除内联代码
            text = InlineCodeRegex.Replace(text, "$1");

            // 移除链接，保留文本
            text = LinkRegex.Replace(text, "$1");

            // 移除图片
            text = ImageRegex.Replace(text, "");

            // 移除标题标记
            text = HeadingRegex.Replace(text, "$2");

            // 移除格式标记
            text = BoldRegex.Replace(text, "$1");
            text = ItalicRegex.Replace(text, "$1");

            // 清理多余的空白
            text = WhitespaceRegex.Replace(text, " ");

            return text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting plain text");
            return string.Empty;
        }
    }

    public async Task<IEnumerable<string>> ExtractHeadingsAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return new List<string>();

            await Task.Delay(1); // 保持异步模式

            var headings = new List<string>();
            var matches = HeadingRegex.Matches(markdown);

            foreach (Match match in matches)
            {
                headings.Add(match.Groups[2].Value.Trim());
            }

            return headings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting headings");
            return new List<string>();
        }
    }

    public async Task<IEnumerable<MarkdownLinkDto>> ExtractLinksAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return new List<MarkdownLinkDto>();

            await Task.Delay(1); // 保持异步模式

            var links = new List<MarkdownLinkDto>();
            var matches = LinkRegex.Matches(markdown);

            foreach (Match match in matches)
            {
                links.Add(new MarkdownLinkDto
                {
                    Text = match.Groups[1].Value,
                    Url = match.Groups[2].Value,
                    IsExternal = match.Groups[2].Value.StartsWith("http")
                });
            }

            return links;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting links");
            return new List<MarkdownLinkDto>();
        }
    }

    public async Task<string> ConvertHtmlToMarkdownAsync(string html)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            await Task.Delay(1); // 保持异步模式

            var markdown = html;

            // 基本的HTML到Markdown转换
            markdown = Regex.Replace(markdown, @"<h([1-6])>(.*?)</h[1-6]>", match =>
            {
                var level = int.Parse(match.Groups[1].Value);
                var text = match.Groups[2].Value;
                return new string('#', level) + " " + text;
            }, RegexOptions.IgnoreCase);

            markdown = Regex.Replace(markdown, @"<strong>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase);
            markdown = Regex.Replace(markdown, @"<em>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase);
            markdown = Regex.Replace(markdown, @"<code>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase);
            markdown = Regex.Replace(markdown, @"<a href=""([^""]*)"">([^<]*)</a>", "[$2]($1)", RegexOptions.IgnoreCase);

            // 移除其他HTML标签
            markdown = HtmlTagRegex.Replace(markdown, "");

            return markdown.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting HTML to markdown");
            return string.Empty;
        }
    }

    public async Task<MarkdownTableOfContentsDto> GenerateTableOfContentsAsync(string markdown)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return new MarkdownTableOfContentsDto();

            await Task.Delay(1); // 保持异步模式

            var items = new List<TocItemDto>();

            var matches = HeadingRegex.Matches(markdown);
            foreach (Match match in matches)
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value.Trim();
                var anchor = text.ToLowerInvariant()
                    .Replace(" ", "-")
                    .Replace(".", "")
                    .Replace(",", "")
                    .Replace("!", "")
                    .Replace("?", "");

                items.Add(new TocItemDto
                {
                    Level = level,
                    Text = text,
                    Anchor = anchor
                });
            }

            var toc = new MarkdownTableOfContentsDto
            {
                Items = items
            };

            return toc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating table of contents");
            return new MarkdownTableOfContentsDto();
        }
    }

    // BlogServiceTests需要的方法
    public async Task<int> CountWordsAsync(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            await Task.Delay(1); // 保持异步模式

            // 先提取纯文本
            var plainText = await ExtractPlainTextAsync(content);

            // 计算单词数
            var matches = WordRegex.Matches(plainText);
            return matches.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting words");
            return 0;
        }
    }

    public async Task<int> EstimateReadingTimeAsync(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            await Task.Delay(1); // 保持异步模式

            var wordCount = await CountWordsAsync(content);

            // 假设每分钟阅读200个单词
            const int wordsPerMinute = 200;
            var minutes = Math.Max(1, (int)Math.Ceiling((double)wordCount / wordsPerMinute));

            return minutes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating reading time");
            return 1;
        }
    }

    public async Task<string> ExtractSummaryAsync(string content, int maxLength = 200)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            await Task.Delay(1); // 保持异步模式

            // 提取纯文本
            var plainText = await ExtractPlainTextAsync(content);

            if (plainText.Length <= maxLength)
                return plainText;

            // 在单词边界截断
            var truncated = plainText.Substring(0, maxLength);
            var lastSpaceIndex = truncated.LastIndexOf(' ');

            if (lastSpaceIndex > 0)
            {
                truncated = truncated.Substring(0, lastSpaceIndex);
            }

            return truncated + "...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting summary");
            return string.Empty;
        }
    }
}