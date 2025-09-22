using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MapleBlog.Domain.Entities;
using MapleBlog.Domain.Enums;

namespace MapleBlog.Application.Services.Permissions;

/// <summary>
/// 权限规则引擎
/// 用于评估和执行数据权限规则
/// </summary>
public class PermissionRuleEngine
{
    private readonly ILogger<PermissionRuleEngine> _logger;
    private readonly ConditionExpressionParser _expressionParser;

    public PermissionRuleEngine(
        ILogger<PermissionRuleEngine> logger,
        ConditionExpressionParser expressionParser)
    {
        _logger = logger;
        _expressionParser = expressionParser;
    }

    /// <summary>
    /// 评估权限规则
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="rules">权限规则列表</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <param name="operation">操作类型</param>
    /// <returns>权限评估结果</returns>
    public async Task<PermissionEvaluationResult> EvaluateRulesAsync<T>(
        IEnumerable<DataPermissionRule> rules,
        T entity,
        Guid userId,
        DataOperation operation) where T : class
    {
        try
        {
            var effectiveRules = rules
                .Where(r => r.IsEffective() && r.Operation == operation)
                .OrderByDescending(r => r.Priority)
                .ToList();

            if (!effectiveRules.Any())
            {
                return new PermissionEvaluationResult
                {
                    IsAllowed = false,
                    Reason = "No applicable rules found",
                    AppliedRules = new List<DataPermissionRule>()
                };
            }

            var evaluationResults = new List<RuleEvaluationResult>();

            foreach (var rule in effectiveRules)
            {
                var result = await EvaluateRuleAsync(rule, entity, userId);
                evaluationResults.Add(result);

                // 如果找到明确允许的规则，立即返回
                if (result.IsMatch && rule.IsAllowed)
                {
                    return new PermissionEvaluationResult
                    {
                        IsAllowed = true,
                        Reason = $"Allowed by rule: {rule.Id}",
                        AppliedRules = new List<DataPermissionRule> { rule },
                        RuleEvaluations = evaluationResults
                    };
                }

                // 如果找到明确拒绝的规则，记录但继续检查（可能有更高优先级的允许规则）
                if (result.IsMatch && !rule.IsAllowed)
                {
                    return new PermissionEvaluationResult
                    {
                        IsAllowed = false,
                        Reason = $"Denied by rule: {rule.Id}",
                        AppliedRules = new List<DataPermissionRule> { rule },
                        RuleEvaluations = evaluationResults
                    };
                }
            }

            // 默认拒绝
            return new PermissionEvaluationResult
            {
                IsAllowed = false,
                Reason = "No matching rules, default deny",
                AppliedRules = new List<DataPermissionRule>(),
                RuleEvaluations = evaluationResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating permission rules for user {UserId}, operation {Operation}",
                userId, operation);

            return new PermissionEvaluationResult
            {
                IsAllowed = false,
                Reason = $"Evaluation error: {ex.Message}",
                AppliedRules = new List<DataPermissionRule>(),
                Error = ex
            };
        }
    }

