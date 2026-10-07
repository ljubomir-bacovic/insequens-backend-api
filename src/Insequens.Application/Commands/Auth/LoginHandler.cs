using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using Insequens.Contracts.V1.Auth;
using Insequens.Application.Abstractions.Identity;
using Insequens.Domain.Entities;

namespace Insequens.Application.Commands.Auth;

public class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IApplicationDbContext dbContext,
    ILogger<LoginHandler> logger)
    : IRequestHandler<LoginCommand, AuthTokensResponse>
{
    public async Task<AuthTokensResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            logger.LogInformation("Login failed: no account for the email address");
            throw new AuthenticationFailedException();
        }

        var status = await identityService.CheckPasswordSignInAsync(user.Id, request.Password, cancellationToken);
        if (status != PasswordSignInStatus.Succeeded)
        {
            logger.LogInformation("Login failed for user {UserId}: {SignInStatus}", user.Id, status);
            throw new AuthenticationFailedException();
        }

        // Every login starts a new token family, so each device refreshes and logs out independently.
        var refreshToken = tokenService.CreateRefreshToken();
        var storedToken = RefreshToken.Issue(
            user.Id,
            RefreshTokenHash.Compute(refreshToken.Value),
            refreshToken.ExpiresAt.UtcDateTime,
            request.DeviceName,
            request.IpAddress);
        dbContext.RefreshTokens.Add(storedToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthTokenIssuer.CreateResponse(tokenService, user, storedToken.FamilyId, refreshToken);
    }
}
