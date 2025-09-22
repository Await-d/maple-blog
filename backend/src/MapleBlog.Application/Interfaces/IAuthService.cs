using MapleBlog.Application.DTOs;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="request">User registration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication result with user information and tokens</returns>
        Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates a user with email/username and password
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication result with tokens if successful</returns>
        Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates a user with email/username and password with tracking info
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <param name="clientIp">Client IP address</param>
        /// <param name="userAgent">User agent string</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication result with tokens if successful</returns>
        Task<AuthResult> LoginAsync(LoginRequest request, string clientIp, string userAgent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes authentication tokens
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New authentication tokens</returns>
        Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs out a user by invalidating their tokens
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="tokenId">Token ID to invalidate (optional, if null invalidates all user tokens)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> LogoutAsync(Guid userId, string? tokenId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates password reset process by sending reset email
        /// </summary>
        /// <param name="request">Password reset request with email</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets password using reset token
        /// </summary>
        /// <param name="request">Password reset confirmation with token and new password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> ResetPasswordAsync(PasswordResetConfirmRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes user password (requires current password)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Password change request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies user's email address using verification token
        /// </summary>
        /// <param name="request">Email verification request with token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> VerifyEmailAsync(EmailVerificationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resends email verification
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes user's email address
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Email change request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<OperationResult> ChangeEmailAsync(Guid userId, ChangeEmailRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates user profile information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="request">Profile update request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result with updated user data</returns>
        Task<OperationResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user information by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User information</returns>
        Task<OperationResult<UserDto>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if email is available for registration
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <param name="excludeUserId">User ID to exclude from check (for email updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if email is available, false otherwise</returns>
        Task<bool> IsEmailAvailableAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if username is available for registration
        /// </summary>
        /// <param name="userName">Username to check</param>
        /// <param name="excludeUserId">User ID to exclude from check (for username updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if username is available, false otherwise</returns>
        Task<bool> IsUserNameAvailableAsync(string userName, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    }
}