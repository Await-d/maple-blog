using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 用户角色仓储接口
    /// </summary>
    public interface IUserRoleRepository
    {
        /// <summary>
        /// 获取用户角色关联
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户角色关联</returns>
        Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户角色列表</returns>
        Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有有效角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>有效用户角色列表</returns>
        Task<IEnumerable<UserRole>> GetActiveUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的所有用户
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色用户列表</returns>
        Task<IEnumerable<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的所有有效用户
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>有效角色用户列表</returns>
        Task<IEnumerable<UserRole>> GetActiveRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有指定角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有角色</returns>
        Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有任意指定角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有任意角色</returns>
        Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加用户角色
        /// </summary>
        /// <param name="userRole">用户角色关联</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户角色关联</returns>
        Task<UserRole> AddAsync(UserRole userRole, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除用户角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除用户的所有角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> RemoveAllUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的活跃用户数量
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户数量</returns>
        Task<int> GetActiveUserCountByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取即将过期的角色
        /// </summary>
        /// <param name="beforeDate">过期时间界限</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>即将过期的角色列表</returns>
        Task<IEnumerable<UserRole>> GetExpiringRolesAsync(DateTime beforeDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理过期的角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> CleanupExpiredRolesAsync(CancellationToken cancellationToken = default);
    }
}