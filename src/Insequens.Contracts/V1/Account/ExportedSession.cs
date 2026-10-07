namespace Insequens.Contracts.V1.Account;

/// <summary>One issued refresh token. The token itself is never stored, so it cannot be exported.</summary>
public sealed record ExportedSession(
    Guid SessionId,
    string? DeviceName,
    string? IpAddress,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt);
