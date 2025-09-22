using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;

namespace MapleBlog.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the database with sample blog content for development and demonstration
/// </summary>
public class BlogContentSeeder
{
    private readonly BlogDbContext _context;
    private readonly ILogger<BlogContentSeeder> _logger;

    public BlogContentSeeder(BlogDbContext context, ILogger<BlogContentSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with sample data if it's empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await _context.Posts.AnyAsync())
            {
                _logger.LogInformation("Database already contains blog data, skipping seeding.");
                return;
            }

            _logger.LogInformation("Starting blog content seeding...");

            // Seed data in order
            var adminUser = await SeedUsersAsync();
            var categories = await SeedCategoriesAsync(adminUser);
            var tags = await SeedTagsAsync(adminUser);
            await SeedPostsAsync(adminUser, categories, tags);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Blog content seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task<User> SeedUsersAsync()
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = Email.Create("admin@mapleblog.com"),
            PasswordHash = "$2a$11$example_hash_for_admin_user",
            DisplayName = "Blog Administrator",
            Bio = "System administrator and content curator for Maple Blog.",
            EmailConfirmed = true,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow.AddDays(-1),
            AvatarUrl = "/images/avatars/admin.jpg"
        };

        var authorUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "john_doe",
            Email = Email.Create("john@example.com"),
            PasswordHash = "$2a$11$example_hash_for_author_user",
            DisplayName = "John Doe",
            Bio = "Full-stack developer passionate about modern web technologies.",
            EmailConfirmed = true,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow.AddHours(-3),
            AvatarUrl = "/images/avatars/john.jpg"
        };

        await _context.Users.AddRangeAsync(adminUser, authorUser);
        return adminUser;
    }

    private async Task<List<Category>> SeedCategoriesAsync(User adminUser)
    {
        var categories = new List<Category>
        {
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Technology",
                Slug = "technology",
                Description = "Latest trends and insights in technology",
                Color = "#3B82F6",
                IsActive = true,
                PostCount = 0
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Web Development",
                Slug = "web-development",
                Description = "Modern web development techniques and frameworks",
                Color = "#10B981",
                IsActive = true,
                PostCount = 0
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Tutorials",
                Slug = "tutorials",
                Description = "Step-by-step guides and tutorials",
                Color = "#F59E0B",
                IsActive = true,
                PostCount = 0
            }
        };

        await _context.Categories.AddRangeAsync(categories);
        return categories;
    }

    private async Task<List<Tag>> SeedTagsAsync(User adminUser)
    {
        var tags = new List<Tag>
        {
            new Tag { Id = Guid.NewGuid(), Name = "React", Slug = "react", Description = "React.js framework", Color = "#61DAFB", UsageCount = 0 },
            new Tag { Id = Guid.NewGuid(), Name = "TypeScript", Slug = "typescript", Description = "TypeScript language", Color = "#3178C6", UsageCount = 0 },
            new Tag { Id = Guid.NewGuid(), Name = "ASP.NET Core", Slug = "aspnet-core", Description = "ASP.NET Core framework", Color = "#512BD4", UsageCount = 0 },
            new Tag { Id = Guid.NewGuid(), Name = "Performance", Slug = "performance", Description = "Performance optimization", Color = "#EF4444", UsageCount = 0 },
            new Tag { Id = Guid.NewGuid(), Name = "Testing", Slug = "testing", Description = "Software testing", Color = "#8B5CF6", UsageCount = 0 }
        };

        await _context.Tags.AddRangeAsync(tags);
        return tags;
    }

    private async Task SeedPostsAsync(User adminUser, List<Category> categories, List<Tag> tags)
    {
        var techCategory = categories.First(c => c.Slug == "technology");
        var webDevCategory = categories.First(c => c.Slug == "web-development");
        var tutorialsCategory = categories.First(c => c.Slug == "tutorials");

        var reactTag = tags.First(t => t.Slug == "react");
        var typescriptTag = tags.First(t => t.Slug == "typescript");
        var aspnetTag = tags.First(t => t.Slug == "aspnet-core");
        var testingTag = tags.First(t => t.Slug == "testing");

        var posts = new List<Post>
        {
            new Post
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Maple Blog - Your Developer Knowledge Hub",
                Slug = "welcome-to-maple-blog",
                Summary = "Discover Maple Blog, a modern platform designed for developers to share knowledge, best practices, and insights about web development, programming, and technology trends.",
                Content = "Welcome to Maple Blog! We're thrilled to have you here at your new go-to destination for developer-focused content, tutorials, and insights. Maple Blog is built by developers, for developers. Our platform combines modern web technologies with a focus on delivering high-quality, practical content that you can apply in your daily work.",
                CategoryId = techCategory.Id,
                AuthorId = adminUser.Id,
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                IsFeatured = true,
                IsSticky = true,
                AllowComments = true,
                MetaTitle = "Welcome to Maple Blog - Developer Knowledge Hub",
                MetaDescription = "Discover Maple Blog, a modern platform for developers featuring tutorials, best practices, and insights about web development and technology.",
                ReadingTime = 8,
                WordCount = 1200,
                ViewCount = 1500,
                LikeCount = 45,
                CommentCount = 12
            },
            new Post
            {
                Id = Guid.NewGuid(),
                Title = "Building Modern React Applications with TypeScript and Hooks",
                Slug = "building-modern-react-applications-typescript-hooks",
                Summary = "Learn how to build scalable React applications using TypeScript, modern hooks patterns, and best practices for component design and state management.",
                Content = "React has evolved significantly over the years, and with the introduction of Hooks and the growing popularity of TypeScript, building modern React applications has never been more powerful or enjoyable. This comprehensive guide covers essential patterns and best practices for building scalable React applications using TypeScript, modern hooks patterns, and performance optimization techniques.",
                CategoryId = tutorialsCategory.Id,
                AuthorId = adminUser.Id,
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-14),
                AllowComments = true,
                MetaTitle = "Building Modern React Applications with TypeScript and Hooks - Complete Guide",
                MetaDescription = "Master React development with TypeScript, modern hooks patterns, and performance optimization techniques for scalable applications.",
                ReadingTime = 15,
                WordCount = 2800,
                ViewCount = 980,
                LikeCount = 67,
                CommentCount = 23
            },
            new Post
            {
                Id = Guid.NewGuid(),
                Title = "ASP.NET Core Performance Optimization: From Good to Great",
                Slug = "aspnet-core-performance-optimization",
                Summary = "Essential techniques to optimize your ASP.NET Core applications for production environments, including caching strategies, database optimization, and monitoring.",
                Content = "Performance is crucial for web applications. This guide walks you through essential techniques to optimize your ASP.NET Core applications for production environments. Learn about response caching, database optimization strategies, and implementing effective monitoring solutions.",
                CategoryId = webDevCategory.Id,
                AuthorId = adminUser.Id,
                Status = PostStatus.Published,
                PublishedAt = DateTime.UtcNow.AddDays(-7),
                AllowComments = true,
                MetaTitle = "ASP.NET Core Performance Optimization Guide - Production Ready Tips",
                MetaDescription = "Learn advanced performance optimization techniques for ASP.NET Core applications including caching, database optimization, and monitoring.",
                ReadingTime = 12,
                WordCount = 2200,
                ViewCount = 750,
                LikeCount = 38,
                CommentCount = 15
            }
        };

        await _context.Posts.AddRangeAsync(posts);

        // Update tag usage counts
        reactTag.UsageCount = 2;
        typescriptTag.UsageCount = 2;
        aspnetTag.UsageCount = 1;
        testingTag.UsageCount = 1;
        tags.First(t => t.Slug == "performance").UsageCount = 1;

        // Update category post counts
        techCategory.PostCount = 1;
        webDevCategory.PostCount = 1;
        tutorialsCategory.PostCount = 1;
    }
}