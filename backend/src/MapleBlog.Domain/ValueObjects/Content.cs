namespace MapleBlog.Domain.ValueObjects
{
    /// <summary>
    /// Content value object that encapsulates article content with metadata
    /// </summary>
    public class Content : IEquatable<Content>
    {
        public string RawContent { get; private set; }
        public string? ProcessedHtml { get; private set; }
        public int WordCount { get; private set; }
        public int ReadingTimeMinutes { get; private set; }
        public DateTime LastProcessedAt { get; private set; }

        private Content(string rawContent, string? processedHtml = null)
        {
            RawContent = rawContent;
            ProcessedHtml = processedHtml;
            WordCount = CalculateWordCount(rawContent);
            ReadingTimeMinutes = CalculateReadingTime(WordCount);
            LastProcessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new Content value object
        /// </summary>
        /// <param name="rawContent">The raw markdown content</param>
        /// <returns>A new Content instance</returns>
        /// <exception cref="ArgumentException">Thrown when the content is null or exceeds limits</exception>
        public static Content Create(string rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                throw new ArgumentException("Content cannot be null or empty.", nameof(rawContent));

            if (rawContent.Length > 1000000) // 1MB limit for content
                throw new ArgumentException("Content cannot exceed 1MB.", nameof(rawContent));

            return new Content(rawContent.Trim());
        }

        /// <summary>
        /// Creates a content object with pre-processed HTML
        /// </summary>
        /// <param name="rawContent">The raw markdown content</param>
        /// <param name="processedHtml">The processed HTML content</param>
        /// <returns>A new Content instance</returns>
        public static Content CreateWithHtml(string rawContent, string processedHtml)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                throw new ArgumentException("Content cannot be null or empty.", nameof(rawContent));

            if (string.IsNullOrWhiteSpace(processedHtml))
                throw new ArgumentException("Processed HTML cannot be null or empty.", nameof(processedHtml));

            return new Content(rawContent.Trim(), processedHtml.Trim());
        }

        /// <summary>
        /// Updates the processed HTML content
        /// </summary>
        /// <param name="processedHtml">The new processed HTML</param>
        /// <returns>A new Content instance with updated HTML</returns>
        public Content WithProcessedHtml(string processedHtml)
        {
            if (string.IsNullOrWhiteSpace(processedHtml))
                throw new ArgumentException("Processed HTML cannot be null or empty.", nameof(processedHtml));

            return new Content(RawContent, processedHtml.Trim());
        }

        /// <summary>
        /// Updates the raw content
        /// </summary>
        /// <param name="newRawContent">The new raw content</param>
        /// <returns>A new Content instance with updated content</returns>
        public Content WithRawContent(string newRawContent)
        {
            return Create(newRawContent);
        }

        /// <summary>
        /// Calculates word count from raw content
        /// </summary>
        /// <param name="content">The content to count words from</param>
        /// <returns>The number of words</returns>
        private static int CalculateWordCount(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            // Remove markdown syntax for more accurate word count
            var cleanContent = content
                .Replace("#", "")
                .Replace("*", "")
                .Replace("_", "")
                .Replace("`", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("(", "")
                .Replace(")", "");

            var words = cleanContent
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Count();

            return words;
        }

        /// <summary>
        /// Calculates estimated reading time based on word count
        /// Average reading speed: 200 words per minute
        /// </summary>
        /// <param name="wordCount">The number of words</param>
        /// <returns>Estimated reading time in minutes</returns>
        private static int CalculateReadingTime(int wordCount)
        {
            const int averageWordsPerMinute = 200;
            var readingTime = (int)Math.Ceiling((double)wordCount / averageWordsPerMinute);
            return Math.Max(1, readingTime); // Minimum 1 minute reading time
        }

        /// <summary>
        /// Extracts a summary from the content
        /// </summary>
        /// <param name="maxLength">Maximum length of the summary</param>
        /// <returns>A summary of the content</returns>
        public string ExtractSummary(int maxLength = 200)
        {
            if (string.IsNullOrWhiteSpace(RawContent))
                return string.Empty;

            // Remove markdown headers and formatting
            var cleanContent = RawContent
                .Replace("#", "")
                .Replace("*", "")
                .Replace("_", "")
                .Replace("`", "")
                .Trim();

            // Get first paragraph or first few sentences
            var firstParagraph = cleanContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();

            if (string.IsNullOrEmpty(firstParagraph))
                return string.Empty;

            if (firstParagraph.Length <= maxLength)
                return firstParagraph;

            // Truncate at word boundary
            var truncated = firstParagraph.Substring(0, maxLength);
            var lastSpaceIndex = truncated.LastIndexOf(' ');

            if (lastSpaceIndex > 0)
                truncated = truncated.Substring(0, lastSpaceIndex);

            return truncated + "...";
        }

        public override string ToString() => RawContent;

        public override bool Equals(object? obj)
        {
            return obj is Content other && Equals(other);
        }

        public bool Equals(Content? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(RawContent, other.RawContent, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return RawContent.GetHashCode();
        }

        public static bool operator ==(Content? left, Content? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Content? left, Content? right)
        {
            return !(left == right);
        }

        public static implicit operator string(Content content) => content?.RawContent ?? string.Empty;
    }
}