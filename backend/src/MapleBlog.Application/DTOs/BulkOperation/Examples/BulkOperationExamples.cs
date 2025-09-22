using MapleBlog.Application.DTOs.BulkOperation;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Enums;
using UserRoleEnum = MapleBlog.Domain.Enums.UserRole;

namespace MapleBlog.Application.DTOs.BulkOperation.Examples;

/// <summary>
/// Comprehensive examples for using the bulk operation system
/// </summary>
public static class BulkOperationExamples
{
    /// <summary>
    /// Example: Bulk activate users
    /// </summary>
    public static BulkUserOperationRequest BulkActivateUsers()
    {
        return new BulkUserOperationRequest
        {
            Operation = BulkUserOperations.Activate,
            EntityIds = new List<Guid>
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            },
            UseTransaction = true,
            ContinueOnError = false,
            BatchSize = 10,
            Reason = "Batch activation of verified users after email confirmation",
            ClientInfo = new BulkOperationClientInfo
            {
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                Source = "AdminPanel",
                SessionId = "session123"
            },
            Priority = BulkOperationPriority.Normal,
            Tags = new List<string> { "user-management", "activation", "email-verified" }
        };
    }

    /// <summary>
    /// Example: Bulk change user roles
    /// </summary>
    public static BulkUserRoleChangeRequest BulkChangeUserRoles()
    {
        return new BulkUserRoleChangeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555")
            },
            NewRole = UserRoleEnum.Author,
            PreserveExistingRoles = false,
            RolesToPreserve = new List<UserRoleEnum> { UserRoleEnum.User },
            UseTransaction = true,
            Reason = "Promote active users to author status",
            Priority = BulkOperationPriority.High
        };
    }

    /// <summary>
    /// Example: Bulk user deletion with soft delete
    /// </summary>
    public static BulkUserDeleteRequest BulkSoftDeleteUsers()
    {
        return new BulkUserDeleteRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777")
            },
            SoftDelete = true,
            AnonymizeData = false,
            ContentHandling = UserContentHandling.Preserve,
            SendNotification = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            UseTransaction = true,
            Reason = "Remove inactive users who haven't logged in for 2 years",
            Priority = BulkOperationPriority.Low
        };
    }

    /// <summary>
    /// Example: Bulk export users with filtering
    /// </summary>
    public static BulkUserExportRequest BulkExportUsers()
    {
        return new BulkUserExportRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999")
            },
            Format = UserExportFormat.Excel,
            IncludeFields = new List<string>
            {
                "Id", "UserName", "Email", "FirstName", "LastName",
                "Role", "IsActive", "CreatedAt", "LastLoginAt"
            },
            IncludePosts = true,
            IncludeComments = false,
            ContentDateRange = new DateRange
            {
                From = DateTime.UtcNow.AddYears(-1),
                To = DateTime.UtcNow
            },
            EncryptExport = true,
            CompressionLevel = CompressionLevel.Optimal,
            Reason = "Export user data for compliance audit",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk publish posts
    /// </summary>
    public static BulkPostStatusChangeRequest BulkPublishPosts()
    {
        return new BulkPostStatusChangeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            },
            NewStatus = PostStatus.Published,
            UpdatePublishDate = true,
            SendNotifications = true,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Publish scheduled blog posts for this week",
            Priority = BulkOperationPriority.High
        };
    }

    /// <summary>
    /// Example: Bulk change post categories
    /// </summary>
    public static BulkPostCategoryChangeRequest BulkChangePostCategories()
    {
        return new BulkPostCategoryChangeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")
            },
            NewCategoryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            UpdateSeoFromCategory = true,
            AddCategoryTags = true,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Move tech posts to new 'Web Development' category",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk tag operations - add tags to posts
    /// </summary>
    public static BulkPostTagOperationRequest BulkAddTagsToPosts()
    {
        return new BulkPostTagOperationRequest
        {
            Operation = BulkPostOperations.AddTags,
            EntityIds = new List<Guid>
            {
                Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                Guid.Parse("gggggggg-gggg-gggg-gggg-gggggggggggg")
            },
            TagIds = new List<Guid>
            {
                Guid.Parse("hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh"), // "csharp" tag
                Guid.Parse("iiiiiiii-iiii-iiii-iiii-iiiiiiiiiiii")  // "programming" tag
            },
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Add programming tags to technical posts",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk approve comments
    /// </summary>
    public static BulkCommentStatusChangeRequest BulkApproveComments()
    {
        return new BulkCommentStatusChangeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("jjjjjjjj-jjjj-jjjj-jjjj-jjjjjjjjjjjj"),
                Guid.Parse("kkkkkkkk-kkkk-kkkk-kkkk-kkkkkkkkkkkk")
            },
            NewStatus = CommentStatus.Approved,
            ModerationReason = "Comments reviewed and approved by moderator",
            ApplyToReplies = false,
            SendNotifications = true,
            UpdatePostCommentCounts = true,
            NotificationMessage = "Your comment has been approved and is now visible.",
            UseTransaction = true,
            Reason = "Approve pending comments after manual review",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk comment moderation with spam detection
    /// </summary>
    public static BulkCommentModerationRequest BulkMarkCommentsAsSpam()
    {
        return new BulkCommentModerationRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("llllllll-llll-llll-llll-llllllllllll"),
                Guid.Parse("mmmmmmmm-mmmm-mmmm-mmmm-mmmmmmmmmmmm")
            },
            ModerationAction = CommentModerationAction.MarkAsSpam,
            ModerationReason = "Comments identified as spam by automatic detection",
            ApplyToAuthorComments = true,
            AddAuthorsToWatchlist = true,
            Severity = ModerationSeverity.High,
            AuthorMessage = "Your comment was identified as spam and has been removed.",
            SendNotifications = false,
            UpdatePostCommentCounts = true,
            UseTransaction = true,
            Reason = "Remove spam comments detected by automated system",
            Priority = BulkOperationPriority.High
        };
    }

    /// <summary>
    /// Example: Bulk delete comments with reply handling
    /// </summary>
    public static BulkCommentDeleteRequest BulkDeleteComments()
    {
        return new BulkCommentDeleteRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn"),
                Guid.Parse("oooooooo-oooo-oooo-oooo-oooooooooooo")
            },
            SoftDelete = true,
            ReplyHandling = CommentReplyHandling.PreserveReplies,
            DeletionReason = "Comments contained inappropriate content",
            NotifyAuthors = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CreateTombstones = true,
            SendNotifications = true,
            UpdatePostCommentCounts = true,
            UseTransaction = true,
            Reason = "Remove inappropriate comments while preserving replies",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk merge categories
    /// </summary>
    public static BulkCategoryMergeRequest BulkMergeCategories()
    {
        return new BulkCategoryMergeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("pppppppp-pppp-pppp-pppp-pppppppppppp"), // Old "JavaScript" category
                Guid.Parse("qqqqqqqq-qqqq-qqqq-qqqq-qqqqqqqqqqqq")  // Old "JS" category
            },
            TargetCategoryId = Guid.Parse("rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr"), // "Web Development" category
            DeleteSourceCategories = true,
            ConflictResolution = CategoryMergeConflictResolution.KeepTarget,
            MergeChildCategories = true,
            DuplicatePostHandling = DuplicatePostHandling.KeepInTarget,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Consolidate JavaScript categories into main Web Development category",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk category reordering
    /// </summary>
    public static BulkCategoryReorderRequest BulkReorderCategories()
    {
        return new BulkCategoryReorderRequest
        {
            CategoryOrders = new Dictionary<Guid, int>
            {
                [Guid.Parse("ssssssss-ssss-ssss-ssss-ssssssssssss")] = 1, // "Technology" first
                [Guid.Parse("tttttttt-tttt-tttt-tttt-tttttttttttt")] = 2, // "Programming" second
                [Guid.Parse("uuuuuuuu-uuuu-uuuu-uuuu-uuuuuuuuuuuu")] = 3, // "Web Development" third
                [Guid.Parse("vvvvvvvv-vvvv-vvvv-vvvv-vvvvvvvvvvvv")] = 4  // "Tutorials" fourth
            },
            ReorderGlobally = false,
            WithinParentId = null, // Root level categories
            AdjustForGaps = true,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Reorder main categories by importance and usage",
            Priority = BulkOperationPriority.Low
        };
    }

    /// <summary>
    /// Example: Bulk tag merge operation
    /// </summary>
    public static BulkTagMergeRequest BulkMergeTags()
    {
        return new BulkTagMergeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("wwwwwwww-wwww-wwww-wwww-wwwwwwwwwwww"), // "javascript" tag
                Guid.Parse("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"), // "js" tag
                Guid.Parse("yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy")  // "JavaScript" tag
            },
            TargetTagId = Guid.Parse("zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz"), // "JavaScript" canonical tag
            DeleteSourceTags = true,
            ConflictResolution = TagMergeConflictResolution.KeepTarget,
            MergeDescriptions = true,
            NotifyPostAuthors = false,
            MergeReason = "Consolidate JavaScript tag variations",
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Clean up duplicate JavaScript tags",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Bulk tag cleanup - remove unused tags
    /// </summary>
    public static BulkTagCleanupRequest BulkCleanupUnusedTags()
    {
        return new BulkTagCleanupRequest
        {
            UsageThreshold = 0, // Tags with 0 usage
            MaxAgeForUnused = TimeSpan.FromDays(90), // Older than 90 days
            IncludeProtectedTags = false,
            ExcludePatterns = new List<string>
            {
                "^system-.*", // Exclude system tags
                "^featured$", // Exclude "featured" tag
                "^important$" // Exclude "important" tag
            },
            DryRun = false, // Actually perform cleanup
            CreateBackup = true,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Remove unused tags older than 90 days to clean up tag list",
            Priority = BulkOperationPriority.Low
        };
    }

    /// <summary>
    /// Example: Bulk tag consolidation for similar tags
    /// </summary>
    public static BulkTagConsolidationRequest BulkConsolidateSimilarTags()
    {
        return new BulkTagConsolidationRequest
        {
            SimilarityThreshold = 0.85,
            SimilarityAlgorithms = new List<SimilarityAlgorithm>
            {
                SimilarityAlgorithm.LevenshteinDistance,
                SimilarityAlgorithm.JaroWinkler,
                SimilarityAlgorithm.SoundexMatch
            },
            IgnoreCase = true,
            IgnorePluralization = true,
            CustomConsolidationMapping = new Dictionary<Guid, Guid>
            {
                // Force merge "csharp" into "C#"
                [Guid.Parse("11111111-1111-1111-1111-111111111111")] =
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            },
            PreviewOnly = false, // Actually perform consolidation
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            Reason = "Consolidate similar tags to reduce duplication",
            Priority = BulkOperationPriority.Normal
        };
    }

    /// <summary>
    /// Example: Scheduled bulk operation
    /// </summary>
    public static BulkPostStatusChangeRequest ScheduledBulkPublishPosts()
    {
        return new BulkPostStatusChangeRequest
        {
            EntityIds = new List<Guid>
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            },
            NewStatus = PostStatus.Published,
            UpdatePublishDate = true,
            SendNotifications = true,
            UpdateSearchIndex = true,
            ClearCache = true,
            UseTransaction = true,
            ScheduledAt = DateTime.UtcNow.AddHours(24), // Publish tomorrow
            Reason = "Scheduled publish for marketing campaign launch",
            Priority = BulkOperationPriority.Critical,
            Tags = new List<string> { "scheduled", "marketing", "campaign" }
        };
    }

    /// <summary>
    /// Example: Create security context for bulk operations
    /// </summary>
    public static BulkOperationSecurityContext CreateSecurityContext()
    {
        return new BulkOperationSecurityContext
        {
            UserId = Guid.Parse("admin123-1234-5678-9abc-def012345678"),
            UserRoles = UserRoleEnum.Admin | UserRoleEnum.Moderator,
            UserPermissions = new List<string>
            {
                SystemPermission.UserDelete,
                SystemPermission.PostDelete,
                SystemPermission.CommentModerate,
                SystemPermission.CategoryDelete,
                SystemPermission.TagDelete,
                SystemPermission.LogsView
            },
            SecurityRestrictions = new List<string>(),
            MaxEntitiesPerOperation = 1000,
            OperationsRequiringConfirmation = new List<string>
            {
                BulkUserOperations.Delete,
                BulkPostOperations.Delete,
                BulkCategoryOperations.Delete
            },
            ClientInfo = new BulkOperationClientInfo
            {
                IpAddress = "10.0.0.1",
                UserAgent = "AdminPanel/1.0",
                Source = "WebUI",
                SessionId = "admin-session-123"
            }
        };
    }

    /// <summary>
    /// Example: Permission check request
    /// </summary>
    public static BulkOperationPermissionCheckRequest CreatePermissionCheckRequest()
    {
        return new BulkOperationPermissionCheckRequest
        {
            UserId = Guid.Parse("user1234-1234-5678-9abc-def012345678"),
            OperationRequest = BulkActivateUsers(),
            UserRoles = UserRoleEnum.Moderator,
            UserPermissions = new List<string>
            {
                SystemPermission.UserDelete,
                SystemPermission.LogsView
            },
            ClientInfo = new BulkOperationClientInfo
            {
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                Source = "WebUI"
            },
            IsPreview = false
        };
    }

    /// <summary>
    /// Example: Audit filter for querying bulk operation history
    /// </summary>
    public static BulkOperationAuditFilter CreateAuditFilter()
    {
        return new BulkOperationAuditFilter
        {
            UserId = Guid.Parse("user1234-1234-5678-9abc-def012345678"),
            EntityType = "User",
            Operation = BulkUserOperations.Activate,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow
            },
            IsSuccess = true,
            MinEntityCount = 5,
            Page = 1,
            PageSize = 25,
            SortBy = "StartedAt",
            SortOrder = "DESC"
        };
    }
}

