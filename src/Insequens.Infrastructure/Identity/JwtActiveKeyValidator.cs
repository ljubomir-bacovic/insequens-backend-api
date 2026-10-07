using Microsoft.Extensions.Options;

namespace Insequens.Infrastructure.Identity;

/// <summary>Fails startup when every configured signing key has an <c>ActiveFrom</c> in the future.</summary>
public sealed class JwtActiveKeyValidator(TimeProvider timeProvider) : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (options.Keys.Count == 0)
        {
            return ValidateOptionsResult.Skip;
        }

        var now = timeProvider.GetUtcNow();

        return options.Keys.Any(key => key.ActiveFrom <= now)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Jwt:Keys must contain at least one key whose ActiveFrom is not in the future.");
    }
}