    /// <summary>
    /// 评估单个权限规则
    /// </summary>
    /// <param name="rule">权限规则</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>规则评估结果</returns>
    private async Task<RuleEvaluationResult> EvaluateRuleAsync<T>(
        DataPermissionRule rule,
        T entity,
        Guid userId) where T : class
    {
        try
        {
            // 检查用户匹配
            if (rule.UserId != userId)
            {
                return new RuleEvaluationResult
                {
                    Rule = rule,
                    IsMatch = false,
                    Reason = "User ID mismatch"
                };
            }

            // 检查范围匹配
            var scopeMatch = await EvaluateScopeAsync(rule.Scope, entity, userId);
            if (!scopeMatch.IsMatch)
            {
                return new RuleEvaluationResult
                {
                    Rule = rule,
                    IsMatch = false,
                    Reason = $"Scope mismatch: {scopeMatch.Reason}"
                };
            }

            // 检查条件匹配
            if (!string.IsNullOrEmpty(rule.Conditions))
            {
                var conditionMatch = await EvaluateConditionsAsync(rule.Conditions, entity, userId);
                if (!conditionMatch.IsMatch)
                {
                    return new RuleEvaluationResult
                    {
                        Rule = rule,
                        IsMatch = false,
                        Reason = $"Condition mismatch: {conditionMatch.Reason}"
                    };
                }
            }

            return new RuleEvaluationResult
            {
                Rule = rule,
                IsMatch = true,
                Reason = "All criteria matched"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule {RuleId} for user {UserId}", rule.Id, userId);

            return new RuleEvaluationResult
            {
                Rule = rule,
                IsMatch = false,
                Reason = $"Evaluation error: {ex.Message}",
                Error = ex
            };
        }
    }

    /// <summary>
    /// 评估权限范围
    /// </summary>
    /// <param name="scope">权限范围</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>范围评估结果</returns>
    private async Task<ScopeEvaluationResult> EvaluateScopeAsync<T>(
        DataPermissionScope scope,
        T entity,
        Guid userId) where T : class
    {
        switch (scope)
        {
            case DataPermissionScope.Global:
                return new ScopeEvaluationResult { IsMatch = true, Reason = "Global scope" };

            case DataPermissionScope.Own:
                return await EvaluateOwnScopeAsync(entity, userId);

            case DataPermissionScope.Department:
                return await EvaluateDepartmentScopeAsync(entity, userId);

            case DataPermissionScope.Organization:
                return await EvaluateOrganizationScopeAsync(entity, userId);

            case DataPermissionScope.None:
                return new ScopeEvaluationResult { IsMatch = false, Reason = "No scope" };

            default:
                return new ScopeEvaluationResult { IsMatch = false, Reason = "Unknown scope" };
        }
    }

    /// <summary>
    /// 评估"自己"范围
    /// </summary>
    private async Task<ScopeEvaluationResult> EvaluateOwnScopeAsync<T>(T entity, Guid userId)
    {
        if (entity is BaseEntity baseEntity)
        {
            var isOwner = baseEntity.CreatedBy == userId;
            return new ScopeEvaluationResult
            {
                IsMatch = isOwner,
                Reason = isOwner ? "User is owner" : "User is not owner"
            };
        }

        // 对于User实体的特殊处理
        if (entity is User user)
        {
            var isSelf = user.Id == userId;
            return new ScopeEvaluationResult
            {
                IsMatch = isSelf,
                Reason = isSelf ? "User is self" : "User is not self"
            };
        }

        return new ScopeEvaluationResult { IsMatch = false, Reason = "Cannot determine ownership" };
    }

    /// <summary>
    /// 评估部门范围（暂时简化实现）
    /// </summary>
    private async Task<ScopeEvaluationResult> EvaluateDepartmentScopeAsync<T>(T entity, Guid userId)
    {
        // TODO: 实现部门层级权限检查
        // 这里需要用户部门信息和组织架构
        return new ScopeEvaluationResult { IsMatch = true, Reason = "Department scope (simplified)" };
    }

    /// <summary>
    /// 评估组织范围（暂时简化实现）
    /// </summary>
    private async Task<ScopeEvaluationResult> EvaluateOrganizationScopeAsync<T>(T entity, Guid userId)
    {
        // TODO: 实现组织层级权限检查
        return new ScopeEvaluationResult { IsMatch = true, Reason = "Organization scope (simplified)" };
    }

    /// <summary>
    /// 评估条件表达式
    /// </summary>
    /// <param name="conditions">条件JSON</param>
    /// <param name="entity">实体对象</param>
    /// <param name="userId">用户ID</param>
    /// <returns>条件评估结果</returns>
    private async Task<ConditionEvaluationResult> EvaluateConditionsAsync<T>(
        string conditions,
        T entity,
        Guid userId)
    {
        try
        {
            var conditionDict = JsonSerializer.Deserialize<Dictionary<string, object>>(conditions);
            if (conditionDict == null)
            {
                return new ConditionEvaluationResult { IsMatch = true, Reason = "No conditions to evaluate" };
            }

            return await _expressionParser.EvaluateConditionsAsync(conditionDict, entity, userId);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid condition JSON: {Conditions}", conditions);
            return new ConditionEvaluationResult { IsMatch = false, Reason = "Invalid condition format" };
        }
    }

    /// <summary>
    /// 生成查询过滤表达式
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="rules">权限规则列表</param>
    /// <param name="userId">用户ID</param>
    /// <param name="operation">操作类型</param>
    /// <returns>过滤表达式</returns>
    public Expression<Func<T, bool>> GenerateQueryFilter<T>(
        IEnumerable<DataPermissionRule> rules,
        Guid userId,
        DataOperation operation) where T : BaseEntity
    {
        var effectiveRules = rules
            .Where(r => r.IsEffective() && r.Operation == operation && r.IsAllowed)
            .OrderByDescending(r => r.Priority)
            .ToList();

        if (!effectiveRules.Any())
        {
            // 默认拒绝所有
            return x => false;
        }

        // 构建或条件组合
        Expression<Func<T, bool>>? combinedExpression = null;

        foreach (var rule in effectiveRules)
        {
            var ruleExpression = GenerateRuleExpression<T>(rule, userId);

            if (combinedExpression == null)
            {
                combinedExpression = ruleExpression;
            }
            else
            {
                // 使用OR组合多个规则
                combinedExpression = CombineExpressions(combinedExpression, ruleExpression, ExpressionType.OrElse);
            }
        }

        return combinedExpression ?? (x => false);
    }

    /// <summary>
    /// 为单个规则生成表达式
    /// </summary>
    private Expression<Func<T, bool>> GenerateRuleExpression<T>(DataPermissionRule rule, Guid userId) where T : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? condition = null;

        // 根据范围生成条件
        switch (rule.Scope)
        {
            case DataPermissionScope.Own:
                var createdByProperty = Expression.Property(parameter, nameof(BaseEntity.CreatedBy));
                var userIdConstant = Expression.Constant(userId);
                condition = Expression.Equal(createdByProperty, userIdConstant);
                break;

            case DataPermissionScope.Global:
                condition = Expression.Constant(true);
                break;

            case DataPermissionScope.None:
                condition = Expression.Constant(false);
                break;

            default:
                // 对于其他范围，暂时允许所有（需要进一步实现）
                condition = Expression.Constant(true);
                break;
        }

        // TODO: 添加条件表达式的处理
        if (!string.IsNullOrEmpty(rule.Conditions))
        {
            // 这里可以解析JSON条件并生成相应的Expression
            // 暂时简化处理
        }

        return condition != null
            ? Expression.Lambda<Func<T, bool>>(condition, parameter)
            : x => false;
    }

