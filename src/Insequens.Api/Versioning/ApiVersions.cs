namespace Insequens.Api.Versioning;

/// <summary>
/// The API versions, for <c>[ApiVersion]</c>. v1 is frozen except for bugs and security fixes; breaking changes go
/// into v2. The URL segment (<c>/v1/</c>, <c>/v2/</c>) selects the version.
/// </summary>
public static class ApiVersions
{
    public const double V1 = 1.0;
    public const double V2 = 2.0;

    /// <summary>One OpenAPI document per version, served at <c>/openapi/{name}.json</c>.</summary>
    public static readonly string[] DocumentNames = ["v1", "v2"];
}
