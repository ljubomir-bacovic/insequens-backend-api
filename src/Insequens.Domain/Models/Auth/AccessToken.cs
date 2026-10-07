namespace Insequens.Domain.Models.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
