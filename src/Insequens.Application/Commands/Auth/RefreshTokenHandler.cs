using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;

namespace Insequens.Application.Commands.Auth;

public class RefreshTokenHandler(
    IIdentityService identityService,
    ITokenService tokenService,
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

        if (!await identityService.ValidateRefreshTokenAsync(accessToken.UserId, request.RefreshToken, cancellationToken))
        {
            logger.LogInformation("Token refresh failed for user {UserId}: invalid refresh token", accessToken.UserId);
            throw new AuthenticationFailedException();
        }

        var user = await identityService.FindByIdAsync(accessToken.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException();

        return await AuthTokenIssuer.IssueAsync(tokenService, identityService, user, cancellationToken);
    }
}
