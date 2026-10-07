using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Commands.Auth;

public class RefreshTokenHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<RefreshTokenHandler> logger)
    : IRequestHandler<RefreshTokenCommand, AuthTokensResponse>
{
    public async Task<AuthTokensResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var accessToken = await tokenService.ValidateExpiredAccessTokenAsync(request.Token);
        if (!accessToken.IsValid)
        {
            logger.LogInformation("Token refresh failed: invalid access token");
            throw new AuthenticationFailedException();
        }

        var tokenHash = RefreshTokenHash.Compute(request.RefreshToken);
        var storedToken = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
        if (storedToken is null || storedToken.UserId != accessToken.UserId)
        {
            logger.LogInformation("Token refresh failed for user {UserId}: unknown refresh token", accessToken.UserId);
            throw new AuthenticationFailedException();
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (storedToken.RevokedAt is not null)
        {
            // A rotated or revoked token came back: someone holds a copy. End the whole session.
            await dbContext.RevokeRefreshTokensAsync(storedToken.UserId, storedToken.FamilyId, now, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoked session {SessionId}",
                storedToken.UserId,
                storedToken.FamilyId);
            throw new AuthenticationFailedException();
        }

        if (!storedToken.IsActive(now))
        {
            logger.LogInformation("Token refresh failed for user {UserId}: refresh token expired", storedToken.UserId);
            throw new AuthenticationFailedException();
        }

        var user = await identityService.FindByIdAsync(storedToken.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException();

        var refreshToken = tokenService.CreateRefreshToken();
        var replacement = storedToken.Rotate(
            RefreshTokenHash.Compute(refreshToken.Value),
            refreshToken.ExpiresAt.UtcDateTime,
            request.IpAddress,
            now);
        dbContext.RefreshTokens.Add(replacement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent request rotated the same token first; that request keeps the session.
            logger.LogInformation("Token refresh failed for user {UserId}: refresh token already rotated", storedToken.UserId);
            throw new AuthenticationFailedException();
        }

        return await AuthTokenIssuer.CreateResponseAsync(
            tokenService, identityService, user, replacement.FamilyId, refreshToken, cancellationToken);
    }
}
