using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// 权限仓储接口
    /// </summary>
    public interface IPermissionRepository : IRepository<Permission>
    {
        /// <summary>
        /// 根据名称获取权限
        /// </summary>
        /// <param name="name">权限名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限实体</returns>
        Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据资源和操作获取权限
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限实体</returns>
        Task<Permission?> GetByResourceActionAsync(string resource, string action, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据资源、操作和作用域获取权限
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="scope">权限作用域</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限实体</returns>
        Task<Permission?> GetByResourceActionScopeAsync(string resource, string action, PermissionScope scope, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定资源和操作的所有作用域权限
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="action">操作名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetPermissionsWithScopeAsync(string resource, string action, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有权限
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据资源获取权限列表
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>权限列表</returns>
        Task<IEnumerable<Permission>> GetPermissionsByResourceAsync(string resource, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取系统权限列表
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>系统权限列表</returns>
        Task<IEnumerable<Permission>> GetSystemPermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查权限是否存在
        /// </summary>
        /// <param name="name">权限名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建权限
        /// </summary>
        /// <param name="permission">权限实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的权限实体</returns>
        Task<Permission> CreateAsync(Permission permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量创建权限
        /// </summary>
        /// <param name="permissions">权限列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        Task<bool> CreateBatchAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default);
    }
}