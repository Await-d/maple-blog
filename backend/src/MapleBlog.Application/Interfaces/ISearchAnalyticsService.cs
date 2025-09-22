using MapleBlog.Application.DTOs.Search;

namespace MapleBlog.Application.Interfaces;

public interface ISearchAnalyticsService
{
    Task RecordSearchAsync(string searchTerm, string? userId = null, string? ipAddress = null);
    Task<IEnumerable<SearchTermStatsDto>> GetPopularSearchTermsAsync(int count = 20);
    Task<IEnumerable<SearchTermStatsDto>> GetSearchTermsByPeriodAsync(DateTime startDate, DateTime endDate);
    Task<SearchAnalyticsDto> GetSearchAnalyticsAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string partialTerm, int count = 10);
    Task<bool> ClearSearchHistoryAsync(string userId);
    Task<IEnumerable<SearchTermStatsDto>> GetUserSearchHistoryAsync(string userId, int count = 50);
    Task<SearchTrendDto> GetSearchTrendsAsync(int days = 30);

    // Additional methods required by Controllers
    Task<QueryAnalysisResponse> GetQueryAnalysisAsync(string query, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<SearchPerformanceAnalysis> GetPerformanceAnalysisAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<List<SearchOptimizationSuggestion>> GetOptimizationSuggestionsAsync(CancellationToken cancellationToken = default);
    Task<SearchAnalyticsOverview> GetAnalyticsOverviewAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<SearchTrendAnalysis> GetSearchTrendAsync(TrendPeriod period, CancellationToken cancellationToken = default);
}