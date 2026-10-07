using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Insequens.Api.ErrorHandling;

/// <summary>
/// Describes error responses as they are sent: <c>application/problem+json</c>, with the <c>traceId</c> every
/// problem carries. Clients generated from the documents (INS-076) then read error bodies correctly.
/// </summary>
public sealed class ProblemDetailsOpenApiTransformer : IOpenApiOperationTransformer, IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var (status, response) in operation.Responses ?? [])
        {
            if (status.Length != 3 || status[0] is not ('4' or '5') || response.Content is not { Count: > 0 } content)
            {
                continue;
            }

            var schema = content.Values.First().Schema;
            content.Clear();
            content[ProblemResponses.ContentType] = new OpenApiMediaType { Schema = schema };
        }

        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type.IsAssignableTo(typeof(ProblemDetails)))
        {
            schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
            schema.Properties[ProblemDetailsServiceCollectionExtensions.TraceIdKey] = new OpenApiSchema { Type = JsonSchemaType.String };
        }

        return Task.CompletedTask;
    }
}
