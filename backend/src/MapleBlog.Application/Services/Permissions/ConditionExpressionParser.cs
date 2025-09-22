using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MapleBlog.Application.Services.Permissions;

/// <summary>
/// 条件表达式解析器
/// 用于解析和评估JSON格式的权限条件表达式
/// </summary>
public class ConditionExpressionParser
{
    private readonly ILogger<ConditionExpressionParser> _logger;

    public ConditionExpressionParser(ILogger<ConditionExpressionParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 评估条件表达式
    /// </summary>
    /// <param name="conditions">条件字典</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>条件评估结果</returns>
    public async Task<ConditionEvaluationResult> EvaluateConditionsAsync<T>(
        Dictionary<string, object> conditions,
        T entity,
        Guid userId)
    {
        try
        {
            foreach (var condition in conditions)
            {
                var result = await EvaluateConditionAsync(condition.Key, condition.Value, entity, userId);
                if (!result.IsMatch)
                {
                    return result;
                }
            }

            return new ConditionEvaluationResult
            {
                IsMatch = true,
                Reason = "All conditions matched"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating conditions for entity type {EntityType}", typeof(T).Name);
            return new ConditionEvaluationResult
            {
                IsMatch = false,
                Reason = $"Evaluation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 评估单个条件
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <param name="expectedValue">期望值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>条件评估结果</returns>
    private async Task<ConditionEvaluationResult> EvaluateConditionAsync<T>(
        string propertyName,
        object expectedValue,
        T entity,
        Guid userId)
    {
        try
        {
            // 处理特殊占位符
            var processedExpectedValue = ProcessPlaceholders(expectedValue, userId);

            // 处理复杂条件对象
            if (expectedValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                return await EvaluateComplexConditionAsync(propertyName, jsonElement, entity, userId);
            }

            // 获取实体属性值
            var actualValue = GetPropertyValue(entity, propertyName);
            if (actualValue == null && processedExpectedValue != null)
            {
                return new ConditionEvaluationResult
                {
                    IsMatch = false,
                    Reason = $"Property {propertyName} is null but expected {processedExpectedValue}"
                };
            }

            // 比较值
            var isMatch = CompareValues(actualValue, processedExpectedValue);

            return new ConditionEvaluationResult
            {
                IsMatch = isMatch,
                Reason = isMatch
                    ? $"Property {propertyName} matches expected value"
                    : $"Property {propertyName} ({actualValue}) does not match expected value ({processedExpectedValue})"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating condition {PropertyName} for entity type {EntityType}",
                propertyName, typeof(T).Name);

            return new ConditionEvaluationResult
            {
                IsMatch = false,
                Reason = $"Condition evaluation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 评估复杂条件（支持操作符）
    /// </summary>
    private async Task<ConditionEvaluationResult> EvaluateComplexConditionAsync<T>(
        string propertyName,
        JsonElement conditionObject,
        T entity,
        Guid userId)
    {
        var actualValue = GetPropertyValue(entity, propertyName);

        foreach (var property in conditionObject.EnumerateObject())
        {
            var operatorName = property.Name.ToLowerInvariant();
            var operatorValue = ProcessPlaceholders(property.Value, userId);

            var result = EvaluateOperator(operatorName, actualValue, operatorValue);
            if (!result.IsMatch)
            {
                return new ConditionEvaluationResult
                {
                    IsMatch = false,
                    Reason = $"Property {propertyName} failed {operatorName} check: {result.Reason}"
                };
            }
        }

        return new ConditionEvaluationResult
        {
            IsMatch = true,
            Reason = $"Property {propertyName} passed all operator checks"
        };
    }

    /// <summary>
    /// 评估操作符
    /// </summary>
    private ConditionEvaluationResult EvaluateOperator(string operatorName, object? actualValue, object? expectedValue)
    {
        return operatorName switch
        {
            "eq" or "equals" => new ConditionEvaluationResult
            {
                IsMatch = CompareValues(actualValue, expectedValue),
                Reason = $"Equality check: {actualValue} == {expectedValue}"
            },

            "ne" or "notequals" => new ConditionEvaluationResult
            {
                IsMatch = !CompareValues(actualValue, expectedValue),
                Reason = $"Inequality check: {actualValue} != {expectedValue}"
            },

            "gt" or "greaterthan" => new ConditionEvaluationResult
            {
                IsMatch = CompareNumeric(actualValue, expectedValue, (a, b) => a > b),
                Reason = $"Greater than check: {actualValue} > {expectedValue}"
            },

            "gte" or "greaterthanorequal" => new ConditionEvaluationResult
            {
                IsMatch = CompareNumeric(actualValue, expectedValue, (a, b) => a >= b),
                Reason = $"Greater than or equal check: {actualValue} >= {expectedValue}"
            },

            "lt" or "lessthan" => new ConditionEvaluationResult
            {
                IsMatch = CompareNumeric(actualValue, expectedValue, (a, b) => a < b),
                Reason = $"Less than check: {actualValue} < {expectedValue}"
            },

            "lte" or "lessthanorequal" => new ConditionEvaluationResult
            {
                IsMatch = CompareNumeric(actualValue, expectedValue, (a, b) => a <= b),
                Reason = $"Less than or equal check: {actualValue} <= {expectedValue}"
            },

            "in" or "contains" => new ConditionEvaluationResult
            {
                IsMatch = EvaluateInOperator(actualValue, expectedValue),
                Reason = $"In/Contains check: {actualValue} in {expectedValue}"
            },

            "notin" or "notcontains" => new ConditionEvaluationResult
            {
                IsMatch = !EvaluateInOperator(actualValue, expectedValue),
                Reason = $"Not in/Not contains check: {actualValue} not in {expectedValue}"
            },

            "startswith" => new ConditionEvaluationResult
            {
                IsMatch = EvaluateStringOperator(actualValue, expectedValue, (a, b) => a.StartsWith(b, StringComparison.OrdinalIgnoreCase)),
                Reason = $"Starts with check: {actualValue} starts with {expectedValue}"
            },

            "endswith" => new ConditionEvaluationResult
            {
                IsMatch = EvaluateStringOperator(actualValue, expectedValue, (a, b) => a.EndsWith(b, StringComparison.OrdinalIgnoreCase)),
                Reason = $"Ends with check: {actualValue} ends with {expectedValue}"
            },

            "regex" => new ConditionEvaluationResult
            {
                IsMatch = EvaluateRegexOperator(actualValue, expectedValue),
                Reason = $"Regex check: {actualValue} matches pattern {expectedValue}"
            },

            "isnull" => new ConditionEvaluationResult
            {
                IsMatch = actualValue == null,
                Reason = $"Is null check: {actualValue} is null"
            },

            "isnotnull" => new ConditionEvaluationResult
            {
                IsMatch = actualValue != null,
                Reason = $"Is not null check: {actualValue} is not null"
            },

            _ => new ConditionEvaluationResult
            {
                IsMatch = false,
                Reason = $"Unknown operator: {operatorName}"
            }
        };
    }

    /// <summary>
    /// 处理占位符
    /// </summary>
    private object? ProcessPlaceholders(object? value, Guid userId)
    {
        if (value is string stringValue)
        {
            return stringValue.Replace("{UserId}", userId.ToString(), StringComparison.OrdinalIgnoreCase)
                             .Replace("{CurrentDate}", DateTime.UtcNow.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
                             .Replace("{CurrentDateTime}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), StringComparison.OrdinalIgnoreCase);
        }

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => ProcessPlaceholders(jsonElement.GetString(), userId),
                JsonValueKind.Number => jsonElement.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value
            };
        }

        return value;
    }

    /// <summary>
    /// 获取属性值
    /// </summary>
    private object? GetPropertyValue<T>(T entity, string propertyName)
    {
        if (entity == null) return null;

        try
        {
            var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
            {
                // 尝试查找嵌套属性（如 User.Role）
                if (propertyName.Contains('.'))
                {
                    return GetNestedPropertyValue(entity, propertyName);
                }

                _logger.LogWarning("Property {PropertyName} not found in type {TypeName}", propertyName, typeof(T).Name);
                return null;
            }

            return property.GetValue(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property value {PropertyName} from type {TypeName}", propertyName, typeof(T).Name);
            return null;
        }
    }

    /// <summary>
    /// 获取嵌套属性值
    /// </summary>
    private object? GetNestedPropertyValue<T>(T entity, string propertyPath)
    {
        var parts = propertyPath.Split('.');
        object? current = entity;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null) return null;

            current = property.GetValue(current);
        }

        return current;
    }

    /// <summary>
    /// 比较值
    /// </summary>
    private bool CompareValues(object? actual, object? expected)
    {
        if (actual == null && expected == null) return true;
        if (actual == null || expected == null) return false;

        // 尝试转换类型后比较
        try
        {
            if (actual.GetType() != expected.GetType())
            {
                expected = Convert.ChangeType(expected, actual.GetType());
            }

            return actual.Equals(expected);
        }
        catch
        {
            return actual.ToString()?.Equals(expected.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
        }
    }

    /// <summary>
    /// 数值比较
    /// </summary>
    private bool CompareNumeric(object? actual, object? expected, Func<decimal, decimal, bool> comparer)
    {
        try
        {
            var actualDecimal = Convert.ToDecimal(actual);
            var expectedDecimal = Convert.ToDecimal(expected);
            return comparer(actualDecimal, expectedDecimal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 字符串操作
    /// </summary>
    private bool EvaluateStringOperator(object? actual, object? expected, Func<string, string, bool> operation)
    {
        var actualString = actual?.ToString();
        var expectedString = expected?.ToString();

        if (actualString == null || expectedString == null) return false;

        return operation(actualString, expectedString);
    }

    /// <summary>
    /// In操作符评估
    /// </summary>
    private bool EvaluateInOperator(object? actual, object? expected)
    {
        if (actual == null || expected == null) return false;

        // 如果expected是数组
        if (expected is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            return jsonElement.EnumerateArray()
                .Any(item => CompareValues(actual, ProcessPlaceholders(item, Guid.Empty)));
        }

        // 如果expected是字符串（逗号分隔）
        if (expected is string expectedString)
        {
            var values = expectedString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim());

            return values.Any(v => CompareValues(actual, v));
        }

        return false;
    }

    /// <summary>
    /// 正则表达式操作符评估
    /// </summary>
    private bool EvaluateRegexOperator(object? actual, object? expected)
    {
        try
        {
            var actualString = actual?.ToString();
            var patternString = expected?.ToString();

            if (actualString == null || patternString == null) return false;

            return System.Text.RegularExpressions.Regex.IsMatch(actualString, patternString,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}