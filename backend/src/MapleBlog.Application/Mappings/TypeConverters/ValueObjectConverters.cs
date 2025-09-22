using AutoMapper;
using MapleBlog.Domain.ValueObjects;
using MapleBlog.Domain.Enums;
using System.Text.Json;

namespace MapleBlog.Application.Mappings.TypeConverters
{
    /// <summary>
    /// Email值对象转换器
    /// </summary>
    public class EmailToStringConverter : ITypeConverter<Email, string>
    {
        public string Convert(Email source, string destination, ResolutionContext context)
        {
            return source?.Value ?? string.Empty;
        }
    }

    /// <summary>
    /// 字符串到Email转换器
    /// </summary>
    public class StringToEmailConverter : ITypeConverter<string, Email>
    {
        public Email Convert(string source, Email destination, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source) ? Email.Create("user@example.com") : Email.Create(source);
        }
    }

    /// <summary>
    /// CommentContent值对象转换器
    /// </summary>
    public class CommentContentToStringConverter : ITypeConverter<CommentContent, string>
    {
        public string Convert(CommentContent source, string destination, ResolutionContext context)
        {
            return source?.RawContent ?? string.Empty;
        }
    }

    /// <summary>
    /// 字符串到CommentContent转换器
    /// </summary>
    public class StringToCommentContentConverter : ITypeConverter<string, CommentContent>
    {
        public CommentContent Convert(string source, CommentContent destination, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source) ? CommentContent.Create(string.Empty) : CommentContent.Create(source);
        }
    }

    /// <summary>
    /// Content值对象转换器
    /// </summary>
    public class ContentToStringConverter : ITypeConverter<Content, string>
    {
        public string Convert(Content source, string destination, ResolutionContext context)
        {
            return source?.RawContent ?? string.Empty;
        }
    }

    /// <summary>
    /// ThreadPath值对象转换器
    /// </summary>
    public class ThreadPathToStringConverter : ITypeConverter<ThreadPath, string>
    {
        public string Convert(ThreadPath source, string destination, ResolutionContext context)
        {
            return source?.Path ?? string.Empty;
        }
    }

    /// <summary>
    /// Slug值对象转换器
    /// </summary>
    public class SlugToStringConverter : ITypeConverter<Slug, string>
    {
        public string Convert(Slug source, string destination, ResolutionContext context)
        {
            return source?.Value ?? string.Empty;
        }
    }

    /// <summary>
    /// 字符串到Slug转换器
    /// </summary>
    public class StringToSlugConverter : ITypeConverter<string, Slug>
    {
        public Slug Convert(string source, Slug destination, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source) ? Slug.Create(source ?? string.Empty) : Slug.Create(source);
        }
    }

    /// <summary>
    /// 枚举转换器基类
    /// </summary>
    public abstract class EnumConverterBase<TEnum> where TEnum : struct, Enum
    {
        protected string ConvertEnumToString(TEnum source)
        {
            return source.ToString();
        }

        protected TEnum ConvertStringToEnum(string source)
        {
            if (string.IsNullOrEmpty(source))
                return default(TEnum);

            if (Enum.TryParse<TEnum>(source, true, out var result))
                return result;

            return default(TEnum);
        }
    }

    /// <summary>
    /// UserRole枚举转换器
    /// </summary>
    public class UserRoleToStringConverter : EnumConverterBase<UserRole>, ITypeConverter<UserRole, string>
    {
        public string Convert(UserRole source, string destination, ResolutionContext context)
        {
            return ConvertEnumToString(source);
        }
    }

    /// <summary>
    /// 字符串到UserRole转换器
    /// </summary>
    public class StringToUserRoleConverter : EnumConverterBase<UserRole>, ITypeConverter<string, UserRole>
    {
        public UserRole Convert(string source, UserRole destination, ResolutionContext context)
        {
            return ConvertStringToEnum(source);
        }
    }

    /// <summary>
    /// PostStatus枚举转换器
    /// </summary>
    public class PostStatusToStringConverter : EnumConverterBase<PostStatus>, ITypeConverter<PostStatus, string>
    {
        public string Convert(PostStatus source, string destination, ResolutionContext context)
        {
            return ConvertEnumToString(source);
        }
    }

    /// <summary>
    /// CommentStatus枚举转换器
    /// </summary>
    public class CommentStatusToStringConverter : EnumConverterBase<CommentStatus>, ITypeConverter<CommentStatus, string>
    {
        public string Convert(CommentStatus source, string destination, ResolutionContext context)
        {
            return ConvertEnumToString(source);
        }
    }

    /// <summary>
    /// 日期时间转换器 - 处理时区转换
    /// </summary>
    public class DateTimeToUtcConverter : ITypeConverter<DateTime, DateTime>
    {
        public DateTime Convert(DateTime source, DateTime destination, ResolutionContext context)
        {
            return source.Kind == DateTimeKind.Unspecified ?
                DateTime.SpecifyKind(source, DateTimeKind.Utc) :
                source.ToUniversalTime();
        }
    }

    /// <summary>
    /// 日期时间格式化转换器
    /// </summary>
    public class DateTimeToFormattedStringConverter : ITypeConverter<DateTime, string>
    {
        public string Convert(DateTime source, string destination, ResolutionContext context)
        {
            // 可以根据用户偏好或上下文信息决定格式
            return source.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    /// <summary>
    /// 可空日期时间格式化转换器
    /// </summary>
    public class NullableDateTimeToFormattedStringConverter : ITypeConverter<DateTime?, string>
    {
        public string Convert(DateTime? source, string destination, ResolutionContext context)
        {
            return source?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        }
    }

    /// <summary>
    /// JSON对象转换器
    /// </summary>
    public class ObjectToJsonStringConverter : ITypeConverter<object, string>
    {
        public string Convert(object source, string destination, ResolutionContext context)
        {
            if (source == null) return string.Empty;

            try
            {
                return JsonSerializer.Serialize(source, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch
            {
                return source.ToString() ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// JSON字符串转对象转换器
    /// </summary>
    public class JsonStringToObjectConverter<T> : ITypeConverter<string, T>
    {
        public T Convert(string source, T destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
                return default(T);

            try
            {
                return JsonSerializer.Deserialize<T>(source, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return default(T);
            }
        }
    }

    /// <summary>
    /// 文件路径转URL转换器
    /// </summary>
    public class FilePathToUrlConverter : ITypeConverter<string, string>
    {
        private readonly string _baseUrl;

        public FilePathToUrlConverter(string baseUrl = "")
        {
            _baseUrl = baseUrl;
        }

        public string Convert(string source, string destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            // 如果已经是完整URL，直接返回
            if (Uri.IsWellFormedUriString(source, UriKind.Absolute))
                return source;

            // 如果是相对路径，转换为完整URL
            if (!string.IsNullOrEmpty(_baseUrl))
            {
                return new Uri(new Uri(_baseUrl), source).ToString();
            }

            return source;
        }
    }

    /// <summary>
    /// 集合转换器 - 处理空集合
    /// </summary>
    public class SafeCollectionConverter<TSource, TDestination> : ITypeConverter<IEnumerable<TSource>, IEnumerable<TDestination>>
    {
        public IEnumerable<TDestination> Convert(IEnumerable<TSource> source, IEnumerable<TDestination> destination, ResolutionContext context)
        {
            if (source == null)
                return new List<TDestination>();

            var mapper = context.Mapper;
            return source.Select(item => mapper.Map<TDestination>(item)).ToList();
        }
    }

    /// <summary>
    /// 字符串数组转换器 - 处理逗号分隔的字符串
    /// </summary>
    public class CommaSeparatedStringToArrayConverter : ITypeConverter<string, string[]>
    {
        public string[] Convert(string source, string[] destination, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
                return Array.Empty<string>();

            return source.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();
        }
    }

    /// <summary>
    /// 字符串数组转逗号分隔字符串转换器
    /// </summary>
    public class ArrayToCommaSeparatedStringConverter : ITypeConverter<string[], string>
    {
        public string Convert(string[] source, string destination, ResolutionContext context)
        {
            if (source == null || source.Length == 0)
                return string.Empty;

            return string.Join(", ", source.Where(s => !string.IsNullOrEmpty(s)));
        }
    }

    /// <summary>
    /// 数字格式化转换器
    /// </summary>
    public class NumberToFormattedStringConverter : ITypeConverter<long, string>
    {
        public string Convert(long source, string destination, ResolutionContext context)
        {
            // 格式化大数字，如：1,234,567
            return source.ToString("N0");
        }
    }

    /// <summary>
    /// 百分比转换器
    /// </summary>
    public class DoubleToPercentageConverter : ITypeConverter<double, string>
    {
        public string Convert(double source, string destination, ResolutionContext context)
        {
            return $"{source:P2}"; // 转换为百分比格式，保留2位小数
        }
    }

    /// <summary>
    /// 时间跨度转换器
    /// </summary>
    public class TimeSpanToHumanReadableConverter : ITypeConverter<TimeSpan, string>
    {
        public string Convert(TimeSpan source, string destination, ResolutionContext context)
        {
            if (source.TotalMinutes < 1)
                return "不到1分钟";

            if (source.TotalHours < 1)
                return $"{(int)source.TotalMinutes}分钟";

            if (source.TotalDays < 1)
                return $"{(int)source.TotalHours}小时{source.Minutes}分钟";

            return $"{(int)source.TotalDays}天{source.Hours}小时";
        }
    }

    /// <summary>
    /// 可空时间跨度转换器
    /// </summary>
    public class NullableTimeSpanToHumanReadableConverter : ITypeConverter<TimeSpan?, string>
    {
        private readonly TimeSpanToHumanReadableConverter _converter = new();

        public string Convert(TimeSpan? source, string destination, ResolutionContext context)
        {
            return source.HasValue ? _converter.Convert(source.Value, destination, context) : "未知";
        }
    }

    /// <summary>
    /// 风险级别转换器
    /// </summary>
    public class RiskScoreToLevelConverter : ITypeConverter<double, string>
    {
        public string Convert(double source, string destination, ResolutionContext context)
        {
            return source switch
            {
                >= 0.8 => "高风险",
                >= 0.6 => "中风险",
                >= 0.3 => "低风险",
                _ => "安全"
            };
        }
    }

    /// <summary>
    /// 活跃度评分转换器
    /// </summary>
    public class ActivityScoreToLevelConverter : ITypeConverter<double, string>
    {
        public string Convert(double source, string destination, ResolutionContext context)
        {
            return source switch
            {
                >= 0.8 => "非常活跃",
                >= 0.6 => "活跃",
                >= 0.4 => "一般",
                >= 0.2 => "不活跃",
                _ => "非常不活跃"
            };
        }
    }
}