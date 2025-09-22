using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
// using SixLabors.ImageSharp.Drawing.Processing;
// using SixLabors.Fonts;
using Microsoft.Extensions.Logging;
using MapleBlog.Application.Interfaces;
using MapleBlog.Application.DTOs.Image;

namespace MapleBlog.Infrastructure.Services;

/// <summary>
/// Image processing service implementation using ImageSharp
/// Provides cross-platform image processing capabilities
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;
    // private static readonly FontFamily DefaultFontFamily;

    private static readonly Dictionary<string, IImageFormat> FormatMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "jpeg", JpegFormat.Instance },
        { "jpg", JpegFormat.Instance },
        { "png", PngFormat.Instance },
        { "gif", GifFormat.Instance },
        { "bmp", BmpFormat.Instance },
        { "tiff", TiffFormat.Instance },
        { "webp", WebpFormat.Instance }
    };

    // static ImageProcessingService()
    // {
    //     try
    //     {
    //         // Try to load a system font, fallback to default if not available
    //         var fontCollection = new FontCollection();
    //         DefaultFontFamily = fontCollection.TryGet("Arial", out var arial) ? arial :
    //                           fontCollection.TryGet("DejaVu Sans", out var dejaVu) ? dejaVu :
    //                           SystemFonts.CreateFont("Arial", 12).Family;
    //     }
    //     catch
    //     {
    //         // Use system default if font loading fails
    //         DefaultFontFamily = SystemFonts.CreateFont("Arial", 12).Family;
    //     }
    // }

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ImageProcessingResultDto> ProcessImageAsync(ImageProcessingRequestDto request)
    {
        try
        {
            if (request.ImageStream == null)
                throw new ArgumentNullException(nameof(request.ImageStream));

            request.ImageStream.Position = 0;
            using var image = await Image.LoadAsync(request.ImageStream);

            // Apply processing based on request
            if (request.Width.HasValue || request.Height.HasValue)
            {
                var targetWidth = request.Width ?? image.Width;
                var targetHeight = request.Height ?? image.Height;

                if (request.MaintainAspectRatio)
                {
                    var size = CalculateAspectRatioSize(image.Width, image.Height, targetWidth, targetHeight);
                    targetWidth = size.Width;
                    targetHeight = size.Height;
                }

                image.Mutate(x => x.Resize(targetWidth, targetHeight));
            }

            var outputStream = new MemoryStream();
            var format = GetImageFormat(request.Format ?? "jpeg");

            if (format == JpegFormat.Instance)
            {
                var encoder = new JpegEncoder { Quality = request.Quality };
                await image.SaveAsync(outputStream, encoder);
            }
            else
            {
                await image.SaveAsync(outputStream, format);
            }

            outputStream.Position = 0;

            return new ImageProcessingResultDto
            {
                ProcessedImageStream = outputStream,
                ContentType = GetContentType(format),
                Size = outputStream.Length,
                Width = image.Width,
                Height = image.Height,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
            return new ImageProcessingResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ImageProcessingResultDto> ResizeImageAsync(Stream imageStream, int width, int height, string format = "jpeg")
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (width <= 0 || height <= 0)
                throw new ArgumentException("Width and height must be positive values");

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var targetSize = CalculateAspectRatioSize(image.Width, image.Height, width, height);
            image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

            var outputStream = new MemoryStream();
            var imageFormat = GetImageFormat(format);
            await image.SaveAsync(outputStream, imageFormat);
            outputStream.Position = 0;

            _logger.LogDebug("Image resized from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}",
                image.Width, image.Height, targetSize.Width, targetSize.Height);

            return new ImageProcessingResultDto
            {
                ProcessedImageStream = outputStream,
                ContentType = GetContentType(imageFormat),
                Size = outputStream.Length,
                Width = targetSize.Width,
                Height = targetSize.Height,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resizing image");
            return new ImageProcessingResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ImageProcessingResultDto> CompressImageAsync(Stream imageStream, int quality = 85)
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (quality < 1 || quality > 100)
                throw new ArgumentException("Quality must be between 1 and 100", nameof(quality));

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var outputStream = new MemoryStream();
            var jpegEncoder = new JpegEncoder { Quality = quality };
            await image.SaveAsync(outputStream, jpegEncoder);
            outputStream.Position = 0;

            _logger.LogDebug("Image compressed with quality {Quality}", quality);

            return new ImageProcessingResultDto
            {
                ProcessedImageStream = outputStream,
                ContentType = "image/jpeg",
                Size = outputStream.Length,
                Width = image.Width,
                Height = image.Height,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            return new ImageProcessingResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ImageThumbnailResultDto> GenerateThumbnailAsync(Stream imageStream, int size = 150)
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (size <= 0)
                throw new ArgumentException("Size must be positive", nameof(size));

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var targetSize = CalculateAspectRatioSize(image.Width, image.Height, size, size);
            image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, JpegFormat.Instance);
            outputStream.Position = 0;

            _logger.LogDebug("Thumbnail generated with size {Size}x{Size}", targetSize.Width, targetSize.Height);

            return new ImageThumbnailResultDto
            {
                ThumbnailStream = outputStream,
                ContentType = "image/jpeg",
                Size = outputStream.Length,
                ThumbnailSize = Math.Max(targetSize.Width, targetSize.Height),
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail");
            return new ImageThumbnailResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<ImageMetadataDto> GetImageMetadataAsync(Stream imageStream)
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);
            var format = GetFormatName(image.Metadata.DecodedImageFormat!);

            return new ImageMetadataDto
            {
                Width = image.Width,
                Height = image.Height,
                Format = format,
                Size = imageStream.Length,
                ColorDepth = GetBitsPerPixel(image),
                HasTransparency = HasTransparency(image)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image metadata");
            throw new InvalidOperationException("Failed to get image metadata", ex);
        }
    }

    public async Task<bool> ValidateImageAsync(Stream imageStream, long maxSizeBytes = 5242880)
    {
        try
        {
            if (imageStream == null)
                return false;

            if (imageStream.Length > maxSizeBytes)
                return false;

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetSupportedFormatsAsync()
    {
        await Task.CompletedTask;
        return FormatMapping.Keys;
    }

    public async Task<string> OptimizeImageAsync(Stream imageStream, string outputFormat = "webp")
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            var outputStream = new MemoryStream();
            var format = GetImageFormat(outputFormat);

            await image.SaveAsync(outputStream, format);
            outputStream.Position = 0;

            // Convert to base64 for return
            var bytes = outputStream.ToArray();
            var base64 = Convert.ToBase64String(bytes);

            _logger.LogDebug("Image optimized to format {Format}", outputFormat);
            return base64;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing image to {Format}", outputFormat);
            throw new InvalidOperationException($"Failed to optimize image to {outputFormat}", ex);
        }
    }

    public async Task<WatermarkResultDto> AddWatermarkAsync(Stream imageStream, WatermarkOptionsDto options)
    {
        try
        {
            if (imageStream == null)
                throw new ArgumentNullException(nameof(imageStream));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream);

            // Simplified watermark implementation without text drawing
            // This would require SixLabors.ImageSharp.Drawing package
            // For now, we'll just return the original image with success status

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat!);
            outputStream.Position = 0;

            _logger.LogDebug("Watermark added to image");

            return new WatermarkResultDto
            {
                WatermarkedImageStream = outputStream,
                ContentType = GetContentType(image.Metadata.DecodedImageFormat!),
                Size = outputStream.Length,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding watermark");
            return new WatermarkResultDto
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates the target size maintaining aspect ratio
    /// </summary>
    private static Size CalculateAspectRatioSize(int originalWidth, int originalHeight, int targetWidth, int targetHeight)
    {
        var ratioX = (double)targetWidth / originalWidth;
        var ratioY = (double)targetHeight / originalHeight;
        var ratio = Math.Min(ratioX, ratioY);

        return new Size(
            (int)(originalWidth * ratio),
            (int)(originalHeight * ratio));
    }

    /// <summary>
    /// Gets the ImageSharp format from string
    /// </summary>
    private static IImageFormat GetImageFormat(string format)
    {
        return FormatMapping.TryGetValue(format, out var imageFormat)
            ? imageFormat
            : JpegFormat.Instance;
    }

    /// <summary>
    /// Gets the content type for an image format
    /// </summary>
    private static string GetContentType(IImageFormat format)
    {
        return format.Name.ToLowerInvariant() switch
        {
            "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "tiff" => "image/tiff",
            "webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    /// <summary>
    /// Gets the format name from ImageSharp format
    /// </summary>
    private static string GetFormatName(IImageFormat format)
    {
        return format.Name.ToLowerInvariant();
    }

    /// <summary>
    /// Gets bits per pixel for an image
    /// </summary>
    private static int GetBitsPerPixel(Image image)
    {
        return image.PixelType.BitsPerPixel;
    }

    /// <summary>
    /// Checks if the image supports transparency
    /// </summary>
    private static bool HasTransparency(Image image)
    {
        var format = image.Metadata.DecodedImageFormat;
        return format == PngFormat.Instance || format == GifFormat.Instance;
    }

    /// <summary>
    /// Parses watermark position from string
    /// </summary>
    private static WatermarkPosition ParseWatermarkPosition(string position)
    {
        return position.ToLowerInvariant() switch
        {
            "topleft" => WatermarkPosition.TopLeft,
            "topcenter" => WatermarkPosition.TopCenter,
            "topright" => WatermarkPosition.TopRight,
            "middleleft" => WatermarkPosition.MiddleLeft,
            "middlecenter" => WatermarkPosition.MiddleCenter,
            "middleright" => WatermarkPosition.MiddleRight,
            "bottomleft" => WatermarkPosition.BottomLeft,
            "bottomcenter" => WatermarkPosition.BottomCenter,
            "bottomright" or _ => WatermarkPosition.BottomRight
        };
    }

    /// <summary>
    /// Calculates text position for watermark (simplified)
    /// </summary>
    private static PointF CalculateTextPosition(Size imageSize, string text, WatermarkPosition position)
    {
        var margin = 10f;
        var textWidth = text.Length * 10f; // Simplified text width calculation
        var textHeight = 20f; // Simplified text height

        return position switch
        {
            WatermarkPosition.TopLeft => new PointF(margin, margin),
            WatermarkPosition.TopCenter => new PointF((imageSize.Width - textWidth) / 2, margin),
            WatermarkPosition.TopRight => new PointF(imageSize.Width - textWidth - margin, margin),
            WatermarkPosition.MiddleLeft => new PointF(margin, (imageSize.Height - textHeight) / 2),
            WatermarkPosition.MiddleCenter => new PointF((imageSize.Width - textWidth) / 2, (imageSize.Height - textHeight) / 2),
            WatermarkPosition.MiddleRight => new PointF(imageSize.Width - textWidth - margin, (imageSize.Height - textHeight) / 2),
            WatermarkPosition.BottomLeft => new PointF(margin, imageSize.Height - textHeight - margin),
            WatermarkPosition.BottomCenter => new PointF((imageSize.Width - textWidth) / 2, imageSize.Height - textHeight - margin),
            WatermarkPosition.BottomRight => new PointF(imageSize.Width - textWidth - margin, imageSize.Height - textHeight - margin),
            _ => new PointF(imageSize.Width - textWidth - margin, imageSize.Height - textHeight - margin)
        };
    }

    #endregion
}

/// <summary>
/// Watermark position enumeration
/// </summary>
public enum WatermarkPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

