using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository : IRepository<Role>
    {
        /// <summary>
        /// 根据名称获取角色
        /// </summary>
        /// <param name="name">角色名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色实体</returns>
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色及其权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>角色实体（包含权限）</returns>
        Task<Role?> GetRoleWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有活跃角色
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>活跃角色列表</returns>
        Task<IEnumerable<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 清除角色的所有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加角色权限关联
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionId">权限ID</param>
        /// <param name="grantedBy">授予者ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> AddRolePermissionAsync(Guid roleId, Guid permissionId, Guid? grantedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加临时角色权限关联
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionId">权限ID</param>
        /// <param name="expiresAt">过期时间</param>
        /// <param name="grantedBy">授予者ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> AddTemporaryRolePermissionAsync(Guid roleId, Guid permissionId, DateTime expiresAt, Guid? grantedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 移除角色权限关联
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionId">权限ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查角色是否有权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissionId">权限ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否有权限</returns>
        Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="role">角色实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的角色实体</returns>
        Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取角色的用户数量
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>用户数量</returns>
        Task<int> GetUserCountAsync(Guid roleId, CancellationToken cancellationToken = default);
    }
}