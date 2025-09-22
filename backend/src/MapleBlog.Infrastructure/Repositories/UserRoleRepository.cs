using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 用户角色仓储实现
    /// </summary>
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly BlogDbContext _context;

        public UserRoleRepository(BlogDbContext context)
        {
            _context = context;
        }

        public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<UserRole>> GetActiveUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && ur.IsValid())
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<UserRole>> GetRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<UserRole>> GetActiveRoleUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId && ur.IsValid())
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.IsValid(), cancellationToken);
        }

        public async Task<bool> HasAnyRoleAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .AnyAsync(ur => ur.UserId == userId && roleIds.Contains(ur.RoleId) && ur.IsValid(), cancellationToken);
        }

        public async Task<UserRole> AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
        {
            var existingUserRole = await GetAsync(userRole.UserId, userRole.RoleId, cancellationToken);

            if (existingUserRole != null)
            {
                if (existingUserRole.IsValid())
                {
                    // 更新过期时间
                    existingUserRole.SetExpiration(userRole.ExpiresAt);
                    return existingUserRole;
                }
                else
                {
                    // 重新激活
                    existingUserRole.Activate();
                    existingUserRole.SetExpiration(userRole.ExpiresAt);
                    return existingUserRole;
                }
            }
            else
            {
                _context.Set<UserRole>().Add(userRole);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return userRole;
        }

        public async Task<bool> RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRole = await GetAsync(userId, roleId, cancellationToken);
                if (userRole != null)
                {
                    userRole.Deactivate();
                    await _context.SaveChangesAsync(cancellationToken);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveAllUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var userRoles = await GetUserRolesAsync(userId, cancellationToken);
                foreach (var userRole in userRoles)
                {
                    userRole.Deactivate();
                }
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> GetActiveUserCountByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .CountAsync(ur => ur.RoleId == roleId && ur.IsValid(), cancellationToken);
        }

        public async Task<IEnumerable<UserRole>> GetExpiringRolesAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
        {
            return await _context.Set<UserRole>()
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.IsActive && ur.ExpiresAt.HasValue && ur.ExpiresAt <= beforeDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> CleanupExpiredRolesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredRoles = await _context.Set<UserRole>()
                    .Where(ur => ur.IsActive && ur.ExpiresAt.HasValue && ur.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                foreach (var role in expiredRoles)
                {
                    role.Deactivate();
                }

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}