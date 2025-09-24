using MapleBlog.Domain.Entities;
using DomainFile = MapleBlog.Domain.Entities.File;

namespace MapleBlog.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for File entities
    /// </summary>
    public interface IFileRepository : IRepository<DomainFile>
    {
        /// <summary>
        /// Gets files for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user files</returns>
        Task<IReadOnlyList<DomainFile>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files by directory
        /// </summary>
        /// <param name="directory">Directory name</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of files in directory</returns>
        Task<IReadOnlyList<DomainFile>> GetByDirectoryAsync(string directory, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file by hash (for deduplication)
        /// </summary>
        /// <param name="fileHash">File hash</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File with matching hash</returns>
        Task<DomainFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file by file path
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File with matching path</returns>
        Task<DomainFile?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets orphaned files (not in use)
        /// </summary>
        /// <param name="olderThanDays">Files older than specified days</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of orphaned files</returns>
        Task<IReadOnlyList<DomainFile>> GetOrphanedFilesAsync(int olderThanDays = 30, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage statistics</returns>
        Task<(long TotalSize, int TotalFiles, Dictionary<string, long> SizeByDirectory, Dictionary<string, int> FileCountByType)> GetStorageStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates file reference count
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="increment">Increment or decrement amount</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated reference count</returns>
        Task<int> UpdateReferenceCountAsync(Guid fileId, int increment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a file entity
        /// </summary>
        Task<DomainFile> UpdateAsync(DomainFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total file size in bytes
        /// </summary>
        Task<long> GetTotalFileSizeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts today's uploads
        /// </summary>
        Task<int> CountTodayUploadsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts files by content type
        /// </summary>
        Task<int> CountByContentTypeAsync(string contentType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates file usage status
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="isInUse">Whether file is in use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateUsageStatusAsync(Guid fileId, bool isInUse, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records file access
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RecordFileAccessAsync(Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files by content type
        /// </summary>
        /// <param name="contentType">Content type pattern (supports wildcards)</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of files matching content type</returns>
        Task<IReadOnlyList<DomainFile>> GetByContentTypeAsync(string contentType, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches files by filename or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <param name="userId">Optional user ID filter</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching files</returns>
        Task<IReadOnlyList<DomainFile>> SearchFilesAsync(string searchTerm, Guid? userId = null, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all user files (without pagination)
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of user files</returns>
        Task<IReadOnlyList<DomainFile>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user file count for pagination
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count of user files</returns>
        Task<int> GetUserFileCountAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets total file count for pagination
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total count of files</returns>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user storage usage
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total storage used in bytes</returns>
        Task<long> GetUserStorageUsageAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets files created within date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Files created within date range</returns>
        Task<IReadOnlyList<DomainFile>> GetFilesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets used storage space in bytes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Used storage space in bytes</returns>
        Task<long> GetUsedSpaceAsync(CancellationToken cancellationToken = default);
    }
}