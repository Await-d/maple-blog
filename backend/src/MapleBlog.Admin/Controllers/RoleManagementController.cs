using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Admin.Controllers
{
    /// <summary>
    /// 角色管理控制器 - 提供企业级角色和权限管理功能
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public class RoleManagementController : BaseAdminController
    {
        private readonly IRoleManagementService _roleManagementService;
        private readonly IPermissionManagementService _permissionManagementService;

        public RoleManagementController(
            IRoleManagementService roleManagementService,
            IPermissionManagementService permissionManagementService,
            ILogger<RoleManagementController> logger,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
            : base(logger, permissionService, auditLogService)
        {
            _roleManagementService = roleManagementService ?? throw new ArgumentNullException(nameof(roleManagementService));
            _permissionManagementService = permissionManagementService ?? throw new ArgumentNullException(nameof(permissionManagementService));
        }

        /// <summary>
        /// 获取角色管理概览数据
        /// </summary>
        /// <returns>角色管理概览信息</returns>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(RoleManagementOverviewDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoleOverview()
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var overview = await GetRoleOverviewDataAsync();

                await LogAuditAsync(
                    "GetRoleOverview",
                    "RoleManagement",
                    description: "获取角色管理概览数据"
                );

                return Success(overview, "成功获取角色管理概览");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色管理概览");
            }
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <param name="pageNumber">页码，默认1</param>
        /// <param name="pageSize">页大小，默认20，最大100</param>
        /// <param name="searchTerm">搜索关键词（角色名称、描述）</param>
        /// <param name="isActive">活跃状态筛选</param>
        /// <param name="isSystem">系统角色筛选</param>
        /// <param name="sortBy">排序字段</param>
        /// <param name="sortDirection">排序方向</param>
        /// <returns>分页角色列表</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<RoleManagementDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isSystem = null,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortDirection = "desc")
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                // 参数验证
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var roles = await GetRolesPagedAsync(pageNumber, pageSize, searchTerm, isActive, isSystem, sortBy, sortDirection);

                await LogAuditAsync(
                    "GetRoleList",
                    "RoleManagement",
                    description: $"获取角色列表，页码: {pageNumber}，搜索: {searchTerm}"
                );

                return SuccessWithPagination(
                    roles.Items,
                    roles.TotalCount,
                    roles.PageNumber,
                    roles.PageSize,
                    "成功获取角色列表"
                );
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色列表");
            }
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>角色详细信息</returns>
        [HttpGet("{roleId:guid}")]
        [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleDetail([FromRoute] Guid roleId)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var roleDetail = await GetRoleDetailDataAsync(roleId);
                if (roleDetail == null)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "GetRoleDetail",
                    "Role",
                    roleId.ToString(),
                    $"查看角色详细信息: {roleDetail.BasicInfo.Name}"
                );

                return Success(roleDetail, "成功获取角色详细信息");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色详细信息");
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        /// <param name="createRequest">创建角色请求数据</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        [ProducesResponseType(typeof(RoleCreateResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [EnableRateLimiting("RoleCreation")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDto createRequest)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Create");
                if (permissionCheck != null) return permissionCheck;

                // 模型验证
                var modelValidation = ValidateModelState();
                if (modelValidation != null) return modelValidation;

                // 业务验证
                if (string.IsNullOrWhiteSpace(createRequest.Name))
                {
                    return Error("角色名称不能为空");
                }

                var result = await CreateRoleAsync(createRequest);

                if (!result.Success)
                {
                    return Error(string.Join(", ", result.Errors));
                }

                await LogAuditAsync(
                    "CreateRole",
                    "Role",
                    result.RoleId?.ToString(),
                    $"创建角色: {createRequest.Name}",
                    null,
                    createRequest
                );

                return StatusCode(StatusCodes.Status201Created, Success(result, "角色创建成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "创建角色");
            }
        }

        /// <summary>
        /// 更新角色信息
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="updateRequest">更新请求数据</param>
        /// <returns>更新结果</returns>
        [HttpPut("{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateRole(
            [FromRoute] Guid roleId,
            [FromBody] UpdateRoleRequestDto updateRequest)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Update");
                if (permissionCheck != null) return permissionCheck;

                // 检查系统角色保护
                var isSystemRole = await IsSystemRoleAsync(roleId);
                if (isSystemRole && !IsSuperAdmin())
                {
                    return Forbid("系统角色只能由超级管理员修改");
                }

                var success = await UpdateRoleAsync(roleId, updateRequest);
                if (!success)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "UpdateRole",
                    "Role",
                    roleId.ToString(),
                    "更新角色信息",
                    null,
                    updateRequest
                );

                return Success(null, "角色信息更新成功");
            }
            catch (InvalidOperationException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "更新角色信息");
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="forceDelete">是否强制删除（即使有用户关联）</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{roleId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteRole(
            [FromRoute] Guid roleId,
            [FromQuery] bool forceDelete = false)
        {
            try
            {
                // 验证权限
                var requiredPermission = forceDelete ? "RoleManagement.ForceDelete" : "RoleManagement.Delete";
                var permissionCheck = await ValidatePermissionAsync(requiredPermission);
                if (permissionCheck != null) return permissionCheck;

                // 检查系统角色保护
                var isSystemRole = await IsSystemRoleAsync(roleId);
                if (isSystemRole)
                {
                    return Error("系统角色不能被删除");
                }

                var result = await DeleteRoleAsync(roleId, forceDelete);
                if (!result.Success)
                {
                    if (result.HasUsers && !forceDelete)
                    {
                        return Error($"角色被 {result.UserCount} 个用户使用，无法删除。如需强制删除，请使用 forceDelete=true 参数");
                    }
                    return Error(result.ErrorMessage ?? "删除失败");
                }

                await LogAuditAsync(
                    "DeleteRole",
                    "Role",
                    roleId.ToString(),
                    $"删除角色，强制删除: {forceDelete}"
                );

                return Success(null, "角色删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除角色");
            }
        }

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="request">权限分配请求</param>
        /// <returns>分配结果</returns>
        [HttpPost("{roleId:guid}/permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignPermissionsToRole(
            [FromRoute] Guid roleId,
            [FromBody] AssignPermissionsRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.AssignPermission");
                if (permissionCheck != null) return permissionCheck;

                // 检查系统角色保护
                var isSystemRole = await IsSystemRoleAsync(roleId);
                if (isSystemRole && !IsSuperAdmin())
                {
                    return Forbid("系统角色权限只能由超级管理员管理");
                }

                var success = await AssignPermissionsToRoleAsync(roleId, request);
                if (!success)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "AssignPermissionsToRole",
                    "Role",
                    roleId.ToString(),
                    $"为角色分配权限，权限数量: {request.PermissionIds.Count()}"
                );

                return Success(null, "权限分配成功");
            }
            catch (ArgumentException ex)
            {
                return Error(ex.Message);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "分配角色权限");
            }
        }

        /// <summary>
        /// 移除角色权限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="request">移除权限请求</param>
        /// <returns>移除结果</returns>
        [HttpDelete("{roleId:guid}/permissions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemovePermissionsFromRole(
            [FromRoute] Guid roleId,
            [FromBody] RemovePermissionsRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.RemovePermission");
                if (permissionCheck != null) return permissionCheck;

                // 检查系统角色保护
                var isSystemRole = await IsSystemRoleAsync(roleId);
                if (isSystemRole && !IsSuperAdmin())
                {
                    return Forbid("系统角色权限只能由超级管理员管理");
                }

                var success = await RemovePermissionsFromRoleAsync(roleId, request);
                if (!success)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "RemovePermissionsFromRole",
                    "Role",
                    roleId.ToString(),
                    $"移除角色权限，权限数量: {request.PermissionIds.Count()}"
                );

                return Success(null, "权限移除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "移除角色权限");
            }
        }

        /// <summary>
        /// 获取角色权限列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="includeInherited">是否包含继承权限</param>
        /// <returns>角色权限列表</returns>
        [HttpGet("{roleId:guid}/permissions")]
        [ProducesResponseType(typeof(IEnumerable<RolePermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolePermissions(
            [FromRoute] Guid roleId,
            [FromQuery] bool includeInherited = true)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var permissions = await GetRolePermissionsAsync(roleId, includeInherited);
                if (permissions == null)
                {
                    return NotFoundResult("角色", roleId);
                }

                return Success(permissions, "成功获取角色权限列表");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色权限");
            }
        }

        /// <summary>
        /// 获取角色用户列表
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="searchTerm">搜索关键词</param>
        /// <returns>角色用户列表</returns>
        [HttpGet("{roleId:guid}/users")]
        [ProducesResponseType(typeof(PagedResultDto<RoleUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRoleUsers(
            [FromRoute] Guid roleId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                // 参数验证
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var users = await GetRoleUsersPagedAsync(roleId, pageNumber, pageSize, searchTerm);
                if (users == null)
                {
                    return NotFoundResult("角色", roleId);
                }

                return SuccessWithPagination(
                    users.Items,
                    users.TotalCount,
                    users.PageNumber,
                    users.PageSize,
                    "成功获取角色用户列表"
                );
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色用户列表");
            }
        }

        /// <summary>
        /// 批量分配用户到角色
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="request">批量分配请求</param>
        /// <returns>批量操作结果</returns>
        [HttpPost("{roleId:guid}/users/batch-assign")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BatchAssignUsersToRole(
            [FromRoute] Guid roleId,
            [FromBody] BatchAssignUsersRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.AssignUser");
                if (permissionCheck != null) return permissionCheck;

                var result = await BatchAssignUsersToRoleAsync(roleId, request);
                if (result == null)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "BatchAssignUsersToRole",
                    "Role",
                    roleId.ToString(),
                    $"批量分配用户到角色，用户数量: {request.UserIds.Count()}"
                );

                return Success(result, $"批量分配完成，成功: {result.SuccessCount}，失败: {result.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量分配用户到角色");
            }
        }

        /// <summary>
        /// 批量移除角色用户
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="request">批量移除请求</param>
        /// <returns>批量操作结果</returns>
        [HttpPost("{roleId:guid}/users/batch-remove")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BatchRemoveUsersFromRole(
            [FromRoute] Guid roleId,
            [FromBody] BatchRemoveUsersRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.RemoveUser");
                if (permissionCheck != null) return permissionCheck;

                var result = await BatchRemoveUsersFromRoleAsync(roleId, request);
                if (result == null)
                {
                    return NotFoundResult("角色", roleId);
                }

                await LogAuditAsync(
                    "BatchRemoveUsersFromRole",
                    "Role",
                    roleId.ToString(),
                    $"批量移除角色用户，用户数量: {request.UserIds.Count()}"
                );

                return Success(result, $"批量移除完成，成功: {result.SuccessCount}，失败: {result.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量移除角色用户");
            }
        }

        /// <summary>
        /// 获取所有可用权限列表
        /// </summary>
        /// <param name="category">权限分类筛选</param>
        /// <param name="searchTerm">搜索关键词</param>
        /// <returns>权限列表</returns>
        [HttpGet("permissions/available")]
        [ProducesResponseType(typeof(IEnumerable<AvailablePermissionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailablePermissions(
            [FromQuery] string? category = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var permissions = await GetAvailablePermissionsAsync(category, searchTerm);

                return Success(permissions, "成功获取可用权限列表");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取可用权限列表");
            }
        }

        /// <summary>
        /// 获取权限分类列表
        /// </summary>
        /// <returns>权限分类列表</returns>
        [HttpGet("permissions/categories")]
        [ProducesResponseType(typeof(IEnumerable<PermissionCategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPermissionCategories()
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var categories = await GetPermissionCategoriesAsync();

                return Success(categories, "成功获取权限分类列表");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取权限分类列表");
            }
        }

        /// <summary>
        /// 复制角色
        /// </summary>
        /// <param name="roleId">源角色ID</param>
        /// <param name="request">复制角色请求</param>
        /// <returns>复制结果</returns>
        [HttpPost("{roleId:guid}/copy")]
        [ProducesResponseType(typeof(RoleCreateResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CopyRole(
            [FromRoute] Guid roleId,
            [FromBody] CopyRoleRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Create");
                if (permissionCheck != null) return permissionCheck;

                var result = await CopyRoleAsync(roleId, request);
                if (result == null)
                {
                    return NotFoundResult("源角色", roleId);
                }

                if (!result.Success)
                {
                    return Error(string.Join(", ", result.Errors));
                }

                await LogAuditAsync(
                    "CopyRole",
                    "Role",
                    result.RoleId?.ToString(),
                    $"复制角色: {request.NewName}，源角色ID: {roleId}"
                );

                return StatusCode(StatusCodes.Status201Created, Success(result, "角色复制成功"));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "复制角色");
            }
        }

        /// <summary>
        /// 获取角色层次结构
        /// </summary>
        /// <returns>角色层次结构树</returns>
        [HttpGet("hierarchy")]
        [ProducesResponseType(typeof(IEnumerable<RoleHierarchyDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoleHierarchy()
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Read");
                if (permissionCheck != null) return permissionCheck;

                var hierarchy = await GetRoleHierarchyAsync();

                return Success(hierarchy, "成功获取角色层次结构");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "获取角色层次结构");
            }
        }

        /// <summary>
        /// 批量激活/停用角色
        /// </summary>
        /// <param name="request">批量状态更新请求</param>
        /// <returns>批量操作结果</returns>
        [HttpPost("batch/update-status")]
        [ProducesResponseType(typeof(BatchOperationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateRoleStatus([FromBody] BatchUpdateRoleStatusRequestDto request)
        {
            try
            {
                // 验证权限
                var permissionCheck = await ValidatePermissionAsync("RoleManagement.Update");
                if (permissionCheck != null) return permissionCheck;

                var result = await BatchUpdateRoleStatusAsync(request);

                await LogAuditAsync(
                    "BatchUpdateRoleStatus",
                    "RoleManagement",
                    string.Join(",", request.RoleIds),
                    $"批量更新角色状态为: {request.IsActive}，数量: {request.RoleIds.Count()}"
                );

                return Success(result, $"批量状态更新完成，成功: {result.SuccessCount}，失败: {result.FailCount}");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "批量更新角色状态");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 获取角色概览数据
        /// </summary>
        private async Task<RoleManagementOverviewDto> GetRoleOverviewDataAsync()
        {
            // 这里应该调用实际的服务获取数据
            // 简化实现，返回示例数据
            return new RoleManagementOverviewDto
            {
                TotalRoles = 10,
                ActiveRoles = 8,
                SystemRoles = 3,
                CustomRoles = 7,
                TotalPermissions = 50,
                RolesWithUsers = 6,
                UnusedRoles = 2,
                RoleDistribution = new List<RoleTypeDistributionDto>
                {
                    new() { Type = "System", Count = 3, Percentage = 30.0 },
                    new() { Type = "Custom", Count = 7, Percentage = 70.0 }
                },
                PermissionDistribution = new List<PermissionUsageDto>
                {
                    new() { Category = "User Management", Count = 15, Percentage = 30.0 },
                    new() { Category = "Content Management", Count = 20, Percentage = 40.0 },
                    new() { Category = "System", Count = 15, Percentage = 30.0 }
                }
            };
        }

        /// <summary>
        /// 获取分页角色列表
        /// </summary>
        private async Task<PagedResultDto<RoleManagementDto>> GetRolesPagedAsync(
            int pageNumber, int pageSize, string? searchTerm, bool? isActive, bool? isSystem, string sortBy, string sortDirection)
        {
            // 这里应该调用实际的服务获取数据
            // 简化实现，返回示例数据
            var roles = new List<RoleManagementDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Administrator",
                    DisplayName = "系统管理员",
                    Description = "系统管理员角色",
                    IsSystemRole = true,
                    IsActive = true,
                    UserCount = 2,
                    PermissionCount = 50,
                    CreatedAt = DateTime.UtcNow.AddDays(-100),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            return new PagedResultDto<RoleManagementDto>
            {
                Items = roles,
                TotalCount = roles.Count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 获取角色详细信息
        /// </summary>
        private async Task<RoleDetailDto?> GetRoleDetailDataAsync(Guid roleId)
        {
            // 这里应该调用实际的服务获取数据
            // 简化实现，返回示例数据
            return new RoleDetailDto
            {
                BasicInfo = new RoleManagementDto
                {
                    Id = roleId,
                    Name = "Administrator",
                    DisplayName = "系统管理员",
                    Description = "系统管理员角色",
                    IsSystemRole = true,
                    IsActive = true,
                    UserCount = 2,
                    PermissionCount = 50,
                    CreatedAt = DateTime.UtcNow.AddDays(-100),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                Permissions = new List<RolePermissionDto>(),
                Users = new List<RoleUserDto>(),
                Statistics = new RoleStatisticsDto
                {
                    TotalUsers = 2,
                    ActiveUsers = 2,
                    InactiveUsers = 0,
                    NewUsersThisMonth = 0,
                    LastUserAssigned = DateTime.UtcNow.AddDays(-5)
                }
            };
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        private async Task<RoleCreateResultDto> CreateRoleAsync(CreateRoleRequestDto request)
        {
            // 这里应该调用实际的服务创建角色
            // 简化实现
            return new RoleCreateResultDto
            {
                Success = true,
                RoleId = Guid.NewGuid(),
                RoleInfo = new RoleManagementDto
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    DisplayName = request.DisplayName ?? request.Name,
                    Description = request.Description,
                    IsSystemRole = false,
                    IsActive = true,
                    UserCount = 0,
                    PermissionCount = request.PermissionIds?.Count() ?? 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        private async Task<bool> UpdateRoleAsync(Guid roleId, UpdateRoleRequestDto request)
        {
            // 这里应该调用实际的服务更新角色
            return true;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        private async Task<RoleDeleteResultDto> DeleteRoleAsync(Guid roleId, bool forceDelete)
        {
            // 这里应该调用实际的服务删除角色
            return new RoleDeleteResultDto
            {
                Success = true,
                HasUsers = false,
                UserCount = 0
            };
        }

        /// <summary>
        /// 检查是否为系统角色
        /// </summary>
        private async Task<bool> IsSystemRoleAsync(Guid roleId)
        {
            // 这里应该调用实际的服务检查
            return false;
        }

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        private async Task<bool> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsRequestDto request)
        {
            // 这里应该调用实际的服务
            return true;
        }

        /// <summary>
        /// 移除角色权限
        /// </summary>
        private async Task<bool> RemovePermissionsFromRoleAsync(Guid roleId, RemovePermissionsRequestDto request)
        {
            // 这里应该调用实际的服务
            return true;
        }

        /// <summary>
        /// 获取角色权限
        /// </summary>
        private async Task<IEnumerable<RolePermissionDto>?> GetRolePermissionsAsync(Guid roleId, bool includeInherited)
        {
            // 这里应该调用实际的服务
            return new List<RolePermissionDto>();
        }

        /// <summary>
        /// 获取角色用户列表
        /// </summary>
        private async Task<PagedResultDto<RoleUserDto>?> GetRoleUsersPagedAsync(
            Guid roleId, int pageNumber, int pageSize, string? searchTerm)
        {
            // 这里应该调用实际的服务
            return new PagedResultDto<RoleUserDto>
            {
                Items = new List<RoleUserDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 批量分配用户到角色
        /// </summary>
        private async Task<BatchOperationResultDto?> BatchAssignUsersToRoleAsync(
            Guid roleId, BatchAssignUsersRequestDto request)
        {
            // 这里应该调用实际的服务
            return new BatchOperationResultDto
            {
                TotalCount = request.UserIds.Count(),
                SuccessCount = request.UserIds.Count(),
                FailCount = 0,
                Success = true
            };
        }

        /// <summary>
        /// 批量移除角色用户
        /// </summary>
        private async Task<BatchOperationResultDto?> BatchRemoveUsersFromRoleAsync(
            Guid roleId, BatchRemoveUsersRequestDto request)
        {
            // 这里应该调用实际的服务
            return new BatchOperationResultDto
            {
                TotalCount = request.UserIds.Count(),
                SuccessCount = request.UserIds.Count(),
                FailCount = 0,
                Success = true
            };
        }

        /// <summary>
        /// 获取可用权限列表
        /// </summary>
        private async Task<IEnumerable<AvailablePermissionDto>> GetAvailablePermissionsAsync(
            string? category, string? searchTerm)
        {
            // 这里应该调用实际的服务
            return new List<AvailablePermissionDto>();
        }

        /// <summary>
        /// 获取权限分类列表
        /// </summary>
        private async Task<IEnumerable<PermissionCategoryDto>> GetPermissionCategoriesAsync()
        {
            // 这里应该调用实际的服务
            return new List<PermissionCategoryDto>
            {
                new() { Name = "UserManagement", DisplayName = "用户管理", Description = "用户管理相关权限", PermissionCount = 15 },
                new() { Name = "ContentManagement", DisplayName = "内容管理", Description = "内容管理相关权限", PermissionCount = 20 },
                new() { Name = "System", DisplayName = "系统管理", Description = "系统管理相关权限", PermissionCount = 15 }
            };
        }

        /// <summary>
        /// 复制角色
        /// </summary>
        private async Task<RoleCreateResultDto?> CopyRoleAsync(Guid sourceRoleId, CopyRoleRequestDto request)
        {
            // 这里应该调用实际的服务
            return new RoleCreateResultDto
            {
                Success = true,
                RoleId = Guid.NewGuid()
            };
        }

        /// <summary>
        /// 获取角色层次结构
        /// </summary>
        private async Task<IEnumerable<RoleHierarchyDto>> GetRoleHierarchyAsync()
        {
            // 这里应该调用实际的服务
            return new List<RoleHierarchyDto>();
        }

        /// <summary>
        /// 批量更新角色状态
        /// </summary>
        private async Task<BatchOperationResultDto> BatchUpdateRoleStatusAsync(BatchUpdateRoleStatusRequestDto request)
        {
            // 这里应该调用实际的服务
            return new BatchOperationResultDto
            {
                TotalCount = request.RoleIds.Count(),
                SuccessCount = request.RoleIds.Count(),
                FailCount = 0,
                Success = true
            };
        }

        #endregion
    }

    #region Role Management DTOs

    /// <summary>
    /// 角色管理概览DTO
    /// </summary>
    public class RoleManagementOverviewDto
    {
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int SystemRoles { get; set; }
        public int CustomRoles { get; set; }
        public int TotalPermissions { get; set; }
        public int RolesWithUsers { get; set; }
        public int UnusedRoles { get; set; }
        public IEnumerable<RoleTypeDistributionDto> RoleDistribution { get; set; } = new List<RoleTypeDistributionDto>();
        public IEnumerable<PermissionUsageDto> PermissionDistribution { get; set; } = new List<PermissionUsageDto>();
    }

    /// <summary>
    /// 角色管理DTO
    /// </summary>
    public class RoleManagementDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public IEnumerable<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// 角色详细信息DTO
    /// </summary>
    public class RoleDetailDto
    {
        public RoleManagementDto BasicInfo { get; set; } = new();
        public IEnumerable<RolePermissionDto> Permissions { get; set; } = new List<RolePermissionDto>();
        public IEnumerable<RoleUserDto> Users { get; set; } = new List<RoleUserDto>();
        public RoleStatisticsDto Statistics { get; set; } = new();
    }

    /// <summary>
    /// 创建角色请求DTO
    /// </summary>
    public class CreateRoleRequestDto
    {
        [Required(ErrorMessage = "角色名称不能为空")]
        [StringLength(50, ErrorMessage = "角色名称不能超过50个字符")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "显示名称不能超过100个字符")]
        public string? DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<Guid>? PermissionIds { get; set; }

        public IEnumerable<string>? Tags { get; set; }
    }

    /// <summary>
    /// 更新角色请求DTO
    /// </summary>
    public class UpdateRoleRequestDto
    {
        [StringLength(50, ErrorMessage = "角色名称不能超过50个字符")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "显示名称不能超过100个字符")]
        public string? DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public IEnumerable<string>? Tags { get; set; }
    }

    /// <summary>
    /// 角色创建结果DTO
    /// </summary>
    public class RoleCreateResultDto
    {
        public bool Success { get; set; }
        public Guid? RoleId { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
        public IEnumerable<string> Warnings { get; set; } = new List<string>();
        public RoleManagementDto? RoleInfo { get; set; }
    }

    /// <summary>
    /// 角色删除结果DTO
    /// </summary>
    public class RoleDeleteResultDto
    {
        public bool Success { get; set; }
        public bool HasUsers { get; set; }
        public int UserCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 分配权限请求DTO
    /// </summary>
    public class AssignPermissionsRequestDto
    {
        [Required(ErrorMessage = "权限ID列表不能为空")]
        public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();

        public bool ReplaceExisting { get; set; } = true;
    }

    /// <summary>
    /// 移除权限请求DTO
    /// </summary>
    public class RemovePermissionsRequestDto
    {
        [Required(ErrorMessage = "权限ID列表不能为空")]
        public IEnumerable<Guid> PermissionIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 角色权限DTO
    /// </summary>
    public class RolePermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsInherited { get; set; }
        public string? InheritedFrom { get; set; }
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }
    }

    /// <summary>
    /// 角色用户DTO
    /// </summary>
    public class RoleUserDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// 批量分配用户请求DTO
    /// </summary>
    public class BatchAssignUsersRequestDto
    {
        [Required(ErrorMessage = "用户ID列表不能为空")]
        public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();

        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 批量移除用户请求DTO
    /// </summary>
    public class BatchRemoveUsersRequestDto
    {
        [Required(ErrorMessage = "用户ID列表不能为空")]
        public IEnumerable<Guid> UserIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 可用权限DTO
    /// </summary>
    public class AvailablePermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsAssigned { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
    }

    /// <summary>
    /// 权限分类DTO
    /// </summary>
    public class PermissionCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PermissionCount { get; set; }
        public string? Icon { get; set; }
    }

    /// <summary>
    /// 复制角色请求DTO
    /// </summary>
    public class CopyRoleRequestDto
    {
        [Required(ErrorMessage = "新角色名称不能为空")]
        [StringLength(50, ErrorMessage = "角色名称不能超过50个字符")]
        public string NewName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "显示名称不能超过100个字符")]
        public string? NewDisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超过500个字符")]
        public string? NewDescription { get; set; }

        public bool CopyPermissions { get; set; } = true;
        public bool CopyUsers { get; set; } = false;
    }

    /// <summary>
    /// 角色层次结构DTO
    /// </summary>
    public class RoleHierarchyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Level { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public IEnumerable<RoleHierarchyDto> Children { get; set; } = new List<RoleHierarchyDto>();
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    /// <summary>
    /// 批量更新角色状态请求DTO
    /// </summary>
    public class BatchUpdateRoleStatusRequestDto
    {
        [Required(ErrorMessage = "角色ID列表不能为空")]
        public IEnumerable<Guid> RoleIds { get; set; } = new List<Guid>();

        public bool IsActive { get; set; }

        [StringLength(500, ErrorMessage = "状态变更原因不能超过500个字符")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 角色统计信息DTO
    /// </summary>
    public class RoleStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public DateTime? LastUserAssigned { get; set; }
        public TimeSpan? AverageAssignmentDuration { get; set; }
    }

    /// <summary>
    /// 角色类型分布DTO
    /// </summary>
    public class RoleTypeDistributionDto
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// 权限使用情况DTO
    /// </summary>
    public class PermissionUsageDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    #endregion

    #region Mock Interface Definitions (These should be in separate interface files)

    public interface IRoleManagementService
    {
        // Mock interface definition - in real implementation, this would be in a separate file
    }

    public interface IPermissionManagementService
    {
        // Mock interface definition - in real implementation, this would be in a separate file
    }

    #endregion
}