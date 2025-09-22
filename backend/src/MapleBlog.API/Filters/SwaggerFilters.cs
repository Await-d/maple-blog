using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using System.Reflection;

namespace MapleBlog.API.Filters
{
    /// <summary>
    /// Swagger默认值操作过滤器
    /// </summary>
    public class SwaggerDefaultValues : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            // Note: IsDeprecated is not available in newer versions
            // operation.Deprecated |= apiDescription.IsDeprecated();

            foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
            {
                var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
                var response = operation.Responses[responseKey];

                foreach (var contentType in response.Content.Keys)
                {
                    if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                    {
                        response.Content.Remove(contentType);
                    }
                }
            }

            if (operation.Parameters == null)
                return;

            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

                parameter.Description ??= description.ModelMetadata?.Description;

                if (parameter.Schema.Default == null && description.DefaultValue != null)
                {
                    // Note: OpenApiAnyFactory.CreateFromJson is not available in newer versions
                    // parameter.Schema.Default = Microsoft.OpenApi.Any.OpenApiAnyFactory.CreateFromJson(System.Text.Json.JsonSerializer.Serialize(description.DefaultValue));
                }

                parameter.Required |= description.IsRequired;
            }
        }
    }

    /// <summary>
    /// Swagger认证操作过滤器
    /// </summary>
    public class SwaggerAuthOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authAttributes = context.MethodInfo
                .GetCustomAttributes<AuthorizeAttribute>(true)
                .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(true) ?? Array.Empty<AuthorizeAttribute>());

            var allowAnonymousAttributes = context.MethodInfo
                .GetCustomAttributes<AllowAnonymousAttribute>(true)
                .Union(context.MethodInfo.DeclaringType?.GetCustomAttributes<AllowAnonymousAttribute>(true) ?? Array.Empty<AllowAnonymousAttribute>());

            if (allowAnonymousAttributes.Any())
            {
                return;
            }

            if (authAttributes.Any())
            {
                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "Unauthorized - JWT token is missing or invalid"
                });
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Forbidden - Insufficient permissions"
                });

                var jwtbearerScheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [ jwtbearerScheme ] = new List<string>()
                    }
                };

                // 添加角色和策略信息到描述中
                var roles = authAttributes.SelectMany(a => a.Roles?.Split(',') ?? Array.Empty<string>()).Distinct();
                var policies = authAttributes.Where(a => !string.IsNullOrEmpty(a.Policy)).Select(a => a.Policy).Distinct();

                var authInfo = new List<string>();
                if (roles.Any())
                {
                    authInfo.Add($"Required roles: {string.Join(", ", roles)}");
                }
                if (policies.Any())
                {
                    authInfo.Add($"Required policies: {string.Join(", ", policies)}");
                }

                if (authInfo.Any())
                {
                    operation.Description = $"{operation.Description}\n\n**Authorization Requirements:**\n- {string.Join("\n- ", authInfo)}";
                }
            }
        }
    }

    /// <summary>
    /// 枚举文档过滤器
    /// </summary>
    public class SwaggerEnumDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 为枚举类型添加描述
            foreach (var schema in swaggerDoc.Components.Schemas.Where(x => x.Value.Enum?.Any() == true))
            {
                var itemType = context.SchemaRepository.Schemas
                    .FirstOrDefault(x => x.Key == schema.Key).Value;

                if (itemType != null)
                {
                    schema.Value.Description += "<br/>Possible values:<br/>";

                    // Note: GeneratorSettings is not available in newer versions - commenting out entire section
                    /*
                    var enumType = context.SchemaGenerator.GeneratorSettings.SchemaFilters
                        .OfType<ISchemaFilter>()
                        .FirstOrDefault()?.GetType().Assembly
                        .GetTypes()
                        .FirstOrDefault(x => x.Name == schema.Key && x.IsEnum);

                    if (enumType != null)
                    {
                        var enumNames = Enum.GetNames(enumType);
                        var enumValues = Enum.GetValues(enumType);

                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            var enumName = enumNames[i];
                            var enumValue = Convert.ToInt32(enumValues.GetValue(i));

                            var description = enumType.GetField(enumName)?
                                .GetCustomAttribute<DescriptionAttribute>()?.Description ?? enumName;

                            schema.Value.Description += $"- `{enumValue}` = {enumName}: {description}<br/>";
                        }
                    }
                    */
                }
            }
        }
    }

    /// <summary>
    /// 排除字段过滤器
    /// </summary>
    public class SwaggerExcludeFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null || context.Type == null)
                return;

            var excludedProperties = context.Type.GetProperties()
                .Where(t => t.GetCustomAttribute<SwaggerExcludeAttribute>() != null);

            foreach (var excludedProperty in excludedProperties)
            {
                var propertyToHide = schema.Properties.Keys
                    .SingleOrDefault(x => x.ToLower() == excludedProperty.Name.ToLower());

                if (propertyToHide != null)
                {
                    schema.Properties.Remove(propertyToHide);
                }
            }
        }
    }

    /// <summary>
    /// 用于标记需要从Swagger文档中排除的属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SwaggerExcludeAttribute : Attribute
    {
    }
}