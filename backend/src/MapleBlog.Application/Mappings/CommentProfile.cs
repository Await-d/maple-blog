using AutoMapper;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Entities;

namespace MapleBlog.Application.Mappings;

/// <summary>
/// 评论系统AutoMapper配置
/// </summary>
public class CommentProfile : Profile
{
    public CommentProfile()
    {
        CreateCommentMappings();
        CreateNotificationMappings();
        CreateReportMappings();
        CreateUserMappings();
    }

    /// <summary>
    /// 创建评论相关映射
    /// </summary>
    private void CreateCommentMappings()
    {
        // Comment -> CommentDto
        CreateMap<Comment, CommentDto>()
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content.RawContent))
            .ForMember(dest => dest.RenderedContent, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.ThreadPath, opt => opt.MapFrom(src => src.ThreadPath.Path))
            .ForMember(dest => dest.Depth, opt => opt.MapFrom(src => src.ThreadPath.Depth))
            .ForMember(dest => dest.IsLiked, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.CanEdit, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.CanDelete, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Replies, opt => opt.Ignore()); // 避免循环引用，在需要时单独处理

        // Comment -> CommentModerationDto
        CreateMap<Comment, CommentModerationDto>()
            .ForMember(dest => dest.CommentId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Content.RawContent))
            .ForMember(dest => dest.PostTitle, opt => opt.MapFrom(src => src.Post != null ? src.Post.Title : "未知文章"))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Moderator, opt => opt.MapFrom(src => src.Moderator))
            .ForMember(dest => dest.Reports, opt => opt.MapFrom(src => src.Reports.Where(r => !r.IsDeleted)));

        // CommentCreateDto不需要映射到Comment，因为在服务中手动创建

        // CommentStatsDto映射（如果需要从聚合查询结果映射）
        CreateMap<Comment, CommentStatsDto>()
            .ForMember(dest => dest.PostId, opt => opt.MapFrom(src => src.PostId))
            .ForMember(dest => dest.TotalCount, opt => opt.Ignore())
            .ForMember(dest => dest.RootCommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.ReplyCount, opt => opt.Ignore())
            .ForMember(dest => dest.ParticipantCount, opt => opt.Ignore())
            .ForMember(dest => dest.LatestCommentAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.LatestCommentAuthor, opt => opt.MapFrom(src => src.Author));
    }

    /// <summary>
    /// 创建通知相关映射
    /// </summary>
    private void CreateNotificationMappings()
    {
        // Notification -> CommentNotificationDto
        CreateMap<Notification, CommentNotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (CommentNotificationType)src.Type))
            .ForMember(dest => dest.Sender, opt => opt.MapFrom(src => src.Sender))
            .ForMember(dest => dest.Comment, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Metadata));

        // 通知统计信息映射（如果有实体）
        CreateMap<Notification, CommentNotificationStatsDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.RecipientId))
            .ForMember(dest => dest.TotalCount, opt => opt.Ignore())
            .ForMember(dest => dest.UnreadCount, opt => opt.Ignore())
            .ForMember(dest => dest.TodayCount, opt => opt.Ignore())
            .ForMember(dest => dest.TypeCounts, opt => opt.Ignore())
            .ForMember(dest => dest.UnreadTypeCounts, opt => opt.Ignore());
    }

    /// <summary>
    /// 创建举报相关映射
    /// </summary>
    private void CreateReportMappings()
    {
        // CommentReport -> CommentReportDto
        CreateMap<CommentReport, CommentReportDto>()
            .ForMember(dest => dest.Reporter, opt => opt.MapFrom(src => src.Reporter))
            .ForMember(dest => dest.ProcessedBy, opt => opt.MapFrom(src => src.ProcessedByUser));

        // CommentLike映射（如果需要）
        CreateMap<CommentLike, object>()
            .ForMember(dest => dest, opt => opt.MapFrom(src => new { src.Id, src.UserId, src.CreatedAt }));
    }

    /// <summary>
    /// 创建用户相关映射
    /// </summary>
    private void CreateUserMappings()
    {
        // User -> CommentAuthorDto
        CreateMap<User, CommentAuthorDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.DisplayName) ? src.DisplayName : src.UserName))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
            .ForMember(dest => dest.Role, opt => opt.Ignore()) // 将在服务中设置
            .ForMember(dest => dest.IsVip, opt => opt.Ignore()); // 将在服务中设置

        // User -> UserCommentStatsDto (如果需要直接从User映射)
        CreateMap<User, UserCommentStatsDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TotalComments, opt => opt.Ignore())
            .ForMember(dest => dest.TotalLikes, opt => opt.Ignore())
            .ForMember(dest => dest.TotalReplies, opt => opt.Ignore())
            .ForMember(dest => dest.AverageLikes, opt => opt.Ignore())
            .ForMember(dest => dest.MostPopularComment, opt => opt.Ignore())
            .ForMember(dest => dest.LastCommentAt, opt => opt.Ignore())
            .ForMember(dest => dest.RecentActivity, opt => opt.Ignore())
            .ForMember(dest => dest.CommentsByStatus, opt => opt.Ignore());

        // User -> UserModerationStatsDto
        CreateMap<User, UserModerationStatsDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TotalComments, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovedComments, opt => opt.Ignore())
            .ForMember(dest => dest.RejectedComments, opt => opt.Ignore())
            .ForMember(dest => dest.ReportedComments, opt => opt.Ignore())
            .ForMember(dest => dest.SpamComments, opt => opt.Ignore())
            .ForMember(dest => dest.ApprovalRate, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentTrustScore, opt => opt.Ignore())
            .ForMember(dest => dest.RecentCommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.LastReportedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastModeratedAt, opt => opt.Ignore())
            .ForMember(dest => dest.StatusCounts, opt => opt.Ignore())
            .ForMember(dest => dest.MonthlyTrend, opt => opt.Ignore());
    }
}