/// <summary>
/// Integration guide examples showing how to use the bulk operation system
/// </summary>
public static class BulkOperationIntegrationGuide
{
    /// <summary>
    /// Example: Controller method for bulk user operations
    /// </summary>
    public static string ControllerExample = @"
[ApiController]
[Route(""api/[controller]"")]
[RequirePermission(SystemPermission.ManageUsers)]
public class BulkUserController : ControllerBase
{
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IUserContextService _userContextService;

    public BulkUserController(
        IBulkOperationService bulkOperationService,
        IUserContextService userContextService)
    {
        _bulkOperationService = bulkOperationService;
        _userContextService = userContextService;
    }

    [HttpPost(""activate"")]
    public async Task<ActionResult<BulkUserOperationResponse>> BulkActivateUsers(
        [FromBody] BulkUserOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get current user security context
        var securityContext = await _userContextService.GetBulkOperationSecurityContextAsync();

        // Check permissions first
        var permissionCheck = await _bulkOperationService.CheckPermissionsAsync(
            new BulkOperationPermissionCheckRequest
            {
                UserId = securityContext.UserId,
                OperationRequest = request,
                UserRoles = securityContext.UserRoles,
                UserPermissions = securityContext.UserPermissions
            }, cancellationToken);

        if (!permissionCheck.IsAllowed)
        {
            return Forbid(string.Join(""; "", permissionCheck.DenialReasons));
        }

        // Execute the bulk operation
        var response = await _bulkOperationService.ExecuteAsync(
            request, securityContext, cancellationToken);

        return Ok(response);
    }

