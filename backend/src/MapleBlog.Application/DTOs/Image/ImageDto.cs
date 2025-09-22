namespace MapleBlog.Application.DTOs.Image;

public class ImageProcessingRequestDto
{
    public Stream ImageStream { get; set; } = null!;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Format { get; set; }
    public int Quality { get; set; } = 85;
    public bool MaintainAspectRatio { get; set; } = true;
}

public class ImageProcessingResultDto
{
    public Stream ProcessedImageStream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class ImageThumbnailResultDto
{
    public Stream ThumbnailStream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public int ThumbnailSize { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class ImageMetadataDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public long Size { get; set; }
    public int ColorDepth { get; set; }
    public bool HasTransparency { get; set; }
}

public class WatermarkOptionsDto
{
    public string Text { get; set; } = string.Empty;
    public string Position { get; set; } = "BottomRight";
    public float Opacity { get; set; } = 0.5f;
    public int FontSize { get; set; } = 12;
    public string FontColor { get; set; } = "#FFFFFF";
}

public class WatermarkResultDto
{
    public Stream WatermarkedImageStream { get; set; } = null!;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}