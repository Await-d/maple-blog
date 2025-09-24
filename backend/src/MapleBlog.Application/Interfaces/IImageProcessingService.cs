using MapleBlog.Application.DTOs.Image;

namespace MapleBlog.Application.Interfaces;

public interface IImageProcessingService
{
    Task<ImageProcessingResultDto> ProcessImageAsync(ImageProcessingRequestDto request);
    Task<ImageProcessingResultDto> ResizeImageAsync(Stream imageStream, int width, int height, string format = "jpeg");
    Task<ImageProcessingResultDto> CompressImageAsync(Stream imageStream, int quality = 85);
    Task<ImageThumbnailResultDto> GenerateThumbnailAsync(Stream imageStream, int size = 150);
    Task<ImageMetadataDto> GetImageMetadataAsync(Stream imageStream);
    Task<bool> ValidateImageAsync(Stream imageStream, long maxSizeBytes = 5242880); // 5MB default
    Task<IEnumerable<string>> GetSupportedFormatsAsync();
    Task<string> OptimizeImageAsync(Stream imageStream, string outputFormat = "webp");
    Task<WatermarkResultDto> AddWatermarkAsync(Stream imageStream, WatermarkOptionsDto options);

    Task<ImageProcessingResultDto> ConvertImageFormatAsync(Stream imageStream, string targetFormat, CancellationToken cancellationToken = default);
}