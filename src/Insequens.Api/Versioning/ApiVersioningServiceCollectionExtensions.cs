using Asp.Versioning;

namespace Insequens.Api.Versioning;

public static class ApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// URL-segment versioning. Responses report <c>api-supported-versions</c>, and <c>api-deprecated-versions</c>
    /// once a version is marked deprecated. The API explorer groups endpoints as <c>v1</c>, <c>v2</c>, so each
    /// OpenAPI document holds one version.
    /// </summary>
    public static IServiceCollection AddApiVersions(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(ApiVersions.V1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.ReportApiVersions = true;
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
