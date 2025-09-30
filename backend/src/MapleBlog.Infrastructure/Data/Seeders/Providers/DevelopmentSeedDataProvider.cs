using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;
using DomainUserRole = MapleBlog.Domain.Enums.UserRole;
using MapleBlog.Infrastructure.Data.Seeders.Core;

namespace MapleBlog.Infrastructure.Data.Seeders.Providers;

/// <summary>
/// Development environment seed data provider - provides sample data for development and testing
/// </summary>
public class DevelopmentSeedDataProvider : BaseSeedDataProvider
{
    private readonly IConfiguration _configuration;

    private static readonly Guid SystemUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AuthorUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RegularUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid TestUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static readonly IReadOnlyDictionary<string, Guid> CategoryIds = new Dictionary<string, Guid>
    {
        ["Technology"] = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        ["Web Development"] = Guid.Parse("10000000-0000-0000-0000-000000000002"),
        ["Tutorials"] = Guid.Parse("10000000-0000-0000-0000-000000000003"),
        ["News"] = Guid.Parse("10000000-0000-0000-0000-000000000004"),
        ["Reviews"] = Guid.Parse("10000000-0000-0000-0000-000000000005"),
        ["Opinion"] = Guid.Parse("10000000-0000-0000-0000-000000000006"),
        ["Documentation"] = Guid.Parse("10000000-0000-0000-0000-000000000007"),
        ["Testing"] = Guid.Parse("10000000-0000-0000-0000-000000000008")
    };

    public DevelopmentSeedDataProvider(ILogger<DevelopmentSeedDataProvider> logger, IConfiguration configuration)
        : base(logger, "Development")
    {
        _configuration = configuration;
    }

    public override int Priority => 100; // Standard priority

    public override async Task<SeedDataConfiguration> GetSeedDataConfigurationAsync()
    {
        return new SeedDataConfiguration
        {
            AllowSeeding = true,
            RequireConfirmation = false,
            BackupBeforeSeeding = false,
            ClearExistingData = false,
            MaxExistingRecords = 1000, // Allow seeding even with existing data
            CreateAuditLogs = true,
            ValidateAfterSeeding = true,
            ValidationRules = new List<string>
            {
                "DevelopmentData",
                "TestAccounts"
            },
            CustomSettings = new Dictionary<string, object>
            {
                { "AllowTestData", true },
                { "CreateSampleContent", true },
                { "UseDefaultPasswords", true }
            }
        };
    }

    public override async Task<IEnumerable<User>> GetUsersAsync()
    {
        var users = new List<User>();

        // Create system user
        var systemUser = CreateSystemUser(
            "system",
            "system@maple-blog.local",
            "System User",
            "Internal system user for automated operations"
        );
        systemUser.Id = SystemUserId;
        systemUser.Role = DomainUserRole.SuperAdmin | DomainUserRole.Admin;
        systemUser.PasswordHash = CreatePasswordHash("System123!");
        systemUser.IsActive = false;
        users.Add(systemUser);

        // Create admin user
        var adminUser = CreateSystemUser(
            "admin",
            "admin@mapleblog.local",
            "Administrator",
            "Development administrator account"
        );
        adminUser.Id = AdminUserId;
        adminUser.Role = DomainUserRole.Admin | DomainUserRole.Author | DomainUserRole.Moderator;
        adminUser.PasswordHash = CreatePasswordHash("Admin123!");
        users.Add(adminUser);

        // Create author user
        var authorUser = CreateSystemUser(
            "author",
            "author@mapleblog.local",
            "Content Author",
            "Sample content author for development"
        );
        authorUser.Id = AuthorUserId;
        authorUser.Role = DomainUserRole.Author;
        authorUser.PasswordHash = CreatePasswordHash("Author123!");
        users.Add(authorUser);

        // Create regular user
        var regularUser = CreateSystemUser(
            "user",
            "user@mapleblog.local",
            "Regular User",
            "Sample regular user for development testing"
        );
        regularUser.Id = RegularUserId;
        regularUser.Role = DomainUserRole.User;
        regularUser.PasswordHash = CreatePasswordHash("User123!");
        users.Add(regularUser);

        // Create test user
        var testUser = CreateSystemUser(
            "testuser",
            "test@example.com",
            "Test User",
            "Test user for automated testing"
        );
        testUser.Id = TestUserId;
        testUser.Role = DomainUserRole.User;
        testUser.PasswordHash = CreatePasswordHash("Test123!");
        testUser.IsActive = true;
        users.Add(testUser);

        Logger.LogInformation("Created {Count} development users", users.Count);

        return users;
    }

