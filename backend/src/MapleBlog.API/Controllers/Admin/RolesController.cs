using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.API.Attributes;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using System.Security.Claims;

namespace MapleBlog.API.Controllers.Admin
{
    /// <summary>
    /// 角色管理API控制器
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    [RequirePermission("Role.Read")]
    public class RolesController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly ILogger<RolesController> _logger;

        public RolesController(
            IPermissionService permissionService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            ILogger<RolesController> logger)
        {
            _permissionService = permissionService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        /// <returns>角色列表</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAllRoles()
        {
            try
            {
                var roles = await _roleRepository.GetAllAsync();
                var roleDtos = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    IsActive = r.IsActive,
                    UserCount = r.GetUserCount(),
                    CreatedAt = r.CreatedAt
                });

                return Ok(ApiResponse<IEnumerable<RoleDto>>.CreateSuccess(roleDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all roles");
                return StatusCode(500, ApiResponse<IEnumerable<RoleDto>>.Error("获取角色列表失败"));
            }
        }

        /// <summary>
        /// 获取活跃角色
        /// </summary>
        /// <returns>活跃角色列表</returns>
        [HttpGet("active")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetActiveRoles()
        {
            try
            {
                var roles = await _roleRepository.GetActiveRolesAsync();
                var roleDtos = roles.Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    IsActive = r.IsActive,
                    UserCount = r.GetUserCount(),
                    CreatedAt = r.CreatedAt
                });

                return Ok(ApiResponse<IEnumerable<RoleDto>>.CreateSuccess(roleDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active roles");
                return StatusCode(500, ApiResponse<IEnumerable<RoleDto>>.Error("获取活跃角色失败"));
            }
        }

        /// <summary>
        /// 获取角色详情
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>角色详情</returns>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<RoleDetailDto>>> GetRole(Guid id)
        {
            try
            {
                var role = await _roleRepository.GetRoleWithPermissionsAsync(id);
                if (role == null)
                {
                    return NotFound(ApiResponse<RoleDetailDto>.Error("角色不存在"));
                }

                var roleDetailDto = new RoleDetailDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsSystemRole = role.IsSystemRole,
                    IsActive = role.IsActive,
                    UserCount = role.GetUserCount(),
                    CreatedAt = role.CreatedAt,
                    Permissions = role.RolePermissions
                        .Where(rp => rp.IsValid() && rp.Permission != null)
                        .Select(rp => new PermissionDto
                        {
                            Id = rp.Permission!.Id,
                            Name = rp.Permission.Name,
                            Description = rp.Permission.Description,
                            Resource = rp.Permission.Resource,
                            Action = rp.Permission.Action,
                            Scope = rp.Permission.Scope.ToString(),
                            IsSystemPermission = rp.Permission.IsSystemPermission,
                            CreatedAt = rp.Permission.CreatedAt
                        }).ToList()
                };

                return Ok(ApiResponse<RoleDetailDto>.CreateSuccess(roleDetailDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role {RoleId}", id);
                return StatusCode(500, ApiResponse<RoleDetailDto>.Error("获取角色详情失败"));
            }
        }

        /// <summary>
        /// 创建新角色
        /// </summary>
        /// <param name="request">角色创建请求</param>
        /// <returns>创建的角色</returns>
        [HttpPost]
        [RequirePermission("Role.Create")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<RoleDto>.Error("输入数据无效"));
                }

                // 检查角色名称是否已存在
                var existingRole = await _roleRepository.GetByNameAsync(request.Name);
                if (existingRole != null)
                {
                    return BadRequest(ApiResponse<RoleDto>.Error("角色名称已存在"));
                }

                var role = new Domain.Entities.Role();
                role.SetName(request.Name);
                role.Description = request.Description;
                role.IsSystemRole = false; // 用户创建的角色不是系统角色

                var createdRole = await _roleRepository.CreateAsync(role);

                var roleDto = new RoleDto
                {
                    Id = createdRole.Id,
                    Name = createdRole.Name,
                    Description = createdRole.Description,
                    IsSystemRole = createdRole.IsSystemRole,
                    IsActive = createdRole.IsActive,
                    UserCount = 0,
                    CreatedAt = createdRole.CreatedAt
                };

                _logger.LogInformation("Role {RoleName} created successfully", createdRole.Name);
                return Ok(ApiResponse<RoleDto>.CreateSuccess(roleDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, ApiResponse<RoleDto>.Error("创建角色失败"));
            }
        }

        /// <summary>
        /// 更新角色信息
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <param name="request">更新请求</param>
        /// <returns>更新的角色</returns>
        [HttpPut("{id:guid}")]
        [RequirePermission("Role.Update")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<RoleDto>.Error("输入数据无效"));
                }

