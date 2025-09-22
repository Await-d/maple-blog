using MapleBlog.Application.DTOs;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.DTOs.Admin
{
    /// <summary>
    /// 文章管理DTO - 用于管理员界面
    /// </summary>
    public class PostManagementDto
    {
        /// <summary>
        /// 文章ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 文章标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// URL标识符
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 作者信息
        /// </summary>
        public PostAuthorDto? Author { get; set; }

        /// <summary>
        /// 分类信息
        /// </summary>
        public CategoryDto? Category { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public IEnumerable<TagDto> Tags { get; set; } = new List<TagDto>();

        /// <summary>
        /// 文章状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 浏览次数
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// 评论数量
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 点赞数量
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 分享次数
        /// </summary>
        public int ShareCount { get; set; }

        /// <summary>
        /// 字数统计
        /// </summary>
        public int WordCount { get; set; }

        /// <summary>
        /// 预计阅读时间（分钟）
        /// </summary>
        public int ReadingTime { get; set; }

        /// <summary>
        /// 是否推荐
        /// </summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// 是否置顶
        /// </summary>
        public bool IsSticky { get; set; }

        /// <summary>
        /// 是否允许评论
        /// </summary>
        public bool AllowComments { get; set; }

        /// <summary>
        /// 内容警告标签
        /// </summary>
        public IEnumerable<string> ContentWarnings { get; set; } = new List<string>();

        /// <summary>
        /// 审核标记
        /// </summary>
        public IEnumerable<string> ModerationFlags { get; set; } = new List<string>();

        /// <summary>
        /// SEO评分
        /// </summary>
        public double? SeoScore { get; set; }

        /// <summary>
        /// 最后编辑者
        /// </summary>
        public string? LastEditedBy { get; set; }

        /// <summary>
        /// 管理操作权限
        /// </summary>
        public PostManagementPermissionsDto Permissions { get; set; } = new();

        /// <summary>
        /// 统计信息
        /// </summary>
        public PostManagementStatsDto Stats { get; set; } = new();

        /// <summary>
        /// 质量评估
        /// </summary>
        public PostQualityAssessmentDto QualityAssessment { get; set; } = new();
    }

    /// <summary>
    /// 文章管理权限DTO
    /// </summary>
    public class PostManagementPermissionsDto
    {
        /// <summary>
        /// 可以编辑
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// 可以删除
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 可以发布
        /// </summary>
        public bool CanPublish { get; set; }

        /// <summary>
        /// 可以设置推荐
        /// </summary>
        public bool CanFeature { get; set; }

        /// <summary>
        /// 可以置顶
        /// </summary>
        public bool CanStick { get; set; }

        /// <summary>
        /// 可以管理评论
        /// </summary>
        public bool CanManageComments { get; set; }

        /// <summary>
        /// 可以查看统计
        /// </summary>
        public bool CanViewStats { get; set; }
    }

    /// <summary>
    /// 文章管理统计DTO
    /// </summary>
    public class PostManagementStatsDto
    {
        /// <summary>
        /// 今日浏览量
        /// </summary>
        public long TodayViews { get; set; }

        /// <summary>
        /// 昨日浏览量
        /// </summary>
        public long YesterdayViews { get; set; }

        /// <summary>
        /// 本周浏览量
        /// </summary>
        public long WeekViews { get; set; }

        /// <summary>
        /// 本月浏览量
        /// </summary>
        public long MonthViews { get; set; }

        /// <summary>
        /// 平均阅读完成率
        /// </summary>
        public double ReadCompletionRate { get; set; }

        /// <summary>
        /// 跳出率
        /// </summary>
        public double BounceRate { get; set; }

        /// <summary>
        /// 社交分享统计
        /// </summary>
        public Dictionary<string, int> SocialShares { get; set; } = new();

        /// <summary>
        /// 评论参与度
        /// </summary>
        public double CommentEngagementRate { get; set; }

        /// <summary>
        /// 搜索引擎排名
        /// </summary>
        public Dictionary<string, int> SearchRankings { get; set; } = new();
    }

    /// <summary>
    /// 文章质量评估DTO
    /// </summary>
    public class PostQualityAssessmentDto
    {
        /// <summary>
        /// 总体质量评分 (0-100)
        /// </summary>
        public double OverallScore { get; set; }

        /// <summary>
        /// 内容质量评分
        /// </summary>
        public double ContentScore { get; set; }

        /// <summary>
        /// SEO质量评分
        /// </summary>
        public double SeoScore { get; set; }

        /// <summary>
        /// 可读性评分
        /// </summary>
        public double ReadabilityScore { get; set; }

        /// <summary>
        /// 原创性评分
        /// </summary>
        public double OriginalityScore { get; set; }

        /// <summary>
        /// 质量问题列表
        /// </summary>
        public IEnumerable<string> QualityIssues { get; set; } = new List<string>();

        /// <summary>
        /// 改进建议
        /// </summary>
        public IEnumerable<string> Suggestions { get; set; } = new List<string>();

        /// <summary>
        /// 评估时间
        /// </summary>
        public DateTime AssessedAt { get; set; }

        /// <summary>
        /// 评估版本
        /// </summary>
        public string AssessmentVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 评论管理DTO - 用于管理员界面
    /// </summary>
    public class CommentManagementDto
    {
        /// <summary>
        /// 评论ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 评论内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 评论作者
        /// </summary>
        public CommentAuthorDto? Author { get; set; }

        /// <summary>
        /// 关联文章
        /// </summary>
        public PostSummaryDto? Post { get; set; }

        /// <summary>
        /// 父评论
        /// </summary>
        public CommentSummaryDto? ParentComment { get; set; }

        /// <summary>
        /// 评论状态
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 点赞数量
        /// </summary>
        public int LikeCount { get; set; }

        /// <summary>
        /// 回复数量
        /// </summary>
        public int ReplyCount { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// 举报次数
        /// </summary>
        public int ReportCount { get; set; }

        /// <summary>
        /// 审核备注
        /// </summary>
        public string? ModerationNotes { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTime? ModeratedAt { get; set; }

        /// <summary>
        /// 审核人
        /// </summary>
        public UserDto? ModeratedBy { get; set; }

        /// <summary>
        /// 风险评分
        /// </summary>
        public double RiskScore { get; set; }

        /// <summary>
        /// 自动审核标记
        /// </summary>
        public IEnumerable<string> AutoModerationFlags { get; set; } = new List<string>();

        /// <summary>
        /// 管理操作权限
        /// </summary>
        public CommentManagementPermissionsDto Permissions { get; set; } = new();
    }

    /// <summary>
    /// 评论管理权限DTO
    /// </summary>
    public class CommentManagementPermissionsDto
    {
        /// <summary>
        /// 可以编辑
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// 可以删除
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 可以批准
        /// </summary>
        public bool CanApprove { get; set; }

        /// <summary>
        /// 可以拒绝
        /// </summary>
        public bool CanReject { get; set; }

        /// <summary>
        /// 可以标记为垃圾
        /// </summary>
        public bool CanMarkAsSpam { get; set; }

        /// <summary>
        /// 可以查看举报
        /// </summary>
        public bool CanViewReports { get; set; }
    }

    /// <summary>
    /// 评论摘要DTO
    /// </summary>
    public class CommentSummaryDto
    {
        /// <summary>
        /// 评论ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 评论内容摘要
        /// </summary>
        public string ContentPreview { get; set; } = string.Empty;

        /// <summary>
        /// 作者名称
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 文章摘要DTO
    /// </summary>
    public class PostSummaryDto
    {
        /// <summary>
        /// 文章ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 文章标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// URL标识符
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// 作者名称
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;

        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// 浏览次数
        /// </summary>
        public long ViewCount { get; set; }

        /// <summary>
        /// 文章状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>
    /// 角色DTO
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// 角色ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 角色描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否系统角色
        /// </summary>
        public bool IsSystem { get; set; }

        /// <summary>
        /// 权限列表
        /// </summary>
        public IEnumerable<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();

        /// <summary>
        /// 用户数量
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 权限DTO
    /// </summary>
    public class PermissionDto
    {
        /// <summary>
        /// 权限ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 权限名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 权限分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 权限范围
        /// </summary>
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// 是否系统权限
        /// </summary>
        public bool IsSystemPermission { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}