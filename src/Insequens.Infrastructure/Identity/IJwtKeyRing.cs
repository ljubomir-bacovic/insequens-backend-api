using Microsoft.IdentityModel.Tokens;

namespace Insequens.Infrastructure.Identity;

/// <summary>
/// The configured JWT signing keys. Shared by token creation and by the JWT bearer handler so both
/// use the same issuer, audience, algorithm and keys.
/// </summary>
public interface IJwtKeyRing
{
    /// <summary>Credentials for the newest key whose <c>ActiveFrom</c> has passed.</summary>
    SigningCredentials GetSigningCredentials();

    /// <summary>Parameters that accept a token signed with any configured key.</summary>
    TokenValidationParameters CreateValidationParameters(bool validateLifetime);
}
