using System.Text.RegularExpressions;

namespace MapleBlog.Domain.ValueObjects
{
    /// <summary>
    /// Email value object that ensures valid email format
    /// </summary>
    public class Email : IEquatable<Email>
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; private set; }

        private Email(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new Email value object if the format is valid
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>A new Email instance</returns>
        /// <exception cref="ArgumentException">Thrown when the email format is invalid</exception>
        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            email = email.Trim().ToLowerInvariant();

            if (!IsValidFormat(email))
                throw new ArgumentException($"Invalid email format: {email}", nameof(email));

            if (email.Length > 254) // RFC 5321 limit
                throw new ArgumentException("Email address cannot exceed 254 characters.", nameof(email));

            return new Email(email);
        }

        /// <summary>
        /// Validates the email format without throwing exceptions
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool IsValidFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Validates the email format without throwing exceptions (alias for IsValidFormat)
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool IsValidEmail(string email)
        {
            return IsValidFormat(email);
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj)
        {
            return obj is Email other && Equals(other);
        }

        public bool Equals(Email? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
        }

        public static bool operator ==(Email? left, Email? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(Email? left, Email? right)
        {
            return !(left == right);
        }

        public static implicit operator string(Email email) => email?.Value ?? string.Empty;
    }
}