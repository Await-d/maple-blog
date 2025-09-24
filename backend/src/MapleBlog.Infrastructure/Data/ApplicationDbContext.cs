using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;
using MapleBlog.Domain.ValueObjects;
using BCrypt.Net;

namespace MapleBlog.Infrastructure.Data
{
    /// <summary>
    /// Application database context for Entity Framework Core - Authentication Module
    /// </summary>
    public class ApplicationDbContext : DbContext, MapleBlog.Application.Interfaces.IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Users DbSet
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// Posts DbSet
        /// </summary>
        public DbSet<Post> Posts { get; set; } = null!;

        /// <summary>
        /// PostAttachments DbSet
        /// </summary>
        public DbSet<PostAttachment> PostAttachments { get; set; } = null!;

        /// <summary>
        /// Categories DbSet
        /// </summary>
        public DbSet<Category> Categories { get; set; } = null!;

        /// <summary>
        /// Tags DbSet
        /// </summary>
        public DbSet<Tag> Tags { get; set; } = null!;

        /// <summary>
        /// PostTags DbSet
        /// </summary>
        public DbSet<PostTag> PostTags { get; set; } = null!;

        /// <summary>
        /// Comments DbSet
        /// </summary>
        public DbSet<Comment> Comments { get; set; } = null!;

        /// <summary>
        /// Roles DbSet
        /// </summary>
        public DbSet<Role> Roles { get; set; } = null!;

        /// <summary>
        /// Permissions DbSet
        /// </summary>
        public DbSet<Permission> Permissions { get; set; } = null!;

        /// <summary>
        /// UserRoles DbSet
        /// </summary>
        public DbSet<Domain.Entities.UserRole> UserRoles { get; set; } = null!;

        /// <summary>
        /// RolePermissions DbSet
        /// </summary>
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;

        /// <summary>
        /// Email Verification Tokens DbSet
        /// </summary>
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;

        /// <summary>
        /// Search Queries DbSet
        /// </summary>
        public DbSet<SearchQuery> SearchQueries { get; set; } = null!;

        /// <summary>
        /// Search Indexes DbSet
        /// </summary>
        public DbSet<SearchIndex> SearchIndexes { get; set; } = null!;

        /// <summary>
        /// Popular Searches DbSet
        /// </summary>
        public DbSet<PopularSearch> PopularSearches { get; set; } = null!;

        /// <summary>
        /// Files DbSet
        /// </summary>
        public DbSet<Domain.Entities.File> Files { get; set; } = null!;

        /// <summary>
        /// Login History DbSet
        /// </summary>
        public DbSet<LoginHistory> LoginHistories { get; set; } = null!;

        /// <summary>
        /// Data Permission Rules DbSet
        /// </summary>
        public DbSet<DataPermissionRule> DataPermissionRules { get; set; } = null!;

        /// <summary>
        /// Temporary Permissions DbSet
        /// </summary>
        public DbSet<TemporaryPermission> TemporaryPermissions { get; set; } = null!;

        /// <summary>
        /// Audit Logs DbSet
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        /// <summary>
        /// Storage Quota Configurations DbSet
        /// </summary>
        public DbSet<StorageQuotaConfiguration> StorageQuotaConfigurations { get; set; } = null!;

        /// <summary>
        /// File Shares DbSet
        /// </summary>
        public DbSet<Domain.Entities.FileShare> FileShares { get; set; } = null!;

        /// <summary>
        /// File Access Logs DbSet
        /// </summary>
        public DbSet<Domain.Entities.FileAccessLog> FileAccessLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure authentication entities
            ConfigureUserEntity(modelBuilder);
            ConfigureEmailVerificationTokenEntity(modelBuilder);

            // Configure permission entities
            ConfigureRoleEntity(modelBuilder);
            ConfigurePermissionEntity(modelBuilder);
            ConfigureUserRoleEntity(modelBuilder);
            ConfigureRolePermissionEntity(modelBuilder);

            // Configure blog entities
            ConfigurePostEntity(modelBuilder);
            ConfigureCategoryEntity(modelBuilder);
            ConfigureTagEntity(modelBuilder);
            ConfigurePostTagEntity(modelBuilder);
            ConfigureCommentEntity(modelBuilder);
            ConfigureFileEntity(modelBuilder);
            ConfigureLoginHistoryEntity(modelBuilder);
            ConfigureSearchQueryEntity(modelBuilder);
            ConfigureSearchIndexEntity(modelBuilder);

            // Configure permission system entities
            ConfigureDataPermissionRuleEntity(modelBuilder);
            ConfigureTemporaryPermissionEntity(modelBuilder);
            ConfigureAuditLogEntity(modelBuilder);
            ConfigureStorageQuotaConfigurationEntity(modelBuilder);

            // Configure file sharing entities
            ConfigureFileShareEntity(modelBuilder);
            ConfigureFileAccessLogEntity(modelBuilder);

            // Configure value objects
            ConfigureValueObjects(modelBuilder);

