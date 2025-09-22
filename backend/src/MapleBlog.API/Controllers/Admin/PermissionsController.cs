using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MapleBlog.API.Attributes;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;

namespace MapleBlog.API.Controllers.Admin
{
    /// <summary>
    /// 权限管理API控制器
    /// </summary>
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    [RequirePermission("System.Admin")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(
            IPermissionService permissionService,
            IPermissionRepository permissionRepository,
            ILogger<PermissionsController> logger)
        {
            _permissionService = permissionService;
            _permissionRepository = permissionRepository;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有权限
        /// </summary>
        /// <returns>权限列表</returns>
        [HttpGet]
        [RequirePermission("Permission.Read")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionRepository.GetAllPermissionsAsync();
                var permissionDtos = permissions.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope.ToString(),
                    IsSystemPermission = p.IsSystemPermission,
                    CreatedAt = p.CreatedAt
                });

                return Ok(ApiResponse<IEnumerable<PermissionDto>>.CreateSuccess(permissionDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all permissions");
                return StatusCode(500, ApiResponse<IEnumerable<PermissionDto>>.Error("获取权限列表失败"));
            }
        }

        /// <summary>
        /// 根据资源获取权限
        /// </summary>
        /// <param name="resource">资源名称</param>
        /// <returns>权限列表</returns>
        [HttpGet("by-resource/{resource}")]
        [RequirePermission("Permission.Read")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetPermissionsByResource(string resource)
        {
            try
            {
                var permissions = await _permissionRepository.GetPermissionsByResourceAsync(resource);
                var permissionDtos = permissions.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope.ToString(),
                    IsSystemPermission = p.IsSystemPermission,
                    CreatedAt = p.CreatedAt
                });

                return Ok(ApiResponse<IEnumerable<PermissionDto>>.CreateSuccess(permissionDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions for resource {Resource}", resource);
                return StatusCode(500, ApiResponse<IEnumerable<PermissionDto>>.Error("获取资源权限失败"));
            }
        }

        /// <summary>
        /// 获取系统权限
        /// </summary>
        /// <returns>系统权限列表</returns>
        [HttpGet("system")]
        [RequirePermission("Permission.Read")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetSystemPermissions()
        {
            try
            {
                var permissions = await _permissionRepository.GetSystemPermissionsAsync();
                var permissionDtos = permissions.Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    Scope = p.Scope.ToString(),
                    IsSystemPermission = p.IsSystemPermission,
                    CreatedAt = p.CreatedAt
                });

                return Ok(ApiResponse<IEnumerable<PermissionDto>>.CreateSuccess(permissionDtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system permissions");
                return StatusCode(500, ApiResponse<IEnumerable<PermissionDto>>.Error("获取系统权限失败"));
            }
        }

        /// <summary>
        /// 创建新权限
        /// </summary>
        /// <param name="request">权限创建请求</param>
        /// <returns>创建的权限</returns>
        [HttpPost]
        [RequirePermission("Permission.Create")]
        public async Task<ActionResult<ApiResponse<PermissionDto>>> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<PermissionDto>.Error("输入数据无效"));
                }

                // 检查权限是否已存在
                var existingPermission = await _permissionRepository.GetByNameAsync(request.Name);
                if (existingPermission != null)
                {
                    return BadRequest(ApiResponse<PermissionDto>.Error("权限名称已存在"));
                }

                var permission = new Domain.Entities.Permission();
                if (Enum.TryParse<PermissionScope>(request.Scope, out var scope))
                {
                    permission.SetResourceAction(request.Resource, request.Action, scope);
                }
                else
                {
                    permission.SetResourceAction(request.Resource, request.Action);
                }

                permission.Description = request.Description;
                permission.IsSystemPermission = false; // 用户创建的权限不是系统权限

                var createdPermission = await _permissionRepository.CreateAsync(permission);

                var permissionDto = new PermissionDto
                {
                    Id = createdPermission.Id,
                    Name = createdPermission.Name,
                    Description = createdPermission.Description,
                    Resource = createdPermission.Resource,
                    Action = createdPermission.Action,
                    Scope = createdPermission.Scope.ToString(),
                    IsSystemPermission = createdPermission.IsSystemPermission,
                    CreatedAt = createdPermission.CreatedAt
                };

                _logger.LogInformation("Permission {PermissionName} created successfully", createdPermission.Name);
                return Ok(ApiResponse<PermissionDto>.CreateSuccess(permissionDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return StatusCode(500, ApiResponse<PermissionDto>.Error("创建权限失败"));
            }
        }

        /// <summary>
        /// 初始化默认权限
        /// </summary>
        /// <returns>操作结果</returns>
        [HttpPost("initialize")]
        [RequirePermission("System.Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> InitializeDefaultPermissions()
        {
            try
            {
                var result = await _permissionService.InitializeDefaultPermissionsAsync();
                if (result)
                {
                    _logger.LogInformation("Default permissions initialized successfully");
                    return Ok(ApiResponse<bool>.CreateSuccess(true, "默认权限初始化成功"));
                }
                else
                {
                    return BadRequest(ApiResponse<bool>.Error("默认权限初始化失败"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing default permissions");
                return StatusCode(500, ApiResponse<bool>.Error("初始化默认权限时发生错误"));
            }
        }
    }

    /// <summary>
    /// 权限DTO
    /// </summary>
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
        public bool IsSystemPermission { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 创建权限请求
    /// </summary>
    public class CreatePermissionRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Scope { get; set; } = "Own";
    }

}