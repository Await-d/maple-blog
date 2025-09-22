using System.Security.Claims;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// JWT服务接口
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// 生成访问令牌
        /// </summary>
        string GenerateAccessToken(User user);

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        string GenerateRefreshToken();

        /// <summary>
        /// 从令牌获取声明主体
        /// </summary>
        ClaimsPrincipal? GetPrincipalFromToken(string token);

        /// <summary>
        /// 验证令牌
        /// </summary>
        bool ValidateToken(string token);

        /// <summary>
        /// 验证令牌异步版本
        /// </summary>
        Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证令牌并获取声明主体
        /// </summary>
        Task<ClaimsPrincipal?> ValidateTokenAndGetPrincipalAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取令牌过期时间
        /// </summary>
        DateTime GetTokenExpiration(string token);

        /// <summary>
        /// 从令牌获取用户ID
        /// </summary>
        string? GetUserIdFromToken(string token);

        /// <summary>
        /// 从令牌获取用户名
        /// </summary>
        string? GetUserNameFromToken(string token);

        /// <summary>
        /// 从令牌获取角色
        /// </summary>
        string? GetRoleFromToken(string token);

        /// <summary>
        /// 检查令牌是否即将过期
        /// </summary>
        bool IsTokenExpiringSoon(string token, int minutesThreshold = 5);

        /// <summary>
        /// 撤销令牌
        /// </summary>
        Task RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查令牌是否已撤销
        /// </summary>
        Task<bool> IsTokenRevokedAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 生成令牌对（访问令牌和刷新令牌）
        /// </summary>
        Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新令牌
        /// </summary>
        Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将令牌加入黑名单
        /// </summary>
        Task BlacklistTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将用户的所有令牌加入黑名单
        /// </summary>
        Task BlacklistAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}