    public override async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        var categories = new List<Category>
        {
            CreateCategoryWithId("Technology", "Latest trends and insights in technology", "#3B82F6"),
            CreateCategoryWithId("Web Development", "Modern web development techniques and frameworks", "#10B981"),
            CreateCategoryWithId("Tutorials", "Step-by-step guides and tutorials", "#F59E0B"),
            CreateCategoryWithId("News", "Industry news and updates", "#EF4444"),
            CreateCategoryWithId("Reviews", "Product and service reviews", "#8B5CF6"),
            CreateCategoryWithId("Opinion", "Personal opinions and thoughts", "#6B7280"),
            CreateCategoryWithId("Documentation", "Technical documentation and guides", "#3B82F6"),
            CreateCategoryWithId("Testing", "Testing and QA related content", "#EC4899")
        };

        return categories;
    }

    public override async Task<IEnumerable<Tag>> GetTagsAsync()
    {
        return new List<Tag>
        {
            // Technology Tags
            CreateTag("React", "React.js framework", "#61DAFB"),
            CreateTag("TypeScript", "TypeScript language", "#3178C6"),
            CreateTag("ASP.NET Core", "ASP.NET Core framework", "#512BD4"),
            CreateTag("C#", "C# programming language", "#239120"),
            CreateTag("JavaScript", "JavaScript programming language", "#F7DF1E"),
            CreateTag("HTML", "HTML markup language", "#E34F26"),
            CreateTag("CSS", "CSS styling language", "#1572B6"),

            // Development Tags
            CreateTag("Frontend", "Frontend development", "#3B82F6"),
            CreateTag("Backend", "Backend development", "#10B981"),
            CreateTag("Full Stack", "Full stack development", "#F59E0B"),
            CreateTag("DevOps", "DevOps and deployment", "#FF6B6B"),
            CreateTag("Testing", "Software testing", "#8B5CF6"),
            CreateTag("Performance", "Performance optimization", "#EF4444"),
            CreateTag("Security", "Security best practices", "#DC2626"),

            // General Tags
            CreateTag("Tutorial", "Tutorial content", "#3B82F6"),
            CreateTag("Guide", "How-to guides", "#10B981"),
            CreateTag("Tips", "Tips and tricks", "#F59E0B"),
            CreateTag("Best Practices", "Best practices", "#8B5CF6"),
            CreateTag("Beginner", "Beginner friendly", "#10B981"),
            CreateTag("Advanced", "Advanced topics", "#EF4444"),
            CreateTag("Example", "Code examples", "#6B7280"),
            CreateTag("Demo", "Demonstration content", "#EC4899")
        };
    }

    public override async Task<IEnumerable<Post>> GetPostsAsync()
    {
        var posts = new List<Post>();

        // Sample welcome post
        var welcomePost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Welcome to Maple Blog Development Environment",
            Slug = "welcome-to-maple-blog-development",
            Summary = "Welcome to the Maple Blog development environment. This is a sample post to help you get started with content creation and testing.",
            Content = CreateWelcomePostContent(),
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-30),
            IsFeatured = true,
            IsSticky = true,
            AllowComments = true,
            MetaTitle = "Welcome to Maple Blog Development",
            MetaDescription = "Welcome to the Maple Blog development environment with sample content for testing.",
            ReadingTime = 5,
            WordCount = 800,
            ViewCount = 100,
            LikeCount = 15,
            CommentCount = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        };
        welcomePost.AuthorId = AdminUserId;
        welcomePost.CategoryId = GetCategoryId("Technology");
        posts.Add(welcomePost);

        // Sample tutorial post
        var tutorialPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "Getting Started with React and TypeScript",
            Slug = "getting-started-react-typescript",
            Summary = "Learn how to set up a new React project with TypeScript, including best practices and common patterns.",
            Content = CreateTutorialPostContent(),
            Status = PostStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-15),
            AllowComments = true,
            MetaTitle = "React TypeScript Tutorial - Getting Started Guide",
            MetaDescription = "Complete guide to setting up React with TypeScript including best practices and examples.",
            ReadingTime = 12,
            WordCount = 2400,
            ViewCount = 250,
            LikeCount = 35,
            CommentCount = 8,
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            UpdatedAt = DateTime.UtcNow.AddDays(-15)
        };
        tutorialPost.AuthorId = AuthorUserId;
        tutorialPost.CategoryId = GetCategoryId("Tutorials");
        posts.Add(tutorialPost);

        // Sample draft post
        var draftPost = new Post
        {
            Id = Guid.NewGuid(),
            Title = "ASP.NET Core Performance Optimization (Draft)",
            Slug = "aspnet-core-performance-optimization-draft",
            Summary = "Tips and techniques for optimizing ASP.NET Core applications. This is a draft post for testing.",
            Content = CreateDraftPostContent(),
            Status = PostStatus.Draft,
            AllowComments = true,
            MetaTitle = "ASP.NET Core Performance Tips",
            MetaDescription = "Learn how to optimize your ASP.NET Core applications for better performance.",
            ReadingTime = 8,
            WordCount = 1600,
            ViewCount = 0,
            LikeCount = 0,
            CommentCount = 0,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };
        draftPost.AuthorId = AuthorUserId;
        draftPost.CategoryId = GetCategoryId("Documentation");
        posts.Add(draftPost);

        Logger.LogInformation("Created {Count} development posts", posts.Count);

        return posts;
    }

    public override async Task<IEnumerable<SystemConfiguration>> GetSystemConfigurationsAsync()
    {
        var baseConfigs = (await base.GetDefaultSystemConfigurationsAsync()).ToList();

        // Add development-specific configurations
        var developmentConfigs = new List<SystemConfiguration>
        {
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Environment.Mode",
                Value = "Development",
                Description = "Current environment mode",
                Category = "System",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Debug.Enabled",
                Value = "true",
                Description = "Enable debug mode",
                Category = "Development",
                IsPublic = false,
                DataType = "boolean"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Logging.Level",
                Value = "Information",
                Description = "Default logging level for development",
                Category = "Development",
                IsPublic = false,
                DataType = "string"
            },
            new SystemConfiguration
            {
                Id = Guid.NewGuid(),
                Key = "Testing.EnableTestData",
                Value = "true",
                Description = "Enable test data generation",
                Category = "Development",
                IsPublic = false,
                DataType = "boolean"
            }
        };

        return baseConfigs.Concat(developmentConfigs);
    }

    private static Guid GetCategoryId(string name)
    {
        if (CategoryIds.TryGetValue(name, out var id))
        {
            return id;
        }

        throw new KeyNotFoundException($"Category identifier not configured for '{name}'.");
    }

    private Category CreateCategoryWithId(string name, string description, string color)
    {
        var category = CreateCategory(name, description, color);
        category.Id = GetCategoryId(name);
        category.DisplayName = category.Name;
        return category;
    }

    public override async Task<IEnumerable<RolePermissionAssignment>> GetRolePermissionAssignmentsAsync()
    {
        var permissions = (await GetDefaultPermissionsAsync())
            .Select(p => string.IsNullOrWhiteSpace(p.Name) ? $"{p.Resource}.{p.Action}" : p.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!permissions.Any())
        {
            return Array.Empty<RolePermissionAssignment>();
        }

        var permissionSet = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        var assignments = new List<RolePermissionAssignment>();

        // SuperAdmin receives all permissions
        assignments.AddRange(permissionSet.Select(name => new RolePermissionAssignment("SuperAdmin", name, SystemUserId)));

        // Admin receives broad management permissions
        var adminPrefixes = new[] { "System.", "Users.", "Roles.", "Categories.", "Tags.", "Posts.", "Files.", "Comments." };
        var adminPermissions = permissionSet
            .Where(name => adminPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        assignments.AddRange(adminPermissions.Select(name => new RolePermissionAssignment("Admin", name, SystemUserId)));

        void AddAssignments(string roleName, IEnumerable<string> permissionNames)
        {
            foreach (var permissionName in permissionNames)
            {
                if (permissionSet.Contains(permissionName))
                {
                    assignments.Add(new RolePermissionAssignment(roleName, permissionName, SystemUserId));
                }
                else
                {
                    Logger.LogWarning("Skipping role permission assignment for {Role}. Permission '{Permission}' not found in seed set.", roleName, permissionName);
                }
            }
        }

        AddAssignments("Author", new[]
        {
            "Posts.Create", "Posts.Read", "Posts.Update", "Posts.Publish",
            "Categories.Read", "Tags.Read", "Files.Upload", "Files.Download",
            "Comments.Read", "Comments.Moderate"
        });

        AddAssignments("User", new[]
        {
            "Posts.Read", "Comments.Create", "Comments.Read", "Files.Download"
        });

        // Deduplicate assignments (case-insensitive)
        var deduplicated = new Dictionary<string, RolePermissionAssignment>(StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in assignments)
        {
            var key = $"{assignment.RoleName.Trim().ToUpperInvariant()}|{assignment.PermissionName.Trim().ToUpperInvariant()}";
            if (!deduplicated.ContainsKey(key))
            {
                deduplicated[key] = assignment;
            }
        }

        return deduplicated.Values;
    }

    public override Task<IEnumerable<UserRoleAssignment>> GetUserRoleAssignmentsAsync()
    {
        var assignments = new List<UserRoleAssignment>
        {
            new UserRoleAssignment("system", "SuperAdmin", SystemUserId),
            new UserRoleAssignment("admin", "Admin", SystemUserId),
            new UserRoleAssignment("admin", "Author", SystemUserId),
            new UserRoleAssignment("author", "Author", AdminUserId),
            new UserRoleAssignment("user", "User", AdminUserId),
            new UserRoleAssignment("testuser", "User", AdminUserId)
        };

        return Task.FromResult<IEnumerable<UserRoleAssignment>>(assignments);
    }

    #region Content Generation Methods

    private string CreateWelcomePostContent()
    {
        return @"# Welcome to Maple Blog Development Environment

Welcome to the Maple Blog development environment! This platform is designed to provide a modern, efficient blogging experience for developers and content creators.

## Features

- **Modern Technology Stack**: Built with ASP.NET Core 10 and React 19
- **Rich Content Editor**: Support for Markdown with live preview
- **Responsive Design**: Mobile-first design that works on all devices
- **SEO Optimized**: Built-in SEO features for better search visibility
- **Fast Performance**: Optimized for speed with caching and compression

## Getting Started

1. **Create Content**: Start writing your first blog post
2. **Customize**: Adjust settings to match your preferences
3. **Engage**: Enable comments and interact with your audience
4. **Analyze**: Monitor your content performance with built-in analytics

## Development Features

This development environment includes:

- Sample data for testing
- Debug logging enabled
- Hot reload capabilities
- Test user accounts
- Sample content and categories

## Next Steps

- Explore the admin interface
- Try creating a new post
- Test the comment system
- Check the analytics dashboard

Happy blogging! 🚀";
    }

    private string CreateTutorialPostContent()
    {
        return @"# Getting Started with React and TypeScript

React and TypeScript make a powerful combination for building modern web applications. In this tutorial, we'll walk through setting up a new project and explore best practices.

## Prerequisites

- Node.js 18 or later
- Basic knowledge of JavaScript
- Familiarity with React concepts

## Setting Up the Project

### Step 1: Create a New React App

```bash
npx create-react-app my-app --template typescript
cd my-app
npm start
```

### Step 2: Project Structure

Your project structure should look like this:

```
my-app/
├── public/
├── src/
│   ├── components/
│   ├── hooks/
│   ├── types/
│   └── utils/
├── package.json
└── tsconfig.json
```

## TypeScript Configuration

### Essential Types

```typescript
// types/index.ts
export interface User {
  id: string;
  name: string;
  email: string;
}

export interface Post {
  id: string;
  title: string;
  content: string;
  author: User;
  createdAt: Date;
}
```

### Component Example

```typescript
import React from 'react';

interface Props {
  title: string;
  content: string;
}

const BlogPost: React.FC<Props> = ({ title, content }) => {
  return (
    <article>
      <h1>{title}</h1>
      <div dangerouslySetInnerHTML={{ __html: content }} />
    </article>
  );
};

export default BlogPost;
```

## Best Practices

1. **Use Strict Mode**: Enable strict TypeScript checking
2. **Interface Over Type**: Prefer interfaces for object shapes
3. **Generic Components**: Use generics for reusable components
4. **Proper Error Handling**: Handle errors gracefully

## Conclusion

React and TypeScript provide excellent developer experience and type safety. Start small and gradually adopt more advanced patterns as your application grows.

Happy coding! 💻";
    }

    private string CreateDraftPostContent()
    {
        return @"# ASP.NET Core Performance Optimization

*This is a draft post for testing purposes*

Performance is crucial for web applications. Here are some key optimization techniques for ASP.NET Core applications.

## Caching Strategies

### Response Caching
- Use built-in response caching middleware
- Configure cache profiles for different content types
- Implement cache invalidation strategies

### Distributed Caching
- Use Redis for distributed caching
- Cache frequently accessed data
- Implement cache-aside pattern

## Database Optimization

### Entity Framework Core
- Use AsNoTracking() for read-only queries
- Implement proper indexing
- Use compiled queries for frequently executed queries

## More content to be added...

*This post is still being written and will be published soon.*";
    }

    #endregion
}