    /// <summary>
    /// 组合两个表达式
    /// </summary>
    private Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2,
        ExpressionType type)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var body1 = ReplaceParameter(expr1.Body, expr1.Parameters[0], parameter);
        var body2 = ReplaceParameter(expr2.Body, expr2.Parameters[0], parameter);

        var combined = type == ExpressionType.AndAlso
            ? Expression.AndAlso(body1, body2)
            : Expression.OrElse(body1, body2);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// 替换表达式参数
    /// </summary>
    private Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    /// <summary>
    /// 参数替换访问器
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}

/// <summary>
/// 权限评估结果
/// </summary>
public class PermissionEvaluationResult
{
    /// <summary>
    /// 是否允许访问
    /// </summary>
    public bool IsAllowed { get; set; }

    /// <summary>
    /// 评估原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 应用的权限规则
    /// </summary>
    public ICollection<DataPermissionRule> AppliedRules { get; set; } = new List<DataPermissionRule>();

    /// <summary>
    /// 规则评估详情
    /// </summary>
    public ICollection<RuleEvaluationResult> RuleEvaluations { get; set; } = new List<RuleEvaluationResult>();

    /// <summary>
    /// 评估错误（如果有）
    /// </summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// 规则评估结果
/// </summary>
public class RuleEvaluationResult
{
    /// <summary>
    /// 评估的规则
    /// </summary>
    public DataPermissionRule Rule { get; set; } = null!;

    /// <summary>
    /// 是否匹配
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// 评估原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 评估错误（如果有）
    /// </summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// 范围评估结果
/// </summary>
public class ScopeEvaluationResult
{
    /// <summary>
    /// 是否匹配
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// 评估原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 条件评估结果
/// </summary>
public class ConditionEvaluationResult
{
    /// <summary>
    /// 是否匹配
    /// </summary>
    public bool IsMatch { get; set; }

    /// <summary>
    /// 评估原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}