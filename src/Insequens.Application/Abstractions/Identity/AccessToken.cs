namespace Insequens.Application.Abstractions.Identity;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
