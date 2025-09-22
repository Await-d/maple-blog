using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 角色仓储实现
    /// </summary>
    public class RoleRepository : BlogBaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(r => r.NormalizedName == name.ToUpperInvariant(), cancellationToken);
        }

        public async Task<Role?> GetRoleWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        }

        public async Task<IEnumerable<Role>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ClearRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            try
            {
                var rolePermissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync(cancellationToken);

                if (rolePermissions.Any())
                {
                    _context.Set<RolePermission>().RemoveRange(rolePermissions);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddRolePermissionAsync(Guid roleId, Guid permissionId, Guid? grantedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查是否已存在有效权限
                var existingPermission = await _context.Set<RolePermission>()
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

                if (existingPermission != null)
                {
                    if (existingPermission.IsValid())
                        return true;

                    // 如果存在但无效，重新激活
                    existingPermission.Activate();
                }
                else
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        GrantedBy = grantedBy
                    };

                    _context.Set<RolePermission>().Add(rolePermission);
                }

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddTemporaryRolePermissionAsync(Guid roleId, Guid permissionId, DateTime expiresAt, Guid? grantedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    GrantedBy = grantedBy
                };

                rolePermission.SetTemporary(expiresAt);

                _context.Set<RolePermission>().Add(rolePermission);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var rolePermission = await _context.Set<RolePermission>()
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

                if (rolePermission != null)
                {
                    _context.Set<RolePermission>().Remove(rolePermission);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<RolePermission>()
                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);
        }

        public async Task<int> GetUserCountAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .CountAsync(ur => ur.RoleId == roleId && ur.IsActive &&
                                 (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow), cancellationToken);
        }

        /// <summary>
        /// 创建新的角色
        /// </summary>
        public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
        {
            _dbSet.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
            return role;
        }
    }
}