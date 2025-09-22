using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Search;
using MapleBlog.Domain.Constants;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.ValueObjects;
using AppSearchAnalyticsService = MapleBlog.Application.Interfaces.ISearchAnalyticsService;
using SearchRequest = MapleBlog.Application.DTOs.Search.SearchRequest;
using SearchHistoryItem = MapleBlog.Application.DTOs.Search.SearchHistoryItem;
using SearchResultItem = MapleBlog.Application.DTOs.Search.SearchResultItem;
using SearchResponse = MapleBlog.Application.DTOs.Search.SearchResponse;
using PopularSearchItem = MapleBlog.Application.DTOs.Search.PopularSearchItem;
using SearchFilters = MapleBlog.Application.DTOs.Search.SearchFilters;
using SearchSort = MapleBlog.Application.DTOs.Search.SearchSort;
using DateRange = MapleBlog.Application.DTOs.Search.DateRange;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 搜索API控制器
/// 提供搜索查询、建议、历史等HTTP API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("SearchPolicy")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly ILogger<SearchController> _logger;
    private readonly ISearchService _searchService;
    private readonly AppSearchAnalyticsService _analyticsService;

    public SearchController(
        ILogger<SearchController> logger,
        ISearchService searchService,
        AppSearchAnalyticsService analyticsService)
    {
        _logger = logger;
        _searchService = searchService;
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// 执行搜索
    /// </summary>
    /// <param name="request">搜索请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SearchResponse>> SearchAsync(
        [FromBody] SearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Search request received for query: {Query}", request.Query);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 构建搜索请求
            var searchRequest = new SearchRequest
            {
                Query = request.Query?.Trim() ?? string.Empty,
                Page = request.Page,
                PageSize = Math.Min(request.PageSize, SearchConstants.Index.MaxPageSize),
                SearchType = request.SearchType,
                UserId = GetCurrentUserId(),
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            // 应用过滤器
            if (request.Filters != null)
            {
                searchRequest.Filters = MapFilters(request.Filters);
            }

            // 应用排序
            if (request.Sort != null)
            {
                searchRequest.Sort = MapSort(request.Sort);
            }

            // 执行搜索
            var response = await _searchService.SearchAsync(searchRequest, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "搜索失败",
                    Detail = response.ErrorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Search completed for query: {Query}, results: {ResultCount}, time: {ExecutionTime}ms",
                request.Query, response.TotalCount, response.ExecutionTime);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "搜索服务错误",
                Detail = "搜索服务暂时不可用，请稍后重试",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取搜索建议
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="count">建议数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索建议列表</returns>
    [HttpGet("suggestions")]
    [ResponseCache(Duration = 900, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> GetSuggestionsAsync(
        [FromQuery] string query,
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "查询关键词不能为空",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (query.Length < SearchConstants.Limits.MinQueryLength)
            {
                return Ok(new List<string>());
            }

            count = Math.Min(Math.Max(count, 1), SearchConstants.Limits.MaxSuggestions);

            var suggestions = await _searchService.GetSuggestionsAsync(query, count, cancellationToken);

            return Ok(suggestions.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for query: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "建议服务错误",
                Detail = "获取搜索建议失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取热门搜索
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="timeRange">时间范围（小时）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>热门搜索列表</returns>
    [HttpGet("popular")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(List<PopularSearchItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PopularSearchItem>>> GetPopularSearchesAsync(
        [FromQuery] int count = 10,
        [FromQuery] int timeRange = 168, // 默认7天
        CancellationToken cancellationToken = default)
    {
        try
        {
            count = Math.Min(Math.Max(count, 1), 50);
            var timeSpan = TimeSpan.FromHours(Math.Max(timeRange, 1));

            var popularSearches = await _searchService.GetPopularSearchesAsync(count, timeSpan, cancellationToken);

            return Ok(popularSearches.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting popular searches");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "热门搜索服务错误",
                Detail = "获取热门搜索失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取用户搜索历史
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索历史列表</returns>
    [HttpGet("history")]
    [Authorize]
    [ProducesResponseType(typeof(List<SearchHistoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SearchHistoryItem>>> GetSearchHistoryAsync(
        [FromQuery] int count = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "未授权",
                    Detail = "需要登录才能查看搜索历史",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            count = Math.Min(Math.Max(count, 1), SearchConstants.Limits.MaxSearchHistory);

            var history = await _searchService.GetSearchHistoryAsync(userId.Value, count, cancellationToken);

            return Ok(history.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search history for user: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "搜索历史服务错误",
                Detail = "获取搜索历史失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 清除用户搜索历史
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("history")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> ClearSearchHistoryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "未授权",
                    Detail = "需要登录才能清除搜索历史",
                    Status = StatusCodes.Status401Unauthorized
                });
            }

            var success = await _searchService.ClearSearchHistoryAsync(userId.Value, cancellationToken);

            return Ok(new ApiResponse
            {
                Success = success,
                Message = success ? "搜索历史已清除" : "清除搜索历史失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing search history for user: {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "服务错误",
                Detail = "清除搜索历史失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取个性化推荐
    /// </summary>
    /// <param name="baseQuery">基础查询</param>
    /// <param name="count">数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐内容列表</returns>
    [HttpGet("recommendations")]
    [Authorize]
    [ProducesResponseType(typeof(List<SearchResultItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SearchResultItem>>> GetRecommendationsAsync(
        [FromQuery] string? baseQuery = null,
        [FromQuery] int count = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Ok(new List<SearchResultItem>());
            }

            count = Math.Min(Math.Max(count, 1), 20);

            var recommendations = await _searchService.GetRecommendationsAsync(userId.Value, baseQuery, cancellationToken);

            return Ok(recommendations.Take(count).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations for user: {UserId}", GetCurrentUserId());
            return Ok(new List<SearchResultItem>());
        }
    }

    /// <summary>
    /// 记录搜索点击
    /// </summary>
    /// <param name="request">点击记录请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("click")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> RecordSearchClickAsync(
        [FromBody] SearchClickRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _searchService.RecordSearchClickAsync(
                request.SearchId,
                request.ResultId,
                request.Position,
                cancellationToken);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = "点击记录成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search click");
            return Ok(new ApiResponse
            {
                Success = false,
                Message = "点击记录失败"
            });
        }
    }

    /// <summary>
    /// 获取搜索分析概览
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索分析数据</returns>
    [HttpGet("analytics/overview")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SearchAnalyticsOverview), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SearchAnalyticsOverview>> GetAnalyticsOverviewAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var overview = await _analyticsService.GetAnalyticsOverviewAsync(startDate, endDate, cancellationToken);
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search analytics overview");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "分析服务错误",
                Detail = "获取搜索分析数据失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取搜索趋势分析
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="period">时间粒度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>趋势分析数据</returns>
    [HttpGet("analytics/trend")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SearchTrendAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchTrendAnalysis>> GetSearchTrendAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] TrendPeriod period = TrendPeriod.Daily,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trendAnalysis = await _analyticsService.GetSearchTrendAsync(period, cancellationToken);
            return Ok(trendAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search trend analysis");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "分析服务错误",
                Detail = "获取搜索趋势分析失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取查询分析
    /// </summary>
    /// <param name="query">查询关键词</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询分析数据</returns>
    [HttpGet("analytics/query")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(QueryAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<QueryAnalysisResponse>> GetQueryAnalysisAsync(
        [FromQuery] string? query = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryAnalysis = await _analyticsService.GetQueryAnalysisAsync(query, startDate, endDate, cancellationToken);
            return Ok(queryAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting query analysis for: {Query}", query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "分析服务错误",
                Detail = "获取查询分析失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取搜索性能分析
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>性能分析数据</returns>
    [HttpGet("analytics/performance")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SearchPerformanceAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchPerformanceAnalysis>> GetPerformanceAnalysisAsync(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var performanceAnalysis = await _analyticsService.GetPerformanceAnalysisAsync(startDate, endDate, cancellationToken);
            return Ok(performanceAnalysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search performance analysis");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "分析服务错误",
                Detail = "获取性能分析失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取搜索优化建议
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>优化建议列表</returns>
    [HttpGet("analytics/suggestions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<SearchOptimizationSuggestion>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SearchOptimizationSuggestion>>> GetOptimizationSuggestionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var suggestions = await _analyticsService.GetOptimizationSuggestionsAsync(cancellationToken);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting optimization suggestions");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "分析服务错误",
                Detail = "获取优化建议失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    #region 私有方法

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress()
    {
        // 检查X-Forwarded-For头（代理/负载均衡器）
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').FirstOrDefault()?.Trim();
        }

        // 检查X-Real-IP头
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 使用RemoteIpAddress
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// 映射过滤器
    /// </summary>
    private SearchFilters MapFilters(SearchFiltersDto filtersDto)
    {
        return new SearchFilters
        {
            ContentTypes = filtersDto.ContentTypes ?? new List<string>(),
            Categories = filtersDto.CategoryIds?.Select(g => g.ToString()).ToList() ?? new List<string>(),
            Tags = filtersDto.TagIds?.Select(g => g.ToString()).ToList() ?? new List<string>(),
            Authors = filtersDto.AuthorIds?.Select(g => g.ToString()).ToList() ?? new List<string>(),
            DateRange = (filtersDto.StartDate.HasValue || filtersDto.EndDate.HasValue) ? new DateRange
            {
                From = filtersDto.StartDate,
                To = filtersDto.EndDate
            } : null
        };
    }

    /// <summary>
    /// 映射排序
    /// </summary>
    private SearchSort MapSort(SearchSortDto sortDto)
    {
        return new SearchSort
        {
            Field = sortDto.Field,
            Direction = sortDto.Direction
        };
    }

    #endregion
}

#region DTO类

/// <summary>
/// 搜索请求DTO
/// </summary>
public class SearchRequestDto
{
    /// <summary>
    /// 查询关键词
    /// </summary>
    [Required(ErrorMessage = "搜索关键词不能为空")]
    [StringLength(SearchConstants.Limits.MaxQueryLength, MinimumLength = SearchConstants.Limits.MinQueryLength, ErrorMessage = "查询长度必须在{2}-{1}个字符之间")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 页码
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    [Range(1, SearchConstants.Index.MaxPageSize, ErrorMessage = "每页大小必须在1-{2}之间")]
    public int PageSize { get; set; } = SearchConstants.Index.DefaultPageSize;

    /// <summary>
    /// 搜索类型
    /// </summary>
    public string? SearchType { get; set; }

    /// <summary>
    /// 过滤条件
    /// </summary>
    public SearchFiltersDto? Filters { get; set; }

    /// <summary>
    /// 排序条件
    /// </summary>
    public SearchSortDto? Sort { get; set; }
}

/// <summary>
/// 搜索过滤器DTO
/// </summary>
public class SearchFiltersDto
{
    /// <summary>
    /// 内容类型
    /// </summary>
    public List<string>? ContentTypes { get; set; }

    /// <summary>
    /// 分类ID列表
    /// </summary>
    public List<Guid>? CategoryIds { get; set; }

    /// <summary>
    /// 标签ID列表
    /// </summary>
    public List<Guid>? TagIds { get; set; }

    /// <summary>
    /// 作者ID列表
    /// </summary>
    public List<Guid>? AuthorIds { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 最小分数
    /// </summary>
    [Range(0, float.MaxValue, ErrorMessage = "最小分数必须大于等于0")]
    public float? MinScore { get; set; }
}

/// <summary>
/// 搜索排序DTO
/// </summary>
public class SearchSortDto
{
    /// <summary>
    /// 排序字段
    /// </summary>
    [Required(ErrorMessage = "排序字段不能为空")]
    public string Field { get; set; } = SearchConstants.SortFields.Relevance;

    /// <summary>
    /// 排序方向
    /// </summary>
    public string Direction { get; set; } = SearchConstants.SortDirections.Descending;
}

/// <summary>
/// 搜索点击记录DTO
/// </summary>
public class SearchClickRequestDto
{
    /// <summary>
    /// 搜索ID
    /// </summary>
    [Required(ErrorMessage = "搜索ID不能为空")]
    public Guid SearchId { get; set; }

    /// <summary>
    /// 结果ID
    /// </summary>
    [Required(ErrorMessage = "结果ID不能为空")]
    public Guid ResultId { get; set; }

    /// <summary>
    /// 结果类型
    /// </summary>
    [Required(ErrorMessage = "结果类型不能为空")]
    public string ResultType { get; set; } = string.Empty;

    /// <summary>
    /// 点击位置
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "点击位置必须大于0")]
    public int Position { get; set; }
}


#endregion