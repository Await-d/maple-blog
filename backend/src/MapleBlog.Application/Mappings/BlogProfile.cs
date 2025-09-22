using AutoMapper;
using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Mappings;

/// <summary>
/// AutoMapper profile for Blog entity mappings
/// </summary>
public class BlogProfile : Profile
{
    public BlogProfile()
    {
        CreatePostMappings();
        CreateCategoryMappings();
        CreateTagMappings();
    }

    private void CreatePostMappings()
    {
        // Post -> PostDto
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)))
            .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => new PostStatsDto
            {
                ViewCount = src.ViewCount,
                LikeCount = src.LikeCount,
                CommentCount = src.CommentCount,
                ShareCount = src.ShareCount
            }))
            .ForMember(dest => dest.Settings, opt => opt.MapFrom(src => new PostSettingsDto
            {
                AllowComments = src.AllowComments,
                IsFeatured = src.IsFeatured,
                IsSticky = src.IsSticky
            }))
            .ForMember(dest => dest.Seo, opt => opt.MapFrom(src => new PostSeoDto
            {
                MetaTitle = src.MetaTitle,
                MetaDescription = src.MetaDescription,
                MetaKeywords = src.MetaKeywords,
                CanonicalUrl = src.CanonicalUrl,
                OgTitle = src.OgTitle,
                OgDescription = src.OgDescription,
                OgImageUrl = src.OgImageUrl
            }))
            .ForMember(dest => dest.ContentInfo, opt => opt.MapFrom(src => new PostContentDto
            {
                ReadingTime = src.ReadingTime,
                WordCount = src.WordCount,
                Language = src.Language
            }));

        // Post -> PostListDto
        CreateMap<Post, PostListDto>()
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag)))
            .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => new PostStatsDto
            {
                ViewCount = src.ViewCount,
                LikeCount = src.LikeCount,
                CommentCount = src.CommentCount,
                ShareCount = src.ShareCount
            }))
            .ForMember(dest => dest.ContentInfo, opt => opt.MapFrom(src => new PostContentDto
            {
                ReadingTime = src.ReadingTime,
                WordCount = src.WordCount,
                Language = src.Language
            }));

        // User -> PostAuthorDto
        CreateMap<User, PostAuthorDto>()
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.DisplayName));

        // CreatePostRequest -> Post
        CreateMap<CreatePostRequest, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.PublishedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ViewCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.ShareCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.PostStatus.Draft))
            .ForMember(dest => dest.ReadingTime, opt => opt.Ignore()) // Calculated by service
            .ForMember(dest => dest.WordCount, opt => opt.Ignore()) // Calculated by service
            .ForMember(dest => dest.Author, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore()) // Handled by service
            .ForMember(dest => dest.PostAttachments, opt => opt.Ignore())
            .ForMember(dest => dest.PostRevisions, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        // UpdatePostRequest -> Post (for updating existing entity)
        CreateMap<UpdatePostRequest, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Preserve existing status
            .ForMember(dest => dest.PublishedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ViewCount, opt => opt.Ignore())
            .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
            .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.ShareCount, opt => opt.Ignore())
            .ForMember(dest => dest.ReadingTime, opt => opt.Ignore()) // Recalculated by service
            .ForMember(dest => dest.WordCount, opt => opt.Ignore()) // Recalculated by service
            .ForMember(dest => dest.Author, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore()) // Handled by service
            .ForMember(dest => dest.PostAttachments, opt => opt.Ignore())
            .ForMember(dest => dest.PostRevisions, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
    }

    private void CreateCategoryMappings()
    {
        // Category -> CategoryDto
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.Parent, opt => opt.MapFrom(src => src.Parent))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
            .ForMember(dest => dest.Appearance, opt => opt.MapFrom(src => new CategoryAppearanceDto
            {
                Color = src.Color,
                Icon = src.Icon,
                CoverImageUrl = src.CoverImageUrl
            }))
            .ForMember(dest => dest.Seo, opt => opt.MapFrom(src => new CategorySeoDto
            {
                MetaTitle = src.MetaTitle,
                MetaDescription = src.MetaDescription,
                MetaKeywords = src.MetaKeywords
            }));

        // Category -> CategoryListDto
        CreateMap<Category, CategoryListDto>()
            .ForMember(dest => dest.Appearance, opt => opt.MapFrom(src => new CategoryAppearanceDto
            {
                Color = src.Color,
                Icon = src.Icon,
                CoverImageUrl = src.CoverImageUrl
            }));

        // Category -> CategoryTreeDto
        CreateMap<Category, CategoryTreeDto>()
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
            .ForMember(dest => dest.Appearance, opt => opt.MapFrom(src => new CategoryAppearanceDto
            {
                Color = src.Color,
                Icon = src.Icon,
                CoverImageUrl = src.CoverImageUrl
            }));

        // CreateCategoryRequest -> Category
        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Posts, opt => opt.Ignore())
            .ForMember(dest => dest.TreePath, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.Level, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.PostCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
            .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        // UpdateCategoryRequest -> Category (for updating existing entity)
        CreateMap<UpdateCategoryRequest, Category>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore())
            .ForMember(dest => dest.Posts, opt => opt.Ignore())
            .ForMember(dest => dest.TreePath, opt => opt.Ignore()) // Updated by service if parent changes
            .ForMember(dest => dest.Level, opt => opt.Ignore()) // Updated by service if parent changes
            .ForMember(dest => dest.PostCount, opt => opt.Ignore())
            .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
            .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
    }

    private void CreateTagMappings()
    {
        // Tag -> TagDto
        CreateMap<Tag, TagDto>();

        // Tag -> TagListDto
        CreateMap<Tag, TagListDto>();

        // Tag -> TagCloudDto
        CreateMap<Tag, TagCloudDto>()
            .ForMember(dest => dest.Weight, opt => opt.Ignore()); // Calculated by service

        // Tag -> TagAutoCompleteDto
        CreateMap<Tag, TagAutoCompleteDto>();

        // CreateTagRequest -> Tag
        CreateMap<CreateTagRequest, Tag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UsageCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        // UpdateTagRequest -> Tag (for updating existing entity)
        CreateMap<UpdateTagRequest, Tag>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UsageCount, opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Tag -> TagSuggestionDto
        CreateMap<Tag, TagSuggestionDto>()
            .ForMember(dest => dest.RelevanceScore, opt => opt.Ignore()) // Set by service
            .ForMember(dest => dest.IsExisting, opt => opt.MapFrom(src => true));
    }
}