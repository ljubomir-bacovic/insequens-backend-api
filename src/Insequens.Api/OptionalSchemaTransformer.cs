using Insequens.Contracts.V2;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Insequens.Api;

/// <summary>
/// Describes <see cref="Optional{T}"/> as it travels: the value or null, not an object. A request with an
/// optional field is a partial update, so none of its properties is required.
/// </summary>
public sealed class OptionalSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (IsOptional(type))
        {
            var valueType = type.GetGenericArguments()[0];
            valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            schema.Properties?.Clear();
            schema.Required?.Clear();
            schema.Type = JsonSchemaType.String | JsonSchemaType.Null;
            schema.Format = valueType == typeof(DateOnly) ? "date" : null;
        }
        else if (context.JsonTypeInfo.Properties.Any(property => IsOptional(property.PropertyType)))
        {
            schema.Required?.Clear();
        }

        return Task.CompletedTask;
    }

    private static bool IsOptional(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>);
}