/// <summary>
/// 评论系统映射扩展
/// </summary>
public static class CommentMappingExtensions
{
    /// <summary>
    /// 映射评论树结构
    /// </summary>
    /// <param name="mapper">映射器</param>
    /// <param name="comments">评论列表</param>
    /// <param name="currentUserId">当前用户ID</param>
    /// <returns>评论DTO树结构</returns>
    public static IList<CommentDto> MapCommentTree(this IMapper mapper, IEnumerable<Comment> comments, Guid? currentUserId = null)
    {
        var commentList = comments.ToList();
        var commentDtos = commentList.Select(c => mapper.Map<CommentDto>(c)).ToList();

        // 设置用户相关的权限信息
        if (currentUserId.HasValue)
        {
            foreach (var dto in commentDtos)
            {
                var comment = commentList.First(c => c.Id == dto.Id);
                var isAuthor = comment.AuthorId == currentUserId.Value;
                var isAdmin = false; // 这里需要从上下文获取用户角色信息

                var updatedDto = dto with
                {
                    IsLiked = comment.Likes.Any(l => l.UserId == currentUserId.Value && !l.IsDeleted),
                    CanEdit = comment.CanBeEditedBy(currentUserId.Value, isAdmin),
                    CanDelete = comment.CanBeDeletedBy(currentUserId.Value, isAdmin)
                };

                // 更新列表中的项
                var index = commentDtos.IndexOf(dto);
                commentDtos[index] = updatedDto;
            }
        }

        // 构建树结构
        var rootComments = commentDtos.Where(c => c.ParentId == null).ToList();
        foreach (var rootComment in rootComments)
        {
            BuildCommentTree(rootComment, commentDtos);
        }

        return rootComments;
    }

    /// <summary>
    /// 递归构建评论树
    /// </summary>
    /// <param name="parent">父评论</param>
    /// <param name="allComments">所有评论</param>
    private static void BuildCommentTree(CommentDto parent, IList<CommentDto> allComments)
    {
        var children = allComments.Where(c => c.ParentId == parent.Id).OrderBy(c => c.CreatedAt).ToList();

        if (children.Any())
        {
            var updatedParent = parent with { Replies = children };
            var parentIndex = allComments.IndexOf(parent);
            allComments[parentIndex] = updatedParent;

            foreach (var child in children)
            {
                BuildCommentTree(child, allComments);
            }
        }
    }

    /// <summary>
    /// 映射分页结果
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TDestination">目标类型</typeparam>
    /// <param name="mapper">映射器</param>
    /// <param name="source">源分页结果</param>
    /// <returns>目标分页结果</returns>
    public static CommentPagedResultDto<TDestination> MapPagedResult<TSource, TDestination>(
        this IMapper mapper,
        CommentPagedResultDto<TSource> source)
    {
        return new CommentPagedResultDto<TDestination>
        {
            Items = mapper.Map<IList<TDestination>>(source.Items),
            TotalCount = source.TotalCount,
            TotalPages = source.TotalPages,
            CurrentPage = source.CurrentPage,
            PageSize = source.PageSize,
            HasNextPage = source.HasNextPage,
            HasPreviousPage = source.HasPreviousPage
        };
    }

    /// <summary>
    /// 映射通用分页结果到评论分页结果
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    /// <param name="items">项目列表</param>
    /// <param name="totalCount">总数</param>
    /// <param name="currentPage">当前页</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>分页结果</returns>
    public static CommentPagedResultDto<T> ToPagedResult<T>(
        this IList<T> items,
        int totalCount,
        int currentPage,
        int pageSize)
    {
        return new CommentPagedResultDto<T>
        {
            Items = items,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            CurrentPage = currentPage,
            PageSize = pageSize,
            HasNextPage = currentPage * pageSize < totalCount,
            HasPreviousPage = currentPage > 1
        };
    }
}