using Insequens.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Insequens.Api.Security;

public sealed class ConfigureJwtBearerOptions(IJwtKeyRing keyRing) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            Configure(options);
        }
    }

    public void Configure(JwtBearerOptions options)
    {
        options.TokenValidationParameters = keyRing.CreateValidationParameters(validateLifetime: true);
    }
}
