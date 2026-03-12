using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Serilog;

namespace Insequens.Api;

/// <summary>
/// Document transformer to add JWT Bearer authentication to OpenAPI specification
/// </summary>
public class JwtBearerSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        try
        {
            const string schemeKey = "Bearer";
            
            // Add security scheme to components
            document.Components ??= new();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[schemeKey] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            };

            // Create a security requirement with OpenApiSecuritySchemeReference
            var securityRequirement = new OpenApiSecurityRequirement();
            
            // Create a reference to the security scheme
            var schemeReference = new OpenApiSecuritySchemeReference(schemeKey, document);
            
            // Add the security requirement using the reference
            securityRequirement.Add(schemeReference, new List<string>());

            // Apply security requirement to all operations
            if (document.Paths != null)
            {
                foreach (var path in document.Paths.Values)
                {
                    if (path?.Operations != null)
                    {
                        foreach (var operation in path.Operations.Values)
                        {
                            if (operation != null)
                            {
                                operation.Security ??= new List<OpenApiSecurityRequirement>();
                                operation.Security.Add(securityRequirement);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error using Serilog
            Log.Error(ex, "Error in JwtBearerSecurityDocumentTransformer while adding Bearer security scheme to OpenAPI document");
            throw;
        }

        return Task.CompletedTask;
    }
}
