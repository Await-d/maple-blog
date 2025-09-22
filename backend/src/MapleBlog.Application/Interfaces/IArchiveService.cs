using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Archive;

namespace MapleBlog.Application.Interfaces;

public interface IArchiveService
{
    Task<ArchiveDto> GetArchiveByMonthAsync(int year, int month);
    Task<ArchiveDto> GetArchiveByYearAsync(int year);
    Task<IEnumerable<ArchiveItemDto>> GetArchiveTimelineAsync();
    Task<IEnumerable<PostListDto>> GetPostsByArchivePeriodAsync(int year, int month);
    Task<ArchiveStatsDto> GetArchiveStatsAsync();
    Task<IEnumerable<ArchiveItemDto>> GetPopularArchivePeriodsAsync(int count = 10);
    Task<bool> RefreshArchiveCacheAsync();
    Task<IEnumerable<ArchiveItemDto>> SearchArchiveAsync(string searchTerm);

    // 新增缺失的方法
    Task<CategoryArchiveResponse> GetCategoryArchiveAsync(CategoryArchiveRequest request, CancellationToken cancellationToken = default);
    Task<TagArchiveResponse> GetTagArchiveAsync(TagArchiveRequest request, CancellationToken cancellationToken = default);
    Task<ArchiveNavigationResponse> GetArchiveNavigationAsync(CancellationToken cancellationToken = default);

    // Controller期望的额外方法
    Task<TimeArchiveResponse> GetTimeArchiveAsync(TimeArchiveRequest request, CancellationToken cancellationToken = default);
    Task<ArchiveStatsResponse> GetArchiveStatsAsync(CancellationToken cancellationToken);
    Task<ArchiveSearchResponse> SearchArchiveAsync(ArchiveSearchRequest request, CancellationToken cancellationToken = default);
}