using FluentValidation;
using MapleBlog.Application.DTOs.BulkOperation;
using MapleBlog.Application.Interfaces;

namespace MapleBlog.Application.Validators;

/// <summary>
/// Base validator for bulk operation requests
/// </summary>
/// <typeparam name="TRequest">Type of bulk operation request</typeparam>
public abstract class BaseBulkOperationValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : class, IBulkOperationRequest
{
    protected BaseBulkOperationValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty()
            .WithMessage("Operation ID is required");

        RuleFor(x => x.EntityType)
            .NotEmpty()
            .WithMessage("Entity type is required")
            .MaximumLength(100)
            .WithMessage("Entity type cannot exceed 100 characters");

        RuleFor(x => x.Operation)
            .NotEmpty()
            .WithMessage("Operation is required")
            .MaximumLength(100)
            .WithMessage("Operation cannot exceed 100 characters");

        RuleFor(x => x.BatchSize)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Batch size must be non-negative")
            .LessThanOrEqualTo(10000)
            .WithMessage("Batch size cannot exceed 10,000");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }

    /// <summary>
    /// Validates entity IDs collection
    /// </summary>
    protected void ValidateEntityIds<TKey>(List<TKey> entityIds, int maxCount = 10000)
        where TKey : IEquatable<TKey>
    {
        RuleFor(x => entityIds)
            .NotEmpty()
            .WithMessage("At least one entity ID is required")
            .Must(ids => ids.Count <= maxCount)
            .WithMessage($"Cannot process more than {maxCount} items at once")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Entity IDs must be unique");
    }
}

/// <summary>
/// Validator for generic bulk operation requests
/// </summary>
public class BulkOperationRequestValidator : BaseBulkOperationValidator<BulkOperationRequest>
{
    public BulkOperationRequestValidator()
    {
        RuleFor(x => x.EntityIds)
            .NotEmpty()
            .WithMessage("At least one entity ID is required")
            .Must(ids => ids.Count <= 10000)
            .WithMessage("Cannot process more than 10,000 items at once")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Entity IDs must be unique");

        RuleFor(x => x.ClientInfo)
            .SetValidator(new BulkOperationClientInfoValidator()!)
            .When(x => x.ClientInfo != null);

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Scheduled time must be in the future")
            .When(x => x.ScheduledAt.HasValue);

        RuleFor(x => x.Tags)
            .Must(tags => tags.All(tag => !string.IsNullOrWhiteSpace(tag)))
            .WithMessage("All tags must be non-empty")
            .When(x => x.Tags.Any());
    }
}

