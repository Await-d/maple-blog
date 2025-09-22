namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Service interface for Markdown processing operations
    /// </summary>
    public interface IMarkdownService
    {
        /// <summary>
        /// Converts Markdown content to HTML
        /// </summary>
        /// <param name="markdownContent">Markdown content to convert</param>
        /// <param name="options">Processing options</param>
        /// <returns>HTML content</returns>
        Task<string> ToHtmlAsync(string markdownContent, MarkdownProcessingOptions? options = null);

        /// <summary>
        /// Generates a table of contents from Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content to analyze</param>
        /// <param name="options">TOC generation options</param>
        /// <returns>Table of contents</returns>
        Task<TableOfContents> GenerateTableOfContentsAsync(
            string markdownContent,
            TocGenerationOptions? options = null);

        /// <summary>
        /// Extracts images from Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content to analyze</param>
        /// <returns>List of image references</returns>
        Task<IReadOnlyList<ImageReference>> ExtractImagesAsync(string markdownContent);

        /// <summary>
        /// Extracts links from Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content to analyze</param>
        /// <returns>List of link references</returns>
        Task<IReadOnlyList<LinkReference>> ExtractLinksAsync(string markdownContent);

        /// <summary>
        /// Sanitizes HTML content to prevent XSS attacks
        /// </summary>
        /// <param name="htmlContent">HTML content to sanitize</param>
        /// <param name="options">Sanitization options</param>
        /// <returns>Sanitized HTML content</returns>
        Task<string> SanitizeHtmlAsync(string htmlContent, HtmlSanitizationOptions? options = null);

        /// <summary>
        /// Validates Markdown content for security and formatting issues
        /// </summary>
        /// <param name="markdownContent">Markdown content to validate</param>
        /// <param name="options">Validation options</param>
        /// <returns>Validation result</returns>
        Task<MarkdownValidationResult> ValidateContentAsync(
            string markdownContent,
            MarkdownValidationOptions? options = null);

        /// <summary>
        /// Extracts a plain text summary from Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content</param>
        /// <param name="maxLength">Maximum length of the summary</param>
        /// <param name="preserveFormatting">Whether to preserve some formatting</param>
        /// <returns>Plain text summary</returns>
        Task<string> ExtractSummaryAsync(
            string markdownContent,
            int maxLength = 200,
            bool preserveFormatting = false);

        /// <summary>
        /// Counts words in Markdown content (excluding markup)
        /// </summary>
        /// <param name="markdownContent">Markdown content</param>
        /// <returns>Word count</returns>
        Task<int> CountWordsAsync(string markdownContent);

        /// <summary>
        /// Estimates reading time for Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content</param>
        /// <param name="wordsPerMinute">Average words per minute (default: 200)</param>
        /// <returns>Estimated reading time in minutes</returns>
        Task<int> EstimateReadingTimeAsync(string markdownContent, int wordsPerMinute = 200);

        /// <summary>
        /// Preprocesses Markdown content (e.g., replace placeholders, process includes)
        /// </summary>
        /// <param name="markdownContent">Markdown content to preprocess</param>
        /// <param name="context">Processing context</param>
        /// <returns>Preprocessed Markdown content</returns>
        Task<string> PreprocessContentAsync(string markdownContent, MarkdownProcessingContext? context = null);

        /// <summary>
        /// Processes code blocks and applies syntax highlighting
        /// </summary>
        /// <param name="markdownContent">Markdown content with code blocks</param>
        /// <param name="options">Code highlighting options</param>
        /// <returns>Processed content with syntax highlighting</returns>
        Task<string> ProcessCodeBlocksAsync(
            string markdownContent,
            CodeHighlightingOptions? options = null);

        /// <summary>
        /// Converts Markdown content to plain text
        /// </summary>
        /// <param name="markdownContent">Markdown content</param>
        /// <param name="preserveLineBreaks">Whether to preserve line breaks</param>
        /// <returns>Plain text content</returns>
        Task<string> ToPlainTextAsync(string markdownContent, bool preserveLineBreaks = false);

        /// <summary>
        /// Processes mathematical expressions in Markdown content
        /// </summary>
        /// <param name="markdownContent">Markdown content with math expressions</param>
        /// <param name="options">Math processing options</param>
        /// <returns>Content with processed mathematical expressions</returns>
        Task<string> ProcessMathExpressionsAsync(
            string markdownContent,
            MathProcessingOptions? options = null);
    }

    /// <summary>
    /// Markdown processing options
    /// </summary>
    public class MarkdownProcessingOptions
    {
        /// <summary>
        /// Enable GitHub Flavored Markdown extensions
        /// </summary>
        public bool EnableGitHubFlavored { get; init; } = true;

        /// <summary>
        /// Enable syntax highlighting for code blocks
        /// </summary>
        public bool EnableSyntaxHighlighting { get; init; } = true;

        /// <summary>
        /// Enable mathematical expressions processing
        /// </summary>
        public bool EnableMathExpressions { get; init; } = true;

        /// <summary>
        /// Enable emoji processing
        /// </summary>
        public bool EnableEmojis { get; init; } = true;

        /// <summary>
        /// Enable automatic linking of URLs
        /// </summary>
        public bool EnableAutoLinks { get; init; } = true;

        /// <summary>
        /// Enable table processing
        /// </summary>
        public bool EnableTables { get; init; } = true;

        /// <summary>
        /// Enable task list processing
        /// </summary>
        public bool EnableTaskLists { get; init; } = true;

        /// <summary>
        /// Generate heading IDs for anchor linking
        /// </summary>
        public bool GenerateHeadingIds { get; init; } = true;

        /// <summary>
        /// Custom CSS classes to add to generated HTML
        /// </summary>
        public IDictionary<string, string> CustomClasses { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// Base URL for relative links and images
        /// </summary>
        public string? BaseUrl { get; init; }
    }

    /// <summary>
    /// Table of contents generation options
    /// </summary>
    public class TocGenerationOptions
    {
        /// <summary>
        /// Minimum heading level to include (1-6)
        /// </summary>
        public int MinHeadingLevel { get; init; } = 2;

        /// <summary>
        /// Maximum heading level to include (1-6)
        /// </summary>
        public int MaxHeadingLevel { get; init; } = 4;

        /// <summary>
        /// Maximum depth of nested headings
        /// </summary>
        public int MaxDepth { get; init; } = 3;

        /// <summary>
        /// Generate HTML output for TOC
        /// </summary>
        public bool GenerateHtml { get; init; } = false;

        /// <summary>
        /// CSS class for TOC container
        /// </summary>
        public string ContainerClass { get; init; } = "table-of-contents";
    }

    /// <summary>
    /// HTML sanitization options
    /// </summary>
    public class HtmlSanitizationOptions
    {
        /// <summary>
        /// Allowed HTML tags
        /// </summary>
        public IReadOnlyList<string> AllowedTags { get; init; } = new List<string>();

        /// <summary>
        /// Allowed HTML attributes
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedAttributes { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

        /// <summary>
        /// Allowed URI schemes for links and images
        /// </summary>
        public IReadOnlyList<string> AllowedUriSchemes { get; init; } = new List<string> { "http", "https", "mailto" };

        /// <summary>
        /// Remove empty elements
        /// </summary>
        public bool RemoveEmptyElements { get; init; } = true;

        /// <summary>
        /// Encode HTML entities
        /// </summary>
        public bool EncodeHtmlEntities { get; init; } = true;
    }

    /// <summary>
    /// Markdown validation options
    /// </summary>
    public class MarkdownValidationOptions
    {
        /// <summary>
        /// Maximum content length
        /// </summary>
        public int MaxContentLength { get; init; } = 1000000; // 1MB

        /// <summary>
        /// Check for potentially malicious content
        /// </summary>
        public bool CheckForMaliciousContent { get; init; } = true;

        /// <summary>
        /// Validate image URLs
        /// </summary>
        public bool ValidateImageUrls { get; init; } = true;

        /// <summary>
        /// Validate link URLs
        /// </summary>
        public bool ValidateLinkUrls { get; init; } = true;

        /// <summary>
        /// Maximum number of images allowed
        /// </summary>
        public int MaxImagesCount { get; init; } = 100;

        /// <summary>
        /// Maximum number of links allowed
        /// </summary>
        public int MaxLinksCount { get; init; } = 200;
    }

    /// <summary>
    /// Code highlighting options
    /// </summary>
    public class CodeHighlightingOptions
    {
        /// <summary>
        /// Default language for code blocks without language specification
        /// </summary>
        public string DefaultLanguage { get; init; } = "text";

        /// <summary>
        /// Theme for syntax highlighting
        /// </summary>
        public string Theme { get; init; } = "default";

        /// <summary>
        /// Show line numbers
        /// </summary>
        public bool ShowLineNumbers { get; init; } = false;

        /// <summary>
        /// Highlight specific lines (line numbers)
        /// </summary>
        public IReadOnlyList<int> HighlightedLines { get; init; } = new List<int>();

        /// <summary>
        /// Maximum lines in a code block
        /// </summary>
        public int MaxLines { get; init; } = 1000;
    }

    /// <summary>
    /// Math processing options
    /// </summary>
    public class MathProcessingOptions
    {
        /// <summary>
        /// Math renderer to use (MathJax, KaTeX, etc.)
        /// </summary>
        public string Renderer { get; init; } = "MathJax";

        /// <summary>
        /// Inline math delimiter
        /// </summary>
        public string InlineDelimiter { get; init; } = "$";

        /// <summary>
        /// Block math delimiter
        /// </summary>
        public string BlockDelimiter { get; init; } = "$$";

        /// <summary>
        /// Enable LaTeX-style math commands
        /// </summary>
        public bool EnableLatexCommands { get; init; } = true;
    }

    /// <summary>
    /// Markdown processing context
    /// </summary>
    public class MarkdownProcessingContext
    {
        /// <summary>
        /// Current user ID (for permission checks)
        /// </summary>
        public Guid? UserId { get; init; }

        /// <summary>
        /// Post ID (for content-specific processing)
        /// </summary>
        public Guid? PostId { get; init; }

        /// <summary>
        /// Base path for file includes
        /// </summary>
        public string? BasePath { get; init; }

        /// <summary>
        /// Custom variables for content substitution
        /// </summary>
        public IDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Table of contents structure
    /// </summary>
    public class TableOfContents
    {
        /// <summary>
        /// TOC entries
        /// </summary>
        public IReadOnlyList<TocEntry> Entries { get; init; } = new List<TocEntry>();

        /// <summary>
        /// HTML representation of TOC
        /// </summary>
        public string? Html { get; init; }

        /// <summary>
        /// Plain text representation of TOC
        /// </summary>
        public string? PlainText { get; init; }
    }

    /// <summary>
    /// Table of contents entry
    /// </summary>
    public class TocEntry
    {
        /// <summary>
        /// Heading text
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Heading level (1-6)
        /// </summary>
        public int Level { get; init; }

        /// <summary>
        /// Anchor ID for linking
        /// </summary>
        public string? Anchor { get; init; }

        /// <summary>
        /// Child entries (for nested headings)
        /// </summary>
        public IReadOnlyList<TocEntry> Children { get; init; } = new List<TocEntry>();
    }

    /// <summary>
    /// Image reference in Markdown content
    /// </summary>
    public class ImageReference
    {
        /// <summary>
        /// Image URL or path
        /// </summary>
        public string Url { get; init; } = string.Empty;

        /// <summary>
        /// Alternative text
        /// </summary>
        public string? AltText { get; init; }

        /// <summary>
        /// Image title
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Position in the content (character index)
        /// </summary>
        public int Position { get; init; }
    }

    /// <summary>
    /// Link reference in Markdown content
    /// </summary>
    public class LinkReference
    {
        /// <summary>
        /// Link URL
        /// </summary>
        public string Url { get; init; } = string.Empty;

        /// <summary>
        /// Link text
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Link title
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Position in the content (character index)
        /// </summary>
        public int Position { get; init; }

        /// <summary>
        /// Whether the link is external
        /// </summary>
        public bool IsExternal { get; init; }
    }

    /// <summary>
    /// Markdown validation result
    /// </summary>
    public class MarkdownValidationResult
    {
        /// <summary>
        /// Whether the content is valid
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// Validation errors
        /// </summary>
        public IReadOnlyList<string> Errors { get; init; } = new List<string>();

        /// <summary>
        /// Validation warnings
        /// </summary>
        public IReadOnlyList<string> Warnings { get; init; } = new List<string>();

        /// <summary>
        /// Content statistics
        /// </summary>
        public ContentStatistics Statistics { get; init; } = new();
    }

    /// <summary>
    /// Content statistics
    /// </summary>
    public class ContentStatistics
    {
        /// <summary>
        /// Total character count
        /// </summary>
        public int CharacterCount { get; init; }

        /// <summary>
        /// Word count (excluding markup)
        /// </summary>
        public int WordCount { get; init; }

        /// <summary>
        /// Number of paragraphs
        /// </summary>
        public int ParagraphCount { get; init; }

        /// <summary>
        /// Number of headings
        /// </summary>
        public int HeadingCount { get; init; }

        /// <summary>
        /// Number of images
        /// </summary>
        public int ImageCount { get; init; }

        /// <summary>
        /// Number of links
        /// </summary>
        public int LinkCount { get; init; }

        /// <summary>
        /// Number of code blocks
        /// </summary>
        public int CodeBlockCount { get; init; }

        /// <summary>
        /// Estimated reading time in minutes
        /// </summary>
        public int EstimatedReadingTime { get; init; }
    }
}