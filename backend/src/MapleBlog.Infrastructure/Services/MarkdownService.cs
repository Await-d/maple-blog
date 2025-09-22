using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.Infrastructure.Services
{
    /// <summary>
    /// Markdown processing service implementation
    /// Note: This is a basic implementation. For production use, consider using Markdig library
    /// </summary>
    public class MarkdownService : IMarkdownService
    {
        private readonly ILogger<MarkdownService> _logger;

        // Basic Markdown regex patterns
        private static readonly Regex HeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ImageRegex = new(@"!\[([^\]]*)\]\(([^)]+)(?:\s+""([^""]*)"")?\)", RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new(@"\[([^\]]+)\]\(([^)]+)(?:\s+""([^""]*)"")?\)", RegexOptions.Compiled);
        private static readonly Regex CodeBlockRegex = new(@"```(\w+)?\n(.*?)\n```", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);
        private static readonly Regex StrikethroughRegex = new(@"~~([^~]+)~~", RegexOptions.Compiled);
        private static readonly Regex BlockquoteRegex = new(@"^>\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ListItemRegex = new(@"^(\s*)[-*+]\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex OrderedListItemRegex = new(@"^(\s*)\d+\.\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex HorizontalRuleRegex = new(@"^(-{3,}|\*{3,}|_{3,})$", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex WordRegex = new(@"\b\w+\b", RegexOptions.Compiled);
        private static readonly Regex MathInlineRegex = new(@"\$([^$]+)\$", RegexOptions.Compiled);
        private static readonly Regex MathBlockRegex = new(@"\$\$\n?(.*?)\n?\$\$", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex TableRegex = new(@"^\|(.+)\|$", RegexOptions.Multiline | RegexOptions.Compiled);

        // HTML entities mapping
        private static readonly Dictionary<string, string> HtmlEntities = new()
        {
            { "&", "&amp;" },
            { "<", "&lt;" },
            { ">", "&gt;" },
            { "\"", "&quot;" },
            { "'", "&#39;" }
        };

        // Dangerous tags and attributes for sanitization
        private static readonly HashSet<string> DangerousTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "script", "style", "iframe", "object", "embed", "form", "input", "textarea", "select", "button"
        };

        private static readonly HashSet<string> DangerousAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            "onclick", "onload", "onmouseover", "onfocus", "onblur", "onchange", "onsubmit", "javascript"
        };

        public MarkdownService(ILogger<MarkdownService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ToHtmlAsync(string markdownContent, MarkdownProcessingOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return string.Empty;

            options ??= new MarkdownProcessingOptions();

            try
            {
                var html = markdownContent;

                // Preprocess content if needed
                if (!string.IsNullOrEmpty(options.BaseUrl))
                {
                    html = await PreprocessContentAsync(html, new MarkdownProcessingContext { BasePath = options.BaseUrl });
                }

                // Process mathematical expressions first
                if (options.EnableMathExpressions)
                {
                    html = await ProcessMathExpressionsAsync(html);
                }

                // Process code blocks
                if (options.EnableSyntaxHighlighting)
                {
                    html = await ProcessCodeBlocksAsync(html);
                }

                // Convert basic Markdown elements
                html = ProcessHeadings(html, options.GenerateHeadingIds);
                html = ProcessImages(html, options.BaseUrl);
                html = ProcessLinks(html);
                html = ProcessTextFormatting(html);
                html = ProcessBlockquotes(html);
                html = ProcessLists(html);
                html = ProcessHorizontalRules(html);

                // Process tables if enabled
                if (options.EnableTables)
                {
                    html = ProcessTables(html);
                }

                // Process task lists if enabled
                if (options.EnableTaskLists)
                {
                    html = ProcessTaskLists(html);
                }

                // Convert paragraphs
                html = ProcessParagraphs(html);

                // Apply custom classes
                html = ApplyCustomClasses(html, options.CustomClasses);

                // Sanitize the output
                html = await SanitizeHtmlAsync(html);

                _logger.LogDebug("Converted Markdown to HTML. Input length: {InputLength}, Output length: {OutputLength}",
                    markdownContent.Length, html.Length);

                return html;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Markdown to HTML");
                return $"<p>Error processing Markdown content: {ex.Message}</p>";
            }
        }

        public async Task<TableOfContents> GenerateTableOfContentsAsync(
            string markdownContent,
            TocGenerationOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return new TableOfContents();

            options ??= new TocGenerationOptions();

            try
            {
                var entries = new List<TocEntry>();
                var matches = HeadingRegex.Matches(markdownContent);

                foreach (Match match in matches)
                {
                    var level = match.Groups[1].Value.Length; // Count of # characters
                    var text = match.Groups[2].Value.Trim();

                    if (level >= options.MinHeadingLevel && level <= options.MaxHeadingLevel)
                    {
                        var anchor = GenerateAnchor(text);
                        var entry = new TocEntry
                        {
                            Text = text,
                            Level = level,
                            Anchor = anchor
                        };
                        entries.Add(entry);
                    }
                }

                // Build hierarchical structure
                var hierarchicalEntries = BuildHierarchicalToc(entries, options.MaxDepth);

                var toc = new TableOfContents
                {
                    Entries = hierarchicalEntries
                };

                if (options.GenerateHtml)
                {
                    toc = new TableOfContents
                    {
                        Entries = toc.Entries,
                        Html = GenerateTocHtml(hierarchicalEntries, options.ContainerClass),
                        PlainText = toc.PlainText
                    };
                }

                _logger.LogDebug("Generated TOC with {EntryCount} entries", entries.Count);
                return toc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating table of contents");
                return new TableOfContents();
            }
        }

        public async Task<IReadOnlyList<ImageReference>> ExtractImagesAsync(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return new List<ImageReference>();

            var images = new List<ImageReference>();
            var matches = ImageRegex.Matches(markdownContent);

            foreach (Match match in matches)
            {
                var image = new ImageReference
                {
                    AltText = match.Groups[1].Value,
                    Url = match.Groups[2].Value,
                    Title = match.Groups.Count > 3 ? match.Groups[3].Value : null,
                    Position = match.Index
                };
                images.Add(image);
            }

            _logger.LogDebug("Extracted {ImageCount} images from Markdown content", images.Count);
            return images;
        }

        public async Task<IReadOnlyList<LinkReference>> ExtractLinksAsync(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return new List<LinkReference>();

            var links = new List<LinkReference>();
            var matches = LinkRegex.Matches(markdownContent);

            foreach (Match match in matches)
            {
                var url = match.Groups[2].Value;
                var link = new LinkReference
                {
                    Text = match.Groups[1].Value,
                    Url = url,
                    Title = match.Groups.Count > 3 ? match.Groups[3].Value : null,
                    Position = match.Index,
                    IsExternal = IsExternalUrl(url)
                };
                links.Add(link);
            }

            _logger.LogDebug("Extracted {LinkCount} links from Markdown content", links.Count);
            return links;
        }

        public async Task<string> SanitizeHtmlAsync(string htmlContent, HtmlSanitizationOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                return string.Empty;

            options ??= new HtmlSanitizationOptions();

            try
            {
                var sanitized = htmlContent;

                // Remove dangerous tags
                foreach (var tag in DangerousTags)
                {
                    var openTagPattern = $@"<{tag}[^>]*>";
                    var closeTagPattern = $@"</{tag}>";
                    sanitized = Regex.Replace(sanitized, openTagPattern, "", RegexOptions.IgnoreCase);
                    sanitized = Regex.Replace(sanitized, closeTagPattern, "", RegexOptions.IgnoreCase);
                }

                // Remove dangerous attributes
                foreach (var attr in DangerousAttributes)
                {
                    var attrPattern = $@"\s{attr}\s*=\s*[""'][^""']*[""']";
                    sanitized = Regex.Replace(sanitized, attrPattern, "", RegexOptions.IgnoreCase);
                }

                // Encode HTML entities if enabled
                if (options.EncodeHtmlEntities)
                {
                    foreach (var entity in HtmlEntities)
                    {
                        sanitized = sanitized.Replace(entity.Key, entity.Value);
                    }
                }

                // Remove empty elements if enabled
                if (options.RemoveEmptyElements)
                {
                    sanitized = Regex.Replace(sanitized, @"<(\w+)[^>]*>\s*</\1>", "", RegexOptions.IgnoreCase);
                }

                _logger.LogDebug("Sanitized HTML content. Original length: {OriginalLength}, Sanitized length: {SanitizedLength}",
                    htmlContent.Length, sanitized.Length);

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing HTML content");
                return string.Empty;
            }
        }

        public async Task<MarkdownValidationResult> ValidateContentAsync(
            string markdownContent,
            MarkdownValidationOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return new MarkdownValidationResult { IsValid = true };

            options ??= new MarkdownValidationOptions();
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Check content length
                if (markdownContent.Length > options.MaxContentLength)
                {
                    errors.Add($"Content exceeds maximum length of {options.MaxContentLength} characters");
                }

                // Extract and validate images
                var images = await ExtractImagesAsync(markdownContent);
                if (images.Count > options.MaxImagesCount)
                {
                    warnings.Add($"Content contains {images.Count} images, which exceeds the recommended maximum of {options.MaxImagesCount}");
                }

                // Extract and validate links
                var links = await ExtractLinksAsync(markdownContent);
                if (links.Count > options.MaxLinksCount)
                {
                    warnings.Add($"Content contains {links.Count} links, which exceeds the recommended maximum of {options.MaxLinksCount}");
                }

                // Check for malicious content if enabled
                if (options.CheckForMaliciousContent)
                {
                    var maliciousPatterns = new[]
                    {
                        @"javascript:",
                        @"data:",
                        @"<script",
                        @"onclick=",
                        @"onload=",
                        @"eval\(",
                        @"document\."
                    };

                    foreach (var pattern in maliciousPatterns)
                    {
                        if (Regex.IsMatch(markdownContent, pattern, RegexOptions.IgnoreCase))
                        {
                            errors.Add($"Content contains potentially malicious pattern: {pattern}");
                        }
                    }
                }

                // Generate statistics
                var statistics = new ContentStatistics
                {
                    CharacterCount = markdownContent.Length,
                    WordCount = await CountWordsAsync(markdownContent),
                    ParagraphCount = markdownContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length,
                    HeadingCount = HeadingRegex.Matches(markdownContent).Count,
                    ImageCount = images.Count,
                    LinkCount = links.Count,
                    CodeBlockCount = CodeBlockRegex.Matches(markdownContent).Count,
                    EstimatedReadingTime = await EstimateReadingTimeAsync(markdownContent)
                };

                var result = new MarkdownValidationResult
                {
                    IsValid = !errors.Any(),
                    Errors = errors,
                    Warnings = warnings,
                    Statistics = statistics
                };

                _logger.LogDebug("Validated Markdown content. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                    result.IsValid, errors.Count, warnings.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Markdown content");
                return new MarkdownValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "Error during content validation" }
                };
            }
        }

        public async Task<string> ExtractSummaryAsync(
            string markdownContent,
            int maxLength = 200,
            bool preserveFormatting = false)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return string.Empty;

            try
            {
                var plainText = await ToPlainTextAsync(markdownContent, false);

                if (plainText.Length <= maxLength)
                    return plainText;

                // Find the best place to cut the text
                var summary = plainText.Substring(0, maxLength);
                var lastSentenceEnd = summary.LastIndexOfAny(new[] { '.', '!', '?' });
                var lastWordEnd = summary.LastIndexOf(' ');

                if (lastSentenceEnd > maxLength / 2)
                {
                    summary = plainText.Substring(0, lastSentenceEnd + 1);
                }
                else if (lastWordEnd > 0)
                {
                    summary = plainText.Substring(0, lastWordEnd) + "...";
                }
                else
                {
                    summary += "...";
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting summary");
                return string.Empty;
            }
        }

        public async Task<int> CountWordsAsync(string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return 0;

            try
            {
                var plainText = await ToPlainTextAsync(markdownContent);
                var matches = WordRegex.Matches(plainText);
                return matches.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting words");
                return 0;
            }
        }

        public async Task<int> EstimateReadingTimeAsync(string markdownContent, int wordsPerMinute = 200)
        {
            var wordCount = await CountWordsAsync(markdownContent);
            return Math.Max(1, (int)Math.Ceiling((double)wordCount / wordsPerMinute));
        }

        public async Task<string> PreprocessContentAsync(string markdownContent, MarkdownProcessingContext? context = null)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return string.Empty;

            var processed = markdownContent;

            // Replace variables if context is provided
            if (context?.Variables != null)
            {
                foreach (var variable in context.Variables)
                {
                    var placeholder = $"{{{{{variable.Key}}}}}";
                    processed = processed.Replace(placeholder, variable.Value);
                }
            }

            return processed;
        }

        public async Task<string> ProcessCodeBlocksAsync(
            string markdownContent,
            CodeHighlightingOptions? options = null)
        {
            options ??= new CodeHighlightingOptions();

            return CodeBlockRegex.Replace(markdownContent, match =>
            {
                var language = match.Groups[1].Value;
                var code = match.Groups[2].Value;

                if (string.IsNullOrEmpty(language))
                    language = options.DefaultLanguage;

                var lines = code.Split('\n');
                if (lines.Length > options.MaxLines)
                {
                    code = string.Join('\n', lines.Take(options.MaxLines)) + "\n... (truncated)";
                }

                var codeHtml = $"<pre><code class=\"language-{language}\">{EscapeHtml(code)}</code></pre>";

                if (options.ShowLineNumbers)
                {
                    // Simple line number implementation
                    var numberedLines = lines.Select((line, index) =>
                        $"<span class=\"line-number\">{index + 1}</span>{EscapeHtml(line)}");
                    codeHtml = $"<pre class=\"code-with-lines\"><code class=\"language-{language}\">{string.Join('\n', numberedLines)}</code></pre>";
                }

                return codeHtml;
            });
        }

        public async Task<string> ToPlainTextAsync(string markdownContent, bool preserveLineBreaks = false)
        {
            if (string.IsNullOrWhiteSpace(markdownContent))
                return string.Empty;

            var plainText = markdownContent;

            // Remove code blocks first
            plainText = CodeBlockRegex.Replace(plainText, "");

            // Remove inline code
            plainText = InlineCodeRegex.Replace(plainText, "$1");

            // Remove images
            plainText = ImageRegex.Replace(plainText, "");

            // Replace links with just the text
            plainText = LinkRegex.Replace(plainText, "$1");

            // Remove formatting
            plainText = BoldRegex.Replace(plainText, "$1");
            plainText = ItalicRegex.Replace(plainText, "$1");
            plainText = StrikethroughRegex.Replace(plainText, "$1");

            // Remove headings markers
            plainText = HeadingRegex.Replace(plainText, "$2");

            // Remove blockquote markers
            plainText = BlockquoteRegex.Replace(plainText, "$1");

            // Remove list markers
            plainText = ListItemRegex.Replace(plainText, "$2");
            plainText = OrderedListItemRegex.Replace(plainText, "$2");

            // Remove horizontal rules
            plainText = HorizontalRuleRegex.Replace(plainText, "");

            // Handle line breaks
            if (!preserveLineBreaks)
            {
                plainText = Regex.Replace(plainText, @"\s+", " ");
            }

            return plainText.Trim();
        }

        public async Task<string> ProcessMathExpressionsAsync(
            string markdownContent,
            MathProcessingOptions? options = null)
        {
            options ??= new MathProcessingOptions();

            // Process block math
            var processed = MathBlockRegex.Replace(markdownContent, match =>
            {
                var mathContent = match.Groups[1].Value;
                return $"<div class=\"math-block\" data-math=\"{EscapeHtml(mathContent)}\">$${mathContent}$$</div>";
            });

            // Process inline math
            processed = MathInlineRegex.Replace(processed, match =>
            {
                var mathContent = match.Groups[1].Value;
                return $"<span class=\"math-inline\" data-math=\"{EscapeHtml(mathContent)}\">${mathContent}$</span>";
            });

            return processed;
        }

        #region Private Helper Methods

        private string ProcessHeadings(string content, bool generateIds)
        {
            return HeadingRegex.Replace(content, match =>
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value.Trim();
                var id = generateIds ? $" id=\"{GenerateAnchor(text)}\"" : "";
                return $"<h{level}{id}>{text}</h{level}>";
            });
        }

        private string ProcessImages(string content, string? baseUrl)
        {
            return ImageRegex.Replace(content, match =>
            {
                var alt = EscapeHtml(match.Groups[1].Value);
                var src = match.Groups[2].Value;
                var title = match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value)
                    ? $" title=\"{EscapeHtml(match.Groups[3].Value)}\""
                    : "";

                if (!string.IsNullOrEmpty(baseUrl) && !IsAbsoluteUrl(src))
                {
                    src = $"{baseUrl.TrimEnd('/')}/{src.TrimStart('/')}";
                }

                return $"<img src=\"{src}\" alt=\"{alt}\"{title} />";
            });
        }

        private string ProcessLinks(string content)
        {
            return LinkRegex.Replace(content, match =>
            {
                var text = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                var title = match.Groups.Count > 3 && !string.IsNullOrEmpty(match.Groups[3].Value)
                    ? $" title=\"{EscapeHtml(match.Groups[3].Value)}\""
                    : "";

                var target = IsExternalUrl(url) ? " target=\"_blank\" rel=\"noopener noreferrer\"" : "";
                return $"<a href=\"{url}\"{title}{target}>{text}</a>";
            });
        }

        private string ProcessTextFormatting(string content)
        {
            // Bold
            content = BoldRegex.Replace(content, "<strong>$1</strong>");

            // Italic
            content = ItalicRegex.Replace(content, "<em>$1</em>");

            // Strikethrough
            content = StrikethroughRegex.Replace(content, "<del>$1</del>");

            // Inline code
            content = InlineCodeRegex.Replace(content, "<code>$1</code>");

            return content;
        }

        private string ProcessBlockquotes(string content)
        {
            var lines = content.Split('\n');
            var result = new StringBuilder();
            var inBlockquote = false;

            foreach (var line in lines)
            {
                var match = BlockquoteRegex.Match(line);
                if (match.Success)
                {
                    if (!inBlockquote)
                    {
                        result.AppendLine("<blockquote>");
                        inBlockquote = true;
                    }
                    result.AppendLine($"<p>{match.Groups[1].Value}</p>");
                }
                else
                {
                    if (inBlockquote)
                    {
                        result.AppendLine("</blockquote>");
                        inBlockquote = false;
                    }
                    result.AppendLine(line);
                }
            }

            if (inBlockquote)
            {
                result.AppendLine("</blockquote>");
            }

            return result.ToString();
        }

        private string ProcessLists(string content)
        {
            // This is a simplified list processor
            // A full implementation would handle nested lists properly

            var lines = content.Split('\n');
            var result = new StringBuilder();
            var inUnorderedList = false;
            var inOrderedList = false;

            foreach (var line in lines)
            {
                var unorderedMatch = ListItemRegex.Match(line);
                var orderedMatch = OrderedListItemRegex.Match(line);

                if (unorderedMatch.Success)
                {
                    if (!inUnorderedList)
                    {
                        if (inOrderedList)
                        {
                            result.AppendLine("</ol>");
                            inOrderedList = false;
                        }
                        result.AppendLine("<ul>");
                        inUnorderedList = true;
                    }
                    result.AppendLine($"<li>{unorderedMatch.Groups[2].Value}</li>");
                }
                else if (orderedMatch.Success)
                {
                    if (!inOrderedList)
                    {
                        if (inUnorderedList)
                        {
                            result.AppendLine("</ul>");
                            inUnorderedList = false;
                        }
                        result.AppendLine("<ol>");
                        inOrderedList = true;
                    }
                    result.AppendLine($"<li>{orderedMatch.Groups[2].Value}</li>");
                }
                else
                {
                    if (inUnorderedList)
                    {
                        result.AppendLine("</ul>");
                        inUnorderedList = false;
                    }
                    if (inOrderedList)
                    {
                        result.AppendLine("</ol>");
                        inOrderedList = false;
                    }
                    result.AppendLine(line);
                }
            }

            if (inUnorderedList) result.AppendLine("</ul>");
            if (inOrderedList) result.AppendLine("</ol>");

            return result.ToString();
        }

        private string ProcessHorizontalRules(string content)
        {
            return HorizontalRuleRegex.Replace(content, "<hr />");
        }

        private string ProcessTables(string content)
        {
            // Basic table processing - a full implementation would be more complex
            var lines = content.Split('\n');
            var result = new StringBuilder();
            var inTable = false;
            var headerProcessed = false;

            foreach (var line in lines)
            {
                if (TableRegex.IsMatch(line))
                {
                    if (!inTable)
                    {
                        result.AppendLine("<table>");
                        inTable = true;
                        headerProcessed = false;
                    }

                    var cells = line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();

                    if (!headerProcessed)
                    {
                        result.AppendLine("<thead><tr>");
                        foreach (var cell in cells)
                        {
                            result.AppendLine($"<th>{cell}</th>");
                        }
                        result.AppendLine("</tr></thead><tbody>");
                        headerProcessed = true;
                    }
                    else if (!line.Contains("---")) // Skip separator line
                    {
                        result.AppendLine("<tr>");
                        foreach (var cell in cells)
                        {
                            result.AppendLine($"<td>{cell}</td>");
                        }
                        result.AppendLine("</tr>");
                    }
                }
                else
                {
                    if (inTable)
                    {
                        result.AppendLine("</tbody></table>");
                        inTable = false;
                    }
                    result.AppendLine(line);
                }
            }

            if (inTable)
            {
                result.AppendLine("</tbody></table>");
            }

            return result.ToString();
        }

        private string ProcessTaskLists(string content)
        {
            var taskListRegex = new Regex(@"^(\s*)[-*+]\s+\[([ x])\]\s+(.+)$", RegexOptions.Multiline);
            return taskListRegex.Replace(content, match =>
            {
                var indent = match.Groups[1].Value;
                var isChecked = match.Groups[2].Value == "x";
                var text = match.Groups[3].Value;
                var checkedAttr = isChecked ? " checked" : "";
                return $"{indent}<input type=\"checkbox\" disabled{checkedAttr} /> {text}";
            });
        }

        private string ProcessParagraphs(string content)
        {
            // Split content into blocks and wrap non-HTML blocks in <p> tags
            var blocks = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var block in blocks)
            {
                var trimmedBlock = block.Trim();
                if (string.IsNullOrEmpty(trimmedBlock))
                    continue;

                // Check if it's already an HTML block
                if (trimmedBlock.StartsWith("<") && (
                    trimmedBlock.StartsWith("<h") ||
                    trimmedBlock.StartsWith("<p") ||
                    trimmedBlock.StartsWith("<div") ||
                    trimmedBlock.StartsWith("<blockquote") ||
                    trimmedBlock.StartsWith("<ul") ||
                    trimmedBlock.StartsWith("<ol") ||
                    trimmedBlock.StartsWith("<pre") ||
                    trimmedBlock.StartsWith("<table") ||
                    trimmedBlock.StartsWith("<hr")))
                {
                    result.AppendLine(trimmedBlock);
                }
                else
                {
                    result.AppendLine($"<p>{trimmedBlock}</p>");
                }
            }

            return result.ToString();
        }

        private string ApplyCustomClasses(string content, IDictionary<string, string> customClasses)
        {
            foreach (var customClass in customClasses)
            {
                var pattern = $@"<{customClass.Key}(\s[^>]*)?>";
                var replacement = $@"<{customClass.Key} class=""{customClass.Value}""$1>";
                content = Regex.Replace(content, pattern, replacement, RegexOptions.IgnoreCase);
            }
            return content;
        }

        private static string GenerateAnchor(string text)
        {
            var anchor = text.ToLowerInvariant();
            anchor = Regex.Replace(anchor, @"[^\w\s-]", "");
            anchor = Regex.Replace(anchor, @"\s+", "-");
            anchor = anchor.Trim('-');
            return anchor;
        }

        private static bool IsAbsoluteUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }

        private static bool IsExternalUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Scheme == "http" || uri.Scheme == "https";
            }
            return false;
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        private static IReadOnlyList<TocEntry> BuildHierarchicalToc(List<TocEntry> entries, int maxDepth)
        {
            // Simplified hierarchical TOC builder
            // A full implementation would properly handle nested structures
            return entries.Where(e => e.Level <= maxDepth).ToList();
        }

        private static string GenerateTocHtml(IReadOnlyList<TocEntry> entries, string containerClass)
        {
            if (!entries.Any())
                return string.Empty;

            var html = new StringBuilder();
            html.AppendLine($"<div class=\"{containerClass}\">");
            html.AppendLine("<ul>");

            foreach (var entry in entries)
            {
                var link = string.IsNullOrEmpty(entry.Anchor)
                    ? entry.Text
                    : $"<a href=\"#{entry.Anchor}\">{entry.Text}</a>";
                html.AppendLine($"<li>{link}</li>");
            }

            html.AppendLine("</ul>");
            html.AppendLine("</div>");

            return html.ToString();
        }

        #endregion
    }
}