using Microsoft.Extensions.Options;

namespace Insequens.Api.Configuration;

public sealed class CorsOptionsValidator(IHostEnvironment environment) : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        var origins = options.AllowedOrigins.Where(origin => !string.IsNullOrWhiteSpace(origin)).ToArray();

        if (origins.Length == 0 && !environment.IsDevelopment())
        {
            return ValidateOptionsResult.Fail("Cors:AllowedOrigins must be configured outside Development.");
        }

        var invalidOrigins = origins
            .Where(origin => !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            .ToArray();

        return invalidOrigins.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Cors:AllowedOrigins must contain absolute http or https origins. Invalid: {string.Join(", ", invalidOrigins)}.");
    }
}