    [HttpPost(""preview"")]
    public async Task<ActionResult<BulkOperationPreviewResult>> PreviewBulkOperation(
        [FromBody] BulkUserOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var securityContext = await _userContextService.GetBulkOperationSecurityContextAsync();

        var preview = await _bulkOperationService.PreviewAsync(
            request, securityContext, cancellationToken);

        return Ok(preview);
    }
}";

    /// <summary>
    /// Example: Service implementation for bulk operations
    /// </summary>
    public static string ServiceImplementationExample = @"
public class BulkUserOperationService : IBulkOperationService<User, BulkUserOperationRequest, BulkUserOperationResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BulkUserOperationService> _logger;

    public BulkUserOperationService(
        IUserRepository userRepository,
        IPermissionService permissionService,
        IAuditService auditService,
        ILogger<BulkUserOperationService> logger)
    {
        _userRepository = userRepository;
        _permissionService = permissionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<BulkUserOperationResponse> ExecuteAsync(
        BulkUserOperationRequest request,
        BulkOperationSecurityContext securityContext,
        CancellationToken cancellationToken = default)
    {
        var auditEntry = new BulkOperationAuditEntry
        {
            OperationId = request.OperationId,
            UserId = securityContext.UserId,
            EntityType = request.EntityType,
            Operation = request.Operation,
            EntityCount = request.EntityIds.Count,
            EntityIds = request.EntityIds,
            Reason = request.Reason,
            ClientInfo = request.ClientInfo
        };

        try
        {
            // Validate entities exist and user has permissions
            var entityPermissionCheck = await _permissionService.CheckEntityPermissionsAsync(
                new BulkOperationEntityPermissionCheckRequest
                {
                    UserId = securityContext.UserId,
                    EntityIds = request.EntityIds,
                    EntityType = request.EntityType,
                    Operation = request.Operation,
                    UserRoles = securityContext.UserRoles,
                    UserPermissions = securityContext.UserPermissions
                }, cancellationToken);

            var allowedEntityIds = entityPermissionCheck.AllowedEntityIds;
            var deniedEntities = entityPermissionCheck.DeniedEntities;

            var response = new BulkUserOperationResponse
            {
                OperationId = request.OperationId,
                EntityType = request.EntityType,
                Operation = request.Operation,
                TotalItems = allowedEntityIds.Count,
                StartedAt = DateTime.UtcNow
            };

            // Process each entity
            var successCount = 0;
            var failureCount = 0;

            foreach (var entityId in allowedEntityIds)
            {
                try
                {
                    await ProcessSingleUserAsync(entityId, request.Operation, cancellationToken);
                    successCount++;

                    response.ProcessedUsers.Add(new UserSummaryDto
                    {
                        Id = entityId,
                        // ... populate other fields
                        ChangesApplied = { $""Applied {request.Operation} operation"" }
                    });
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, ""Failed to process user {UserId} in bulk operation {OperationId}"",
                        entityId, request.OperationId);

                    response.FailedUsers.Add(new FailedUserDto
                    {
                        Id = entityId,
                        ErrorMessage = ex.Message,
                        ErrorCode = ""PROCESSING_FAILED""
                    });
                }
            }

            // Add denied entities to failed list
            foreach (var deniedEntity in deniedEntities)
            {
                response.FailedUsers.Add(new FailedUserDto
                {
                    Id = deniedEntity.EntityId,
                    ErrorMessage = deniedEntity.Reason,
                    ErrorCode = deniedEntity.ErrorCode
                });
            }

            response.SuccessCount = successCount;
            response.FailureCount = failureCount + deniedEntities.Count;
            response.IsSuccess = failureCount == 0 && deniedEntities.Count == 0;
            response.Status = response.IsSuccess ?
                BulkOperationStatus.Completed :
                BulkOperationStatus.CompletedWithErrors;
            response.CompletedAt = DateTime.UtcNow;

            // Record audit entry
            auditEntry.IsSuccess = response.IsSuccess;
            auditEntry.CompletedAt = response.CompletedAt;
            auditEntry.Counts = new BulkOperationAuditCounts
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalCount = request.EntityIds.Count
            };

            await _auditService.LogBulkOperationAsync(auditEntry, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ""Bulk user operation {OperationId} failed"", request.OperationId);

            auditEntry.IsSuccess = false;
            auditEntry.CompletedAt = DateTime.UtcNow;
            auditEntry.Errors.Add(ex.Message);

            await _auditService.LogBulkOperationAsync(auditEntry, cancellationToken);

            return BulkUserOperationResponse.CreateFailure(request.OperationId, ex.Message);
        }
    }

    private async Task ProcessSingleUserAsync(Guid userId, string operation, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($""User {userId} not found"");

        switch (operation)
        {
            case BulkUserOperations.Activate:
                user.IsActive = true;
                break;
            case BulkUserOperations.Deactivate:
                user.IsActive = false;
                break;
            // ... handle other operations
            default:
                throw new InvalidOperationException($""Unsupported operation: {operation}"");
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}";

    /// <summary>
    /// Example: Progress tracking with SignalR
    /// </summary>
    public static string ProgressTrackingExample = @"
public class BulkOperationHub : Hub
{
    public async Task JoinOperationGroup(string operationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $""operation-{operationId}"");
    }

    public async Task LeaveOperationGroup(string operationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $""operation-{operationId}"");
    }
}

