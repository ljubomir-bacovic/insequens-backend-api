using Microsoft.Extensions.Options;
using AspNetCoreCorsOptions = Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions;

namespace Insequens.Api.Configuration;

public sealed class ConfigureCorsPolicy(
    IOptions<CorsOptions> corsOptions,
    IHostEnvironment environment,
    ILogger<ConfigureCorsPolicy> logger) : IConfigureOptions<AspNetCoreCorsOptions>
{
    public const string PolicyName = "InsequensPolicy";

    public void Configure(AspNetCoreCorsOptions options)
    {
        var allowedOrigins = corsOptions.Value.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        options.AddPolicy(PolicyName, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
            else if (environment.IsDevelopment())
            {
                logger.LogWarning("Cors:AllowedOrigins is empty in Development. Falling back to open CORS for local testing only.");
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
        });
    }
}
