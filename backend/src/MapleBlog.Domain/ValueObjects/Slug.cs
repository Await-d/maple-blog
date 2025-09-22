using System.Text.RegularExpressions;

namespace MapleBlog.Domain.ValueObjects
{
    /// <summary>
    /// Slug value object that ensures URL-friendly identifiers
    /// </summary>
    public class Slug : IEquatable<Slug>
    {
        private static readonly Regex SlugRegex = new Regex(
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.Compiled);

        private static readonly Regex InvalidCharsRegex = new Regex(
            @"[^a-z0-9\-\s]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WhitespaceRegex = new Regex(
            @"\s+",
            RegexOptions.Compiled);

        private static readonly Regex MultipleHyphensRegex = new Regex(
            @"-+",
            RegexOptions.Compiled);

        public string Value { get; private set; }

        private Slug(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new Slug value object from a title or text
        /// </summary>
        /// <param name="text">The text to convert to a slug</param>
        /// <returns>A new Slug instance</returns>
        /// <exception cref="ArgumentException">Thrown when the text cannot be converted to a valid slug</exception>
        public static Slug Create(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be null or empty.", nameof(text));

            var slug = GenerateSlug(text);

            if (string.IsNullOrEmpty(slug))
                throw new ArgumentException($"Cannot generate valid slug from text: {text}", nameof(text));

            if (slug.Length > 100) // Reasonable URL length limit
                slug = slug.Substring(0, 100).TrimEnd('-');

            return new Slug(slug);
        }

        /// <summary>
        /// Creates a slug directly from a pre-validated slug string
        /// </summary>
        /// <param name="slug">The slug string</param>
        /// <returns>A new Slug instance</returns>
        /// <exception cref="ArgumentException">Thrown when the slug format is invalid</exception>
        public static Slug FromString(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug cannot be null or empty.", nameof(slug));

            slug = slug.Trim().ToLowerInvariant();

            if (!IsValidFormat(slug))
                throw new ArgumentException($"Invalid slug format: {slug}", nameof(slug));

            return new Slug(slug);
        }

        /// <summary>
        /// Generates a URL-friendly slug from text
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <returns>A URL-friendly slug</returns>
        private static string GenerateSlug(string text)
        {
            // Convert to lowercase and trim
            var slug = text.Trim().ToLowerInvariant();

            // Remove invalid characters
            slug = InvalidCharsRegex.Replace(slug, "");

            // Replace whitespace with hyphens
            slug = WhitespaceRegex.Replace(slug, "-");

            // Replace multiple consecutive hyphens with single hyphen
            slug = MultipleHyphensRegex.Replace(slug, "-");

            // Remove leading and trailing hyphens
            slug = slug.Trim('-');

            return slug;
        }

        /// <summary>
        /// Validates the slug format without throwing exceptions
        /// </summary>
        /// <param name="slug">The slug to validate</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool IsValidFormat(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            return SlugRegex.IsMatch(slug);
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
        {
            return obj is Slug other && Equals(other);
        }

        public bool Equals(Slug? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(Slug? left, Slug? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Slug? left, Slug? right)
        {
            return !(left == right);
        }

        public static implicit operator string(Slug slug) => slug?.Value ?? string.Empty;
    }
}