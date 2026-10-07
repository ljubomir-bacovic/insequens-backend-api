using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Insequens.Api;

/// <summary>
/// Declares the JWT bearer scheme and requires it on every operation except those marked
/// <c>[AllowAnonymous]</c>, which the fallback authorization policy leaves open.
/// </summary>
public class JwtBearerSecurityDocumentTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    private const string SchemeKey = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeKey] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.",
        };

        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeKey, context.Document)] = [],
        });

        return Task.CompletedTask;
    }
}