public class BulkOperationProgressService
{
    private readonly IHubContext<BulkOperationHub> _hubContext;

    public BulkOperationProgressService(IHubContext<BulkOperationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<BulkOperationResponse> ExecuteWithProgressAsync(
        IBulkOperationRequest request,
        BulkOperationSecurityContext securityContext,
        CancellationToken cancellationToken = default)
    {
        var progress = new Progress<BulkOperationProgress>(async progressInfo =>
        {
            await _hubContext.Clients.Group($""operation-{request.OperationId}"")
                .SendAsync(""ProgressUpdate"", progressInfo, cancellationToken);
        });

        return await _bulkOperationService.ExecuteWithProgressAsync(
            request, securityContext, progress, cancellationToken);
    }
}

// Client-side JavaScript for progress tracking
const connection = new signalR.HubConnectionBuilder()
    .withUrl(""/bulkOperationHub"")
    .build();

connection.start().then(function () {
    connection.invoke(""JoinOperationGroup"", operationId);

    connection.on(""ProgressUpdate"", function (progress) {
        updateProgressBar(progress.progressPercentage);
        updateStatusText(progress.currentStage);
        updateItemCounts(progress.successCount, progress.failureCount);
    });
});";

    /// <summary>
    /// Example: Permission configuration
    /// </summary>
    public static string PermissionConfigurationExample = @"
public class BulkOperationPermissionConfiguration
{
    public static Dictionary<string, BulkOperationPermission> GetPermissionMap()
    {
        return new Dictionary<string, BulkOperationPermission>
        {
            [$""{BulkUserOperations.Activate}""] = new BulkOperationPermission
            {
                EntityType = ""User"",
                Operation = BulkUserOperations.Activate,
                RequiredPermissions = { SystemPermission.ManageUsers },
                RequiredRoles = { UserRoleEnum.Moderator },
                RequiresElevatedPermissions = false,
                RequiresConfirmation = false,
                MaxItemLimit = 100,
                RequiresAuditApproval = false
            },
            [$""{BulkUserOperations.Delete}""] = new BulkOperationPermission
            {
                EntityType = ""User"",
                Operation = BulkUserOperations.Delete,
                RequiredPermissions = { SystemPermission.ManageUsers },
                RequiredRoles = { UserRoleEnum.Admin },
                RequiresElevatedPermissions = true,
                RequiresConfirmation = true,
                MaxItemLimit = 50,
                RequiresAuditApproval = true,
                TimeRestriction = new TimeRestriction
                {
                    StartTime = new TimeOnly(9, 0), // 9 AM
                    EndTime = new TimeOnly(17, 0),  // 5 PM
                    ExcludeWeekends = true,
                    ExcludeHolidays = true
                }
            },
            // ... more permission configurations
        };
    }
}";

