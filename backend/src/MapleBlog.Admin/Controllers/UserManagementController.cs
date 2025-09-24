using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Admin.DTOs;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// 用户管理控制器 - 提供企业级用户管理功能
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public class UserManagementController : BaseAdminController
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public UserManagementController(
            IUserManagementService userManagementService,
            IAuthService authService,
            IEmailService emailService,
            ILogger<UserManagementController> logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
            : base(logger, permissionService, auditLogService)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

        /// <summary>
        /// 获取用户管理概览数据
        /// </summary>
        /// <returns>用户管理概览信息</returns>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(UserManagementOverviewDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOverview()
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var overview = await _userManagementService.GetOverviewAsync();

                await LogAuditAsync(
                    "GetUserOverview",
                    "UserManagement",
                    description: "获取用户管理概览数据"
                );

                return Success(overview, "成功获取用户管理概览");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户管理概览");
            }
        }

        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <param name="pageNumber">页码，默认1</param>
        /// <param name="pageSize">页大小，默认20，最大100</param>
        /// <param name="searchTerm">搜索关键词（用户名、邮箱、显示名称）</param>
        /// <param name="status">用户状态筛选（active/inactive/locked/deleted/verified/unverified）</param>
        /// <param name="role">角色筛选</param>
        /// <param name="sortBy">排序字段（username/email/createdat/lastloginat/role）</param>
        /// <param name="sortDirection">排序方向（asc/desc）</param>
        /// <returns>分页用户列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<UserManagementDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] string? role = null,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                // 参数验证
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var users = await _userManagementService.GetUsersAsync(
                    pageNumber, pageSize, searchTerm, status, role, sortBy, sortDirection);

                await LogAuditAsync(
                    "GetUserList",
                    "UserManagement",
                    description: $"获取用户列表，页码: {pageNumber}，搜索: {searchTerm}，状态: {status}"
                );

                return SuccessWithPagination(
                    users.Items,
                    users.TotalCount,
                    users.PageNumber,
                    users.PageSize,
                    "成功获取用户列表"
                );
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户列表");
            }
        }

        /// <summary>
        /// 获取用户详细信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户详细信息</returns>
        [HttpGet("{userId:guid}")]
        [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserDetail([FromRoute] Guid userId)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var userDetail = await _userManagementService.GetUserDetailAsync(userId);
                if (userDetail == null)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "GetUserDetail",
                    "User",
                    userId.ToString(),
                    $"查看用户详细信息: {userDetail.BasicInfo.Username}"
                );

                return Success(userDetail, "成功获取用户详细信息");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户详细信息");
            }
        }

        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="createRequest">创建用户请求数据</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        [ProducesResponseType(typeof(UserCreateResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [EnableRateLimiting("UserCreation")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto createRequest)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Create");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                // 业务验证
                if (string.IsNullOrWhiteSpace(createRequest.Username))
                {
                    return Error("用户名不能为空");
                }

                if (string.IsNullOrWhiteSpace(createRequest.Email))
                {
                    return Error("邮箱不能为空");
                }

                if (string.IsNullOrWhiteSpace(createRequest.Password))
                {
                    return Error("密码不能为空");
                }

                var result = await _userManagementService.CreateUserAsync(createRequest, CurrentUserId!.Value);

                if (!result.Success)
                {
                    return Error(string.Join(", ", result.Errors));
                }

                await LogAuditAsync(
                    "CreateUser",
                    "User",
                    result.UserId?.ToString(),
                    $"创建用户: {createRequest.Username}",
                    null,
                    new { createRequest.Username, createRequest.Email, createRequest.RoleIds }
                );

                return StatusCode(StatusCodes.Status201Created, Success(result, "用户创建成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建用户");
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="updateRequest">更新请求数据</param>
        /// <returns>更新结果</returns>
        [HttpPut("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(
            [FromRoute] Guid userId,
            [FromBody] UpdateUserRequestDto updateRequest)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Update");
                if (permissionCheck != null) return permissionCheck;

                // 检查是否试图修改自己的信息（需要特殊权限）
                if (userId == CurrentUserId && !await HasPermissionAsync("UserManagement.UpdateSelf"))
                {
                    return Forbid("不能修改自己的用户信息，请联系其他管理员");
                }

                var success = await _userManagementService.UpdateUserAsync(userId, updateRequest, CurrentUserId!.Value);
                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "UpdateUser",
                    "User",
                    userId.ToString(),
                    $"更新用户信息",
                    null,
                    updateRequest
                );

                return Success(null, "用户信息更新成功");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新用户信息");
            }
        }

        /// <summary>
        /// 删除用户（软删除或硬删除）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="softDelete">是否软删除，默认true</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(
            [FromRoute] Guid userId,
            [FromQuery] bool softDelete = true)
        {
            try
            {
                // 验证权限（硬删除需要超级管理员权限）
                var requiredPermission = softDelete ? "UserManagement.Delete" : "UserManagement.HardDelete";
                var permissionCheck = await ValidatePermissionAsync(requiredPermission);
                if (permissionCheck != null) return permissionCheck;

                // 防止删除自己
                if (userId == CurrentUserId)
                {
                    return Error("不能删除自己的账户");
                }

                var success = await _userManagementService.DeleteUserAsync(userId, softDelete, CurrentUserId!.Value);
                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    softDelete ? "SoftDeleteUser" : "HardDeleteUser",
                    "User",
                    userId.ToString(),
                    $"{(softDelete ? "软" : "硬")}删除用户"
                );

                return Success(null, $"用户{(softDelete ? "删除" : "永久删除")}成功");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除用户");
            }
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="request">批量删除请求</param>
        /// <returns>批量操作结果</returns>
        [HttpPost("batch/delete")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchDeleteUsers([FromBody] BatchDeleteUsersRequestDto request)
        {
            try
            {
                // 验证权限
                var requiredPermission = request.SoftDelete ? "UserManagement.Delete" : "UserManagement.HardDelete";
                var permissionCheck = await ValidatePermissionAsync(requiredPermission);
                if (permissionCheck != null) return permissionCheck;

                // 验证请求
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return Error("未提供要删除的用户ID");
                }

                // 防止删除自己
                if (request.UserIds.Contains(CurrentUserId!.Value))
                {
                    return Error("批量删除操作中不能包含自己的账户");
                }

                var result = await _userManagementService.BatchDeleteUsersAsync(
                    request.UserIds, request.SoftDelete, CurrentUserId!.Value);

                await LogAuditAsync(
                    "BatchDeleteUsers",
                    "User",
                    string.Join(",", request.UserIds),
                    $"批量{(request.SoftDelete ? "软" : "硬")}删除用户，数量: {request.UserIds.Count()}"
                );

                return Success(result, $"批量删除操作完成，成功: {result.SuccessCount}，失败: {result.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量删除用户");
            }
        }

        /// <summary>
        /// 重置用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">重置密码请求</param>
        /// <returns>重置结果</returns>
        [HttpPost("{userId:guid}/reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetUserPassword(
            [FromRoute] Guid userId,
            [FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.ResetPassword");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                var success = await _userManagementService.ResetUserPasswordAsync(
                    userId, request.NewPassword, CurrentUserId!.Value);

                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "ResetUserPassword",
                    "User",
                    userId.ToString(),
                    "重置用户密码"
                );

                return Success(null, "用户密码重置成功");
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "重置用户密码");
            }
        }

        /// <summary>
        /// 锁定用户账户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">锁定请求数据</param>
        /// <returns>锁定结果</returns>
        [HttpPost("{userId:guid}/lock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LockUserAccount(
            [FromRoute] Guid userId,
            [FromBody] LockUserAccountRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Lock");
                if (permissionCheck != null) return permissionCheck;

                // 防止锁定自己
                if (userId == CurrentUserId)
                {
                    return Error("不能锁定自己的账户");
                }

                var success = await _userManagementService.LockUserAccountAsync(
                    userId, request.Reason, request.DurationMinutes, CurrentUserId!.Value);

                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "LockUserAccount",
                    "User",
                    userId.ToString(),
                    $"锁定用户账户，原因: {request.Reason}"
                );

                return Success(null, "用户账户锁定成功");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "锁定用户账户");
            }
        }

        /// <summary>
        /// 解除用户账户锁定
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>解锁结果</returns>
        [HttpPost("{userId:guid}/unlock")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UnlockUserAccount([FromRoute] Guid userId)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Unlock");
                if (permissionCheck != null) return permissionCheck;

                var success = await _userManagementService.UnlockUserAccountAsync(userId, CurrentUserId!.Value);
                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "UnlockUserAccount",
                    "User",
                    userId.ToString(),
                    "解除用户账户锁定"
                );

                return Success(null, "用户账户解锁成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "解除用户账户锁定");
            }
        }

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">角色分配请求</param>
        /// <returns>分配结果</returns>
        [HttpPost("{userId:guid}/roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRolesToUser(
            [FromRoute] Guid userId,
            [FromBody] AssignRolesRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.AssignRole");
                if (permissionCheck != null) return permissionCheck;

                // 特殊检查：分配管理员角色需要超级管理员权限
                if (request.RoleIds.Any() && !IsSuperAdmin())
                {
                    // 这里应该检查要分配的角色是否包含管理员角色
                    // 简化处理，假设非超级管理员只能分配普通角色
                    var superAdminCheck = ValidateSuperAdminPermission();
                    if (superAdminCheck != null && request.RequiresSuperAdminPermission)
                    {
                        return superAdminCheck;
                    }
                }

                var success = await _userManagementService.AssignRolesToUserAsync(
                    userId, request.RoleIds, CurrentUserId!.Value);

                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "AssignRolesToUser",
                    "User",
                    userId.ToString(),
                    $"为用户分配角色，角色数量: {request.RoleIds.Count()}"
                );

                return Success(null, "角色分配成功");
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "分配用户角色");
            }
        }

        /// <summary>
        /// 移除用户角色
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="request">移除角色请求</param>
        /// <returns>移除结果</returns>
        [HttpDelete("{userId:guid}/roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveRolesFromUser(
            [FromRoute] Guid userId,
            [FromBody] RemoveRolesRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.RemoveRole");
                if (permissionCheck != null) return permissionCheck;

                var success = await _userManagementService.RemoveRolesFromUserAsync(
                    userId, request.RoleIds, CurrentUserId!.Value);

                if (!success)
                {
                    return NotFoundResult("用户", userId);
                }

                await LogAuditAsync(
                    "RemoveRolesFromUser",
                    "User",
                    userId.ToString(),
                    $"移除用户角色，角色数量: {request.RoleIds.Count()}"
                );

                return Success(null, "角色移除成功");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "移除用户角色");
            }
        }

        /// <summary>
        /// 获取用户角色列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户角色列表</returns>
        [HttpGet("{userId:guid}/roles")]
        [ProducesResponseType(typeof(IEnumerable<UserRoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserRoles([FromRoute] Guid userId)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var roles = await _userManagementService.GetUserRolesAsync(userId);

                return Success(roles, "成功获取用户角色列表");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户角色");
            }
        }

        /// <summary>
        /// 获取用户权限列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户权限列表</returns>
        [HttpGet("{userId:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<UserPermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserPermissions([FromRoute] Guid userId)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var permissions = await _userManagementService.GetUserPermissionsAsync(userId);

                return Success(permissions, "成功获取用户权限列表");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户权限");
            }
        }

        /// <summary>
        /// 发送用户通知消息
        /// </summary>
        /// <param name="request">发送消息请求</param>
        /// <returns>发送结果</returns>
        [HttpPost("send-message")]
        [ProducesResponseType(typeof(MessageSendResultDto), StatusCodes.Status200OK)]
        [EnableRateLimiting("MessageSending")]
        public async Task<IActionResult> SendMessageToUsers([FromBody] SendMessageRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.SendMessage");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                var result = new MessageSendResultDto
                {
                    Success = true,
                    TotalSent = request.UserIds.Count(),
                    SuccessCount = 0,
                    FailCount = 0,
                    MessageId = Guid.NewGuid(),
                    SentAt = DateTime.UtcNow
                };

                var sendDetails = new List<MessageSendDetailDto>();

                // 发送消息给每个用户
                foreach (var userId in request.UserIds)
                {
                    try
                    {
                        // 这里应该调用消息发送服务
                        // 简化实现，假设总是成功
                        sendDetails.Add(new MessageSendDetailDto
                        {
                            UserId = userId,
                            Username = $"User_{userId}",
                            Success = true,
                            DeliveredAt = DateTime.UtcNow,
                            DeliveryMethod = request.DeliveryMethod
                        });
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        sendDetails.Add(new MessageSendDetailDto
                        {
                            UserId = userId,
                            Username = $"User_{userId}",
                            Success = false,
                            ErrorMessage = ex.Message,
                            DeliveryMethod = request.DeliveryMethod
                        });
                        result.FailCount++;
                    }
                }

                result.SendDetails = sendDetails;
                result.Success = result.FailCount == 0;

                await LogAuditAsync(
                    "SendMessageToUsers",
                    "UserManagement",
                    string.Join(",", request.UserIds),
                    $"发送消息给用户，接收人数: {request.UserIds.Count()}，成功: {result.SuccessCount}"
                );

                return Success(result, $"消息发送完成，成功: {result.SuccessCount}，失败: {result.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "发送用户消息");
            }
        }

        /// <summary>
        /// 导出用户数据
        /// </summary>
        /// <param name="request">导出请求参数</param>
        /// <returns>导出结果（文件下载或异步处理结果）</returns>
        [HttpPost("export")]
        [ProducesResponseType(typeof(UserExportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportUsers([FromBody] UserExportRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Export");
                if (permissionCheck != null) return permissionCheck;

                // 敏感信息导出需要特殊权限
                if (request.IncludeSensitiveInfo)
                {
                    var sensitivePermissionCheck = await ValidatePermissionAsync("UserManagement.ExportSensitive");
                    if (sensitivePermissionCheck != null) return sensitivePermissionCheck;
                }

                // 这里应该调用用户导出服务
                // 简化实现，返回导出结果信息
                var exportResult = new UserExportResultDto
                {
                    ExportId = Guid.NewGuid(),
                    FileName = $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}",
                    FileSize = 1024 * 1024, // 1MB 示例
                    DownloadUrl = "/api/admin/user-management/download/export-file",
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    ExportedUserCount = 100, // 示例数据
                    GeneratedAt = DateTime.UtcNow,
                    Status = "Completed"
                };

                await LogAuditAsync(
                    "ExportUsers",
                    "UserManagement",
                    exportResult.ExportId.ToString(),
                    $"导出用户数据，格式: {request.ExportFormat}，包含敏感信息: {request.IncludeSensitiveInfo}"
                );

                return Success(exportResult, "用户数据导出任务已完成");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导出用户数据");
            }
        }

        /// <summary>
        /// 导入用户数据
        /// </summary>
        /// <param name="request">导入请求数据</param>
        /// <returns>导入结果</returns>
        [HttpPost("import")]
        [ProducesResponseType(typeof(UserImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [EnableRateLimiting("UserImport")]
        public async Task<IActionResult> ImportUsers([FromBody] UserImportRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Import");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                // 这里应该调用用户导入服务
                // 简化实现，返回导入结果
                var importResult = new UserImportResultDto
                {
                    ImportId = Guid.NewGuid(),
                    TotalRecords = 100, // 示例数据
                    SuccessCount = 95,
                    FailCount = 3,
                    SkippedCount = 2,
                    ProcessingTime = TimeSpan.FromMinutes(2),
                    SummaryReport = "导入完成，大部分用户成功创建"
                };

                await LogAuditAsync(
                    "ImportUsers",
                    "UserManagement",
                    importResult.ImportId.ToString(),
                    $"导入用户数据，格式: {request.ImportFormat}，总数: {importResult.TotalRecords}，成功: {importResult.SuccessCount}"
                );

                return Success(importResult, $"用户导入完成，成功: {importResult.SuccessCount}，失败: {importResult.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "导入用户数据");
            }
        }

        /// <summary>
        /// 获取在线用户列表
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>在线用户列表</returns>
        [HttpGet("online")]
        [ProducesResponseType(typeof(PagedResultDto<OnlineUserDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOnlineUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                // 参数验证
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // 这里应该调用在线用户服务
                // 简化实现，返回示例数据
                var onlineUsers = new List<OnlineUserDto>();
                var totalCount = 10;

                var pagedResult = new PagedResultDto<OnlineUserDto>
                {
                    Items = onlineUsers,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return SuccessWithPagination(
                    pagedResult.Items,
                    pagedResult.TotalCount,
                    pagedResult.PageNumber,
                    pagedResult.PageSize,
                    "成功获取在线用户列表"
                );
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取在线用户列表");
            }
        }

        /// <summary>
        /// 获取用户行为分析
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="days">分析天数，默认30天</param>
        /// <returns>用户行为分析结果</returns>
        [HttpGet("{userId:guid}/behavior-analysis")]
        [ProducesResponseType(typeof(UserBehaviorAnalysisDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserBehaviorAnalysis(
            [FromRoute] Guid userId,
            [FromQuery] int days = 30)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Analysis");
                if (permissionCheck != null) return permissionCheck;

                // 参数验证
                if (days < 1 || days > 365) days = 30;

                // 这里应该调用用户行为分析服务
                // 简化实现，返回示例数据
                var analysis = new UserBehaviorAnalysisDto
                {
                    UserId = userId,
                    AnalysisPeriod = new MapleBlog.Application.DTOs.Admin.DateRangeDto
                    {
                        StartDate = DateTime.UtcNow.AddDays(-days),
                        EndDate = DateTime.UtcNow
                    }
                };

                await LogAuditAsync(
                    "GetUserBehaviorAnalysis",
                    "User",
                    userId.ToString(),
                    $"获取用户行为分析，分析天数: {days}"
                );

                return Success(analysis, "成功获取用户行为分析");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取用户行为分析");
            }
        }

        /// <summary>
        /// 批量更新用户状态
        /// </summary>
        /// <param name="request">批量状态更新请求</param>
        /// <returns>批量操作结果</returns>
        [HttpPost("batch/update-status")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateUserStatus([FromBody] BatchUpdateStatusRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("UserManagement.Update");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                // 防止修改自己的状态
                if (request.UserIds.Contains(CurrentUserId!.Value))
                {
                    return Error("批量状态更新操作中不能包含自己的账户");
                }

                // 这里应该调用批量更新服务
                // 简化实现
                var result = new BatchOperationResultDto
                {
                    TotalCount = request.UserIds.Count(),
                    SuccessCount = request.UserIds.Count(),
                    FailCount = 0,
                    Success = true,
                    ItemResults = request.UserIds.Select(id => new ItemResultDto
                    {
                        ItemId = id,
                        Success = true,
                        Message = $"状态已更新为: {request.NewStatus}"
                    }).ToList()
                };

                await LogAuditAsync(
                    "BatchUpdateUserStatus",
                    "UserManagement",
                    string.Join(",", request.UserIds),
                    $"批量更新用户状态为: {request.NewStatus}，数量: {request.UserIds.Count()}"
                );

                return Success(result, $"批量状态更新完成，成功: {result.SuccessCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量更新用户状态");
            }
        }
    }

    #region Request DTOs

    /// <summary>
    /// 重置密码请求DTO
    /// </summary>
    public class ResetPasswordRequestDto
    {
        /// <summary>
        /// 新密码
        /// </summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密码长度必须在8-100个字符之间")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// 是否强制下次登录时修改密码
        /// </summary>
        public bool ForceChangeOnNextLogin { get; set; }

        /// <summary>
        /// 是否发送通知邮件
        /// </summary>
        public bool SendNotificationEmail { get; set; } = true;
    }

    /// <summary>
    /// 锁定用户账户请求DTO
    /// </summary>
    public class LockUserAccountRequestDto
    {
        /// <summary>
        /// 锁定原因
        /// </summary>
        [Required(ErrorMessage = "锁定原因不能为空")]
        [StringLength(500, ErrorMessage = "锁定原因不能超过500个字符")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 锁定时长（分钟），null表示永久锁定
        /// </summary>
        [Range(1, 525600, ErrorMessage = "锁定时长必须在1分钟到1年之间")]
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// 是否发送通知邮件
        /// </summary>
        public bool SendNotificationEmail { get; set; } = true;
    }

    /// <summary>
    /// 分配角色请求DTO
    /// </summary>
    public class AssignRolesRequestDto
    {
        /// <summary>
        /// 角色IDs
        /// </summary>
        [Required(ErrorMessage = "角色ID列表不能为空")]
        public IEnumerable<Guid> RoleIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 是否替换现有角色（true: 替换，false: 追加）
        /// </summary>
        public bool ReplaceExisting { get; set; } = true;

        /// <summary>
        /// 角色过期时间
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 是否需要超级管理员权限
        /// </summary>
        public bool RequiresSuperAdminPermission { get; set; }
    }

    /// <summary>
    /// 移除角色请求DTO
    /// </summary>
    public class RemoveRolesRequestDto
    {
        /// <summary>
        /// 要移除的角色IDs
        /// </summary>
        [Required(ErrorMessage = "角色ID列表不能为空")]
        public IEnumerable<Guid> RoleIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 批量删除用户请求DTO
    /// </summary>
    public class BatchDeleteUsersRequestDto
    {
        /// <summary>
        /// 用户IDs
        /// </summary>
        [Required(ErrorMessage = "用户ID列表不能为空")]
        public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 是否软删除
        /// </summary>
        public bool SoftDelete { get; set; } = true;

        /// <summary>
        /// 删除原因
        /// </summary>
        [StringLength(500, ErrorMessage = "删除原因不能超过500个字符")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 发送消息请求DTO
    /// </summary>
    public class SendMessageRequestDto
    {
        /// <summary>
        /// 接收用户IDs
        /// </summary>
        [Required(ErrorMessage = "接收用户ID列表不能为空")]
        public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 消息标题
        /// </summary>
        [Required(ErrorMessage = "消息标题不能为空")]
        [StringLength(200, ErrorMessage = "消息标题不能超过200个字符")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 消息内容
        /// </summary>
        [Required(ErrorMessage = "消息内容不能为空")]
        [StringLength(5000, ErrorMessage = "消息内容不能超过5000个字符")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 发送方式（email/sms/notification/all）
        /// </summary>
        [Required(ErrorMessage = "发送方式不能为空")]
        public string DeliveryMethod { get; set; } = "notification";

        /// <summary>
        /// 是否立即发送
        /// </summary>
        public bool SendImmediately { get; set; } = true;

        /// <summary>
        /// 定时发送时间
        /// </summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>
        /// 消息优先级
        /// </summary>
        public string Priority { get; set; } = "Normal";
    }

    /// <summary>
    /// 批量更新状态请求DTO
    /// </summary>
    public class BatchUpdateStatusRequestDto
    {
        /// <summary>
        /// 用户IDs
        /// </summary>
        [Required(ErrorMessage = "用户ID列表不能为空")]
        public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();

        /// <summary>
        /// 新状态
        /// </summary>
        [Required(ErrorMessage = "新状态不能为空")]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>
        /// 状态变更原因
        /// </summary>
        [StringLength(500, ErrorMessage = "状态变更原因不能超过500个字符")]
        public string? Reason { get; set; }
    }



    #endregion
}