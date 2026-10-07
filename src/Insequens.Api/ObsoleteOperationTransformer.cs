using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Insequens.Api;

/// <summary>Marks an <c>[Obsolete]</c> action <c>deprecated</c>, with the obsolete message as the reason.</summary>
public sealed class ObsoleteOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var obsolete = context.Description.ActionDescriptor.EndpointMetadata.OfType<ObsoleteAttribute>().FirstOrDefault();
        if (obsolete is not null)
        {
            operation.Deprecated = true;
            operation.Description = string.Join(" ", new[] { operation.Description, $"Deprecated: {obsolete.Message}" }.Where(text => !string.IsNullOrEmpty(text)));
        }

        return Task.CompletedTask;
    }
}
