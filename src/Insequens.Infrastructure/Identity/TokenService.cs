using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Infrastructure.Identity;

public sealed class TokenService(
    IJwtKeyRing keyRing,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<TokenService> logger) : ITokenService
{
    private const int RefreshTokenByteLength = 64;
    private const string RoleClaimType = "role";

    // MapInboundClaims = false keeps the short JWT claim names (nameid) when reading a token back.
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public AccessToken CreateAccessToken(Guid userId, IEnumerable<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now + options.Value.AccessTokenLifetime;
        var subject = new ClaimsIdentity(
        [
            // The same claim names the v1 tokens carried, so existing clients keep working.
            new Claim(JwtRegisteredClaimNames.NameId, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            .. claims.Select(ToJwtClaim),
        ]);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Value.Issuer,
            Audience = options.Value.Audience,
            Subject = subject,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = keyRing.GetSigningCredentials(),
        });

        return new AccessToken(token, expiresAt);
    }

    public IssuedRefreshToken CreateRefreshToken() => new(
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(RefreshTokenByteLength)),
        timeProvider.GetUtcNow() + options.Value.RefreshTokenLifetime);

    public async Task<ExpiredAccessTokenResult> ValidateExpiredAccessTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ExpiredAccessTokenResult.Invalid;
        }

        var result = await _handler.ValidateTokenAsync(token, keyRing.CreateValidationParameters(validateLifetime: false));
        if (!result.IsValid)
        {
            logger.LogInformation("Access token rejected: {Reason}", result.Exception?.GetType().Name);
            return ExpiredAccessTokenResult.Invalid;
        }

        var userIdClaim = result.ClaimsIdentity.FindFirst(JwtRegisteredClaimNames.NameId)?.Value;

        return Guid.TryParse(userIdClaim, out var userId)
            ? ExpiredAccessTokenResult.Valid(userId)
            : ExpiredAccessTokenResult.Invalid;
    }

    // The short JWT names, which the JWT bearer handler maps back to ClaimTypes.Name and ClaimTypes.Role.
    private static Claim ToJwtClaim(Claim claim) => claim.Type switch
    {
        ClaimTypes.Name => new Claim(JwtRegisteredClaimNames.UniqueName, claim.Value),
        ClaimTypes.Role => new Claim(RoleClaimType, claim.Value),
        _ => claim,
    };
}
