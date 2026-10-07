using Insequens.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Commands.Auth;

internal static class RefreshTokenRevocation
{
    /// <summary>
    /// Revokes the user's active refresh tokens, only those of one family when <paramref name="familyId"/> is set.
    /// The caller saves.
    /// </summary>
    public static async Task RevokeRefreshTokensAsync(
        this IApplicationDbContext dbContext,
        Guid userId,
        Guid? familyId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .Where(token => familyId == null || token.FamilyId == familyId)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(now);
        }
    }
}