                var role = await _roleRepository.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(ApiResponse<RoleDto>.Error("角色不存在"));
                }

                if (role.IsSystemRole)
                {
                    return BadRequest(ApiResponse<RoleDto>.Error("系统角色不能修改"));
                }

                // 检查名称是否被其他角色使用
                if (role.Name != request.Name)
                {
                    var existingRole = await _roleRepository.GetByNameAsync(request.Name);
                    if (existingRole != null && existingRole.Id != id)
                    {
                        return BadRequest(ApiResponse<RoleDto>.Error("角色名称已存在"));
                    }

                    role.SetName(request.Name);
                }

                role.Description = request.Description;
                role.IsActive = request.IsActive;

                _roleRepository.Update(role);
                await _roleRepository.SaveChangesAsync();

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsSystemRole = role.IsSystemRole,
                    IsActive = role.IsActive,
                    UserCount = role.GetUserCount(),
                    CreatedAt = role.CreatedAt
                };

                _logger.LogInformation("Role {RoleName} updated successfully", role.Name);
                return Ok(ApiResponse<RoleDto>.CreateSuccess(roleDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", id);
                return StatusCode(500, ApiResponse<RoleDto>.Error("更新角色失败"));
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <returns>操作结果</returns>
        [HttpDelete("{id:guid}")]
        [RequirePermission("Role.Delete")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteRole(Guid id)
        {
            try
            {
                var role = await _roleRepository.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(ApiResponse<bool>.Error("角色不存在"));
                }

                if (role.IsSystemRole)
                {
                    return BadRequest(ApiResponse<bool>.Error("系统角色不能删除"));
                }

                // 检查是否有用户使用此角色
                var userCount = await _userRoleRepository.GetActiveUserCountByRoleAsync(id);
                if (userCount > 0)
                {
                    return BadRequest(ApiResponse<bool>.Error($"该角色还有 {userCount} 个用户在使用，无法删除"));
                }

                _roleRepository.Remove(role);
                await _roleRepository.SaveChangesAsync();

                _logger.LogInformation("Role {RoleName} deleted successfully", role.Name);
                return Ok(ApiResponse<bool>.CreateSuccess(true, "角色删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", id);
                return StatusCode(500, ApiResponse<bool>.Error("删除角色失败"));
            }
        }

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        /// <param name="id">角色ID</param>
        /// <param name="request">权限分配请求</param>
        /// <returns>操作结果</returns>
        [HttpPost("{id:guid}/permissions")]
        [RequirePermission("Role.AssignPermissions")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignPermissions(Guid id, [FromBody] AssignPermissionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<bool>.Error("输入数据无效"));
                }

                var role = await _roleRepository.GetByIdAsync(id);
                if (role == null)
                {
                    return NotFound(ApiResponse<bool>.Error("角色不存在"));
                }

                if (role.IsSystemRole)
                {
                    return BadRequest(ApiResponse<bool>.Error("系统角色权限不能修改"));
                }

                var result = await _permissionService.AssignPermissionsToRoleAsync(id, request.PermissionIds);
                if (result)
                {
                    _logger.LogInformation("Permissions assigned to role {RoleName} successfully", role.Name);
                    return Ok(ApiResponse<bool>.CreateSuccess(true, "权限分配成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<bool>.Error("权限分配失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permissions to role {RoleId}", id);
                return StatusCode(500, ApiResponse<bool>.Error("分配权限时发生错误"));
            }
        }

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        /// <param name="request">用户角色分配请求</param>
        /// <returns>操作结果</returns>
        [HttpPost("assign-to-user")]
        [RequirePermission("User.ManageRoles")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRoleToUser([FromBody] AssignRoleToUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<bool>.Error("输入数据无效"));
                }

                var result = await _permissionService.AssignRoleToUserAsync(
                    request.UserId,
                    request.RoleId,
                    request.ExpiresAt);

                if (result)
                {
                    _logger.LogInformation("Role {RoleId} assigned to user {UserId} successfully",
                        request.RoleId, request.UserId);
                    return Ok(ApiResponse<bool>.CreateSuccess(true, "角色分配成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<bool>.Error("角色分配失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}",
                    request.RoleId, request.UserId);
                return StatusCode(500, ApiResponse<bool>.Error("分配角色时发生错误"));
            }
        }

        /// <summary>
        /// 从用户移除角色
        /// </summary>
        /// <param name="request">用户角色移除请求</param>
        /// <returns>操作结果</returns>
        [HttpPost("remove-from-user")]
        [RequirePermission("User.ManageRoles")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveRoleFromUser([FromBody] RemoveRoleFromUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<bool>.Error("输入数据无效"));
                }

                var result = await _permissionService.RemoveRoleFromUserAsync(request.UserId, request.RoleId);

                if (result)
                {
                    _logger.LogInformation("Role {RoleId} removed from user {UserId} successfully",
                        request.RoleId, request.UserId);
                    return Ok(ApiResponse<bool>.CreateSuccess(true, "角色移除成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<bool>.Error("角色移除失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}",
                    request.RoleId, request.UserId);
                return StatusCode(500, ApiResponse<bool>.Error("移除角色时发生错误"));
            }
        }
    }

    /// <summary>
    /// 角色DTO
    /// </summary>
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 角色详情DTO
    /// </summary>
    public class RoleDetailDto : RoleDto
    {
        public ICollection<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }

    /// <summary>
    /// 创建角色请求
    /// </summary>
    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新角色请求
    /// </summary>
    public class UpdateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// 权限分配请求
    /// </summary>
    public class AssignPermissionsRequest
    {
        public ICollection<Guid> PermissionIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 用户角色分配请求
    /// </summary>
    public class AssignRoleToUserRequest
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 用户角色移除请求
    /// </summary>
    public class RemoveRoleFromUserRequest
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}