            // Seed data
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Configures the User entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureUserEntity(ModelBuilder modelBuilder)
        {
            var userEntity = modelBuilder.Entity<User>();

            // Primary key
            userEntity.HasKey(u => u.Id);

            // Email property configuration with Email value object conversion
            userEntity.Property(u => u.Email)
                .HasMaxLength(254)
                .IsRequired();

            userEntity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            // String properties
            userEntity.Property(u => u.UserName)
                .HasMaxLength(50)
                .IsRequired();

            userEntity.Property(u => u.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            userEntity.Property(u => u.SecurityStamp)
                .HasMaxLength(255)
                .IsRequired();

            userEntity.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired(false);

            userEntity.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired(false);

            userEntity.Property(u => u.DisplayName)
                .HasMaxLength(100)
                .IsRequired(false);

            userEntity.Property(u => u.AvatarUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            userEntity.Property(u => u.Bio)
                .IsRequired(false);

            userEntity.Property(u => u.Website)
                .HasMaxLength(255)
                .IsRequired(false);

            userEntity.Property(u => u.Location)
                .HasMaxLength(100)
                .IsRequired(false);

            userEntity.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired(false);

            userEntity.Property(u => u.Gender)
                .HasMaxLength(10)
                .IsRequired(false);

            // Role enum configuration
            userEntity.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(MapleBlog.Domain.Enums.UserRole.User);

            // Boolean properties
            userEntity.Property(u => u.EmailConfirmed)
                .HasDefaultValue(false);

            userEntity.Property(u => u.PhoneNumberConfirmed)
                .HasDefaultValue(false);

            userEntity.Property(u => u.TwoFactorEnabled)
                .HasDefaultValue(false);

            userEntity.Property(u => u.LockoutEnabled)
                .HasDefaultValue(true);

            userEntity.Property(u => u.IsVerified)
                .HasDefaultValue(false);

            userEntity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            // Numeric properties
            userEntity.Property(u => u.AccessFailedCount)
                .HasDefaultValue(0);

            // Nullable DateTime properties
            userEntity.Property(u => u.LastLoginAt)
                .IsRequired(false);

            userEntity.Property(u => u.EmailVerificationTokenExpiry)
                .IsRequired(false);

            userEntity.Property(u => u.PasswordResetTokenExpiresAt)
                .IsRequired(false);

            userEntity.Property(u => u.LockoutEndDateUtc)
                .IsRequired(false);

            userEntity.Property(u => u.DateOfBirth)
                .IsRequired(false);

            // Token properties
            userEntity.Property(u => u.EmailVerificationToken)
                .HasMaxLength(255)
                .IsRequired(false);

            userEntity.Property(u => u.PasswordResetToken)
                .HasMaxLength(255)
                .IsRequired(false);

            // Base entity properties
            userEntity.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            userEntity.Property(u => u.UpdatedAt)
                .IsRequired(false);

            userEntity.Property(u => u.CreatedBy)
                .IsRequired(false);

            userEntity.Property(u => u.UpdatedBy)
                .IsRequired(false);

            userEntity.Property(u => u.IsDeleted)
                .HasDefaultValue(false);

            userEntity.Property(u => u.DeletedAt)
                .IsRequired(false);

            userEntity.Property(u => u.DeletedBy)
                .IsRequired(false);

            // Indexes for performance
            userEntity.HasIndex(u => u.UserName)
                .IsUnique()
                .HasDatabaseName("IX_Users_UserName");

            userEntity.HasIndex(u => u.CreatedAt)
                .HasDatabaseName("IX_Users_CreatedAt");

            userEntity.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_Users_IsActive");

            userEntity.HasIndex(u => u.Role)
                .HasDatabaseName("IX_Users_Role");

            userEntity.HasIndex(u => u.EmailVerificationToken)
                .HasDatabaseName("IX_Users_EmailVerificationToken");

            userEntity.HasIndex(u => u.PasswordResetToken)
                .HasDatabaseName("IX_Users_PasswordResetToken");

            userEntity.HasIndex(u => u.IsDeleted)
                .HasDatabaseName("IX_Users_IsDeleted");

            // Composite indexes for common authentication queries
            userEntity.HasIndex(u => new { u.IsActive, u.IsDeleted })
                .HasDatabaseName("IX_Users_Active_NotDeleted");

            userEntity.HasIndex(u => new { u.Role, u.IsActive })
                .HasDatabaseName("IX_Users_Role_Active");

            // Table name
            userEntity.ToTable("Users");
        }

        /// <summary>
        /// Configures the EmailVerificationToken entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureEmailVerificationTokenEntity(ModelBuilder modelBuilder)
        {
            var tokenEntity = modelBuilder.Entity<EmailVerificationToken>();

            // Primary key
            tokenEntity.HasKey(t => t.Id);

            // String properties
            tokenEntity.Property(t => t.Token)
                .HasMaxLength(255)
                .IsRequired();

            tokenEntity.Property(t => t.Email)
                .HasMaxLength(254)
                .IsRequired();

            tokenEntity.Property(t => t.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            tokenEntity.Property(t => t.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            // Enum property
            tokenEntity.Property(t => t.TokenType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(EmailTokenType.EmailVerification);

            // Boolean properties
            tokenEntity.Property(t => t.IsUsed)
                .HasDefaultValue(false);

            // DateTime properties
            tokenEntity.Property(t => t.ExpiresAt)
                .IsRequired();

            tokenEntity.Property(t => t.UsedAt)
                .IsRequired(false);

            // Foreign key relationship
            tokenEntity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            tokenEntity.HasIndex(t => t.Token)
                .IsUnique()
                .HasDatabaseName("IX_EmailVerificationTokens_Token");

            tokenEntity.HasIndex(t => t.Email)
                .HasDatabaseName("IX_EmailVerificationTokens_Email");

            tokenEntity.HasIndex(t => t.UserId)
                .HasDatabaseName("IX_EmailVerificationTokens_UserId");

            tokenEntity.HasIndex(t => t.ExpiresAt)
                .HasDatabaseName("IX_EmailVerificationTokens_ExpiresAt");

            tokenEntity.HasIndex(t => t.TokenType)
                .HasDatabaseName("IX_EmailVerificationTokens_TokenType");

            tokenEntity.HasIndex(t => t.IsUsed)
                .HasDatabaseName("IX_EmailVerificationTokens_IsUsed");

            tokenEntity.HasIndex(t => t.CreatedAt)
                .HasDatabaseName("IX_EmailVerificationTokens_CreatedAt");

            // Composite indexes for common queries
            tokenEntity.HasIndex(t => new { t.Email, t.TokenType, t.IsUsed })
                .HasDatabaseName("IX_EmailVerificationTokens_Email_Type_Used");

            tokenEntity.HasIndex(t => new { t.UserId, t.TokenType, t.IsUsed })
                .HasDatabaseName("IX_EmailVerificationTokens_UserId_Type_Used");

            tokenEntity.HasIndex(t => new { t.ExpiresAt, t.IsUsed })
                .HasDatabaseName("IX_EmailVerificationTokens_ExpiresAt_Used");

            // Table name
            tokenEntity.ToTable("EmailVerificationTokens");
        }

        /// <summary>
        /// Configures the Post entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigurePostEntity(ModelBuilder modelBuilder)
        {
            var postEntity = modelBuilder.Entity<Post>();

            // Primary key
            postEntity.HasKey(p => p.Id);

            // String properties
            postEntity.Property(p => p.Title)
                .HasMaxLength(200)
                .IsRequired();

            postEntity.Property(p => p.Slug)
                .HasMaxLength(200)
                .IsRequired();

            postEntity.Property(p => p.Summary)
                .HasMaxLength(500)
                .IsRequired(false);

            postEntity.Property(p => p.Content)
                .IsRequired();

            postEntity.Property(p => p.ContentType)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("markdown");

            // SEO properties
            postEntity.Property(p => p.MetaTitle)
                .HasMaxLength(200)
                .IsRequired(false);

            postEntity.Property(p => p.MetaDescription)
                .HasMaxLength(500)
                .IsRequired(false);

            postEntity.Property(p => p.MetaKeywords)
                .HasMaxLength(500)
                .IsRequired(false);

            postEntity.Property(p => p.CanonicalUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            // Social media properties
            postEntity.Property(p => p.OgTitle)
                .HasMaxLength(200)
                .IsRequired(false);

            postEntity.Property(p => p.OgDescription)
                .HasMaxLength(500)
                .IsRequired(false);

            postEntity.Property(p => p.OgImageUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            postEntity.Property(p => p.Language)
                .HasMaxLength(10)
                .IsRequired()
                .HasDefaultValue("zh-CN");

            // Enum properties
            postEntity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(PostStatus.Draft);

            // Default values for counts
            postEntity.Property(p => p.ViewCount).HasDefaultValue(0);
            postEntity.Property(p => p.LikeCount).HasDefaultValue(0);
            postEntity.Property(p => p.CommentCount).HasDefaultValue(0);
            postEntity.Property(p => p.ShareCount).HasDefaultValue(0);

            // Boolean properties with defaults
            postEntity.Property(p => p.AllowComments).HasDefaultValue(true);
            postEntity.Property(p => p.IsFeatured).HasDefaultValue(false);
            postEntity.Property(p => p.IsSticky).HasDefaultValue(false);

            // Foreign key relationships
            postEntity.HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            postEntity.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            postEntity.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Posts_Slug");

            postEntity.HasIndex(p => p.Status)
                .HasDatabaseName("IX_Posts_Status");

            postEntity.HasIndex(p => p.PublishedAt)
                .HasDatabaseName("IX_Posts_PublishedAt");

            postEntity.HasIndex(p => p.AuthorId)
                .HasDatabaseName("IX_Posts_AuthorId");

            postEntity.HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Posts_CategoryId");

            postEntity.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Posts_CreatedAt");

            postEntity.HasIndex(p => p.IsDeleted)
                .HasDatabaseName("IX_Posts_IsDeleted");

            // Composite indexes for common queries
            postEntity.HasIndex(p => new { p.Status, p.PublishedAt })
                .HasDatabaseName("IX_Posts_Status_PublishedAt");

            postEntity.HasIndex(p => new { p.AuthorId, p.Status })
                .HasDatabaseName("IX_Posts_AuthorId_Status");

            postEntity.HasIndex(p => new { p.CategoryId, p.Status, p.PublishedAt })
                .HasDatabaseName("IX_Posts_CategoryId_Status_PublishedAt");

            // Table name
            postEntity.ToTable("Posts");
        }

        /// <summary>
        /// Configures the Category entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureCategoryEntity(ModelBuilder modelBuilder)
        {
            var categoryEntity = modelBuilder.Entity<Category>();

            // Primary key
            categoryEntity.HasKey(c => c.Id);

            // String properties
            categoryEntity.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            categoryEntity.Property(c => c.Slug)
                .HasMaxLength(100)
                .IsRequired();

            categoryEntity.Property(c => c.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            categoryEntity.Property(c => c.Color)
                .HasMaxLength(7)
                .IsRequired(false);

            categoryEntity.Property(c => c.Icon)
                .HasMaxLength(50)
                .IsRequired(false);

            categoryEntity.Property(c => c.CoverImageUrl)
                .HasMaxLength(500)
                .IsRequired(false);

            // Default values
            categoryEntity.Property(c => c.PostCount).HasDefaultValue(0);
            categoryEntity.Property(c => c.SortOrder).HasDefaultValue(0);
            categoryEntity.Property(c => c.IsActive).HasDefaultValue(true);

            // Hierarchical relationship (self-referencing)
            categoryEntity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indexes
            categoryEntity.HasIndex(c => c.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Categories_Slug");

            categoryEntity.HasIndex(c => c.Name)
                .HasDatabaseName("IX_Categories_Name");

            categoryEntity.HasIndex(c => c.ParentId)
                .HasDatabaseName("IX_Categories_ParentId");

            categoryEntity.HasIndex(c => c.DisplayOrder)
                .HasDatabaseName("IX_Categories_DisplayOrder");

            categoryEntity.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Categories_IsActive");

            categoryEntity.HasIndex(c => c.IsDeleted)
                .HasDatabaseName("IX_Categories_IsDeleted");

            // Table name
            categoryEntity.ToTable("Categories");
        }

        /// <summary>
        /// Configures the Tag entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureTagEntity(ModelBuilder modelBuilder)
        {
            var tagEntity = modelBuilder.Entity<Tag>();

            // Primary key
            tagEntity.HasKey(t => t.Id);

            // String properties
            tagEntity.Property(t => t.Name)
                .HasMaxLength(50)
                .IsRequired();

            tagEntity.Property(t => t.Slug)
                .HasMaxLength(50)
                .IsRequired();

            tagEntity.Property(t => t.Description)
                .HasMaxLength(200)
                .IsRequired(false);

            tagEntity.Property(t => t.Color)
                .HasMaxLength(7)
                .IsRequired(false);

            // Default values
            tagEntity.Property(t => t.UsageCount).HasDefaultValue(0);
            tagEntity.Property(t => t.IsActive).HasDefaultValue(true);

            // Indexes
            tagEntity.HasIndex(t => t.Name)
                .IsUnique()
                .HasDatabaseName("IX_Tags_Name");

            tagEntity.HasIndex(t => t.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Tags_Slug");

            tagEntity.HasIndex(t => t.UsageCount)
                .HasDatabaseName("IX_Tags_UsageCount");

            tagEntity.HasIndex(t => t.IsActive)
                .HasDatabaseName("IX_Tags_IsActive");

            tagEntity.HasIndex(t => t.IsDeleted)
                .HasDatabaseName("IX_Tags_IsDeleted");

            // Table name
            tagEntity.ToTable("Tags");
        }

        /// <summary>
        /// Configures the PostTag entity mapping (many-to-many)
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigurePostTagEntity(ModelBuilder modelBuilder)
        {
            var postTagEntity = modelBuilder.Entity<PostTag>();

            // Composite primary key
            postTagEntity.HasKey(pt => new { pt.PostId, pt.TagId });

            // Relationships
            postTagEntity.HasOne(pt => pt.Post)
                .WithMany(p => p.PostTags)
                .HasForeignKey(pt => pt.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            postTagEntity.HasOne(pt => pt.Tag)
                .WithMany(t => t.PostTags)
                .HasForeignKey(pt => pt.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            postTagEntity.HasIndex(pt => pt.PostId)
                .HasDatabaseName("IX_PostTags_PostId");

            postTagEntity.HasIndex(pt => pt.TagId)
                .HasDatabaseName("IX_PostTags_TagId");

            // Table name
            postTagEntity.ToTable("PostTags");
        }

        /// <summary>
        /// Configures the Comment entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureCommentEntity(ModelBuilder modelBuilder)
        {
            var commentEntity = modelBuilder.Entity<Comment>();

            // Primary key
            commentEntity.HasKey(c => c.Id);

            // String properties
            commentEntity.Property(c => c.Content)
                .IsRequired();

            commentEntity.Property(c => c.AuthorName)
                .HasMaxLength(100)
                .IsRequired();

            commentEntity.Property(c => c.AuthorEmail)
                .HasMaxLength(254)
                .IsRequired();

            // AuthorWebsite property doesn't exist in Comment entity
            // Removing this configuration

            commentEntity.Property(c => c.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            commentEntity.Property(c => c.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            // Default values
            commentEntity.Property(c => c.LikeCount).HasDefaultValue(0);
            // IsApproved is a computed property based on Status
            // Removing this configuration

            // Relationships
            commentEntity.HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            commentEntity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Self-referencing for nested comments
            commentEntity.HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indexes
            commentEntity.HasIndex(c => c.PostId)
                .HasDatabaseName("IX_Comments_PostId");

            commentEntity.HasIndex(c => c.AuthorId)
                .HasDatabaseName("IX_Comments_AuthorId");

            commentEntity.HasIndex(c => c.ParentId)
                .HasDatabaseName("IX_Comments_ParentId");

            commentEntity.HasIndex(c => c.CreatedAt)
                .HasDatabaseName("IX_Comments_CreatedAt");

            commentEntity.HasIndex(c => c.Status)
                .HasDatabaseName("IX_Comments_Status");

            commentEntity.HasIndex(c => c.IsDeleted)
                .HasDatabaseName("IX_Comments_IsDeleted");

            // Composite indexes
            commentEntity.HasIndex(c => new { c.PostId, c.Status, c.CreatedAt })
                .HasDatabaseName("IX_Comments_PostId_Status_CreatedAt");

            // Table name
            commentEntity.ToTable("Comments");
        }

        /// <summary>
        /// Configures the File entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureFileEntity(ModelBuilder modelBuilder)
        {
            var fileEntity = modelBuilder.Entity<Domain.Entities.File>();

            // Primary key
            fileEntity.HasKey(f => f.Id);

            // String properties
            fileEntity.Property(f => f.OriginalFileName)
                .HasMaxLength(255)
                .IsRequired();

            fileEntity.Property(f => f.FileName)
                .HasMaxLength(255)
                .IsRequired();

            fileEntity.Property(f => f.Extension)
                .HasMaxLength(10)
                .IsRequired();

            fileEntity.Property(f => f.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            fileEntity.Property(f => f.FilePath)
                .HasMaxLength(500)
                .IsRequired();

            fileEntity.Property(f => f.Directory)
                .HasMaxLength(100)
                .IsRequired();

            fileEntity.Property(f => f.FileHash)
                .HasMaxLength(64)
                .IsRequired(false);

            fileEntity.Property(f => f.Tags)
                .HasMaxLength(500)
                .IsRequired(false);

            fileEntity.Property(f => f.Description)
                .HasMaxLength(1000)
                .IsRequired(false);

            fileEntity.Property(f => f.MetadataJson)
                .IsRequired(false);

            fileEntity.Property(f => f.UploadIpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            fileEntity.Property(f => f.UploadUserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            // Numeric properties with defaults
            fileEntity.Property(f => f.FileSize).IsRequired();
            fileEntity.Property(f => f.ReferenceCount).HasDefaultValue(0);
            fileEntity.Property(f => f.AccessCount).HasDefaultValue(0);
            fileEntity.Property(f => f.ImageWidth).IsRequired(false);
            fileEntity.Property(f => f.ImageHeight).IsRequired(false);

            // Boolean properties with defaults
            fileEntity.Property(f => f.IsInUse).HasDefaultValue(false);
            fileEntity.Property(f => f.IsPublic).HasDefaultValue(true);

            // Enum properties
            fileEntity.Property(f => f.AccessLevel)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(FileAccessLevel.Public);

            // Foreign key relationships
            fileEntity.HasOne(f => f.User)
                .WithMany(u => u.Files)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            fileEntity.HasIndex(f => f.UserId)
                .HasDatabaseName("IX_Files_UserId");

            fileEntity.HasIndex(f => f.FileName)
                .HasDatabaseName("IX_Files_FileName");

            fileEntity.HasIndex(f => f.FileHash)
                .HasDatabaseName("IX_Files_FileHash");

            fileEntity.HasIndex(f => f.Directory)
                .HasDatabaseName("IX_Files_Directory");

            fileEntity.HasIndex(f => f.ContentType)
                .HasDatabaseName("IX_Files_ContentType");

            fileEntity.HasIndex(f => f.IsInUse)
                .HasDatabaseName("IX_Files_IsInUse");

            fileEntity.HasIndex(f => f.AccessLevel)
                .HasDatabaseName("IX_Files_AccessLevel");

            fileEntity.HasIndex(f => f.CreatedAt)
                .HasDatabaseName("IX_Files_CreatedAt");

            fileEntity.HasIndex(f => f.IsDeleted)
                .HasDatabaseName("IX_Files_IsDeleted");

            // Composite indexes for common queries
            fileEntity.HasIndex(f => new { f.UserId, f.IsDeleted })
                .HasDatabaseName("IX_Files_UserId_IsDeleted");

            fileEntity.HasIndex(f => new { f.Directory, f.IsInUse })
                .HasDatabaseName("IX_Files_Directory_IsInUse");

            fileEntity.HasIndex(f => new { f.AccessLevel, f.IsPublic, f.IsDeleted })
                .HasDatabaseName("IX_Files_AccessLevel_IsPublic_IsDeleted");

            // Table name
            fileEntity.ToTable("Files");
        }

        /// <summary>
        /// Configures the LoginHistory entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureLoginHistoryEntity(ModelBuilder modelBuilder)
        {
            var loginHistoryEntity = modelBuilder.Entity<LoginHistory>();

            // Primary key
            loginHistoryEntity.HasKey(lh => lh.Id);

            // String properties
            loginHistoryEntity.Property(lh => lh.Email)
                .HasMaxLength(254)
                .IsRequired();

            loginHistoryEntity.Property(lh => lh.UserName)
                .HasMaxLength(50)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.FailureReason)
                .HasMaxLength(500)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.UserAgent)
                .HasMaxLength(1000)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.DeviceInfo)
                .HasMaxLength(200)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.BrowserInfo)
                .HasMaxLength(200)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.OperatingSystem)
                .HasMaxLength(200)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.Location)
                .HasMaxLength(200)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.Country)
                .HasMaxLength(100)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.City)
                .HasMaxLength(100)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.SessionId)
                .HasMaxLength(255)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.TwoFactorMethod)
                .HasMaxLength(50)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.MetadataJson)
                .IsRequired(false);

            loginHistoryEntity.Property(lh => lh.RiskFactors)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Enum properties
            loginHistoryEntity.Property(lh => lh.Result)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired()
                .HasDefaultValue(LoginResult.Failed);

            loginHistoryEntity.Property(lh => lh.LoginType)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired()
                .HasDefaultValue(LoginType.Standard);

            // Boolean properties with defaults
            loginHistoryEntity.Property(lh => lh.IsSuccessful).HasDefaultValue(false);
            loginHistoryEntity.Property(lh => lh.TwoFactorUsed).HasDefaultValue(false);
            loginHistoryEntity.Property(lh => lh.IsFlagged).HasDefaultValue(false);
            loginHistoryEntity.Property(lh => lh.IsBlocked).HasDefaultValue(false);

            // Numeric properties with defaults
            loginHistoryEntity.Property(lh => lh.RiskScore).HasDefaultValue(0);
            loginHistoryEntity.Property(lh => lh.SessionDurationMinutes).IsRequired(false);

            // DateTime properties
            loginHistoryEntity.Property(lh => lh.SessionExpiresAt).IsRequired(false);
            loginHistoryEntity.Property(lh => lh.LogoutAt).IsRequired(false);

            // Foreign key relationships
            loginHistoryEntity.HasOne(lh => lh.User)
                .WithMany(u => u.LoginHistories)
                .HasForeignKey(lh => lh.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            loginHistoryEntity.HasIndex(lh => lh.UserId)
                .HasDatabaseName("IX_LoginHistories_UserId");

            loginHistoryEntity.HasIndex(lh => lh.Email)
                .HasDatabaseName("IX_LoginHistories_Email");

            loginHistoryEntity.HasIndex(lh => lh.IpAddress)
                .HasDatabaseName("IX_LoginHistories_IpAddress");

            loginHistoryEntity.HasIndex(lh => lh.IsSuccessful)
                .HasDatabaseName("IX_LoginHistories_IsSuccessful");

            loginHistoryEntity.HasIndex(lh => lh.Result)
                .HasDatabaseName("IX_LoginHistories_Result");

            loginHistoryEntity.HasIndex(lh => lh.LoginType)
                .HasDatabaseName("IX_LoginHistories_LoginType");

            loginHistoryEntity.HasIndex(lh => lh.CreatedAt)
                .HasDatabaseName("IX_LoginHistories_CreatedAt");

            loginHistoryEntity.HasIndex(lh => lh.RiskScore)
                .HasDatabaseName("IX_LoginHistories_RiskScore");

            loginHistoryEntity.HasIndex(lh => lh.IsFlagged)
                .HasDatabaseName("IX_LoginHistories_IsFlagged");

            loginHistoryEntity.HasIndex(lh => lh.IsBlocked)
                .HasDatabaseName("IX_LoginHistories_IsBlocked");

            // Composite indexes for common queries
            loginHistoryEntity.HasIndex(lh => new { lh.UserId, lh.IsSuccessful, lh.CreatedAt })
                .HasDatabaseName("IX_LoginHistories_UserId_Success_CreatedAt");

            loginHistoryEntity.HasIndex(lh => new { lh.Email, lh.IsSuccessful, lh.CreatedAt })
                .HasDatabaseName("IX_LoginHistories_Email_Success_CreatedAt");

            loginHistoryEntity.HasIndex(lh => new { lh.IpAddress, lh.CreatedAt })
                .HasDatabaseName("IX_LoginHistories_IpAddress_CreatedAt");

            loginHistoryEntity.HasIndex(lh => new { lh.Result, lh.CreatedAt })
                .HasDatabaseName("IX_LoginHistories_Result_CreatedAt");

            // Table name
            loginHistoryEntity.ToTable("LoginHistories");
        }

        /// <summary>
        /// Configures value object converters
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureValueObjects(ModelBuilder modelBuilder)
        {
            // Email value object converter
            var emailConverter = new ValueConverter<Email, string>(
                v => v.Value,
                v => Email.Create(v));

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .HasConversion(emailConverter);

            // CommentContent value object converter - store as JSON
            var commentContentConverter = new ValueConverter<CommentContent, string>(
                v => System.Text.Json.JsonSerializer.Serialize(new {
                    RawContent = v.RawContent,
                    ProcessedContent = v.ProcessedContent,
                    ContentType = v.ContentType,
                    ContainsSensitiveContent = v.ContainsSensitiveContent,
                    Summary = v.Summary
                }, (System.Text.Json.JsonSerializerOptions?)null),
                v => CommentContentFromJson(v));

            modelBuilder.Entity<Comment>()
                .Property(c => c.Content)
                .HasConversion(commentContentConverter)
                .HasColumnType("TEXT");

            // ThreadPath value object converter - store as path string
            var threadPathConverter = new ValueConverter<ThreadPath, string>(
                v => v.Path,
                v => ThreadPath.FromString(v));

            modelBuilder.Entity<Comment>()
                .Property(c => c.ThreadPath)
                .HasConversion(threadPathConverter)
                .HasColumnType("TEXT");

            // Note: Post, Category, and Tag entities use string properties for Slug and Content
            // The Slug and Content value objects are used in the application layer for validation and processing
        }

        /// <summary>
        /// Seeds initial data
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void SeedData(ModelBuilder modelBuilder)
        {
            // Create default admin user
            var adminId = new Guid("11111111-1111-1111-1111-111111111111");
            var adminEmail = "admin@mapleblog.com";

            // Use real BCrypt password hashing (password: "Admin123!")
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

            var adminUser = new
            {
                Id = adminId,
                Email = Email.Create(adminEmail),
                UserName = "admin",
                PasswordHash = adminPasswordHash,
                SecurityStamp = Guid.NewGuid().ToString(),
                FirstName = "System",
                LastName = "Administrator",
                DisplayName = "Administrator",
                AvatarUrl = (string?)null,
                Bio = (string?)null,
                Website = (string?)null,
                Location = (string?)null,
                PhoneNumber = (string?)null,
                Gender = (string?)null,
                Role = MapleBlog.Domain.Enums.UserRole.Admin,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                IsVerified = true,
                IsActive = true,
                AccessFailedCount = 0,
                LastLoginAt = (DateTime?)null,
                EmailVerificationToken = (string?)null,
                EmailVerificationTokenExpiry = (DateTime?)null,
                PasswordResetToken = (string?)null,
                PasswordResetTokenExpiresAt = (DateTime?)null,
                LockoutEndDateUtc = (DateTime?)null,
                DateOfBirth = (DateTime?)null,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = (DateTime?)null,
                CreatedBy = (Guid?)null,
                UpdatedBy = (Guid?)null,
                IsDeleted = false,
                DeletedAt = (DateTime?)null,
                DeletedBy = (Guid?)null,
                Version = (byte[]?)null
            };

            modelBuilder.Entity<User>().HasData(adminUser);
        }

        /// <summary>
        /// Configures database provider specific options
        /// </summary>
        /// <param name="optionsBuilder">The options builder</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Suppress accidental entity type warning for List<object>
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.AccidentalEntityType));

            // Enable sensitive data logging in development
            if (!optionsBuilder.IsConfigured)
            {
#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
#endif
            }
        }

        /// <summary>
        /// Saves changes with automatic timestamp updates
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of affected entries</returns>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Saves changes with automatic timestamp updates
        /// </summary>
        /// <returns>Number of affected entries</returns>
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        /// <summary>
        /// Updates timestamps for modified entities
        /// </summary>
        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Configures the Role entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureRoleEntity(ModelBuilder modelBuilder)
        {
            var roleEntity = modelBuilder.Entity<Role>();

            // Primary key
            roleEntity.HasKey(r => r.Id);

            // String properties
            roleEntity.Property(r => r.Name)
                .HasMaxLength(50)
                .IsRequired();

            roleEntity.Property(r => r.NormalizedName)
                .HasMaxLength(50)
                .IsRequired();

            roleEntity.Property(r => r.Description)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Boolean properties with defaults
            roleEntity.Property(r => r.IsSystemRole)
                .HasDefaultValue(false);

            roleEntity.Property(r => r.IsActive)
                .HasDefaultValue(true);

            // Indexes
            roleEntity.HasIndex(r => r.Name)
                .IsUnique()
                .HasDatabaseName("IX_Roles_Name");

            roleEntity.HasIndex(r => r.NormalizedName)
                .IsUnique()
                .HasDatabaseName("IX_Roles_NormalizedName");

            roleEntity.HasIndex(r => r.IsActive)
                .HasDatabaseName("IX_Roles_IsActive");

            roleEntity.HasIndex(r => r.IsSystemRole)
                .HasDatabaseName("IX_Roles_IsSystemRole");

            // Table name
            roleEntity.ToTable("Roles");
        }

        /// <summary>
        /// Configures the Permission entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigurePermissionEntity(ModelBuilder modelBuilder)
        {
            var permissionEntity = modelBuilder.Entity<Permission>();

            // Primary key
            permissionEntity.HasKey(p => p.Id);

            // String properties
            permissionEntity.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            permissionEntity.Property(p => p.Resource)
                .HasMaxLength(50)
                .IsRequired();

            permissionEntity.Property(p => p.Action)
                .HasMaxLength(50)
                .IsRequired();

            permissionEntity.Property(p => p.Description)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Boolean properties with defaults
            permissionEntity.Property(p => p.IsSystemPermission)
                .HasDefaultValue(false);

            // Indexes
            permissionEntity.HasIndex(p => p.Name)
                .IsUnique()
                .HasDatabaseName("IX_Permissions_Name");

            permissionEntity.HasIndex(p => new { p.Resource, p.Action })
                .IsUnique()
                .HasDatabaseName("IX_Permissions_Resource_Action");

            permissionEntity.HasIndex(p => p.Resource)
                .HasDatabaseName("IX_Permissions_Resource");

            permissionEntity.HasIndex(p => p.IsSystemPermission)
                .HasDatabaseName("IX_Permissions_IsSystemPermission");

            // Table name
            permissionEntity.ToTable("Permissions");
        }

        /// <summary>
        /// Configures the UserRole entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureUserRoleEntity(ModelBuilder modelBuilder)
        {
            var userRoleEntity = modelBuilder.Entity<Domain.Entities.UserRole>();

            // Composite primary key
            userRoleEntity.HasKey(ur => new { ur.UserId, ur.RoleId });

            // Properties
            userRoleEntity.Property(ur => ur.AssignedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            userRoleEntity.Property(ur => ur.IsActive)
                .HasDefaultValue(true);

            userRoleEntity.Property(ur => ur.ExpiresAt)
                .IsRequired(false);

            // Relationships
            userRoleEntity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            userRoleEntity.HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            userRoleEntity.HasOne(ur => ur.Assigner)
                .WithMany(u => u.AssignedUserRoles)
                .HasForeignKey(ur => ur.AssignedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes
            userRoleEntity.HasIndex(ur => ur.UserId)
                .HasDatabaseName("IX_UserRoles_UserId");

            userRoleEntity.HasIndex(ur => ur.RoleId)
                .HasDatabaseName("IX_UserRoles_RoleId");

            userRoleEntity.HasIndex(ur => new { ur.UserId, ur.IsActive, ur.ExpiresAt })
                .HasDatabaseName("IX_UserRoles_UserId_IsActive_ExpiresAt");

            userRoleEntity.HasIndex(ur => ur.AssignedBy)
                .HasDatabaseName("IX_UserRoles_AssignedBy");

            // Table name
            userRoleEntity.ToTable("UserRoles");
        }

        /// <summary>
        /// Configures the RolePermission entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureRolePermissionEntity(ModelBuilder modelBuilder)
        {
            var rolePermissionEntity = modelBuilder.Entity<RolePermission>();

            // Composite primary key
            rolePermissionEntity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Properties
            rolePermissionEntity.Property(rp => rp.GrantedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            rolePermissionEntity.HasOne(rp => rp.Role)
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            rolePermissionEntity.HasOne(rp => rp.Permission)
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            rolePermissionEntity.HasOne(rp => rp.Granter)
                .WithMany(u => u.GrantedRolePermissions)
                .HasForeignKey(rp => rp.GrantedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes
            rolePermissionEntity.HasIndex(rp => rp.RoleId)
                .HasDatabaseName("IX_RolePermissions_RoleId");

            rolePermissionEntity.HasIndex(rp => rp.PermissionId)
                .HasDatabaseName("IX_RolePermissions_PermissionId");

            rolePermissionEntity.HasIndex(rp => rp.GrantedBy)
                .HasDatabaseName("IX_RolePermissions_GrantedBy");

            // Table name
            rolePermissionEntity.ToTable("RolePermissions");
        }

        /// <summary>
        /// Configures the SearchQuery entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureSearchQueryEntity(ModelBuilder modelBuilder)
        {
            var searchQueryEntity = modelBuilder.Entity<SearchQuery>();

            // Primary key
            searchQueryEntity.HasKey(sq => sq.Id);

            // String properties
            searchQueryEntity.Property(sq => sq.Query)
                .HasMaxLength(500)
                .IsRequired();

            searchQueryEntity.Property(sq => sq.NormalizedQuery)
                .HasMaxLength(500)
                .IsRequired();

            searchQueryEntity.Property(sq => sq.SearchType)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("general");

            searchQueryEntity.Property(sq => sq.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            searchQueryEntity.Property(sq => sq.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            // Numeric properties with defaults
            searchQueryEntity.Property(sq => sq.ResultCount)
                .HasDefaultValue(0);

            searchQueryEntity.Property(sq => sq.ExecutionTime)
                .IsRequired(false);

            // DateTime properties
            searchQueryEntity.Property(sq => sq.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Complex properties stored as JSON
            searchQueryEntity.Property(sq => sq.Filters)
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null))
                .HasColumnType("TEXT")
                .IsRequired(false);

            searchQueryEntity.Property(sq => sq.ClickedResults)
                .HasConversion(
                    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<List<object>>(v, (System.Text.Json.JsonSerializerOptions?)null))
                .HasColumnType("TEXT")
                .IsRequired(false);

            // Foreign key relationships
            searchQueryEntity.HasOne(sq => sq.User)
                .WithMany(u => u.SearchQueries)
                .HasForeignKey(sq => sq.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            searchQueryEntity.HasIndex(sq => sq.UserId)
                .HasDatabaseName("IX_SearchQueries_UserId");

            searchQueryEntity.HasIndex(sq => sq.NormalizedQuery)
                .HasDatabaseName("IX_SearchQueries_NormalizedQuery");

            searchQueryEntity.HasIndex(sq => sq.SearchType)
                .HasDatabaseName("IX_SearchQueries_SearchType");

            searchQueryEntity.HasIndex(sq => sq.CreatedAt)
                .HasDatabaseName("IX_SearchQueries_CreatedAt");

            searchQueryEntity.HasIndex(sq => sq.IpAddress)
                .HasDatabaseName("IX_SearchQueries_IpAddress");

            // Composite indexes for common queries
            searchQueryEntity.HasIndex(sq => new { sq.UserId, sq.CreatedAt })
                .HasDatabaseName("IX_SearchQueries_UserId_CreatedAt");

            searchQueryEntity.HasIndex(sq => new { sq.SearchType, sq.CreatedAt })
                .HasDatabaseName("IX_SearchQueries_SearchType_CreatedAt");

            // Table name
            searchQueryEntity.ToTable("SearchQueries");
        }

        /// <summary>
        /// Configures the SearchIndex entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureSearchIndexEntity(ModelBuilder modelBuilder)
        {
            var searchIndexEntity = modelBuilder.Entity<SearchIndex>();

            // Primary key
            searchIndexEntity.HasKey(si => si.Id);

            // String properties
            searchIndexEntity.Property(si => si.EntityType)
                .HasMaxLength(50)
                .IsRequired();

            searchIndexEntity.Property(si => si.Title)
                .HasMaxLength(500)
                .IsRequired(false);

            searchIndexEntity.Property(si => si.Content)
                .IsRequired(false);

            searchIndexEntity.Property(si => si.Keywords)
                .IsRequired(false);

            searchIndexEntity.Property(si => si.SearchVector)
                .IsRequired(false);

            searchIndexEntity.Property(si => si.Language)
                .HasMaxLength(10)
                .IsRequired()
                .HasDefaultValue("zh-CN");

            // Decimal properties with defaults
            searchIndexEntity.Property(si => si.TitleWeight)
                .HasDefaultValue(1.0m);

            searchIndexEntity.Property(si => si.ContentWeight)
                .HasDefaultValue(0.5m);

            searchIndexEntity.Property(si => si.KeywordWeight)
                .HasDefaultValue(0.8m);

            // Boolean properties with defaults
            searchIndexEntity.Property(si => si.IsActive)
                .HasDefaultValue(true);

            // DateTime properties
            searchIndexEntity.Property(si => si.IndexedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            searchIndexEntity.Property(si => si.LastUpdatedAt)
                .IsRequired(false);

            // Indexes for performance
            searchIndexEntity.HasIndex(si => si.EntityId)
                .HasDatabaseName("IX_SearchIndexes_EntityId");

            searchIndexEntity.HasIndex(si => si.EntityType)
                .HasDatabaseName("IX_SearchIndexes_EntityType");

            searchIndexEntity.HasIndex(si => si.Language)
                .HasDatabaseName("IX_SearchIndexes_Language");

            searchIndexEntity.HasIndex(si => si.IndexedAt)
                .HasDatabaseName("IX_SearchIndexes_IndexedAt");

            searchIndexEntity.HasIndex(si => si.IsActive)
                .HasDatabaseName("IX_SearchIndexes_IsActive");

            // Composite indexes for common queries
            searchIndexEntity.HasIndex(si => new { si.EntityType, si.EntityId })
                .IsUnique()
                .HasDatabaseName("IX_SearchIndexes_EntityType_EntityId");

            searchIndexEntity.HasIndex(si => new { si.Language, si.IsActive })
                .HasDatabaseName("IX_SearchIndexes_Language_IsActive");

            // Table name
            searchIndexEntity.ToTable("SearchIndexes");
        }

        /// <summary>
        /// Helper method to deserialize CommentContent from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>CommentContent value object</returns>
        private static CommentContent CommentContentFromJson(string json)
        {
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
            var rawContent = data.GetProperty("RawContent").GetString() ?? "";
            var contentType = data.GetProperty("ContentType").GetString() ?? "markdown";
            return CommentContent.Create(rawContent, contentType);
        }

        /// <summary>
        /// Configures the DataPermissionRule entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureDataPermissionRuleEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DataPermissionRule>();

            // Primary key
            entity.HasKey(r => r.Id);

            // String properties
            entity.Property(r => r.ResourceType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(r => r.Conditions)
                .HasColumnType("TEXT")
                .IsRequired(false);

            entity.Property(r => r.Remarks)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Enum properties
            entity.Property(r => r.Operation)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(r => r.Scope)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(r => r.Source)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(PermissionSource.Direct);

            // Boolean properties with defaults
            entity.Property(r => r.IsAllowed)
                .HasDefaultValue(true);

            entity.Property(r => r.IsTemporary)
                .HasDefaultValue(false);

            entity.Property(r => r.IsActive)
                .HasDefaultValue(true);

            // Numeric properties with defaults
            entity.Property(r => r.Priority)
                .HasDefaultValue(0);

            // DateTime properties
            entity.Property(r => r.EffectiveFrom)
                .IsRequired(false);

            entity.Property(r => r.EffectiveTo)
                .IsRequired(false);

            // Foreign key relationships
            entity.HasOne(r => r.User)
                .WithMany(u => u.DataPermissionRules)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DataPermissionRules_User");

            entity.HasOne(r => r.Role)
                .WithMany()
                .HasForeignKey(r => r.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasOne(d => d.GrantedByUser)
                .WithMany(u => u.GrantedDataPermissionRules)
                .HasForeignKey(r => r.GrantedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false)
                .HasConstraintName("FK_DataPermissionRules_GrantedByUser");

            // Indexes for performance
            entity.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_DataPermissionRules_UserId");

            entity.HasIndex(r => r.RoleId)
                .HasDatabaseName("IX_DataPermissionRules_RoleId");

            entity.HasIndex(r => r.ResourceType)
                .HasDatabaseName("IX_DataPermissionRules_ResourceType");

            entity.HasIndex(r => r.Operation)
                .HasDatabaseName("IX_DataPermissionRules_Operation");

            entity.HasIndex(r => r.IsActive)
                .HasDatabaseName("IX_DataPermissionRules_IsActive");

            entity.HasIndex(r => r.Priority)
                .HasDatabaseName("IX_DataPermissionRules_Priority");

            entity.HasIndex(r => new { r.EffectiveFrom, r.EffectiveTo })
                .HasDatabaseName("IX_DataPermissionRules_EffectivePeriod");

            // Composite indexes for common queries
            entity.HasIndex(r => new { r.UserId, r.ResourceType, r.Operation, r.IsActive })
                .HasDatabaseName("IX_DataPermissionRules_User_Resource_Operation_Active");

            entity.HasIndex(r => new { r.RoleId, r.ResourceType, r.Operation, r.IsActive })
                .HasDatabaseName("IX_DataPermissionRules_Role_Resource_Operation_Active");

            // Table name
            entity.ToTable("DataPermissionRules");
        }

        /// <summary>
        /// Configures the TemporaryPermission entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureTemporaryPermissionEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<TemporaryPermission>();

            // Primary key
            entity.HasKey(p => p.Id);

            // String properties
            entity.Property(p => p.ResourceType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Reason)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(p => p.RevokeReason)
                .HasMaxLength(1000)
                .IsRequired(false);

            // Enum properties
            entity.Property(p => p.Operation)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(TemporaryPermissionType.Temporary);

            // Boolean properties with defaults
            entity.Property(p => p.IsRevoked)
                .HasDefaultValue(false);

            entity.Property(p => p.IsActive)
                .HasDefaultValue(true);

            // Numeric properties with defaults
            entity.Property(p => p.UsageLimit)
                .HasDefaultValue(0);

            entity.Property(p => p.UsedCount)
                .HasDefaultValue(0);

            // DateTime properties
            entity.Property(p => p.EffectiveFrom)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(p => p.ExpiresAt)
                .IsRequired();

            entity.Property(p => p.RevokedAt)
                .IsRequired(false);

            entity.Property(p => p.LastUsedAt)
                .IsRequired(false);

            // Foreign key relationships
            entity.HasOne(p => p.User)
                .WithMany(u => u.TemporaryPermissions)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TemporaryPermissions_User");

            entity.HasOne(p => p.GrantedByUser)
                .WithMany(u => u.GrantedTemporaryPermissions)
                .HasForeignKey(p => p.GrantedBy)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_TemporaryPermissions_GrantedByUser");

            entity.HasOne(p => p.DelegatedFromUser)
                .WithMany(u => u.DelegatedTemporaryPermissions)
                .HasForeignKey(p => p.DelegatedFrom)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false)
                .HasConstraintName("FK_TemporaryPermissions_DelegatedFromUser");

            entity.HasOne(p => p.RevokedByUser)
                .WithMany(u => u.RevokedTemporaryPermissions)
                .HasForeignKey(p => p.RevokedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false)
                .HasConstraintName("FK_TemporaryPermissions_RevokedByUser");

            // Indexes for performance
            entity.HasIndex(p => p.UserId)
                .HasDatabaseName("IX_TemporaryPermissions_UserId");

            entity.HasIndex(p => p.ResourceType)
                .HasDatabaseName("IX_TemporaryPermissions_ResourceType");

            entity.HasIndex(p => p.ResourceId)
                .HasDatabaseName("IX_TemporaryPermissions_ResourceId");

            entity.HasIndex(p => p.Operation)
                .HasDatabaseName("IX_TemporaryPermissions_Operation");

            entity.HasIndex(p => p.Type)
                .HasDatabaseName("IX_TemporaryPermissions_Type");

            entity.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_TemporaryPermissions_IsActive");

            entity.HasIndex(p => p.IsRevoked)
                .HasDatabaseName("IX_TemporaryPermissions_IsRevoked");

            entity.HasIndex(p => new { p.EffectiveFrom, p.ExpiresAt })
                .HasDatabaseName("IX_TemporaryPermissions_EffectivePeriod");

            // Composite indexes for common queries
            entity.HasIndex(p => new { p.UserId, p.ResourceType, p.ResourceId, p.Operation, p.IsActive })
                .HasDatabaseName("IX_TemporaryPermissions_User_Resource_Operation_Active");

            entity.HasIndex(p => new { p.Type, p.IsActive, p.ExpiresAt })
                .HasDatabaseName("IX_TemporaryPermissions_Type_Active_Expires");

            entity.HasIndex(p => new { p.DelegatedFrom, p.Type, p.IsActive })
                .HasDatabaseName("IX_TemporaryPermissions_DelegatedFrom_Type_Active");

            // Table name
            entity.ToTable("TemporaryPermissions");
        }

        /// <summary>
        /// Configures the AuditLog entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AuditLog>();

            // Primary key
            entity.HasKey(a => a.Id);

            // String properties
            entity.Property(a => a.TableName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(a => a.Action)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(a => a.KeyValues)
                .HasColumnType("TEXT")
                .IsRequired();

            entity.Property(a => a.OldValues)
                .HasColumnType("TEXT")
                .IsRequired(false);

            entity.Property(a => a.NewValues)
                .HasColumnType("TEXT")
                .IsRequired(false);

            entity.Property(a => a.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(a => a.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            entity.Property(a => a.AdditionalInfo)
                .HasColumnType("TEXT")
                .IsRequired(false);

            // DateTime properties
            entity.Property(a => a.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key relationships
            entity.HasOne(a => a.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            entity.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AuditLogs_UserId");

            entity.HasIndex(a => a.TableName)
                .HasDatabaseName("IX_AuditLogs_TableName");

            entity.HasIndex(a => a.Action)
                .HasDatabaseName("IX_AuditLogs_Action");

            entity.HasIndex(a => a.CreatedAt)
                .HasDatabaseName("IX_AuditLogs_CreatedAt");

            // Composite indexes for common queries
            entity.HasIndex(a => new { a.TableName, a.Action, a.CreatedAt })
                .HasDatabaseName("IX_AuditLogs_Table_Action_CreatedAt");

            entity.HasIndex(a => new { a.UserId, a.CreatedAt })
                .HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");

            // Table name
            entity.ToTable("AuditLogs");
        }

        /// <summary>
        /// Configures the StorageQuotaConfiguration entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureStorageQuotaConfigurationEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<StorageQuotaConfiguration>();

            // Primary key
            entity.HasKey(s => s.Id);

            // Enum properties
            entity.Property(s => s.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Numeric properties
            entity.Property(s => s.MaxQuotaBytes)
                .IsRequired();

            entity.Property(s => s.MaxFileCount)
                .IsRequired(false);

            entity.Property(s => s.MaxFileSize)
                .IsRequired(false);

            // String properties
            entity.Property(s => s.AllowedFileTypes)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(s => s.ForbiddenFileTypes)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(s => s.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            // Boolean properties with defaults
            entity.Property(s => s.AllowPublicFiles)
                .HasDefaultValue(true);

            entity.Property(s => s.AutoCleanupEnabled)
                .HasDefaultValue(false);

            entity.Property(s => s.IsActive)
                .HasDefaultValue(true);

            // Numeric properties with defaults
            entity.Property(s => s.WarningThreshold)
                .HasDefaultValue(80);

            entity.Property(s => s.CriticalThreshold)
                .HasDefaultValue(95);

            entity.Property(s => s.Priority)
                .HasDefaultValue(0);

            entity.Property(s => s.AutoCleanupDays)
                .IsRequired(false);

            // DateTime properties
            entity.Property(s => s.EffectiveFrom)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(s => s.EffectiveTo)
                .IsRequired(false);

            // Indexes for performance
            entity.HasIndex(s => s.Role)
                .HasDatabaseName("IX_StorageQuotaConfigurations_Role");

            entity.HasIndex(s => s.IsActive)
                .HasDatabaseName("IX_StorageQuotaConfigurations_IsActive");

            entity.HasIndex(s => s.Priority)
                .HasDatabaseName("IX_StorageQuotaConfigurations_Priority");

            entity.HasIndex(s => new { s.EffectiveFrom, s.EffectiveTo })
                .HasDatabaseName("IX_StorageQuotaConfigurations_EffectivePeriod");

            // Composite indexes for common queries
            entity.HasIndex(s => new { s.Role, s.IsActive, s.Priority })
                .HasDatabaseName("IX_StorageQuotaConfigurations_Role_Active_Priority");

            entity.HasIndex(s => new { s.IsActive, s.EffectiveFrom, s.EffectiveTo })
                .HasDatabaseName("IX_StorageQuotaConfigurations_Active_EffectivePeriod");

            // Table name
            entity.ToTable("StorageQuotaConfigurations");
        }

        /// <summary>
        /// Configures the FileShare entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureFileShareEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Domain.Entities.FileShare>();

            // Primary key
            entity.HasKey(fs => fs.Id);

            // String properties
            entity.Property(fs => fs.ShareId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(fs => fs.SharedWithEmail)
                .HasMaxLength(254)
                .IsRequired(false);

            entity.Property(fs => fs.Permission)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(fs => fs.PasswordHash)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(fs => fs.Message)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(fs => fs.RevocationReason)
                .HasMaxLength(500)
                .IsRequired(false);

            // Boolean properties with defaults
            entity.Property(fs => fs.IsActive)
                .HasDefaultValue(true);

            entity.Property(fs => fs.RequiresAuthentication)
                .HasDefaultValue(true);

            entity.Property(fs => fs.NotificationSent)
                .HasDefaultValue(false);

            // Numeric properties with defaults
            entity.Property(fs => fs.AccessCount)
                .HasDefaultValue(0);

            // Foreign key relationships
            entity.HasOne(fs => fs.File)
                .WithMany()
                .HasForeignKey(fs => fs.FileId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(fs => fs.SharedBy)
                .WithMany()
                .HasForeignKey(fs => fs.SharedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            entity.HasOne(fs => fs.SharedWith)
                .WithMany()
                .HasForeignKey(fs => fs.SharedWithId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(fs => fs.RevokedBy)
                .WithMany()
                .HasForeignKey(fs => fs.RevokedById)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            entity.HasIndex(fs => fs.ShareId)
                .IsUnique()
                .HasDatabaseName("IX_FileShares_ShareId");

            entity.HasIndex(fs => fs.FileId)
                .HasDatabaseName("IX_FileShares_FileId");

            entity.HasIndex(fs => fs.SharedById)
                .HasDatabaseName("IX_FileShares_SharedById");

            entity.HasIndex(fs => fs.SharedWithId)
                .HasDatabaseName("IX_FileShares_SharedWithId");

            entity.HasIndex(fs => fs.IsActive)
                .HasDatabaseName("IX_FileShares_IsActive");

            entity.HasIndex(fs => fs.ExpiresAt)
                .HasDatabaseName("IX_FileShares_ExpiresAt");

            // Table name
            entity.ToTable("FileShares");
        }

        /// <summary>
        /// Configures the FileAccessLog entity mapping
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        private static void ConfigureFileAccessLogEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Domain.Entities.FileAccessLog>();

            // Primary key
            entity.HasKey(log => log.Id);

            // String properties
            entity.Property(log => log.IpAddress)
                .HasMaxLength(45)
                .IsRequired(false);

            entity.Property(log => log.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(log => log.ErrorMessage)
                .HasMaxLength(1000)
                .IsRequired(false);

            entity.Property(log => log.Metadata)
                .HasColumnType("TEXT")
                .IsRequired(false);

            // Enum properties
            entity.Property(log => log.AccessType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Foreign key relationships
            entity.HasOne(log => log.File)
                .WithMany()
                .HasForeignKey(log => log.FileId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            entity.HasOne(log => log.FileShare)
                .WithMany(fs => fs.AccessLogs)
                .HasForeignKey(log => log.FileShareId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(log => log.User)
                .WithMany()
                .HasForeignKey(log => log.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Indexes for performance
            entity.HasIndex(log => log.FileId)
                .HasDatabaseName("IX_FileAccessLogs_FileId");

            entity.HasIndex(log => log.UserId)
                .HasDatabaseName("IX_FileAccessLogs_UserId");

            entity.HasIndex(log => log.FileShareId)
                .HasDatabaseName("IX_FileAccessLogs_FileShareId");

            entity.HasIndex(log => log.AccessType)
                .HasDatabaseName("IX_FileAccessLogs_AccessType");

            entity.HasIndex(log => log.CreatedAt)
                .HasDatabaseName("IX_FileAccessLogs_CreatedAt");

            // Table name
            entity.ToTable("FileAccessLogs");
        }
    }
}