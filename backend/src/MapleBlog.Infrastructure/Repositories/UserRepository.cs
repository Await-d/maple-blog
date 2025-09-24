using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Infrastructure.Data;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of user repository
    /// </summary>
    public class UserRepository : BlogBaseRepository<User>, IUserRepository
    {
        public UserRepository(BlogDbContext context) : base(context)
        {
        }

        public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            try
            {
                var emailValueObject = Email.Create(email);
                return await FindByEmailAsync(emailValueObject, cancellationToken);
            }
            catch (ArgumentException)
            {
                // Invalid email format
                return null;
            }
        }

        public async Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
        }

        public async Task<User?> FindByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u =>
                    u.EmailVerificationToken == token &&
                    u.EmailVerificationTokenExpiry.HasValue &&
                    u.EmailVerificationTokenExpiry.Value > DateTime.UtcNow,
                    cancellationToken);
        }

        public async Task<User?> FindByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(u =>
                    u.PasswordResetToken == token &&
                    u.PasswordResetTokenExpiresAt.HasValue &&
                    u.PasswordResetTokenExpiresAt.Value > DateTime.UtcNow,
                    cancellationToken);
        }

        public async Task<bool> IsEmailInUseAsync(Email email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            if (email == null)
                return false;

            var query = _dbSet.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<bool> IsUserNameInUseAsync(string userName, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            var query = _dbSet.Where(u => u.UserName == userName);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        /// <summary>
        /// Gets users by role enum with Flags support
        /// </summary>
        /// <param name="role">Role to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of users with the specified role</returns>
        public async Task<IReadOnlyList<User>> GetUsersByRoleAsync(Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            if (role == Domain.Enums.UserRole.None)
                return new List<User>();

            // Support both exact role match and HasRole for Flags enum
            return await _dbSet
                .Where(u => (u.Role & role) == role && u.IsActive)
                .OrderBy(u => u.UserName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetActiveUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await GetPagedAsync(
                pageNumber,
                pageSize,
                u => u.IsActive,
                query => query.OrderBy(u => u.UserName),
                cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetUsersRegisteredBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetUnverifiedUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => !u.EmailConfirmed && u.IsActive)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(u => u.LockoutEndDateUtc.HasValue && u.LockoutEndDateUtc.Value > now)
                .OrderByDescending(u => u.LockoutEndDateUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var usersWithExpiredTokens = await _dbSet
                .Where(u =>
                    (u.EmailVerificationToken != null &&
                     u.EmailVerificationTokenExpiry.HasValue &&
                     u.EmailVerificationTokenExpiry.Value <= now) ||
                    (u.PasswordResetToken != null &&
                     u.PasswordResetTokenExpiresAt.HasValue &&
                     u.PasswordResetTokenExpiresAt.Value <= now))
                .ToListAsync(cancellationToken);

            foreach (var user in usersWithExpiredTokens)
            {
                // Clear expired email verification tokens
                if (user.EmailVerificationToken != null &&
                    user.EmailVerificationTokenExpiry.HasValue &&
                    user.EmailVerificationTokenExpiry.Value <= now)
                {
                    // Use reflection to clear the token since the domain method requires valid inputs
                    var property = typeof(User).GetProperty("EmailVerificationToken");
                    property?.SetValue(user, null);
                    property = typeof(User).GetProperty("EmailVerificationTokenExpiry");
                    property?.SetValue(user, null);
                }

                // Clear expired password reset tokens
                if (user.PasswordResetToken != null &&
                    user.PasswordResetTokenExpiresAt.HasValue &&
                    user.PasswordResetTokenExpiresAt.Value <= now)
                {
                    // Use reflection to clear the token since the domain method requires valid inputs
                    var property = typeof(User).GetProperty("PasswordResetToken");
                    property?.SetValue(user, null);
                    property = typeof(User).GetProperty("PasswordResetTokenExpiresAt");
                    property?.SetValue(user, null);
                }
            }

            if (usersWithExpiredTokens.Any())
            {
                UpdateRange(usersWithExpiredTokens);
                await SaveChangesAsync(cancellationToken);
            }

            return usersWithExpiredTokens.Count;
        }

        // Note: Method changed to return single role since we're using enum-based roles
        public async Task<Domain.Enums.UserRole?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbSet.FindAsync(new object[] { userId }, cancellationToken);
            return user?.Role;
        }

        /// <summary>
        /// Gets user roles as enum flags (replaces legacy method)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User roles as enum flags</returns>
        public async Task<Domain.Enums.UserRole> GetUserRolesEnumAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbSet.FindAsync(new object[] { userId }, cancellationToken);
            return user?.Role ?? Domain.Enums.UserRole.None;
        }

        /// <summary>
        /// Checks if user has specific role using enum
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="role">Role to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user has the role</returns>
        public async Task<bool> HasRoleAsync(Guid userId, Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            var user = await _dbSet.FindAsync(new object[] { userId }, cancellationToken);
            return user?.HasRole(role) ?? false;
        }

        /// <summary>
        /// Gets user IDs by role enum
        /// </summary>
        /// <param name="role">Role to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user IDs</returns>
        public async Task<IEnumerable<Guid>> GetUserIdsByRoleAsync(Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => (u.Role & role) == role && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Checks if user has specific permission
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="permission">Permission to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if user has the permission</returns>
        public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
        {
            var user = await _dbSet.FindAsync(new object[] { userId }, cancellationToken);
            return user?.HasPermission(permission) ?? false;
        }

        // Domain interface compatibility methods
        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await FindByUserNameAsync(userName);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await FindByEmailAsync(email);
        }

        public async Task<bool> UserNameExistsAsync(string userName)
        {
            return await IsUserNameInUseAsync(userName, null, CancellationToken.None);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            try
            {
                var emailValueObject = Email.Create(email);
                return await _dbSet.AnyAsync(u => u.Email == emailValueObject);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public async Task<User?> GetUserWithRolesAsync(Guid userId)
        {
            // Since we're using enum-based roles, no need for includes
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// Gets users by role with support for Flags enum
        /// </summary>
        /// <param name="roleName">Role name or comma-separated role names</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Users and total count</returns>
        public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersByRoleAsync(string roleName, int pageNumber = 1, int pageSize = 10)
        {
            // Try to parse role name(s) to enum
            var role = Domain.Enums.UserRoleExtensions.FromDisplayString(roleName);

            if (role == Domain.Enums.UserRole.None)
            {
                // Try single role parse as fallback
                if (!Enum.TryParse<Domain.Enums.UserRole>(roleName, true, out role))
                {
                    return (new List<User>(), 0);
                }
            }

            // For Flags enum, we need to check if user has ANY of the specified roles
            var individualRoles = role.GetIndividualRoles();

            var query = _dbSet.Where(u => u.IsActive);

            if (individualRoles.Length == 1)
            {
                // Single role - exact match or HasRole check
                var singleRole = individualRoles[0];
                query = query.Where(u => (u.Role & singleRole) == singleRole);
            }
            else if (individualRoles.Length > 1)
            {
                // Multiple roles - user must have at least one
                query = query.Where(u => individualRoles.Any(r => (u.Role & r) == r));
            }
            else
            {
                // No valid roles
                return (new List<User>(), 0);
            }

            query = query.OrderBy(u => u.UserName);

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        /// <summary>
        /// Gets users by role enum
        /// </summary>
        /// <param name="role">Role to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of users with the role</returns>
        public async Task<IEnumerable<User>> GetUsersByRoleEnumAsync(Domain.Enums.UserRole role, CancellationToken cancellationToken = default)
        {
            var userIds = await GetUserIdsByRoleAsync(role, cancellationToken);
            return await _dbSet.Where(u => userIds.Contains(u.Id)).ToListAsync(cancellationToken);
        }

        public async Task<(IEnumerable<User> Users, int TotalCount)> GetActiveUsersAsync(int days = 30, int pageNumber = 1, int pageSize = 10)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var query = _dbSet.Where(u => u.IsActive && u.LastLoginAt >= cutoffDate);

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.LastLoginAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<bool> IsEmailInUseAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var emailValueObject = Email.Create(email);
                return await _dbSet.AnyAsync(u => u.Email == emailValueObject, cancellationToken);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public async Task<bool> IsEmailInUseAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var emailValueObject = Email.Create(email);
                var query = _dbSet.Where(u => u.Email == emailValueObject);
                if (excludeUserId.HasValue)
                    query = query.Where(u => u.Id != excludeUserId.Value);

                return await query.AnyAsync(cancellationToken);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public async Task<bool> IsUserNameInUseAsync(string userName, CancellationToken cancellationToken = default)
        {
            return await IsUserNameInUseAsync(userName, null, cancellationToken);
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Update(user);
            await SaveChangesAsync(cancellationToken);
        }

        async Task<User> IRepository<User>.UpdateAsync(User user, CancellationToken cancellationToken)
        {
            await UpdateAsync(user, cancellationToken);
            return user;
        }

        /// <summary>
        /// Counts users with Author role or higher
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Count of authors</returns>
        public async Task<int> CountAuthorsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(u =>
                (u.Role & Domain.Enums.UserRole.Author) == Domain.Enums.UserRole.Author &&
                u.IsActive, cancellationToken);
        }

        /// <summary>
        /// Gets active users with Author role or higher
        /// </summary>
        /// <param name="count">Maximum number of users to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of active authors</returns>
        public async Task<IEnumerable<User>> GetActiveAuthorsAsync(int count, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(u => (u.Role & Domain.Enums.UserRole.Author) == Domain.Enums.UserRole.Author && u.IsActive)
                .OrderByDescending(u => u.LastLoginAt)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await GetByIdAsync(id, cancellationToken);
            if (user == null)
                return false;

            await RemoveAsync(id, cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return true;
        }

        // Missing interface implementations for new role-based methods

        /// <summary>
        /// Gets users by role ID (Guid-based)
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of users with the role</returns>
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            // Since we use enum-based roles, we need to convert Guid to role
            // This is a compatibility method - in practice, we'd use the enum-based method
            // For now, return empty list as this method is likely legacy
            return await Task.FromResult(new List<User>());
        }

        /// <summary>
        /// Gets user roles by user ID (returns UserRole entities)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of UserRole entities</returns>
        public async Task<IEnumerable<Domain.Entities.UserRole>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Since we use enum-based roles, this method returns empty list
            // Modern code should use GetUserRolesEnumAsync instead
            return await Task.FromResult(new List<Domain.Entities.UserRole>());
        }

        /// <summary>
        /// Gets specific user role relationship
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="roleId">Role ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UserRole entity if exists</returns>
        public async Task<Domain.Entities.UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            // Since we use enum-based roles, this method returns null
            // Modern code should use HasRoleAsync instead
            return await Task.FromResult<Domain.Entities.UserRole?>(null);
        }
    }
}