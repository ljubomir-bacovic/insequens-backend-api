namespace Insequens.Domain.Models.Auth;

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);
