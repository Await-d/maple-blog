using MapleBlog.Application.DTOs.Markdown;

namespace MapleBlog.Application.Interfaces;

public interface IApplicationMarkdownService
{
    Task<string> ConvertToHtmlAsync(string markdown);
    Task<MarkdownValidationResult> ValidateMarkdownAsync(string markdown);
    Task<string> SanitizeHtmlAsync(string html);
    Task<MarkdownStatsDto> GetMarkdownStatsAsync(string markdown);
    Task<string> ExtractPlainTextAsync(string markdown);
    Task<IEnumerable<string>> ExtractHeadingsAsync(string markdown);
    Task<IEnumerable<MarkdownLinkDto>> ExtractLinksAsync(string markdown);
    Task<string> ConvertHtmlToMarkdownAsync(string html);
    Task<MarkdownTableOfContentsDto> GenerateTableOfContentsAsync(string markdown);

    // 添加BlogServiceTests需要的方法
    Task<int> CountWordsAsync(string content);
    Task<int> EstimateReadingTimeAsync(string content);
    Task<string> ExtractSummaryAsync(string content, int maxLength = 200);
}