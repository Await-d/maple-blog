using MapleBlog.Application.DTOs.Admin;

namespace MapleBlog.Application.Interfaces
{
    /// <summary>
    /// 用户管理服务接口
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// 获取用户管理概览
        /// </summary>
        /// <returns>用户管理概览数据</returns>
        Task<UserManagementOverviewDto> GetOverviewAsync();

        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="searchTerm">搜索关键词</param>
        /// <param name="status">用户状态</param>
        /// <param name="role">用户角色</param>
        /// <param name="sortBy">排序字段</param>
        /// <param name="sortDirection">排序方向</param>
        /// <returns>用户分页列表</returns>
        Task<PagedResultDto<UserManagementDto>> GetUsersAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? searchTerm = null,
            string? status = null,
            string? role = null,
            string sortBy = "CreatedAt",
            string sortDirection = "desc");

        /// <summary>
        /// 获取用户详细信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户详细信息</returns>
        Task<UserDetailDto?> GetUserDetailAsync(Guid userId);

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="createRequest">创建用户请求</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>创建结果</returns>
        Task<UserCreateResultDto> CreateUserAsync(CreateUserRequestDto createRequest, Guid operatorId);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="updateRequest">更新请求</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateUserAsync(Guid userId, UpdateUserRequestDto updateRequest, Guid operatorId);

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="softDelete">是否软删除</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteUserAsync(Guid userId, bool softDelete, Guid operatorId);

        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="userIds">用户ID列表</param>
        /// <param name="softDelete">是否软删除</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>批量删除结果</returns>
        Task<BatchOperationResultDto> BatchDeleteUsersAsync(IEnumerable<Guid> userIds, bool softDelete, Guid operatorId);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="newPassword">新密码</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>重置结果</returns>
        Task<bool> ResetUserPasswordAsync(Guid userId, string newPassword, Guid operatorId);

        /// <summary>
        /// 锁定用户账户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="lockReason">锁定原因</param>
        /// <param name="lockDuration">锁定时长（分钟）</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>锁定结果</returns>
        Task<bool> LockUserAccountAsync(Guid userId, string lockReason, int? lockDuration, Guid operatorId);

        /// <summary>
        /// 解锁用户账户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>解锁结果</returns>
        Task<bool> UnlockUserAccountAsync(Guid userId, Guid operatorId);

        /// <summary>
        /// 分配角色给用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>分配结果</returns>
        Task<bool> AssignRolesToUserAsync(Guid userId, IEnumerable<Guid> roleIds, Guid operatorId);

        /// <summary>
        /// 移除用户角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="roleIds">角色ID列表</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>移除结果</returns>
        Task<bool> RemoveRolesFromUserAsync(Guid userId, IEnumerable<Guid> roleIds, Guid operatorId);

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户角色列表</returns>
        Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(Guid userId);

        /// <summary>
        /// 获取用户权限列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户权限列表</returns>
        Task<IEnumerable<UserPermissionDto>> GetUserPermissionsAsync(Guid userId);

        /// <summary>
        /// 获取用户活动日志
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="actionType">操作类型</param>
        /// <returns>用户活动日志分页列表</returns>
        Task<PagedResultDto<UserActivityLogDto>> GetUserActivityLogAsync(
            Guid userId,
            int pageNumber = 1,
            int pageSize = 20,
            string? actionType = null);

        /// <summary>
        /// 获取用户登录历史
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>用户登录历史分页列表</returns>
        Task<PagedResultDto<UserLoginHistoryDto>> GetUserLoginHistoryAsync(Guid userId, int pageNumber = 1, int pageSize = 20);

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户统计信息</returns>
        Task<UserStatisticsDto> GetUserStatisticsAsync(Guid userId);

        /// <summary>
        /// 批量导入用户
        /// </summary>
        /// <param name="importRequest">导入请求参数</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>导入结果</returns>
        Task<UserImportResultDto> BatchImportUsersAsync(UserImportRequestDto importRequest, Guid operatorId);

        /// <summary>
        /// 批量导出用户
        /// </summary>
        /// <param name="exportRequest">导出请求参数</param>
        /// <returns>导出结果</returns>
        Task<UserExportResultDto> BatchExportUsersAsync(UserExportRequestDto exportRequest);

        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        /// <returns>在线用户列表</returns>
        Task<IEnumerable<OnlineUserDto>> GetOnlineUsersAsync();

        /// <summary>
        /// 强制用户下线
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>操作结果</returns>
        Task<bool> ForceUserOfflineAsync(Guid userId, Guid operatorId);

        /// <summary>
        /// 发送系统消息给用户
        /// </summary>
        /// <param name="userIds">用户ID列表</param>
        /// <param name="message">消息内容</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="senderId">发送人ID</param>
        /// <returns>发送结果</returns>
        Task<MessageSendResultDto> SendSystemMessageAsync(IEnumerable<Guid> userIds, string message, string messageType, Guid senderId);

        /// <summary>
        /// 获取用户行为分析
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <returns>用户行为分析数据</returns>
        Task<UserBehaviorAnalysisDto> GetUserBehaviorAnalysisAsync(Guid userId, DateTime startDate, DateTime endDate);
    }
}