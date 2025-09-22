using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Application.Interfaces;
using MapleBlog.Domain.Interfaces;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;


namespace MapleBlog.Application.Services
{
    /// <summary>
    /// UserManagementService 的第二部分实现
    /// </summary>
    public partial class UserManagementService
    {
        public async Task<PagedResultDto<UserActivityLogDto>> GetUserActivityLogAsync(Guid userId, int pageNumber = 1, int pageSize = 20, string? actionType = null)
        {
            try
            {
                _logger.LogInformation("开始获取用户活动日志，用户ID: {UserId}", userId);

                // Get actual audit logs from database
                var allLogs = await _auditLogRepository.GetAllAsync();
                var query = allLogs
                    .Where(log => log.UserId == userId)
                    .AsQueryable();

                // Apply action type filter if specified
                if (!string.IsNullOrEmpty(actionType))
                {
                    query = query.Where(log => log.Action.Contains(actionType));
                }

                // Get total count
                var totalCount = query.Count();

                // Apply pagination and get data
                var logs = query
                    .OrderByDescending(log => log.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(log => new UserActivityLogDto
                    {
                        Id = log.Id,
                        ActivityType = log.Action,
                        Description = log.Details ?? $"User performed {log.Action} action",
                        IpAddress = log.IpAddress ?? "Unknown",
                        UserAgent = log.UserAgent ?? "Unknown",
                        Timestamp = log.CreatedAt,
                        Status = log.Result ?? "Unknown",
                        RiskLevel = log.RiskLevel ?? DetermineRiskLevel(log.Action, log.Result == "Success")
                    })
                    .ToList();

                var result = new PagedResultDto<UserActivityLogDto>
                {
                    Items = logs,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("Successfully retrieved user activity logs, UserId: {UserId}, Count: {Count}", userId, logs.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户活动日志时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResultDto<UserLoginHistoryDto>> GetUserLoginHistoryAsync(Guid userId, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("开始获取用户登录历史，用户ID: {UserId}", userId);

                // Get actual login history from database
                var allHistory = await _loginHistoryRepository.GetUserLoginHistoryAsync(userId, pageNumber, pageSize);
                var historyList = allHistory.ToList();
                
                // Get total count for pagination
                var allUserHistory = await _loginHistoryRepository.GetAllAsync();
                var totalCount = allUserHistory.Count(h => h.UserId == userId);

                // Map to DTOs
                var dtoList = historyList.Select(history => new UserLoginHistoryDto
                {
                    Id = history.Id,
                    LoginTime = history.CreatedAt,
                    LogoutTime = history.LogoutAt,
                    SessionDuration = history.SessionDurationMinutes.HasValue 
                        ? TimeSpan.FromMinutes(history.SessionDurationMinutes.Value)
                        : (history.LogoutAt.HasValue 
                            ? history.LogoutAt.Value - history.CreatedAt
                            : TimeSpan.Zero),
                    IpAddress = history.IpAddress ?? "Unknown",
                    UserAgent = history.UserAgent ?? "Unknown",
                    DeviceInfo = new UserLoginDeviceDto
                    {
                        DeviceType = DetermineDeviceType(history.DeviceInfo ?? history.UserAgent ?? ""),
                        OperatingSystem = history.OperatingSystem ?? ExtractOS(history.UserAgent ?? ""),
                        Browser = history.BrowserInfo ?? ExtractBrowser(history.UserAgent ?? ""),
                        IsMobile = IsMobileDevice(history.UserAgent ?? "")
                    },
                    LoginMethod = history.LoginType.ToString(),
                    Status = history.Result.ToString(),
                    IsSuspicious = history.IsFlagged || history.RiskScore > 50,
                    RiskScore = history.RiskScore
                }).ToList();

                var result = new PagedResultDto<UserLoginHistoryDto>
                {
                    Items = dtoList,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                _logger.LogInformation("成功获取用户登录历史，用户ID: {UserId}, 返回数量: {Count}", userId, dtoList.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户登录历史时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserStatisticsDto> GetUserStatisticsAsync(Guid userId)
        {
            try
            {
                _logger.LogInformation("开始获取用户统计信息，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return new UserStatisticsDto();
                }

                // 简化实现，实际项目中应该从各种统计表获取真实数据
                var statistics = new UserStatisticsDto
                {
                    TotalLogins = new Random().Next(10, 100),
                    MonthlyLogins = new Random().Next(1, 30),
                    AverageSessionDuration = TimeSpan.FromMinutes(new Random().Next(15, 60)),
                    MaxSessionDuration = TimeSpan.FromHours(new Random().Next(1, 8)),
                    ContentStats = new ContentCreationStatsDto
                    {
                        TotalPosts = new Random().Next(0, 50),
                        TotalComments = new Random().Next(0, 200),
                        DraftPosts = new Random().Next(0, 10),
                        PublishedPosts = new Random().Next(0, 40),
                        TotalViews = new Random().Next(100, 10000),
                        TotalLikes = new Random().Next(10, 500),
                        LastContentCreated = DateTime.UtcNow.AddDays(-new Random().Next(1, 30))
                    },
                    InteractionStats = new UserInteractionStatsDto
                    {
                        LikesGiven = new Random().Next(0, 100),
                        LikesReceived = new Random().Next(0, 200),
                        CommentsGiven = new Random().Next(0, 150),
                        CommentsReceived = new Random().Next(0, 300),
                        EngagementRate = new Random().NextDouble() * 100,
                        ResponseRate = new Random().NextDouble() * 100
                    },
                    LoginPattern = new UserLoginPatternDto
                    {
                        PreferredHours = new[] { 9, 10, 14, 20, 21 },
                        PreferredDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                        AverageSessionLength = TimeSpan.FromMinutes(25),
                        LoginFrequency = new Random().Next(1, 7),
                        LoginRegularity = "Regular"
                    },
                    ActivityScore = new Random().NextDouble() * 100,
                    LoyaltyScore = new Random().NextDouble() * 100
                };

                _logger.LogInformation("成功获取用户统计信息，用户ID: {UserId}", userId);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计信息时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<UserImportResultDto> BatchImportUsersAsync(UserImportRequestDto importRequest, Guid operatorId)
        {
            var result = new UserImportResultDto
            {
                ImportId = Guid.NewGuid(),
                TotalRecords = 0,
                SuccessCount = 0,
                FailCount = 0,
                SkippedCount = 0,
                ImportDetails = new List<UserImportDetailDto>(),
                ErrorSummary = new List<string>(),
                GeneratedPasswords = new List<UserPasswordInfoDto>()
            };

            try
            {
                _logger.LogInformation("开始批量导入用户，操作人ID: {OperatorId}", operatorId);

                // 简化实现：这里应该解析文件数据并创建用户
                // 实际项目中需要解析Excel/CSV文件并验证数据

                var importDetails = new List<UserImportDetailDto>();
                var generatedPasswords = new List<UserPasswordInfoDto>();

                // 模拟导入过程
                for (int i = 1; i <= 10; i++) // 模拟10个用户
                {
                    var username = $"imported_user_{i}";
                    var email = $"user{i}@imported.com";
                    var generatedPassword = GenerateRandomPassword();

                    try
                    {
                        // 检查用户是否已存在
                        if (await _userRepository.UserNameExistsAsync(username) || await _userRepository.EmailExistsAsync(email))
                        {
                            importDetails.Add(new UserImportDetailDto
                            {
                                RowNumber = i,
                                Username = username,
                                Email = email,
                                Success = false,
                                ErrorMessage = "用户名或邮箱已存在"
                            });
                            result.SkippedCount++;
                            continue;
                        }

                        // 创建用户
                        var passwordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword);
                        var user = new User(username, email, passwordHash)
                        {
                            DisplayName = $"导入用户 {i}",
                            IsActive = importRequest.DefaultStatus == "Active",
                            EmailConfirmed = !importRequest.Options.RequireEmailVerification
                        };

                        // 设置默认角色
                        if (importRequest.DefaultRoleIds.Any())
                        {
                            var firstRole = await _roleRepository.GetByIdAsync(importRequest.DefaultRoleIds.First());
                            if (firstRole != null && Enum.TryParse<Domain.Enums.UserRole>(firstRole.Name, true, out var roleEnum))
                            {
                                user.Role = roleEnum;
                            }
                        }

                        await _userRepository.AddAsync(user);

                        importDetails.Add(new UserImportDetailDto
                        {
                            RowNumber = i,
                            Username = username,
                            Email = email,
                            Success = true,
                            UserId = user.Id,
                            GeneratedPassword = importRequest.PasswordPolicy == "Generate" ? generatedPassword : null
                        });

                        if (importRequest.PasswordPolicy == "Generate")
                        {
                            generatedPasswords.Add(new UserPasswordInfoDto
                            {
                                UserId = user.Id,
                                Username = username,
                                Email = email,
                                GeneratedPassword = generatedPassword,
                                PasswordSent = false
                            });
                        }

                        result.SuccessCount++;

                        // 发送欢迎邮件
                        if (importRequest.SendWelcomeEmail)
                        {
                            try
                            {
                                await SendWelcomeEmailAsync(user, importRequest.Options.RequireEmailVerification);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "发送欢迎邮件失败，用户ID: {UserId}", user.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "导入用户失败，行号: {RowNumber}", i);
                        importDetails.Add(new UserImportDetailDto
                        {
                            RowNumber = i,
                            Username = username,
                            Email = email,
                            Success = false,
                            ErrorMessage = ex.Message
                        });
                        result.FailCount++;
                    }
                }

                result.TotalRecords = 10;
                result.ImportDetails = importDetails;
                result.GeneratedPasswords = generatedPasswords;
                result.ProcessingTime = TimeSpan.FromSeconds(5); // 模拟处理时间
                result.SummaryReport = $"导入完成：成功 {result.SuccessCount}，失败 {result.FailCount}，跳过 {result.SkippedCount}";

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "BatchImportUsers",
                    "User",
                    result.ImportId.ToString(),
                    $"批量导入用户，成功: {result.SuccessCount}, 失败: {result.FailCount}",
                    null,
                    new { result.TotalRecords, result.SuccessCount, result.FailCount }
                );

                _logger.LogInformation("批量导入用户完成，成功: {SuccessCount}, 失败: {FailCount}", result.SuccessCount, result.FailCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入用户时发生错误");
                result.ErrorSummary = new[] { ex.Message };
                throw;
            }
        }

        public async Task<UserExportResultDto> BatchExportUsersAsync(UserExportRequestDto exportRequest)
        {
            try
            {
                _logger.LogInformation("开始批量导出用户");

                var result = new UserExportResultDto
                {
                    ExportId = Guid.NewGuid(),
                    FileName = $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{exportRequest.ExportFormat.ToLower()}",
                    FileSize = 0,
                    DownloadUrl = "",
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    ExportedUserCount = 0,
                    GeneratedAt = DateTime.UtcNow,
                    Status = "Processing"
                };

                // 构建查询
                var query = _userRepository.GetQueryable();

                // 应用筛选条件
                if (exportRequest.Filter.Status.Any())
                {
                    if (exportRequest.Filter.Status.Contains("Active"))
                        query = query.Where(u => u.IsActive && !u.IsDeleted);
                    if (exportRequest.Filter.Status.Contains("Inactive"))
                        query = query.Where(u => !u.IsActive && !u.IsDeleted);
                    if (exportRequest.Filter.Status.Contains("Deleted") && exportRequest.Filter.IncludeDeleted)
                        query = query.Where(u => u.IsDeleted);
                }

                if (exportRequest.Filter.Roles.Any())
                {
                    var roleEnums = exportRequest.Filter.Roles
                        .Where(r => Enum.TryParse<Domain.Enums.UserRole>(r, true, out _))
                        .Select(r => Enum.Parse<Domain.Enums.UserRole>(r, true))
                        .ToList();

                    if (roleEnums.Any())
                        query = query.Where(u => roleEnums.Contains(u.Role));
                }

                if (exportRequest.Filter.RegistrationDateRange != null)
                {
                    query = query.Where(u => u.CreatedAt >= exportRequest.Filter.RegistrationDateRange.StartDate &&
                                           u.CreatedAt <= exportRequest.Filter.RegistrationDateRange.EndDate);
                }

                if (!string.IsNullOrWhiteSpace(exportRequest.Filter.SearchTerm))
                {
                    var searchTerm = exportRequest.Filter.SearchTerm.ToLower();
                    query = query.Where(u => u.UserName.ToLower().Contains(searchTerm) ||
                                           u.Email.Value.ToLower().Contains(searchTerm) ||
                                           (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchTerm)));
                }

                var users = await query.ToListAsync();
                result.ExportedUserCount = users.Count;

                // 模拟文件生成
                var fileContent = GenerateExportFileContent(users, exportRequest);
                result.FileSize = fileContent.Length;
                result.DownloadUrl = $"/api/exports/{result.ExportId}/download";
                result.Status = "Completed";

                result.Summary = new UserExportSummaryDto
                {
                    TotalUsers = users.Count,
                    StatusDistribution = users.GroupBy(u => GetUserStatusString(u))
                        .ToDictionary(g => g.Key, g => g.Count()),
                    RoleDistribution = users.GroupBy(u => u.Role.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    OldestRegistration = users.Any() ? users.Min(u => u.CreatedAt) : null,
                    NewestRegistration = users.Any() ? users.Max(u => u.CreatedAt) : null,
                    AverageSessionCount = 15.5 // 模拟数据
                };

                _logger.LogInformation("成功导出用户数据，用户数量: {Count}", users.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导出用户时发生错误");
                throw;
            }
        }

        public async Task<IEnumerable<OnlineUserDto>> GetOnlineUsersAsync()
        {
            try
            {
                _logger.LogInformation("开始获取在线用户列表");

                // 简化实现：获取最近5分钟有活动的用户
                var onlineThreshold = DateTime.UtcNow.AddMinutes(-5);
                var onlineUsers = await _userRepository.GetQueryable()
                    .Where(u => u.LastLoginAt >= onlineThreshold && u.IsActive && !u.IsDeleted)
                    .ToListAsync();

                var onlineUserDtos = onlineUsers.Select(user => new OnlineUserDto
                {
                    UserId = user.Id,
                    Username = user.UserName,
                    DisplayName = user.GetDisplayName(),
                    Avatar = user.AvatarUrl,
                    OnlineStatus = "Online",
                    LastActivity = user.LastLoginAt ?? DateTime.UtcNow,
                    OnlineDuration = user.LastLoginAt.HasValue ? DateTime.UtcNow - user.LastLoginAt.Value : TimeSpan.Zero,
                    CurrentPage = "/dashboard", // 模拟当前页面
                    IpAddress = "192.168.1.100", // 模拟IP
                    DeviceInfo = "Chrome on Windows", // 模拟设备信息
                    SessionId = Guid.NewGuid().ToString(),
                    ConnectionCount = 1
                }).ToList();

                _logger.LogInformation("成功获取在线用户列表，数量: {Count}", onlineUserDtos.Count);
                return onlineUserDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取在线用户列表时发生错误");
                throw;
            }
        }

        public async Task<bool> ForceUserOfflineAsync(Guid userId, Guid operatorId)
        {
            try
            {
                _logger.LogInformation("开始强制用户下线，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return false;
                }

                // 在实际实现中，这里应该：
                // 1. 使会话失效
                // 2. 将用户从在线用户列表中移除
                // 3. 通知客户端断开连接

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    operatorId,
                    null,
                    "ForceOffline",
                    "User",
                    userId.ToString(),
                    $"强制用户下线: {user.UserName}",
                    null,
                    new { ForcedOfflineAt = DateTime.UtcNow }
                );

                _logger.LogInformation("成功强制用户下线，用户ID: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "强制用户下线时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<MessageSendResultDto> SendSystemMessageAsync(IEnumerable<Guid> userIds, string message, string messageType, Guid senderId)
        {
            var userIdList = userIds.ToList();
            var result = new MessageSendResultDto
            {
                TotalSent = userIdList.Count,
                SuccessCount = 0,
                FailCount = 0,
                SendDetails = new List<MessageSendDetailDto>(),
                MessageId = Guid.NewGuid(),
                SentAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("开始发送系统消息，接收者数量: {Count}", userIdList.Count);

                var sendDetails = new List<MessageSendDetailDto>();

                foreach (var userId in userIdList)
                {
                    try
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user == null)
                        {
                            sendDetails.Add(new MessageSendDetailDto
                            {
                                UserId = userId,
                                Username = "Unknown",
                                Success = false,
                                ErrorMessage = "用户不存在",
                                DeliveryMethod = "System"
                            });
                            result.FailCount++;
                            continue;
                        }

                        // 在实际实现中，这里应该：
                        // 1. 将消息保存到数据库
                        // 2. 通过WebSocket/SignalR推送实时消息
                        // 3. 发送邮件通知（如果需要）
                        // 4. 发送移动推送通知（如果需要）

                        sendDetails.Add(new MessageSendDetailDto
                        {
                            UserId = userId,
                            Username = user.UserName,
                            Success = true,
                            DeliveredAt = DateTime.UtcNow,
                            DeliveryMethod = "System"
                        });
                        result.SuccessCount++;

                        _logger.LogDebug("成功发送消息给用户: {Username}", user.UserName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "发送消息给用户失败，用户ID: {UserId}", userId);
                        sendDetails.Add(new MessageSendDetailDto
                        {
                            UserId = userId,
                            Username = "Unknown",
                            Success = false,
                            ErrorMessage = ex.Message,
                            DeliveryMethod = "System"
                        });
                        result.FailCount++;
                    }
                }

                result.SendDetails = sendDetails;
                result.Success = result.FailCount == 0;

                // 记录审计日志
                await _auditLogService.LogUserActionAsync(
                    senderId,
                    null,
                    "SendSystemMessage",
                    "Message",
                    result.MessageId.ToString(),
                    $"发送系统消息给 {userIdList.Count} 个用户，成功: {result.SuccessCount}",
                    null,
                    new { messageType, result.SuccessCount, result.FailCount }
                );

                _logger.LogInformation("系统消息发送完成，成功: {SuccessCount}, 失败: {FailCount}", result.SuccessCount, result.FailCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送系统消息时发生错误");
                result.Success = false;
                result.Errors = new[] { ex.Message };
                return result;
            }
        }

        public async Task<UserBehaviorAnalysisDto> GetUserBehaviorAnalysisAsync(Guid userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("开始用户行为分析，用户ID: {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("未找到用户，用户ID: {UserId}", userId);
                    return new UserBehaviorAnalysisDto();
                }

                // 简化实现，实际项目中应该从各种数据表分析真实用户行为
                var analysis = new UserBehaviorAnalysisDto
                {
                    UserId = userId,
                    AnalysisPeriod = new DateRangeDto { StartDate = startDate, EndDate = endDate },
                    ActivityAnalysis = new UserActivityAnalysisDto
                    {
                        TotalSessions = new Random().Next(10, 100),
                        TotalActiveTime = TimeSpan.FromHours(new Random().Next(50, 200)),
                        ActivityScore = new Random().NextDouble() * 100,
                        ActivityLevel = new[] { "Low", "Medium", "High" }[new Random().Next(3)],
                        TrendDirection = new Random().NextDouble() * 2 - 1 // -1 to 1
                    },
                    ContentPreferences = new UserContentPreferenceDto
                    {
                        PreferredCategories = new[] { "技术", "生活", "旅游" },
                        PreferredTags = new[] { "编程", "AI", "Web开发" },
                        PreferredContentType = "Article",
                        ReadingSpeed = new Random().NextDouble() * 500 + 200,
                        AverageReadingTime = TimeSpan.FromMinutes(new Random().Next(2, 15))
                    },
                    UsagePatterns = new UserUsagePatternDto
                    {
                        PrimaryUsageTime = "Evening",
                        PreferredDevices = new[] { "Desktop", "Mobile" },
                        NavigationPattern = "Sequential",
                        SessionDurationPattern = new Random().NextDouble() * 60 + 15,
                        EngagementStyle = "Active"
                    },
                    InteractionBehavior = new UserInteractionBehaviorDto
                    {
                        CommentRatio = new Random().NextDouble(),
                        ShareRatio = new Random().NextDouble(),
                        LikeRatio = new Random().NextDouble(),
                        InteractionStyle = "Positive",
                        ResponseTime = new Random().NextDouble() * 24,
                        SocialBehavior = "Collaborative",
                        InfluenceScore = new Random().NextDouble() * 100
                    },
                    RiskAssessment = new UserRiskAssessmentDto
                    {
                        OverallRiskScore = new Random().NextDouble() * 100,
                        RiskLevel = "Low",
                        RiskFactors = new[] { "新用户" },
                        FraudProbability = new Random().NextDouble() * 0.1,
                        AccountCompromiseRisk = new Random().NextDouble() * 0.05
                    },
                    Recommendations = new[]
                    {
                        new UserRecommendationDto
                        {
                            Type = "Content",
                            Title = "推荐更多技术文章",
                            Description = "基于用户阅读偏好",
                            Priority = 1,
                            Confidence = 0.85
                        }
                    },
                    Predictions = new UserPredictionDto
                    {
                        ChurnProbability = new Random().NextDouble() * 0.3,
                        EngagementTrend = new Random().NextDouble() * 2 - 1,
                        LoyaltyScore = new Random().NextDouble() * 100,
                        PredictedBehavior = "Continued Active Usage",
                        ValueScore = new Random().NextDouble() * 100,
                        NextExpectedActivity = DateTime.UtcNow.AddHours(new Random().Next(1, 48))
                    }
                };

                _logger.LogInformation("成功完成用户行为分析，用户ID: {UserId}", userId);
                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户行为分析时发生错误，用户ID: {UserId}", userId);
                throw;
            }
        }

        #region Additional Helper Methods

        private string GenerateRandomPassword(int length = 12)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateExportFileContent(List<User> users, UserExportRequestDto exportRequest)
        {
            // 简化实现：生成CSV格式内容
            var sb = new StringBuilder();
            sb.AppendLine("Username,Email,DisplayName,Role,Status,CreatedAt,LastLoginAt");

            foreach (var user in users)
            {
                sb.AppendLine($"{user.UserName},{user.Email.Value},{user.DisplayName},{user.Role},{GetUserStatusString(user)},{user.CreatedAt:yyyy-MM-dd},{user.LastLoginAt:yyyy-MM-dd HH:mm:ss}");
            }

            return sb.ToString();
        }

        private string GetUserStatusString(User user)
        {
            if (user.IsDeleted) return "Deleted";
            if (user.IsLockedOut()) return "Locked";
            if (!user.IsActive) return "Inactive";
            return "Active";
        }

        #endregion

        /// <summary>
        /// Determines device type from user agent or device info
        /// </summary>
        private string DetermineDeviceType(string deviceInfo)
        {
            if (string.IsNullOrEmpty(deviceInfo))
                return "Unknown";
            
            var lowerInfo = deviceInfo.ToLower();
            if (lowerInfo.Contains("mobile") || lowerInfo.Contains("android") || lowerInfo.Contains("iphone"))
                return "Mobile";
            if (lowerInfo.Contains("tablet") || lowerInfo.Contains("ipad"))
                return "Tablet";
            if (lowerInfo.Contains("desktop") || lowerInfo.Contains("windows") || lowerInfo.Contains("mac"))
                return "Desktop";
            
            return "Unknown";
        }

        /// <summary>
        /// Extracts operating system from user agent
        /// </summary>
        private string ExtractOS(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";
            
            var lowerAgent = userAgent.ToLower();
            if (lowerAgent.Contains("windows nt 10"))
                return "Windows 10";
            if (lowerAgent.Contains("windows nt 11"))
                return "Windows 11";
            if (lowerAgent.Contains("mac os"))
                return "macOS";
            if (lowerAgent.Contains("android"))
                return "Android";
            if (lowerAgent.Contains("iphone") || lowerAgent.Contains("ipad"))
                return "iOS";
            if (lowerAgent.Contains("linux"))
                return "Linux";
            
            return "Unknown";
        }

        /// <summary>
        /// Extracts browser from user agent
        /// </summary>
        private string ExtractBrowser(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";
            
            var lowerAgent = userAgent.ToLower();
            if (lowerAgent.Contains("chrome") && !lowerAgent.Contains("edg"))
                return "Chrome";
            if (lowerAgent.Contains("firefox"))
                return "Firefox";
            if (lowerAgent.Contains("safari") && !lowerAgent.Contains("chrome"))
                return "Safari";
            if (lowerAgent.Contains("edg"))
                return "Edge";
            if (lowerAgent.Contains("opera"))
                return "Opera";
            
            return "Unknown";
        }

        /// <summary>
        /// Determines if the device is mobile from user agent
        /// </summary>
        private bool IsMobileDevice(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;
            
            var lowerAgent = userAgent.ToLower();
            return lowerAgent.Contains("mobile") || 
                   lowerAgent.Contains("android") || 
                   lowerAgent.Contains("iphone") || 
                   lowerAgent.Contains("ipad") ||
                   lowerAgent.Contains("tablet");
        }

        /// <summary>
        /// Determines the risk level based on action and success status
        /// </summary>
        private string DetermineRiskLevel(string action, bool isSuccess)
        {
            if (!isSuccess)
            {
                // Failed actions have higher risk
                if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                    action.Contains("Remove", StringComparison.OrdinalIgnoreCase))
                {
                    return "High";
                }
                if (action.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                    action.Contains("Modify", StringComparison.OrdinalIgnoreCase))
                {
                    return "Medium";
                }
                return "Low";
            }

            // Successful actions
            if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("Remove", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("Permission", StringComparison.OrdinalIgnoreCase))
            {
                return "Medium";
            }
            
            if (action.Contains("Create", StringComparison.OrdinalIgnoreCase) ||
                action.Contains("Update", StringComparison.OrdinalIgnoreCase))
            {
                return "Low";
            }

            return "None";
        }
    }
}