using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 通知仓储接口
    /// </summary>
    public interface INotificationRepository
    {
        /// <summary>
        /// 根据ID获取通知
        /// </summary>
        Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户通知
        /// </summary>
        Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取未读通知
        /// </summary>
        Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建通知
        /// </summary>
        Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量创建通知
        /// </summary>
        Task<IEnumerable<Notification>> CreateManyAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量添加通知
        /// </summary>
        Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新通知
        /// </summary>
        Task<Notification> UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除通知
        /// </summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 标记为已读
        /// </summary>
        Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量标记为已读
        /// </summary>
        Task MarkManyAsReadAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

        /// <summary>
        /// 标记用户所有通知为已读
        /// </summary>
        Task MarkAllAsReadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取未读通知数量
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 清理过期通知
        /// </summary>
        Task<int> CleanupOldNotificationsAsync(int retentionDays = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取查询接口
        /// </summary>
        IQueryable<Notification> GetQueryable();

        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}