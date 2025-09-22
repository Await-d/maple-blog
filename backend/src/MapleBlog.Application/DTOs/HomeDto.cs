using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.DTOs
{
    /// <summary>
    /// Home page data aggregation DTO
    /// </summary>
    public class HomePageDto
    {
        /// <summary>
        /// Featured posts for hero section
        /// </summary>
        public IReadOnlyList<PostSummaryDto> FeaturedPosts { get; init; } = new List<PostSummaryDto>();

        /// <summary>
        /// Latest published posts
        /// </summary>
        public IReadOnlyList<PostSummaryDto> LatestPosts { get; init; } = new List<PostSummaryDto>();

        /// <summary>
        /// Most popular posts by views
        /// </summary>
        public IReadOnlyList<PostSummaryDto> PopularPosts { get; init; } = new List<PostSummaryDto>();

        /// <summary>
        /// Categories with post counts
        /// </summary>
        public IReadOnlyList<CategorySummaryDto> Categories { get; init; } = new List<CategorySummaryDto>();

        /// <summary>
        /// Popular tags with usage counts
        /// </summary>
        public IReadOnlyList<TagSummaryDto> PopularTags { get; init; } = new List<TagSummaryDto>();

        /// <summary>
        /// Website statistics
        /// </summary>
        public SiteStatsDto SiteStats { get; init; } = new();

        /// <summary>
        /// Personalized recommendations (for authenticated users)
        /// </summary>
        public IReadOnlyList<PostSummaryDto>? RecommendedPosts { get; init; }

        /// <summary>
        /// Active authors with recent activity
        /// </summary>
        public IReadOnlyList<AuthorSummaryDto> ActiveAuthors { get; init; } = new List<AuthorSummaryDto>();

        /// <summary>
        /// Timestamp when this data was generated
        /// </summary>
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Cache expiry time for this data
        /// </summary>
        public DateTime ExpiresAt { get; init; } = DateTime.UtcNow.AddMinutes(30);
    }

    /// <summary>
    /// Post summary DTO for lists and cards
    /// </summary>
    public class PostSummaryDto
    {
        public Guid Id { get; init; }

        [Required]
        [StringLength(200)]
        public string Title { get; init; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Slug { get; init; } = string.Empty;

        public string? Summary { get; init; }

        /// <summary>
        /// Featured image URL
        /// </summary>
        public string? FeaturedImageUrl { get; init; }

        /// <summary>
        /// Open Graph image URL
        /// </summary>
        public string? OgImageUrl { get; init; }

        public DateTime PublishedAt { get; init; }
        public DateTime UpdatedAt { get; init; }

        public int ViewCount { get; init; }
        public int LikeCount { get; init; }
        public int CommentCount { get; init; }

        public int? ReadingTime { get; init; }
        public bool IsFeatured { get; init; }
        public bool IsSticky { get; init; }

        /// <summary>
        /// Category information
        /// </summary>
        public CategorySummaryDto? Category { get; init; }

        /// <summary>
        /// Author information
        /// </summary>
        public AuthorSummaryDto Author { get; init; } = new();

        /// <summary>
        /// Tags associated with this post
        /// </summary>
        public IReadOnlyList<TagSummaryDto> Tags { get; init; } = new List<TagSummaryDto>();
    }

    /// <summary>
    /// Category summary DTO
    /// </summary>
    public class CategorySummaryDto
    {
        public Guid Id { get; init; }

        [Required]
        [StringLength(100)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Slug { get; init; } = string.Empty;

        public string? Description { get; init; }

        /// <summary>
        /// Category color for UI theming
        /// </summary>
        [StringLength(7)]
        public string? Color { get; init; }

        /// <summary>
        /// Icon name or URL
        /// </summary>
        [StringLength(100)]
        public string? Icon { get; init; }

        /// <summary>
        /// Number of published posts in this category
        /// </summary>
        public int PostCount { get; init; }

        /// <summary>
        /// Parent category for hierarchical organization
        /// </summary>
        public Guid? ParentId { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    /// <summary>
    /// Tag summary DTO
    /// </summary>
    public class TagSummaryDto
    {
        public Guid Id { get; init; }

        [Required]
        [StringLength(50)]
        public string Name { get; init; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Slug { get; init; } = string.Empty;

        public string? Description { get; init; }

        /// <summary>
        /// Tag color for UI theming
        /// </summary>
        [StringLength(7)]
        public string? Color { get; init; }

        /// <summary>
        /// Number of posts using this tag
        /// </summary>
        public int PostCount { get; init; }

        /// <summary>
        /// Tag usage frequency (for tag cloud sizing)
        /// </summary>
        public double UsageFrequency { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    /// <summary>
    /// Author summary DTO
    /// </summary>
    public class AuthorSummaryDto
    {
        public Guid Id { get; init; }

        [Required]
        [StringLength(50)]
        public string UserName { get; init; } = string.Empty;

        [StringLength(100)]
        public string? DisplayName { get; init; }

        [StringLength(50)]
        public string? FirstName { get; init; }

        [StringLength(50)]
        public string? LastName { get; init; }

        /// <summary>
        /// Avatar image URL
        /// </summary>
        public string? Avatar { get; init; }

        /// <summary>
        /// Author bio/description
        /// </summary>
        public string? Bio { get; init; }

        /// <summary>
        /// Number of published posts
        /// </summary>
        public int PostCount { get; init; }

        /// <summary>
        /// Total views across all author's posts
        /// </summary>
        public int TotalViews { get; init; }

        /// <summary>
        /// Date of most recent post
        /// </summary>
        public DateTime? LastPostDate { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    /// <summary>
    /// Site statistics DTO
    /// </summary>
    public class SiteStatsDto
    {
        /// <summary>
        /// Total number of published posts
        /// </summary>
        public int TotalPosts { get; init; }

        /// <summary>
        /// Total number of categories
        /// </summary>
        public int TotalCategories { get; init; }

        /// <summary>
        /// Total number of tags
        /// </summary>
        public int TotalTags { get; init; }

        /// <summary>
        /// Total number of registered users
        /// </summary>
        public int TotalUsers { get; init; }

        /// <summary>
        /// Total number of active authors (users with posts)
        /// </summary>
        public int TotalAuthors { get; init; }

        /// <summary>
        /// Total views across all posts
        /// </summary>
        public int TotalViews { get; init; }

        /// <summary>
        /// Total likes across all posts
        /// </summary>
        public int TotalLikes { get; init; }

        /// <summary>
        /// Total comments across all posts
        /// </summary>
        public int TotalComments { get; init; }

        /// <summary>
        /// Number of posts published this month
        /// </summary>
        public int PostsThisMonth { get; init; }

        /// <summary>
        /// Number of posts published this week
        /// </summary>
        public int PostsThisWeek { get; init; }

        /// <summary>
        /// Number of posts published today
        /// </summary>
        public int PostsToday { get; init; }

        /// <summary>
        /// Date of the most recent post
        /// </summary>
        public DateTime? LastPostDate { get; init; }

        /// <summary>
        /// Average posts per month
        /// </summary>
        public double AveragePostsPerMonth { get; init; }

        /// <summary>
        /// Total reading time across all posts (minutes)
        /// </summary>
        public int TotalReadingTime { get; init; }

        /// <summary>
        /// Timestamp when these stats were calculated
        /// </summary>
        public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Personalization preferences for the home page
    /// </summary>
    public class PersonalizationDto
    {
        /// <summary>
        /// User's preferred categories
        /// </summary>
        public IReadOnlyList<Guid> PreferredCategories { get; init; } = new List<Guid>();

        /// <summary>
        /// User's preferred tags
        /// </summary>
        public IReadOnlyList<Guid> PreferredTags { get; init; } = new List<Guid>();

        /// <summary>
        /// User's followed authors
        /// </summary>
        public IReadOnlyList<Guid> FollowedAuthors { get; init; } = new List<Guid>();

        /// <summary>
        /// Theme preference (light, dark, auto)
        /// </summary>
        [StringLength(10)]
        public string Theme { get; init; } = "auto";

        /// <summary>
        /// Layout preference (grid, list, cards)
        /// </summary>
        [StringLength(10)]
        public string Layout { get; init; } = "cards";

        /// <summary>
        /// Number of posts to show per page
        /// </summary>
        public int PostsPerPage { get; init; } = 10;

        /// <summary>
        /// Show reading time estimates
        /// </summary>
        public bool ShowReadingTime { get; init; } = true;

        /// <summary>
        /// Show post summaries in lists
        /// </summary>
        public bool ShowSummaries { get; init; } = true;

        /// <summary>
        /// Language preference
        /// </summary>
        [StringLength(10)]
        public string Language { get; init; } = "zh-CN";

        public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    }
}