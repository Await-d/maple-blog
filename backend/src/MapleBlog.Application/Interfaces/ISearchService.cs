using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Search;

namespace MapleBlog.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto<PostListDto>> SearchPostsAsync(SearchRequestDto searchRequest);
    Task<SearchResultDto<object>> SearchAllAsync(SearchRequestDto searchRequest);
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10);
    Task<bool> IndexPostAsync(Guid postId);
    Task<bool> RemoveFromIndexAsync(Guid postId);
    Task<bool> UpdateIndexAsync(Guid postId);
    Task<bool> RebuildIndexAsync();
    Task<SearchStatsDto> GetSearchStatsAsync();
    Task<IEnumerable<string>> GetPopularSearchTermsAsync(int count = 20);

    // Additional methods required by SearchController
    Task<MapleBlog.Application.DTOs.Search.SearchResponse> SearchAsync(MapleBlog.Application.DTOs.Search.SearchRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetSuggestionsAsync(string query, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapleBlog.Application.DTOs.Search.PopularSearchItem>> GetPopularSearchesAsync(int count, TimeSpan timeSpan, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapleBlog.Application.DTOs.Search.SearchHistoryItem>> GetSearchHistoryAsync(Guid userId, int count, CancellationToken cancellationToken = default);
    Task<bool> ClearSearchHistoryAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapleBlog.Application.DTOs.Search.SearchResultItem>> GetRecommendationsAsync(Guid userId, string? baseQuery, CancellationToken cancellationToken = default);
    Task RecordSearchClickAsync(Guid searchId, Guid resultId, int position, CancellationToken cancellationToken = default);
}