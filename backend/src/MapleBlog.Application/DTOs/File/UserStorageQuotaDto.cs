namespace MapleBlog.Application.DTOs.File;

/// <summary>
/// User storage quota information
/// </summary>
public class UserStorageQuotaDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Current storage usage in bytes
    /// </summary>
    public long CurrentUsage { get; set; }

    /// <summary>
    /// Maximum storage quota in bytes
    /// </summary>
    public long MaxQuota { get; set; }

    /// <summary>
    /// Number of files uploaded by user
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>
    /// Available space remaining in bytes
    /// </summary>
    public long AvailableSpace { get; set; }

    /// <summary>
    /// Usage percentage (0-100)
    /// </summary>
    public double UsagePercentage { get; set; }

    /// <summary>
    /// Whether user has exceeded their quota
    /// </summary>
    public bool IsQuotaExceeded { get; set; }

    /// <summary>
    /// Human-readable current usage
    /// </summary>
    public string FormattedCurrentUsage => FormatBytes(CurrentUsage);

    /// <summary>
    /// Human-readable max quota
    /// </summary>
    public string FormattedMaxQuota => FormatBytes(MaxQuota);

    /// <summary>
    /// Human-readable available space
    /// </summary>
    public string FormattedAvailableSpace => FormatBytes(AvailableSpace);

    /// <summary>
    /// Quota status message
    /// </summary>
    public string StatusMessage
    {
        get
        {
            if (IsQuotaExceeded)
                return "Quota exceeded";
            if (UsagePercentage >= 90)
                return "Quota almost full";
            if (UsagePercentage >= 75)
                return "High usage";
            return "Normal";
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