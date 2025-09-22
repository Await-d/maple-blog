using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 邮箱验证令牌仓储接口
    /// </summary>
    public interface IEmailVerificationTokenRepository : IRepository<EmailVerificationToken>
    {
        /// <summary>
        /// 根据令牌获取验证记录
        /// </summary>
        /// <param name="token">验证令牌</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证令牌实体</returns>
        Task<EmailVerificationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据令牌和类型获取验证记录
        /// </summary>
        /// <param name="token">验证令牌</param>
        /// <param name="tokenType">令牌类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>验证令牌实体</returns>
        Task<EmailVerificationToken?> GetByTokenAndTypeAsync(string token, EmailTokenType tokenType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和令牌类型获取未使用的令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="tokenType">令牌类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>未使用的令牌列表</returns>
        Task<IEnumerable<EmailVerificationToken>> GetUnusedTokensByUserAndTypeAsync(Guid userId, EmailTokenType tokenType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据邮箱和令牌类型获取最近的令牌
        /// </summary>
        /// <param name="email">邮箱地址</param>
        /// <param name="tokenType">令牌类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最近的令牌</returns>
        Task<EmailVerificationToken?> GetLatestTokenByEmailAndTypeAsync(string email, EmailTokenType tokenType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除过期的令牌
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的令牌数量</returns>
        Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除已使用的令牌
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的令牌数量</returns>
        Task<int> DeleteUsedTokensAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和令牌类型删除令牌
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="tokenType">令牌类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的令牌数量</returns>
        Task<int> DeleteTokensByUserAndTypeAsync(Guid userId, EmailTokenType tokenType, CancellationToken cancellationToken = default);
    }
}