namespace MapleBlog.Application.DTOs.Markdown;

public class MarkdownValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Errors { get; set; } = new List<string>();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
}

public class MarkdownStatsDto
{
    public int WordCount { get; set; }
    public int CharacterCount { get; set; }
    public int ParagraphCount { get; set; }
    public int HeadingCount { get; set; }
    public int LinkCount { get; set; }
    public int ImageCount { get; set; }
}

public class MarkdownLinkDto
{
    public string Text { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
}

public class MarkdownTableOfContentsDto
{
    public IEnumerable<TocItemDto> Items { get; set; } = new List<TocItemDto>();
}

public class TocItemDto
{
    public string Text { get; set; } = string.Empty;
    public string Anchor { get; set; } = string.Empty;
    public int Level { get; set; }
    public IEnumerable<TocItemDto> Children { get; set; } = new List<TocItemDto>();
}