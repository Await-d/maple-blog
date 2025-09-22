using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs;

/// <summary>
/// Generic paged result data transfer object for API responses
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// List of items in current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = [];

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Index of first item on current page (0-based)
    /// </summary>
    public int FirstItemIndex => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Index of last item on current page (0-based)
    /// </summary>
    public int LastItemIndex => Math.Min(FirstItemIndex + PageSize - 1, TotalItems - 1);

    /// <summary>
    /// Create a new paged result
    /// </summary>
    /// <param name="items">Items for current page</param>
    /// <param name="totalItems">Total number of items</param>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Items per page</param>
    public static PagedResultDto<T> Create(IEnumerable<T> items, int totalItems, int pageNumber, int pageSize)
    {
        var totalPages = totalItems > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 0;

        return new PagedResultDto<T>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// Create an empty paged result
    /// </summary>
    /// <param name="pageNumber">Current page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Empty paged result</returns>
    public static PagedResultDto<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResultDto<T>
        {
            Items = [],
            TotalItems = 0,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = 0
        };
    }
}