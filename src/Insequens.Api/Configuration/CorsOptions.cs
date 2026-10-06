namespace Insequens.Api.Configuration;

public sealed record CorsOptions
{
    public const string SectionName = "Cors";

    public IList<string> AllowedOrigins { get; init; } = [];
}
