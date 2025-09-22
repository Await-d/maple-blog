namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 权限服务接口
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="permission">权限名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有资源操作权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        Task<bool> HasResourcePermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户的所有权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限集合</returns>
        Task<ISet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的所有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限集合</returns>
        Task<ISet<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionIds">权限ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="expiresAt">过期时间（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, DateTime? expiresAt = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 从用户移除角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查用户是否有指定作用域的资源操作权限
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="requiredScope">所需的权限作用域</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        Task<bool> HasResourcePermissionWithScopeAsync(Guid userId, string resource, string action, Domain.Enums.PermissionScope requiredScope, CancellationToken cancellationToken = default);

        /// <summary>
        /// 初始化默认权限和角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> InitializeDefaultPermissionsAsync(CancellationToken cancellationToken = default);
    }
}