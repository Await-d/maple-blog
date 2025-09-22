using Microsoft.EntityFrameworkCore;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Infrastructure.Data;
using DomainFile = MapleBlog.Domain.Entities.File;

namespace MapleBlog.Infrastructure.Repositories
{
    /// <summary>
    /// Repository implementation for File entities
    /// </summary>
    public class FileRepository : BlogBaseRepository<DomainFile>, IFileRepository
    {
        public FileRepository(BlogDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Gets files for a specific user
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets files by directory
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetByDirectoryAsync(string directory, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(f => f.Directory == directory && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets file by hash (for deduplication)
        /// </summary>
        public async Task<DomainFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .FirstOrDefaultAsync(f => f.FileHash == fileHash && !f.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Gets file by file path
        /// </summary>
        public async Task<DomainFile?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await Context.Files
                .FirstOrDefaultAsync(f => f.FilePath == filePath && !f.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Gets orphaned files (not in use)
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetOrphanedFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            return await Context.Files
                .Where(f => !f.IsInUse &&
                           !f.IsDeleted &&
                           f.CreatedAt <= cutoffDate &&
                           f.ReferenceCount == 0)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        public async Task<(long TotalSize, int TotalFiles, Dictionary<string, long> SizeByDirectory, Dictionary<string, int> FileCountByType)> GetStorageStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var files = await Context.Files
                .Where(f => !f.IsDeleted)
                .Select(f => new { f.FileSize, f.Directory, f.ContentType })
                .ToListAsync(cancellationToken);

            var totalSize = files.Sum(f => f.FileSize);
            var totalFiles = files.Count;

            var sizeByDirectory = files
                .GroupBy(f => f.Directory)
                .ToDictionary(g => g.Key, g => g.Sum(f => f.FileSize));

            var fileCountByType = files
                .GroupBy(f => f.ContentType)
                .ToDictionary(g => g.Key, g => g.Count());

            return (totalSize, totalFiles, sizeByDirectory, fileCountByType);
        }

        /// <summary>
        /// Updates file reference count
        /// </summary>
        public async Task<int> UpdateReferenceCountAsync(Guid fileId, int increment, CancellationToken cancellationToken = default)
        {
            var file = await Context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
                return 0;

            file.ReferenceCount = Math.Max(0, file.ReferenceCount + increment);
            file.IsInUse = file.ReferenceCount > 0;
            file.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);
            return file.ReferenceCount;
        }

        /// <summary>
        /// Updates file usage status
        /// </summary>
        public async Task UpdateUsageStatusAsync(Guid fileId, bool isInUse, CancellationToken cancellationToken = default)
        {
            var file = await Context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
                return;

            file.IsInUse = isInUse;
            file.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Records file access
        /// </summary>
        public async Task RecordFileAccessAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            var file = await Context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
                return;

            file.AccessCount++;
            file.LastAccessedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets files by content type
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetByContentTypeAsync(string contentType, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(f => !f.IsDeleted);

            // Support wildcards in content type
            if (contentType.Contains('*'))
            {
                var pattern = contentType.Replace("*", "%");
                query = query.Where(f => EF.Functions.Like(f.ContentType, pattern));
            }
            else
            {
                query = query.Where(f => f.ContentType == contentType);
            }

            return await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Searches files by filename or description
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> SearchFilesAsync(string searchTerm, Guid? userId = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(f => !f.IsDeleted &&
                           (f.OriginalFileName.Contains(searchTerm) ||
                            f.FileName.Contains(searchTerm) ||
                            (f.Description != null && f.Description.Contains(searchTerm)) ||
                            (f.Tags != null && f.Tags.Contains(searchTerm))));

            if (userId.HasValue)
            {
                query = query.Where(f => f.UserId == userId.Value);
            }

            return await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all user files without pagination
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Gets user file count for pagination
        /// </summary>
        public async Task<int> GetUserFileCountAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(f => f.UserId == userId && !f.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Gets total file count for pagination
        /// </summary>
        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(f => !f.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// Gets user storage usage
        /// </summary>
        public async Task<long> GetUserStorageUsageAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var result = await _dbSet
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .SumAsync(f => f.FileSize, cancellationToken);

            return result;
        }

        /// <summary>
        /// Gets files created within date range
        /// </summary>
        public async Task<IReadOnlyList<DomainFile>> GetFilesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(f => !f.IsDeleted && f.CreatedAt >= startDate && f.CreatedAt <= endDate)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        protected BlogDbContext Context => (BlogDbContext)_context;
    }
}