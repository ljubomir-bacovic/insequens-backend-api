using Insequens.Domain.Exceptions;

namespace Insequens.Domain.Entities;

/// <summary>
/// One refresh token issued to one device. Only its SHA-256 hash is stored. Every login starts a new family; each
/// refresh rotates the token within that family. Presenting a token that was already rotated or revoked means it was
/// copied, so the whole family is revoked.
/// </summary>
public sealed class RefreshToken : AuditableEntity, IOwnedEntity
{
    /// <summary>Base64 of a SHA-256 hash.</summary>
    public const int TokenHashLength = 44;
    public const int DeviceNameMaxLength = 100;

    /// <summary>Long enough for an IPv6 address with an IPv4 tail.</summary>
    public const int IpAddressMaxLength = 45;

    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public Guid FamilyId { get; private set; }
    public string? DeviceName { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>The first token of a new family, issued at login.</summary>
    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        string? deviceName,
        string? createdByIp) =>
        Create(userId, tokenHash, Guid.NewGuid(), expiresAt, deviceName, createdByIp);

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>Retires this token and returns its successor in the same family.</summary>
    public RefreshToken Rotate(string newTokenHash, DateTime newExpiresAt, string? createdByIp, DateTime now)
    {
        if (!IsActive(now))
        {
            throw new RefreshTokenNotActiveException();
        }

        RevokedAt = now;
        ReplacedByTokenHash = newTokenHash;

        return Create(UserId, newTokenHash, FamilyId, newExpiresAt, DeviceName, createdByIp);
    }

    /// <summary>Revokes the token. A token that is already revoked keeps its original revocation time.</summary>
    public void Revoke(DateTime now) => RevokedAt ??= now;

    private static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAt,
        string? deviceName,
        string? createdByIp)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("A token hash is required.", nameof(tokenHash));
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            ExpiresAt = expiresAt,
            DeviceName = Truncate(deviceName, DeviceNameMaxLength),
            CreatedByIp = Truncate(createdByIp, IpAddressMaxLength),
        };
    }

    // Device names are validated before they get here; an IPv6 address with a zone ID can exceed the column.
    private static string? Truncate(string? value, int maximumLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }
}
