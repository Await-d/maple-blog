using AutoMapper;
using MapleBlog.Application.DTOs;
using MapleBlog.Application.DTOs.Admin;
using MapleBlog.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace MapleBlog.Application.Mappings.Validation
{
    /// <summary>
    /// AutoMapper映射验证接口
    /// </summary>
    public interface IMappingValidator
    {
        /// <summary>
        /// 验证映射配置
        /// </summary>
        MappingValidationResult ValidateConfiguration(IMapper mapper);

        /// <summary>
        /// 验证映射结果
        /// </summary>
        MappingValidationResult ValidateMappingResult<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        DataIntegrityValidationResult ValidateDataIntegrity<T>(T entity) where T : class;
    }

    /// <summary>
    /// AutoMapper映射验证器实现
    /// </summary>
    public class MappingValidator : IMappingValidator
    {
        private readonly ILogger<MappingValidator> _logger;

        public MappingValidator(ILogger<MappingValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 验证AutoMapper配置
        /// </summary>
        public MappingValidationResult ValidateConfiguration(IMapper mapper)
        {
            var result = new MappingValidationResult();

            try
            {
                _logger.LogInformation("开始验证AutoMapper配置");

                // 验证所有映射配置
                mapper.ConfigurationProvider.AssertConfigurationIsValid();

                result.IsValid = true;
                result.Message = "AutoMapper配置验证成功";

                _logger.LogInformation("AutoMapper配置验证成功");
            }
            catch (AutoMapperConfigurationException ex)
            {
                result.IsValid = false;
                result.Message = "AutoMapper配置验证失败";
                result.Errors = ex.Errors?.Select(e => e.ToString()).ToList() ?? new List<string> { ex.Message };

                _logger.LogError(ex, "AutoMapper配置验证失败: {Errors}", string.Join(", ", result.Errors));
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = "AutoMapper配置验证出现异常";
                result.Errors = new List<string> { ex.Message };

                _logger.LogError(ex, "AutoMapper配置验证异常");
            }

            return result;
        }

        /// <summary>
        /// 验证映射结果
        /// </summary>
        public MappingValidationResult ValidateMappingResult<TSource, TDestination>(TSource source, TDestination destination)
        {
            var result = new MappingValidationResult();
            var errors = new List<string>();

            try
            {
                // 验证源对象不为空
                if (source == null)
                {
                    errors.Add("源对象不能为空");
                }

                // 验证目标对象不为空
                if (destination == null)
                {
                    errors.Add("映射后的目标对象为空");
                }

                if (source != null && destination != null)
                {
                    // 执行特定类型的验证
                    ValidateSpecificTypes(source, destination, errors);

                    // 验证必需属性
                    ValidateRequiredProperties(destination, errors);

                    // 验证数据一致性
                    ValidateDataConsistency(source, destination, errors);
                }

                result.IsValid = errors.Count == 0;
                result.Errors = errors;
                result.Message = result.IsValid ? "映射验证成功" : "映射验证失败";

                if (!result.IsValid)
                {
                    _logger.LogWarning("映射验证失败 {SourceType} -> {DestinationType}: {Errors}",
                        typeof(TSource).Name, typeof(TDestination).Name, string.Join(", ", errors));
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = "映射验证出现异常";
                result.Errors = new List<string> { ex.Message };

                _logger.LogError(ex, "映射验证异常 {SourceType} -> {DestinationType}",
                    typeof(TSource).Name, typeof(TDestination).Name);
            }

            return result;
        }

        /// <summary>
        /// 验证数据完整性
        /// </summary>
        public DataIntegrityValidationResult ValidateDataIntegrity<T>(T entity) where T : class
        {
            var result = new DataIntegrityValidationResult();
            var validationResults = new List<ValidationResult>();

            try
            {
                var validationContext = new ValidationContext(entity);
                var isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

                result.IsValid = isValid;
                result.ValidationResults = validationResults;
                result.Message = isValid ? "数据完整性验证成功" : "数据完整性验证失败";

                if (!isValid)
                {
                    _logger.LogWarning("数据完整性验证失败 {EntityType}: {Errors}",
                        typeof(T).Name, string.Join(", ", validationResults.Select(v => v.ErrorMessage)));
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = "数据完整性验证出现异常";
                result.Exception = ex;

                _logger.LogError(ex, "数据完整性验证异常 {EntityType}", typeof(T).Name);
            }

            return result;
        }

        #region 私有验证方法

        /// <summary>
        /// 验证特定类型的映射
        /// </summary>
        private void ValidateSpecificTypes<TSource, TDestination>(TSource source, TDestination destination, List<string> errors)
        {
            // User相关验证
            if (source is User sourceUser && destination is UserDto destUserDto)
            {
                ValidateUserMapping(sourceUser, destUserDto, errors);
            }
            else if (source is User sourceUser2 && destination is UserManagementDto destUserMgmtDto)
            {
                ValidateUserManagementMapping(sourceUser2, destUserMgmtDto, errors);
            }
            // Post相关验证
            else if (source is Post sourcePost && destination is PostDto destPostDto)
            {
                ValidatePostMapping(sourcePost, destPostDto, errors);
            }
            // Comment相关验证
            else if (source is Comment sourceComment && destination is CommentDto destCommentDto)
            {
                ValidateCommentMapping(sourceComment, destCommentDto, errors);
            }
        }

        /// <summary>
        /// 验证用户映射
        /// </summary>
        private void ValidateUserMapping(User source, UserDto destination, List<string> errors)
        {
            // 验证基本属性
            if (source.Id != destination.Id)
                errors.Add("用户ID映射不一致");

            if (source.UserName != destination.UserName)
                errors.Add("用户名映射不一致");

            if (source.Email.Value != destination.Email)
                errors.Add("邮箱映射不一致");

            // 验证计算属性
            if (source.GetDisplayName() != destination.DisplayName)
                errors.Add("显示名称映射不一致");

            if (source.GetFullName() != destination.FullName)
                errors.Add("全名映射不一致");

            // 验证状态属性
            if (source.EmailConfirmed != destination.IsEmailVerified)
                errors.Add("邮箱验证状态映射不一致");

            var expectedIsActive = source.IsActive && !source.IsLockedOut();
            if (expectedIsActive != destination.IsActive)
                errors.Add("活跃状态映射不一致");
        }

        /// <summary>
        /// 验证用户管理映射
        /// </summary>
        private void ValidateUserManagementMapping(User source, UserManagementDto destination, List<string> errors)
        {
            // 验证基本属性
            if (source.Id != destination.Id)
                errors.Add("用户ID映射不一致");

            if (source.UserName != destination.Username)
                errors.Add("用户名映射不一致");

            if (source.Email.Value != destination.Email)
                errors.Add("邮箱映射不一致");

            // 验证状态
            if (source.EmailConfirmed != destination.EmailVerified)
                errors.Add("邮箱验证状态映射不一致");

            if (source.PhoneNumberConfirmed != destination.PhoneVerified)
                errors.Add("手机验证状态映射不一致");

            if (source.TwoFactorEnabled != destination.TwoFactorEnabled)
                errors.Add("双因子认证状态映射不一致");

            // 验证时间
            if (source.LockoutEndDateUtc != destination.LockoutEnd)
                errors.Add("锁定结束时间映射不一致");

            if (source.AccessFailedCount != destination.AccessFailedCount)
                errors.Add("失败登录次数映射不一致");
        }

        /// <summary>
        /// 验证文章映射
        /// </summary>
        private void ValidatePostMapping(Post source, PostDto destination, List<string> errors)
        {
            // 验证基本属性
            if (source.Id != destination.Id)
                errors.Add("文章ID映射不一致");

            if (source.Title != destination.Title)
                errors.Add("文章标题映射不一致");

            if (source.Slug != destination.Slug)
                errors.Add("文章Slug映射不一致");

            if (source.Content != destination.Content)
                errors.Add("文章内容映射不一致");

            // 验证统计信息
            if (source.ViewCount != destination.Stats.ViewCount)
                errors.Add("浏览量映射不一致");

            if (source.LikeCount != destination.Stats.LikeCount)
                errors.Add("点赞数映射不一致");

            if (source.CommentCount != destination.Stats.CommentCount)
                errors.Add("评论数映射不一致");

            if (source.ShareCount != destination.Stats.ShareCount)
                errors.Add("分享数映射不一致");

            // 验证设置
            if (source.AllowComments != destination.Settings.AllowComments)
                errors.Add("评论设置映射不一致");

            if (source.IsFeatured != destination.Settings.IsFeatured)
                errors.Add("推荐设置映射不一致");

            if (source.IsSticky != destination.Settings.IsSticky)
                errors.Add("置顶设置映射不一致");
        }

        /// <summary>
        /// 验证评论映射
        /// </summary>
        private void ValidateCommentMapping(Comment source, CommentDto destination, List<string> errors)
        {
            // 验证基本属性
            if (source.Id != destination.Id)
                errors.Add("评论ID映射不一致");

            if (source.Content.RawContent != destination.Content)
                errors.Add("评论内容映射不一致");

            if (source.PostId != destination.PostId)
                errors.Add("文章ID映射不一致");

            if (source.ParentId != destination.ParentId)
                errors.Add("父评论ID映射不一致");

            // 验证层级信息
            if (source.ThreadPath.Path != destination.ThreadPath)
                errors.Add("评论路径映射不一致");

            if (source.ThreadPath.Depth != destination.Depth)
                errors.Add("评论深度映射不一致");

            // 验证状态
            if (source.Status.ToString() != destination.Status.ToString())
                errors.Add("评论状态映射不一致");

            if (source.LikeCount != destination.LikeCount)
                errors.Add("点赞数映射不一致");
        }

        /// <summary>
        /// 验证必需属性
        /// </summary>
        private void ValidateRequiredProperties<T>(T destination, List<string> errors)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(RequiredAttribute), false).Any());

            foreach (var property in properties)
            {
                var value = property.GetValue(destination);
                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    errors.Add($"必需属性 {property.Name} 为空");
                }
            }
        }

        /// <summary>
        /// 验证数据一致性
        /// </summary>
        private void ValidateDataConsistency<TSource, TDestination>(TSource source, TDestination destination, List<string> errors)
        {
            // 验证时间戳一致性
            if (source is BaseEntity sourceEntity && destination is BaseEntityDto destDto)
            {
                if (sourceEntity.CreatedAt != destDto.CreatedAt)
                    errors.Add("创建时间映射不一致");

                if (sourceEntity.UpdatedAt != destDto.UpdatedAt)
                    errors.Add("更新时间映射不一致");
            }

            // 验证枚举值转换
            ValidateEnumConsistency(source, destination, errors);
        }

        /// <summary>
        /// 验证枚举一致性
        /// </summary>
        private void ValidateEnumConsistency<TSource, TDestination>(TSource source, TDestination destination, List<string> errors)
        {
            var sourceProps = typeof(TSource).GetProperties().Where(p => p.PropertyType.IsEnum);
            var destProps = typeof(TDestination).GetProperties().Where(p => p.PropertyType == typeof(string));

            foreach (var sourceProp in sourceProps)
            {
                var destProp = destProps.FirstOrDefault(d => d.Name == sourceProp.Name);
                if (destProp != null)
                {
                    var sourceValue = sourceProp.GetValue(source)?.ToString();
                    var destValue = destProp.GetValue(destination) as string;

                    if (sourceValue != destValue)
                    {
                        errors.Add($"枚举属性 {sourceProp.Name} 映射不一致: {sourceValue} != {destValue}");
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// 映射验证结果
    /// </summary>
    public class MappingValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 数据完整性验证结果
    /// </summary>
    public class DataIntegrityValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 验证结果列表
        /// </summary>
        public List<ValidationResult> ValidationResults { get; set; } = new List<ValidationResult>();

        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// 验证时间
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 基础实体DTO接口（用于验证）
    /// </summary>
    public interface BaseEntityDto
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 映射错误处理器
    /// </summary>
    public class MappingErrorHandler
    {
        private readonly ILogger<MappingErrorHandler> _logger;

        public MappingErrorHandler(ILogger<MappingErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 处理映射异常
        /// </summary>
        public T HandleMappingException<T>(Exception ex, string operationName, object? sourceData = null)
        {
            _logger.LogError(ex, "映射操作 {OperationName} 发生异常", operationName);

            // 记录源数据信息（如果提供）
            if (sourceData != null)
            {
                _logger.LogDebug("源数据类型: {SourceType}", sourceData.GetType().Name);
            }

            // 根据异常类型进行不同处理
            return ex switch
            {
                AutoMapperMappingException mappingEx => HandleAutoMapperException<T>(mappingEx),
                ArgumentNullException argEx => HandleArgumentNullException<T>(argEx),
                InvalidOperationException invalidEx => HandleInvalidOperationException<T>(invalidEx),
                _ => HandleGenericException<T>(ex)
            };
        }

        /// <summary>
        /// 处理AutoMapper异常
        /// </summary>
        private T HandleAutoMapperException<T>(AutoMapperMappingException ex)
        {
            _logger.LogError("AutoMapper映射异常: {Message}, 类型: {Types}",
                ex.Message, ex.Types != null ? $"{ex.Types.Value.SourceType?.Name ?? "Unknown"} -> {ex.Types.Value.DestinationType?.Name ?? "Unknown"}" : "Unknown");

            throw new MappingException($"映射失败: {ex.Message}", ex);
        }

        /// <summary>
        /// 处理参数空异常
        /// </summary>
        private T HandleArgumentNullException<T>(ArgumentNullException ex)
        {
            _logger.LogError("映射参数为空: {ParameterName}", ex.ParamName);
            throw new MappingException($"映射参数不能为空: {ex.ParamName}", ex);
        }

        /// <summary>
        /// 处理无效操作异常
        /// </summary>
        private T HandleInvalidOperationException<T>(InvalidOperationException ex)
        {
            _logger.LogError("映射操作无效: {Message}", ex.Message);
            throw new MappingException($"映射操作无效: {ex.Message}", ex);
        }

        /// <summary>
        /// 处理通用异常
        /// </summary>
        private T HandleGenericException<T>(Exception ex)
        {
            _logger.LogError("映射过程中发生未知异常: {Message}", ex.Message);
            throw new MappingException($"映射失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 自定义映射异常
    /// </summary>
    public class MappingException : Exception
    {
        public MappingException(string message) : base(message) { }
        public MappingException(string message, Exception innerException) : base(message, innerException) { }
    }
}