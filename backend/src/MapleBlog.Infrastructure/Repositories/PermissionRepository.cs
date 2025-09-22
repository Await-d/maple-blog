using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Enums;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// 权限仓储实现
    /// </summary>
    public class PermissionRepository : BlogBaseRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<Permission?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
        }

        public async Task<Permission?> GetByResourceActionAsync(string resource, string action, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action, cancellationToken);
        }

        public async Task<Permission?> GetByResourceActionScopeAsync(string resource, string action, PermissionScope scope, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action && p.Scope == scope, cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsWithScopeAsync(string resource, string action, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action))
                return Enumerable.Empty<Permission>();

            return await _dbSet
                .Where(p => p.Resource == resource && p.Action == action)
                .OrderBy(p => p.Scope)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByResourceAsync(string resource, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return Enumerable.Empty<Permission>();

            return await _dbSet
                .Where(p => p.Resource == resource)
                .OrderBy(p => p.Action)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetSystemPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => p.IsSystemPermission)
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return await _dbSet
                .AnyAsync(p => p.Name == name, cancellationToken);
        }

        public async Task<bool> CreateBatchAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!permissions.Any())
                    return true;

                _dbSet.AddRange(permissions);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建新的权限
        /// </summary>
        public async Task<Permission> CreateAsync(Permission permission, CancellationToken cancellationToken = default)
        {
            _dbSet.Add(permission);
            await _context.SaveChangesAsync(cancellationToken);
            return permission;
        }
    }
}