using MapleBlog.Application.Interfaces;
using MapleBlog.Application.Services;
using MapleBlog.Application.DTOs.Archive;
using MapleBlog.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.API.Controllers;

/// <summary>
/// 归档API控制器
/// 提供内容归档和浏览的HTTP API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("ArchivePolicy")]
[Produces("application/json")]
public class ArchiveController : ControllerBase
{
    private readonly ILogger<ArchiveController> _logger;
    private readonly IArchiveService _archiveService;

    public ArchiveController(
        ILogger<ArchiveController> logger,
        IArchiveService archiveService)
    {
        _logger = logger;
        _archiveService = archiveService;
    }

    /// <summary>
    /// 获取时间归档
    /// </summary>
    /// <param name="archiveType">归档类型</param>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <param name="day">日期</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>时间归档数据</returns>
    [HttpGet("time")]
    [ResponseCache(Duration = 1800, VaryByQueryKeys = new[] { "*" }, VaryByHeader = "Accept,Accept-Language")]
    [ProducesResponseType(typeof(TimeArchiveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TimeArchiveResponse>> GetTimeArchiveAsync(
        [FromQuery] ArchiveType archiveType = ArchiveType.Month,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int? day = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 参数验证
            if (year.HasValue && (year < 2000 || year > DateTime.Now.Year + 1))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "年份参数无效",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (month.HasValue && (month < 1 || month > 12))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "月份参数无效",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (day.HasValue && (day < 1 || day > 31))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "日期参数无效",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            page = Math.Max(page, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 100);

            var request = new TimeArchiveRequest
            {
                ArchiveType = archiveType,
                Year = year,
                Month = month,
                Day = day,
                Page = page,
                PageSize = pageSize
            };

            var response = await _archiveService.GetTimeArchiveAsync(request, cancellationToken);

            _logger.LogInformation("Time archive requested: {ArchiveType} {Year}/{Month}/{Day}, page: {Page}",
                archiveType, year, month, day, page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting time archive for {ArchiveType} {Year}/{Month}/{Day}",
                archiveType, year, month, day);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取时间归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取分类归档
    /// </summary>
    /// <param name="categoryId">分类ID</param>
    /// <param name="includeChildren">是否包含子分类</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类归档数据</returns>
    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(typeof(CategoryArchiveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryArchiveResponse>> GetCategoryArchiveAsync(
        [FromRoute] Guid categoryId,
        [FromQuery] bool includeChildren = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (categoryId == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "分类ID无效",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            page = Math.Max(page, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 100);

            var request = new CategoryArchiveRequest
            {
                CategoryId = categoryId,
                IncludeChildren = includeChildren,
                Page = page,
                PageSize = pageSize
            };

            var response = await _archiveService.GetCategoryArchiveAsync(request, cancellationToken);

            if (string.IsNullOrEmpty(response.CategoryName))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "资源不存在",
                    Detail = "指定的分类不存在",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Category archive requested: {CategoryId}, includeChildren: {IncludeChildren}, page: {Page}",
                categoryId, includeChildren, page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category archive for category: {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取分类归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取标签归档
    /// </summary>
    /// <param name="tagId">标签ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签归档数据</returns>
    [HttpGet("tag/{tagId}")]
    [ProducesResponseType(typeof(TagArchiveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagArchiveResponse>> GetTagArchiveAsync(
        [FromRoute] Guid tagId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tagId == Guid.Empty)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "参数错误",
                    Detail = "标签ID无效",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            page = Math.Max(page, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 100);

            var request = new TagArchiveRequest
            {
                TagId = tagId,
                Page = page,
                PageSize = pageSize
            };

            var response = await _archiveService.GetTagArchiveAsync(request, cancellationToken);

            if (string.IsNullOrEmpty(response.TagName))
            {
                return NotFound(new ProblemDetails
                {
                    Title = "资源不存在",
                    Detail = "指定的标签不存在",
                    Status = StatusCodes.Status404NotFound
                });
            }

            _logger.LogInformation("Tag archive requested: {TagId}, page: {Page}", tagId, page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag archive for tag: {TagId}", tagId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取标签归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取归档统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>归档统计数据</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ArchiveStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ArchiveStatsResponse>> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _archiveService.GetArchiveStatsAsync(cancellationToken);

            _logger.LogDebug("Archive stats requested");

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取归档统计失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取归档导航
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>归档导航数据</returns>
    [HttpGet("navigation")]
    [ProducesResponseType(typeof(ArchiveNavigationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ArchiveNavigationResponse>> GetArchiveNavigationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var navigation = await _archiveService.GetArchiveNavigationAsync(cancellationToken);

            _logger.LogDebug("Archive navigation requested");

            return Ok(navigation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive navigation");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取归档导航失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 搜索归档内容
    /// </summary>
    /// <param name="request">归档搜索请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>归档搜索结果</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(ArchiveSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArchiveSearchResponse>> SearchArchiveAsync(
        [FromBody] ArchiveSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var searchRequest = new ArchiveSearchRequest
            {
                Query = request.Query?.Trim(),
                CategoryId = request.CategoryId,
                TagIds = request.TagIds,
                AuthorId = request.AuthorId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SortBy = request.SortBy ?? "date",
                SortDirection = request.SortDirection ?? "desc",
                Page = Math.Max(request.Page, 1),
                PageSize = Math.Min(Math.Max(request.PageSize, 1), 100)
            };

            var response = await _archiveService.SearchArchiveAsync(searchRequest, cancellationToken);

            _logger.LogInformation("Archive search requested with query: {Query}, page: {Page}",
                request.Query, request.Page);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching archive with query: {Query}", request.Query);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档搜索错误",
                Detail = "搜索归档内容失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取年度归档列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>年度归档列表</returns>
    [HttpGet("years")]
    [ProducesResponseType(typeof(List<YearArchiveItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<YearArchiveItem>>> GetYearArchivesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var timeNavigation = await _archiveService.GetArchiveNavigationAsync(cancellationToken);

            var yearArchives = timeNavigation.TimeNavigation.Select(t => new YearArchiveItem
            {
                Year = t.Year,
                PostCount = t.PostCount,
                Months = t.Months.Select(m => new MonthArchiveItem
                {
                    Month = m.Month,
                    PostCount = m.PostCount,
                    MonthName = GetMonthName(m.Month)
                }).ToList()
            }).ToList();

            return Ok(yearArchives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting year archives");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取年度归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取分类归档列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分类归档列表</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryArchiveItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryArchiveItem>>> GetCategoryArchivesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var navigation = await _archiveService.GetArchiveNavigationAsync(cancellationToken);

            var categoryArchives = navigation.CategoryNavigation.Select(c => new CategoryArchiveItem
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                PostCount = c.PostCount,
                Children = c.Children.Select(child => new CategoryArchiveItem
                {
                    Id = child.Id,
                    Name = child.Name,
                    Slug = child.Slug,
                    PostCount = child.PostCount
                }).ToList()
            }).ToList();

            return Ok(categoryArchives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category archives");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取分类归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取标签归档列表
    /// </summary>
    /// <param name="limit">限制数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>标签归档列表</returns>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(List<TagArchiveItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagArchiveItem>>> GetTagArchivesAsync(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            limit = Math.Min(Math.Max(limit, 1), 100);

            var navigation = await _archiveService.GetArchiveNavigationAsync(cancellationToken);

            var tagArchives = navigation.TagNavigation
                .Take(limit)
                .Select(t => new TagArchiveItem
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    PostCount = t.PostCount
                }).ToList();

            return Ok(tagArchives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tag archives");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "归档服务错误",
                Detail = "获取标签归档失败",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// 获取归档面包屑导航
    /// </summary>
    /// <param name="type">归档类型</param>
    /// <param name="id">归档项目ID</param>
    /// <param name="year">年份（时间归档）</param>
    /// <param name="month">月份（时间归档）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>面包屑导航</returns>
    [HttpGet("breadcrumb")]
    [ProducesResponseType(typeof(List<BreadcrumbItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BreadcrumbItem>>> GetArchiveBreadcrumbAsync(
        [FromQuery] string type,
        [FromQuery] Guid? id = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Text = "首页", Url = "/" },
                new() { Text = "归档", Url = "/archive" }
            };

            switch (type?.ToLowerInvariant())
            {
                case "time":
                    if (year.HasValue)
                    {
                        breadcrumbs.Add(new BreadcrumbItem
                        {
                            Text = $"{year}年",
                            Url = $"/archive/time?year={year}"
                        });

                        if (month.HasValue)
                        {
                            breadcrumbs.Add(new BreadcrumbItem
                            {
                                Text = $"{month:D2}月",
                                Url = $"/archive/time?year={year}&month={month}",
                                IsActive = true
                            });
                        }
                        else
                        {
                            breadcrumbs.Last().IsActive = true;
                        }
                    }
                    break;

                case "category":
                    if (id.HasValue)
                    {
                        var categoryArchive = await _archiveService.GetCategoryArchiveAsync(
                            new CategoryArchiveRequest { CategoryId = id.Value, Page = 1, PageSize = 1 },
                            cancellationToken);

                        if (!string.IsNullOrEmpty(categoryArchive.CategoryName))
                        {
                            // 添加父分类面包屑
                            if (categoryArchive.ParentCategory != null)
                            {
                                breadcrumbs.Add(new BreadcrumbItem
                                {
                                    Text = categoryArchive.ParentCategory.Name,
                                    Url = $"/archive/category/{categoryArchive.ParentCategory.Id}"
                                });
                            }

                            breadcrumbs.Add(new BreadcrumbItem
                            {
                                Text = categoryArchive.CategoryName,
                                Url = $"/archive/category/{id}",
                                IsActive = true
                            });
                        }
                    }
                    break;

                case "tag":
                    if (id.HasValue)
                    {
                        var tagArchive = await _archiveService.GetTagArchiveAsync(
                            new TagArchiveRequest { TagId = id.Value, Page = 1, PageSize = 1 },
                            cancellationToken);

                        if (!string.IsNullOrEmpty(tagArchive.TagName))
                        {
                            breadcrumbs.Add(new BreadcrumbItem
                            {
                                Text = tagArchive.TagName,
                                Url = $"/archive/tag/{id}",
                                IsActive = true
                            });
                        }
                    }
                    break;
            }

            return Ok(breadcrumbs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting archive breadcrumb for type: {Type}, id: {Id}", type, id);
            return Ok(new List<BreadcrumbItem>
            {
                new() { Text = "首页", Url = "/" },
                new() { Text = "归档", Url = "/archive", IsActive = true }
            });
        }
    }

    #region 私有方法

    /// <summary>
    /// 获取月份名称
    /// </summary>
    private static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "一月",
            2 => "二月",
            3 => "三月",
            4 => "四月",
            5 => "五月",
            6 => "六月",
            7 => "七月",
            8 => "八月",
            9 => "九月",
            10 => "十月",
            11 => "十一月",
            12 => "十二月",
            _ => $"{month}月"
        };
    }

    #endregion
}

#region DTO类

/// <summary>
/// 归档搜索请求DTO
/// </summary>
public class ArchiveSearchRequestDto
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    [StringLength(500, ErrorMessage = "搜索关键词长度不能超过500个字符")]
    public string? Query { get; set; }

    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// 标签ID列表
    /// </summary>
    public List<Guid>? TagIds { get; set; }

    /// <summary>
    /// 作者ID
    /// </summary>
    public Guid? AuthorId { get; set; }

    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; set; } = "date";

    /// <summary>
    /// 排序方向
    /// </summary>
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// 页码
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    [Range(1, 100, ErrorMessage = "每页大小必须在1-100之间")]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 年度归档项
/// </summary>
public class YearArchiveItem
{
    /// <summary>
    /// 年份
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 月份列表
    /// </summary>
    public List<MonthArchiveItem> Months { get; set; } = new();
}

/// <summary>
/// 月份归档项
/// </summary>
public class MonthArchiveItem
{
    /// <summary>
    /// 月份
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// 月份名称
    /// </summary>
    public string MonthName { get; set; } = string.Empty;

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }
}

/// <summary>
/// 分类归档项
/// </summary>
public class CategoryArchiveItem
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 分类别名
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }

    /// <summary>
    /// 子分类列表
    /// </summary>
    public List<CategoryArchiveItem> Children { get; set; } = new();
}

/// <summary>
/// 标签归档项
/// </summary>
public class TagArchiveItem
{
    /// <summary>
    /// 标签ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 标签别名
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// 文章数量
    /// </summary>
    public int PostCount { get; set; }
}

/// <summary>
/// 面包屑项
/// </summary>
public class BreadcrumbItem
{
    /// <summary>
    /// 显示文本
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 链接URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 是否为当前页面
    /// </summary>
    public bool IsActive { get; set; }
}

#endregion