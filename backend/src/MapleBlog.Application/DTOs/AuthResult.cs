namespace MapleBlog.Application.DTOs
{
    /// <summary>
    /// Data transfer object for authentication result
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Whether the authentication was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Authentication error message (if any)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// List of authentication errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// JWT access token
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// JWT refresh token
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token type (usually "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Authenticated user information
        /// </summary>
        public UserDto? User { get; set; }

        /// <summary>
        /// Whether email verification is required
        /// </summary>
        public bool RequiresEmailVerification { get; set; }

        /// <summary>
        /// Whether two-factor authentication is required
        /// </summary>
        public bool RequiresTwoFactor { get; set; }

        /// <summary>
        /// Whether the account is locked out
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// Lockout end time (if applicable)
        /// </summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// Creates a successful authentication result
        /// </summary>
        /// <param name="user">Authenticated user</param>
        /// <param name="accessToken">JWT access token</param>
        /// <param name="refreshToken">JWT refresh token</param>
        /// <param name="expiresAt">Token expiration time</param>
        /// <returns>Successful authentication result</returns>
        public static AuthResult CreateSuccess(UserDto user, string accessToken, string refreshToken, DateTime expiresAt)
        {
            return new AuthResult
            {
                Success = true,
                User = user,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                TokenType = "Bearer"
            };
        }

        /// <summary>
        /// Creates a failed authentication result with a single error message
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed authentication result</returns>
        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Errors = { errorMessage }
            };
        }

        /// <summary>
        /// Creates a failed authentication result with multiple errors
        /// </summary>
        /// <param name="errors">List of error messages</param>
        /// <returns>Failed authentication result</returns>
        public static AuthResult Failure(IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorList.FirstOrDefault(),
                Errors = errorList
            };
        }

        /// <summary>
        /// Creates a failed authentication result for email verification required
        /// </summary>
        /// <param name="user">User requiring email verification</param>
        /// <returns>Failed authentication result with email verification flag</returns>
        public static AuthResult CreateRequiresEmailVerification(UserDto user)
        {
            return new AuthResult
            {
                Success = false,
                User = user,
                RequiresEmailVerification = true,
                ErrorMessage = "Email verification is required before you can sign in."
            };
        }

        /// <summary>
        /// Creates a failed authentication result for locked out account
        /// </summary>
        /// <param name="lockoutEnd">When the lockout ends</param>
        /// <returns>Failed authentication result with lockout information</returns>
        public static AuthResult LockedOut(DateTime? lockoutEnd = null)
        {
            var message = lockoutEnd.HasValue
                ? $"Account is locked until {lockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC."
                : "Account is locked due to multiple failed login attempts.";

            return new AuthResult
            {
                Success = false,
                IsLockedOut = true,
                LockoutEnd = lockoutEnd,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// Creates a failed authentication result for two-factor authentication required
        /// </summary>
        /// <param name="user">User requiring two-factor authentication</param>
        /// <returns>Failed authentication result with two-factor flag</returns>
        public static AuthResult CreateRequiresTwoFactor(UserDto user)
        {
            return new AuthResult
            {
                Success = false,
                User = user,
                RequiresTwoFactor = true,
                ErrorMessage = "Two-factor authentication is required."
            };
        }
    }

    /// <summary>
    /// Data transfer object for token refresh result
    /// </summary>
    public class TokenRefreshResult
    {
        /// <summary>
        /// Whether the token refresh was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message (if any)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// New JWT access token
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// New JWT refresh token
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token type (usually "Bearer")
        /// </summary>
        public string TokenType { get; set; } = "Bearer";

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Creates a successful token refresh result
        /// </summary>
        /// <param name="accessToken">New access token</param>
        /// <param name="refreshToken">New refresh token</param>
        /// <param name="expiresAt">Token expiration time</param>
        /// <returns>Successful token refresh result</returns>
        public static TokenRefreshResult CreateSuccess(string accessToken, string refreshToken, DateTime expiresAt)
        {
            return new TokenRefreshResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                TokenType = "Bearer"
            };
        }

        /// <summary>
        /// Creates a failed token refresh result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed token refresh result</returns>
        public static TokenRefreshResult Failure(string errorMessage)
        {
            return new TokenRefreshResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Data transfer object for operation result
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Operation message (success or error)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// List of errors (if any)
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Creates a successful operation result
        /// </summary>
        /// <param name="message">Success message</param>
        /// <returns>Successful operation result</returns>
        public static OperationResult CreateSuccess(string? message = null)
        {
            return new OperationResult
            {
                Success = true,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed operation result with a single error
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed operation result</returns>
        public static OperationResult Failure(string errorMessage)
        {
            return new OperationResult
            {
                Success = false,
                Message = errorMessage,
                Errors = { errorMessage }
            };
        }

        /// <summary>
        /// Creates a failed operation result with multiple errors
        /// </summary>
        /// <param name="errors">List of error messages</param>
        /// <returns>Failed operation result</returns>
        public static OperationResult Failure(IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            return new OperationResult
            {
                Success = false,
                Message = errorList.FirstOrDefault(),
                Errors = errorList
            };
        }
    }

    /// <summary>
    /// Generic data transfer object for operation result with data
    /// </summary>
    /// <typeparam name="T">Type of data returned</typeparam>
    public class OperationResult<T> : OperationResult
    {
        /// <summary>
        /// Result data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Creates a successful operation result with data
        /// </summary>
        /// <param name="data">Result data</param>
        /// <param name="message">Success message</param>
        /// <returns>Successful operation result</returns>
        public static OperationResult<T> CreateSuccess(T data, string? message = null)
        {
            return new OperationResult<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        /// <summary>
        /// Creates a failed operation result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Failed operation result</returns>
        public new static OperationResult<T> Failure(string errorMessage)
        {
            return new OperationResult<T>
            {
                Success = false,
                Message = errorMessage,
                Errors = { errorMessage }
            };
        }

        /// <summary>
        /// Creates a failed operation result with multiple errors
        /// </summary>
        /// <param name="errors">List of error messages</param>
        /// <returns>Failed operation result</returns>
        public new static OperationResult<T> Failure(IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            return new OperationResult<T>
            {
                Success = false,
                Message = errorList.FirstOrDefault(),
                Errors = errorList
            };
        }
    }
}