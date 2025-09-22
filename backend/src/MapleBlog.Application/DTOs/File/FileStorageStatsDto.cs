namespace MapleBlog.Application.DTOs.File;

/// <summary>
/// File storage statistics data transfer object
/// </summary>
public class FileStorageStatsDto
{
    /// <summary>
    /// Total number of files stored
    /// </summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// Total storage used in bytes
    /// </summary>
    public long TotalStorageUsed { get; set; }

    /// <summary>
    /// Number of image files
    /// </summary>
    public int ImageFiles { get; set; }

    /// <summary>
    /// Storage used by images in bytes
    /// </summary>
    public long ImageStorageUsed { get; set; }

    /// <summary>
    /// Number of document files
    /// </summary>
    public int DocumentFiles { get; set; }

    /// <summary>
    /// Storage used by documents in bytes
    /// </summary>
    public long DocumentStorageUsed { get; set; }

    /// <summary>
    /// Number of other/misc files
    /// </summary>
    public int OtherFiles { get; set; }

    /// <summary>
    /// Storage used by other files in bytes
    /// </summary>
    public long OtherStorageUsed { get; set; }

    /// <summary>
    /// Number of files uploaded today
    /// </summary>
    public int FilesToday { get; set; }

    /// <summary>
    /// Number of files uploaded this week
    /// </summary>
    public int FilesThisWeek { get; set; }

    /// <summary>
    /// Number of files uploaded this month
    /// </summary>
    public int FilesThisMonth { get; set; }

    /// <summary>
    /// Average file size in bytes
    /// </summary>
    public long AverageFileSize => TotalFiles > 0 ? TotalStorageUsed / TotalFiles : 0;

    /// <summary>
    /// Largest file size in bytes
    /// </summary>
    public long LargestFileSize { get; set; }

    /// <summary>
    /// Smallest file size in bytes
    /// </summary>
    public long SmallestFileSize { get; set; }

    /// <summary>
    /// Most common file type
    /// </summary>
    public string? MostCommonFileType { get; set; }

    /// <summary>
    /// File type distribution
    /// </summary>
    public Dictionary<string, int> FileTypeDistribution { get; set; } = new();

    /// <summary>
    /// Monthly upload statistics (last 12 months)
    /// </summary>
    public Dictionary<string, int> MonthlyUploads { get; set; } = new();

    /// <summary>
    /// Storage usage by user (top 10 users)
    /// </summary>
    public Dictionary<string, StorageByUserDto> StorageByUser { get; set; } = new();

    /// <summary>
    /// Human-readable total storage used
    /// </summary>
    public string FormattedTotalStorage
    {
        get
        {
            return FormatBytes(TotalStorageUsed);
        }
    }

    /// <summary>
    /// Human-readable image storage used
    /// </summary>
    public string FormattedImageStorage
    {
        get
        {
            return FormatBytes(ImageStorageUsed);
        }
    }

    /// <summary>
    /// Human-readable document storage used
    /// </summary>
    public string FormattedDocumentStorage
    {
        get
        {
            return FormatBytes(DocumentStorageUsed);
        }
    }

    /// <summary>
    /// Human-readable other storage used
    /// </summary>
    public string FormattedOtherStorage
    {
        get
        {
            return FormatBytes(OtherStorageUsed);
        }
    }

    /// <summary>
    /// Human-readable average file size
    /// </summary>
    public string FormattedAverageSize
    {
        get
        {
            return FormatBytes(AverageFileSize);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Storage statistics by user
/// </summary>
public class StorageByUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Number of files uploaded by user
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Total storage used by user in bytes
    /// </summary>
    public long StorageUsed { get; set; }

    /// <summary>
    /// Human-readable storage used
    /// </summary>
    public string FormattedStorage
    {
        get
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = StorageUsed;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}