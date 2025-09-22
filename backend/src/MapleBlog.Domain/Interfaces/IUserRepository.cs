using MapleBlog.Domain.Entities;

namespace MapleBlog.Domain.Interfaces;

/// <summary>
/// 用户仓储接口
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>用户实体</returns>
    Task<User?> GetByUserNameAsync(string userName);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>用户实体</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <returns>是否存在</returns>
    Task<bool> UserNameExistsAsync(string userName);

    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>是否存在</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// 获取用户及其角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户实体（包含角色）</returns>
    Task<User?> GetUserWithRolesAsync(Guid userId);

    /// <summary>
    /// 根据角色获取用户列表
    /// </summary>
    /// <param name="roleName">角色名称</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>用户列表</returns>
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersByRoleAsync(string roleName, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 根据角色ID获取用户列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户列表</returns>
    Task<IEnumerable<User>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃用户
    /// </summary>
    /// <param name="days">天数</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页面大小</param>
    /// <returns>活跃用户列表</returns>
    Task<(IEnumerable<User> Users, int TotalCount)> GetActiveUsersAsync(int days = 30, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// 根据用户名查找用户
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱查找用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据密码重置令牌查找用户
    /// </summary>
    /// <param name="token">密码重置令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<User?> FindByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱验证令牌查找用户
    /// </summary>
    /// <param name="token">邮箱验证令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户实体</returns>
    Task<User?> FindByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否在使用中
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在使用</returns>
    Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查邮箱是否在使用中（排除指定用户）
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在使用</returns>
    Task<bool> IsEmailInUseAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否在使用中
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在使用</returns>
    Task<bool> IsUserNameInUseAsync(string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户名是否在使用中（排除指定用户）
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在使用</returns>
    Task<bool> IsUserNameInUseAsync(string userName, Guid? excludeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色列表</returns>
    Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色</returns>
    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完成任务</returns>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计作者数量
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>作者数量</returns>
    Task<int> CountAuthorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取活跃作者列表
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>活跃作者列表</returns>
    Task<IEnumerable<User>> GetActiveAuthorsAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}