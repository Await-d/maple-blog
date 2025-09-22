using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Domain.Entities
{
    /// <summary>
    /// 审计日志实体
    /// </summary>
    public class AuditLog : BaseEntity
    {
        /// <summary>
        /// 操作用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名（冗余字段，提高查询性能）
        /// </summary>
        [StringLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// 用户邮箱（冗余字段，提高查询性能）
        /// </summary>
        [StringLength(320)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// 操作类型（Create, Update, Delete, Login, Logout等）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 资源类型（User, Post, Role, Permission等）
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// 资源ID
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 资源名称（冗余字段，提高查询和展示性能）
        /// </summary>
        [StringLength(200)]
        public string? ResourceName { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 变更前数据（JSON格式）
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// 变更后数据（JSON格式）
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        [StringLength(45)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User Agent
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [StringLength(500)]
        public string? RequestPath { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        [StringLength(10)]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// 响应状态码
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// 处理时长（毫秒）
        /// </summary>
        public long? Duration { get; set; }

        /// <summary>
        /// 操作结果（Success, Failed, Warning等）
        /// </summary>
        [StringLength(20)]
        public string Result { get; set; } = "Success";

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 额外数据（JSON格式）
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// 详细信息（别名，指向Description字段）
        /// </summary>
        public string? Details
        {
            get => Description;
            set => Description = value;
        }

        /// <summary>
        /// 操作时间戳（别名，指向CreatedAt字段）
        /// </summary>
        public DateTime Timestamp
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        /// <summary>
        /// 表名（别名，指向ResourceType字段）
        /// </summary>
        public string TableName
        {
            get => ResourceType;
            set => ResourceType = value;
        }

        /// <summary>
        /// 主键值（别名，指向ResourceId字段）
        /// </summary>
        public string? KeyValues
        {
            get => ResourceId;
            set => ResourceId = value;
        }

        /// <summary>
        /// 额外信息（别名，指向AdditionalData字段）
        /// </summary>
        public string? AdditionalInfo
        {
            get => AdditionalData;
            set => AdditionalData = value;
        }

        /// <summary>
        /// 风险级别（Low, Medium, High, Critical）
        /// </summary>
        [StringLength(20)]
        public string RiskLevel { get; set; } = "Low";

        /// <summary>
        /// 是否敏感操作
        /// </summary>
        public bool IsSensitive { get; set; } = false;

        /// <summary>
        /// 操作分类（Authentication, Authorization, DataModification, SystemConfiguration等）
        /// </summary>
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 会话ID
        /// </summary>
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// 关联的审计日志ID（用于关联相关操作）
        /// </summary>
        public Guid? CorrelationId { get; set; }

        /// <summary>
        /// 保留期限（天数）
        /// </summary>
        public int? RetentionPeriod { get; set; }

        /// <summary>
        /// 是否已归档
        /// </summary>
        public bool IsArchived { get; set; } = false;

        /// <summary>
        /// 归档时间
        /// </summary>
        public DateTime? ArchivedAt { get; set; }

        // 导航属性

        /// <summary>
        /// 操作用户
        /// </summary>
        public virtual User? User { get; set; }

        // 业务方法

        /// <summary>
        /// 设置用户信息
        /// </summary>
        /// <param name="user">用户对象</param>
        public void SetUser(User? user)
        {
            if (user != null)
            {
                UserId = user.Id;
                UserName = user.UserName;
                UserEmail = user.Email;
            }
            else
            {
                UserId = null;
                UserName = null;
                UserEmail = null;
            }
            UpdateAuditFields();
        }

        /// <summary>
        /// 设置请求信息
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="userAgent">User Agent</param>
        /// <param name="requestPath">请求路径</param>
        /// <param name="httpMethod">HTTP方法</param>
        /// <param name="sessionId">会话ID</param>
        public void SetRequestInfo(string? ipAddress, string? userAgent, string? requestPath, string? httpMethod, string? sessionId)
        {
            IpAddress = ipAddress?.Length > 45 ? ipAddress[..45] : ipAddress;
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent;
            RequestPath = requestPath?.Length > 500 ? requestPath[..500] : requestPath;
            HttpMethod = httpMethod?.Length > 10 ? httpMethod[..10] : httpMethod;
            SessionId = sessionId?.Length > 100 ? sessionId[..100] : sessionId;
            UpdateAuditFields();
        }

        /// <summary>
        /// 设置响应信息
        /// </summary>
        /// <param name="statusCode">状态码</param>
        /// <param name="duration">处理时长</param>
        /// <param name="result">结果</param>
        /// <param name="errorMessage">错误信息</param>
        public void SetResponseInfo(int? statusCode, long? duration, string result = "Success", string? errorMessage = null)
        {
            StatusCode = statusCode;
            Duration = duration;
            Result = result;
            ErrorMessage = errorMessage;

            // 根据状态码和错误信息确定结果
            if (statusCode.HasValue)
            {
                Result = statusCode.Value switch
                {
                    >= 200 and < 300 => "Success",
                    >= 300 and < 400 => "Redirect",
                    >= 400 and < 500 => "ClientError",
                    >= 500 => "ServerError",
                    _ => "Unknown"
                };
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                Result = "Failed";
            }

            UpdateAuditFields();
        }

        /// <summary>
        /// 设置资源信息
        /// </summary>
        /// <param name="resourceType">资源类型</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="resourceName">资源名称</param>
        public void SetResource(string resourceType, string? resourceId, string? resourceName = null)
        {
            ResourceType = resourceType;
            ResourceId = resourceId;
            ResourceName = resourceName?.Length > 200 ? resourceName[..200] : resourceName;
            UpdateAuditFields();
        }

        /// <summary>
        /// 设置审计分类和风险级别
        /// </summary>
        /// <param name="category">分类</param>
        /// <param name="riskLevel">风险级别</param>
        /// <param name="isSensitive">是否敏感</param>
        public void SetClassification(string category, string riskLevel = "Low", bool isSensitive = false)
        {
            Category = category;
            RiskLevel = riskLevel;
            IsSensitive = isSensitive;
            UpdateAuditFields();
        }

        /// <summary>
        /// 获取操作摘要
        /// </summary>
        /// <returns>操作摘要</returns>
        public string GetSummary()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(UserName))
                parts.Add($"用户: {UserName}");

            if (!string.IsNullOrEmpty(Action) && !string.IsNullOrEmpty(ResourceType))
                parts.Add($"操作: {Action} {ResourceType}");

            if (!string.IsNullOrEmpty(ResourceName))
                parts.Add($"资源: {ResourceName}");

            if (!string.IsNullOrEmpty(IpAddress))
                parts.Add($"IP: {IpAddress}");

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// 检查是否为高风险操作
        /// </summary>
        /// <returns>是否高风险</returns>
        public bool IsHighRisk()
        {
            return RiskLevel == "High" || RiskLevel == "Critical" || IsSensitive ||
                   Action.ToLowerInvariant().Contains("delete") ||
                   Action.ToLowerInvariant().Contains("remove") ||
                   Category.ToLowerInvariant().Contains("authentication") ||
                   Category.ToLowerInvariant().Contains("authorization");
        }

        /// <summary>
        /// 获取操作的严重程度分数
        /// </summary>
        /// <returns>严重程度分数（1-10）</returns>
        public int GetSeverityScore()
        {
            var score = RiskLevel switch
            {
                "Critical" => 10,
                "High" => 7,
                "Medium" => 5,
                "Low" => 2,
                _ => 1
            };

            if (IsSensitive) score += 2;
            if (Result == "Failed") score += 1;
            if (StatusCode >= 500) score += 2;
            if (StatusCode >= 400 && StatusCode < 500) score += 1;

            return Math.Min(score, 10);
        }
    }
}