    /// <summary>
    /// Example: Dependency injection setup
    /// </summary>
    public static string DependencyInjectionExample = @"
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBulkOperations(this IServiceCollection services)
    {
        // Core bulk operation services
        services.AddScoped<IBulkOperationService, BulkOperationService>();
        services.AddScoped<IBulkOperationService<User, BulkUserOperationRequest, BulkUserOperationResponse>, BulkUserOperationService>();
        services.AddScoped<IBulkOperationService<Post, BulkPostOperationRequest, BulkPostOperationResponse>, BulkPostOperationService>();
        services.AddScoped<IBulkOperationService<Comment, BulkCommentOperationRequest, BulkCommentOperationResponse>, BulkCommentOperationService>();
        services.AddScoped<IBulkOperationService<Category, BulkCategoryOperationRequest, BulkCategoryOperationResponse>, BulkCategoryOperationService>();
        services.AddScoped<IBulkOperationService<Tag, BulkTagOperationRequest, BulkTagOperationResponse>, BulkTagOperationService>();

        // Permission and audit services
        services.AddScoped<IBulkOperationPermissionService, BulkOperationPermissionService>();
        services.AddScoped<IBulkOperationAuditService, BulkOperationAuditService>();

        // Validators
        services.AddScoped<IValidator<BulkOperationRequest>, BulkOperationRequestValidator>();
        services.AddScoped<IValidator<BulkUserOperationRequest>, BulkUserOperationRequestValidator>();
        services.AddScoped<IValidator<BulkPostOperationRequest>, BulkPostOperationRequestValidator>();
        services.AddScoped<IValidator<BulkCommentOperationRequest>, BulkCommentOperationRequestValidator>();
        services.AddScoped<IValidator<BulkCategoryOperationRequest>, BulkCategoryOperationRequestValidator>();
        services.AddScoped<IValidator<BulkTagOperationRequest>, BulkTagOperationRequestValidator>();

        // Background services for scheduled operations
        services.AddHostedService<BulkOperationSchedulerService>();

        // SignalR for progress tracking
        services.AddSignalR();

        return services;
    }
}

// In Program.cs or Startup.cs
builder.Services.AddBulkOperations();

// Register SignalR hub
app.MapHub<BulkOperationHub>(""/bulkOperationHub"");";
}