/// <summary>
/// Validator for bulk operation client info
/// </summary>
public class BulkOperationClientInfoValidator : AbstractValidator<BulkOperationClientInfo>
{
    public BulkOperationClientInfoValidator()
    {
        RuleFor(x => x.IpAddress)
            .MaximumLength(45)
            .WithMessage("IP address cannot exceed 45 characters")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));

        RuleFor(x => x.UserAgent)
            .MaximumLength(500)
            .WithMessage("User agent cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));

        RuleFor(x => x.Source)
            .MaximumLength(100)
            .WithMessage("Source cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Source));

        RuleFor(x => x.SessionId)
            .MaximumLength(100)
            .WithMessage("Session ID cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SessionId));
    }
}

/// <summary>
/// Validator for bulk user operation requests
/// </summary>
public class BulkUserOperationRequestValidator : BaseBulkOperationValidator<BulkUserOperationRequest>
{
    private readonly IUserManagementService _userManagementService;

    public BulkUserOperationRequestValidator(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));

        ValidateEntityIds(default(List<Guid>)!);

        RuleFor(x => x.Operation)
            .Must(BeValidUserOperation)
            .WithMessage("Invalid user operation");

        RuleFor(x => x.EntityIds)
            .MustAsync(async (ids, cancellation) => await AllUsersExistAsync(ids, cancellation))
            .WithMessage("One or more user IDs do not exist")
            .When(x => x.EntityIds.Any());
    }

    private static bool BeValidUserOperation(string operation)
    {
        var validOperations = new[]
        {
            BulkUserOperations.Activate,
            BulkUserOperations.Deactivate,
            BulkUserOperations.Delete,
            BulkUserOperations.ChangeRole,
            BulkUserOperations.ResetPassword,
            BulkUserOperations.SendEmailVerification,
            BulkUserOperations.Lock,
            BulkUserOperations.Unlock,
            BulkUserOperations.Export
        };

        return validOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AllUsersExistAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var userId in userIds.Take(100)) // Limit validation to first 100 for performance
            {
                var user = await _userManagementService.GetUserDetailAsync(userId);
                if (user == null)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk user role change requests
/// </summary>
public class BulkUserRoleChangeRequestValidator : AbstractValidator<BulkUserRoleChangeRequest>
{
    public BulkUserRoleChangeRequestValidator()
    {
        Include(new BulkUserOperationRequestValidator(null!)); // Will be injected

        RuleFor(x => x.NewRole)
            .IsInEnum()
            .WithMessage("Invalid user role");

        RuleFor(x => x.RolesToPreserve)
            .Must(roles => roles.All(role => Enum.IsDefined(typeof(Domain.Enums.UserRole), role)))
            .WithMessage("All roles to preserve must be valid")
            .When(x => x.RolesToPreserve.Any());
    }
}

/// <summary>
/// Validator for bulk user deletion requests
/// </summary>
public class BulkUserDeleteRequestValidator : AbstractValidator<BulkUserDeleteRequest>
{
    public BulkUserDeleteRequestValidator()
    {
        Include(new BulkUserOperationRequestValidator(null!)); // Will be injected

        RuleFor(x => x.ContentHandling)
            .IsInEnum()
            .WithMessage("Invalid content handling strategy");

        RuleFor(x => x.RetentionPeriod)
            .GreaterThan(TimeSpan.Zero)
            .WithMessage("Retention period must be positive")
            .LessThanOrEqualTo(TimeSpan.FromDays(365))
            .WithMessage("Retention period cannot exceed 365 days")
            .When(x => x.RetentionPeriod.HasValue && x.SoftDelete);
    }
}

/// <summary>
/// Validator for bulk post operation requests
/// </summary>
public class BulkPostOperationRequestValidator : BaseBulkOperationValidator<BulkPostOperationRequest>
{
    private readonly IBlogService _blogService;

    public BulkPostOperationRequestValidator(IBlogService blogService)
    {
        _blogService = blogService ?? throw new ArgumentNullException(nameof(blogService));

        ValidateEntityIds(default(List<Guid>)!);

        RuleFor(x => x.Operation)
            .Must(BeValidPostOperation)
            .WithMessage("Invalid post operation");

        RuleFor(x => x.EntityIds)
            .MustAsync(async (ids, cancellation) => await AllPostsExistAsync(ids, cancellation))
            .WithMessage("One or more post IDs do not exist")
            .When(x => x.EntityIds.Any());
    }

    private static bool BeValidPostOperation(string operation)
    {
        var validOperations = new[]
        {
            BulkPostOperations.Publish,
            BulkPostOperations.Unpublish,
            BulkPostOperations.Draft,
            BulkPostOperations.Archive,
            BulkPostOperations.Delete,
            BulkPostOperations.ChangeCategory,
            BulkPostOperations.AddTags,
            BulkPostOperations.RemoveTags,
            BulkPostOperations.ReplaceTags,
            BulkPostOperations.ChangeAuthor,
            BulkPostOperations.UpdateStatus,
            BulkPostOperations.Export,
            BulkPostOperations.GenerateSummary,
            BulkPostOperations.OptimizeSeo
        };

        return validOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AllPostsExistAsync(List<Guid> postIds, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var postId in postIds.Take(100)) // Limit validation to first 100 for performance
            {
                var post = await _blogService.GetPostByIdAsync(postId, cancellationToken);
                if (post == null)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk post category change requests
/// </summary>
public class BulkPostCategoryChangeRequestValidator : AbstractValidator<BulkPostCategoryChangeRequest>
{
    private readonly ICategoryService _categoryService;

    public BulkPostCategoryChangeRequestValidator(ICategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        Include(new BulkPostOperationRequestValidator(null!)); // Will be injected

        RuleFor(x => x.NewCategoryId)
            .NotEmpty()
            .WithMessage("New category ID is required")
            .MustAsync(async (categoryId, cancellation) => await CategoryExistsAsync(categoryId, cancellation))
            .WithMessage("Category does not exist");
    }

    private async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId, cancellationToken);
            return category != null;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk comment operation requests
/// </summary>
public class BulkCommentOperationRequestValidator : BaseBulkOperationValidator<BulkCommentOperationRequest>
{
    private readonly ICommentService _commentService;

    public BulkCommentOperationRequestValidator(ICommentService commentService)
    {
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService));

        ValidateEntityIds(default(List<Guid>)!);

        RuleFor(x => x.Operation)
            .Must(BeValidCommentOperation)
            .WithMessage("Invalid comment operation");

        RuleFor(x => x.EntityIds)
            .MustAsync(async (ids, cancellation) => await AllCommentsExistAsync(ids, cancellation))
            .WithMessage("One or more comment IDs do not exist")
            .When(x => x.EntityIds.Any());
    }

    private static bool BeValidCommentOperation(string operation)
    {
        var validOperations = new[]
        {
            BulkCommentOperations.Approve,
            BulkCommentOperations.Reject,
            BulkCommentOperations.MarkAsSpam,
            BulkCommentOperations.Delete,
            BulkCommentOperations.Archive,
            BulkCommentOperations.Pin,
            BulkCommentOperations.Unpin,
            BulkCommentOperations.ChangeAuthor,
            BulkCommentOperations.MoveToDifferentPost,
            BulkCommentOperations.Export,
            BulkCommentOperations.Moderate,
            BulkCommentOperations.UpdateStatus
        };

        return validOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AllCommentsExistAsync(List<Guid> commentIds, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var commentId in commentIds.Take(100)) // Limit validation to first 100 for performance
            {
                var comment = await _commentService.GetCommentAsync(commentId, null, cancellationToken);
                if (comment == null)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk category operation requests
/// </summary>
public class BulkCategoryOperationRequestValidator : BaseBulkOperationValidator<BulkCategoryOperationRequest>
{
    private readonly ICategoryService _categoryService;

    public BulkCategoryOperationRequestValidator(ICategoryService categoryService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        ValidateEntityIds(default(List<Guid>)!);

        RuleFor(x => x.Operation)
            .Must(BeValidCategoryOperation)
            .WithMessage("Invalid category operation");

        RuleFor(x => x.EntityIds)
            .MustAsync(async (ids, cancellation) => await AllCategoriesExistAsync(ids, cancellation))
            .WithMessage("One or more category IDs do not exist")
            .When(x => x.EntityIds.Any());
    }

    private static bool BeValidCategoryOperation(string operation)
    {
        var validOperations = new[]
        {
            BulkCategoryOperations.Activate,
            BulkCategoryOperations.Deactivate,
            BulkCategoryOperations.Delete,
            BulkCategoryOperations.Move,
            BulkCategoryOperations.Merge,
            BulkCategoryOperations.Reorder,
            BulkCategoryOperations.UpdateParent,
            BulkCategoryOperations.Export,
            BulkCategoryOperations.UpdateSeo,
            BulkCategoryOperations.UpdateAppearance
        };

        return validOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AllCategoriesExistAsync(List<Guid> categoryIds, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var categoryId in categoryIds.Take(100)) // Limit validation to first 100 for performance
            {
                var category = await _categoryService.GetCategoryByIdAsync(categoryId, cancellationToken);
                if (category == null)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk tag operation requests
/// </summary>
public class BulkTagOperationRequestValidator : BaseBulkOperationValidator<BulkTagOperationRequest>
{
    private readonly ITagService _tagService;

    public BulkTagOperationRequestValidator(ITagService tagService)
    {
        _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));

        ValidateEntityIds(default(List<Guid>)!);

        RuleFor(x => x.Operation)
            .Must(BeValidTagOperation)
            .WithMessage("Invalid tag operation");

        RuleFor(x => x.EntityIds)
            .MustAsync(async (ids, cancellation) => await AllTagsExistAsync(ids, cancellation))
            .WithMessage("One or more tag IDs do not exist")
            .When(x => x.EntityIds.Any());
    }

    private static bool BeValidTagOperation(string operation)
    {
        var validOperations = new[]
        {
            BulkTagOperations.Activate,
            BulkTagOperations.Deactivate,
            BulkTagOperations.Delete,
            BulkTagOperations.Merge,
            BulkTagOperations.Rename,
            BulkTagOperations.UpdateColor,
            BulkTagOperations.UpdateDescription,
            BulkTagOperations.CleanupUnused,
            BulkTagOperations.Export,
            BulkTagOperations.GenerateSlugs,
            BulkTagOperations.ConsolidateSimilar
        };

        return validOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<bool> AllTagsExistAsync(List<Guid> tagIds, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var tagId in tagIds.Take(100)) // Limit validation to first 100 for performance
            {
                var tag = await _tagService.GetTagByIdAsync(tagId, cancellationToken);
                if (tag == null)
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for date range objects
/// </summary>
public class DateRangeValidator : AbstractValidator<DateRange>
{
    public DateRangeValidator()
    {
        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("From date must be less than or equal to To date");

        RuleFor(x => x.To)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("To date cannot be more than 1 day in the future");
    }
}

/// <summary>
/// Validator for bulk tag merge requests
/// </summary>
public class BulkTagMergeRequestValidator : AbstractValidator<BulkTagMergeRequest>
{
    private readonly ITagService _tagService;

    public BulkTagMergeRequestValidator(ITagService tagService)
    {
        _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));

        Include(new BulkTagOperationRequestValidator(tagService));

        RuleFor(x => x.TargetTagId)
            .NotEmpty()
            .WithMessage("Target tag ID is required")
            .MustAsync(async (targetId, cancellation) => await TagExistsAsync(targetId, cancellation))
            .WithMessage("Target tag does not exist");

        RuleFor(x => x.ConflictResolution)
            .IsInEnum()
            .WithMessage("Invalid conflict resolution strategy");

        RuleFor(x => x.MergeReason)
            .MaximumLength(500)
            .WithMessage("Merge reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.MergeReason));

        RuleFor(x => x)
            .Must(x => !x.EntityIds.Contains(x.TargetTagId))
            .WithMessage("Target tag cannot be in the list of source tags to merge");
    }

    private async Task<bool> TagExistsAsync(Guid tagId, CancellationToken cancellationToken)
    {
        try
        {
            var tag = await _tagService.GetTagByIdAsync(tagId, cancellationToken);
            return tag != null;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Validator for bulk tag cleanup requests
/// </summary>
public class BulkTagCleanupRequestValidator : AbstractValidator<BulkTagCleanupRequest>
{
    public BulkTagCleanupRequestValidator()
    {
        Include(new BulkTagOperationRequestValidator(null!)); // Will be injected

        RuleFor(x => x.UsageThreshold)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Usage threshold must be non-negative")
            .LessThanOrEqualTo(1000)
            .WithMessage("Usage threshold cannot exceed 1000");

        RuleFor(x => x.MaxAgeForUnused)
            .GreaterThan(TimeSpan.FromDays(1))
            .WithMessage("Max age for unused tags must be at least 1 day")
            .LessThanOrEqualTo(TimeSpan.FromDays(3650))
            .WithMessage("Max age for unused tags cannot exceed 10 years")
            .When(x => x.MaxAgeForUnused.HasValue);

        RuleFor(x => x.ExcludePatterns)
            .Must(patterns => patterns.All(pattern => IsValidRegexPattern(pattern)))
            .WithMessage("All exclude patterns must be valid regular expressions")
            .When(x => x.ExcludePatterns.Any());
    }

    private static bool IsValidRegexPattern(string pattern)
    {
        try
        {
            System.Text.RegularExpressions.Regex.IsMatch("", pattern);
            return true;
        }
        catch
        {
            return false;
        }
    }
}