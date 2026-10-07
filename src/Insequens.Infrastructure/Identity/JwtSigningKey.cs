namespace Insequens.Infrastructure.Identity;

public sealed record JwtSigningKey
{
    /// <summary>Written to the token's <c>kid</c> header so the validator picks the matching key.</summary>
    public string Id { get; init; } = string.Empty;

    public string Secret { get; init; } = string.Empty;

    /// <summary>Tokens are signed with the newest key whose <c>ActiveFrom</c> has passed.</summary>
    public DateTimeOffset ActiveFrom { get; init; }
}
