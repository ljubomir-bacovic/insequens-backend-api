namespace Insequens.Application.Abstractions.Identity;

public sealed record IssuedRefreshToken(string Value, DateTimeOffset ExpiresAt);
