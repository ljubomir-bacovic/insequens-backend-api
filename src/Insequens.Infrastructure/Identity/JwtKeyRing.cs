using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Insequens.Infrastructure.Identity;

public sealed class JwtKeyRing : IJwtKeyRing
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly IReadOnlyList<RingKey> _keys;

    public JwtKeyRing(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _timeProvider = timeProvider;
        _keys = _options.Keys.Count > 0
            ? _options.Keys.Select(key => new RingKey(key.ActiveFrom, CreateSecurityKey(key.Secret, key.Id))).ToArray()
            // A single Jwt:Key has no id, so its tokens carry no kid header, exactly as before rotation existed.
            : [new RingKey(DateTimeOffset.MinValue, CreateSecurityKey(_options.Key ?? string.Empty, keyId: null))];
    }

    public SigningCredentials GetSigningCredentials()
    {
        var now = _timeProvider.GetUtcNow();
        var activeKey = _keys
            .Where(key => key.ActiveFrom <= now)
            .OrderByDescending(key => key.ActiveFrom)
            .Select(key => key.SecurityKey)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No JWT signing key is active yet.");

        return new SigningCredentials(activeKey, SecurityAlgorithms.HmacSha256);
    }

    public TokenValidationParameters CreateValidationParameters(bool validateLifetime) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeys = _keys.Select(key => key.SecurityKey).ToArray(),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = validateLifetime,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero,
    };

    private static SymmetricSecurityKey CreateSecurityKey(string secret, string? keyId) =>
        new(Encoding.UTF8.GetBytes(secret)) { KeyId = keyId };

    private sealed record RingKey(DateTimeOffset ActiveFrom, SymmetricSecurityKey SecurityKey);
}
