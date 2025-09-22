using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Services;

namespace MapleBlog.API.Controllers
{
    /// <summary>
    /// 认证控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IPasswordResetService _passwordResetService;
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IEmailVerificationService emailVerificationService,
            IPasswordResetService passwordResetService,
            IUserProfileService userProfileService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _emailVerificationService = emailVerificationService;
            _passwordResetService = passwordResetService;
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        /// <param name="request">注册请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>注册结果</returns>
        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var result = await _authService.RegisterAsync(request, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("User registered successfully: {Email}", request.Email);
                    return CreatedAtAction(
                        nameof(GetProfile),
                        new { userId = result.User?.Id },
                        ApiResponse.CreateSuccess(result, "注册成功"));
                }

                return BadRequest(ApiResponse.Error(result.ErrorMessage ?? "注册失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Error("注册过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>登录结果</returns>
        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        [ProducesResponseType(typeof(ApiResponse<AuthResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var clientIp = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.LoginAsync(request, clientIp, userAgent, cancellationToken);

                if (result.Success)
                {
                    _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                    return Ok(ApiResponse.CreateSuccess(result, "登录成功"));
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Login attempt for locked account: {Email}", request.Email);
                    return StatusCode(423, ApiResponse.Error("账户已被锁定，请稍后再试"));
                }

                _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                return Unauthorized(ApiResponse.Error(result.ErrorMessage ?? "登录失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Error("登录过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        /// <param name="request">刷新令牌请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>新的访问令牌</returns>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<TokenResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var applicationRequest = new MapleBlog.Application.DTOs.RefreshTokenRequest
                {
                    RefreshToken = request.RefreshToken
                };
                var result = await _authService.RefreshTokenAsync(applicationRequest, cancellationToken);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(new TokenResult
                    {
                        AccessToken = result.AccessToken!,
                        RefreshToken = result.RefreshToken!,
                        ExpiresAt = result.ExpiresAt ?? DateTime.UtcNow.AddHours(1)
                    }));
                }

                return Unauthorized(ApiResponse.Error(result.ErrorMessage ?? "令牌刷新失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, ApiResponse.Error("令牌刷新过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 用户退出登录
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>退出结果</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var token = GetCurrentToken();

                await _authService.LogoutAsync(userId, token, cancellationToken);

                return Ok(ApiResponse.CreateSuccess("退出登录成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user logout");
                return StatusCode(500, ApiResponse.Error("退出登录过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        /// <param name="request">邮箱验证请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发送结果</returns>
        [HttpPost("send-email-verification")]
        [EnableRateLimiting("EmailPolicy")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SendEmailVerification([FromBody] EmailVerificationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var success = await _emailVerificationService.ResendEmailVerificationAsync(request.Email, cancellationToken);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess("验证邮件已发送"));
                }

                return BadRequest(ApiResponse.Error("发送验证邮件失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Error("发送验证邮件过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        /// <param name="request">邮箱确认请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证结果</returns>
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailConfirmationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var success = await _emailVerificationService.ConfirmEmailAsync(request.Email, request.Token, cancellationToken);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess("邮箱验证成功"));
                }

                return BadRequest(ApiResponse.Error("邮箱验证失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email for: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Error("邮箱验证过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 发送密码重置邮件
        /// </summary>
        /// <param name="request">密码重置请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>发送结果</returns>
        [HttpPost("forgot-password")]
        [EnableRateLimiting("EmailPolicy")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var success = await _passwordResetService.SendPasswordResetEmailAsync(request.Email, cancellationToken);

                // 为了安全，总是返回成功消息
                return Ok(ApiResponse.CreateSuccess("如果该邮箱存在，我们已向其发送密码重置链接"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email for: {Email}", request.Email);
                return Ok(ApiResponse.CreateSuccess("如果该邮箱存在，我们已向其发送密码重置链接"));
            }
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        /// <param name="request">重置密码请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重置结果</returns>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Error("请求数据无效", GetModelStateErrors()));
                }

                var success = await _passwordResetService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);

                if (success)
                {
                    return Ok(ApiResponse.CreateSuccess("密码重置成功"));
                }

                return BadRequest(ApiResponse.Error("密码重置失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", request.Email);
                return StatusCode(500, ApiResponse.Error("密码重置过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 获取当前用户资料
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户资料</returns>
        [HttpGet("profile")]
        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileData>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _userProfileService.GetUserProfileAsync(userId, cancellationToken);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Profile!));
                }

                return NotFound(ApiResponse.Error(result.ErrorMessage ?? "用户不存在"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, ApiResponse.Error("获取用户资料过程中发生内部错误"));
            }
        }

        /// <summary>
        /// 根据用户ID获取用户资料
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户资料</returns>
        [HttpGet("profile/{userId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileData>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _userProfileService.GetUserProfileAsync(userId, cancellationToken);

                if (result.Success)
                {
                    return Ok(ApiResponse.CreateSuccess(result.Profile!));
                }

                return NotFound(ApiResponse.Error(result.ErrorMessage ?? "用户不存在"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for user {UserId}", userId);
                return StatusCode(500, ApiResponse.Error("获取用户资料过程中发生内部错误"));
            }
        }

        #region Helper Methods

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        /// <returns>用户ID</returns>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("无效的用户身份");
            }
            return userId;
        }

        /// <summary>
        /// 获取当前JWT令牌
        /// </summary>
        /// <returns>JWT令牌</returns>
        private string? GetCurrentToken()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                return authHeader["Bearer ".Length..];
            }
            return null;
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        /// <returns>IP地址</returns>
        private string GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// 获取模型状态错误
        /// </summary>
        /// <returns>错误字典</returns>
        private Dictionary<string, string[]> GetModelStateErrors()
        {
            return ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
        }

        #endregion
    }

    #region Request/Response Models

    /// <summary>
    /// 邮箱验证请求
    /// </summary>
    public class EmailVerificationRequest
    {
        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 邮箱确认请求
    /// </summary>
    public class EmailConfirmationRequest
    {
        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "验证令牌是必需的")]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// 忘记密码请求
    /// </summary>
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// 重置密码请求
    /// </summary>
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "邮箱地址是必需的")]
        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "重置令牌是必需的")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密码是必需的")]
        [MinLength(8, ErrorMessage = "密码长度至少为8位")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "密码必须包含大写字母、小写字母、数字和特殊字符")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "确认密码是必需的")]
        [Compare("NewPassword", ErrorMessage = "确认密码与新密码不匹配")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 刷新令牌请求
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "刷新令牌是必需的")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 令牌结果
    /// </summary>
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }


    #endregion
}