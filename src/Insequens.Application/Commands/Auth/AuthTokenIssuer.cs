using System.Security.Claims;
using Insequens.Domain.Models.Auth;
using Insequens.Domain.ServiceContracts;

namespace Insequens.Application.Commands.Auth;

internal static class AuthTokenIssuer
{
    public static async Task<AuthTokensResponse> IssueAsync(
        ITokenService tokenService,
        IIdentityService identityService,
        AuthUser user,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.CreateAccessToken(user.Id, [new Claim(ClaimTypes.Name, user.Email)]);
        var refreshToken = tokenService.CreateRefreshToken();

        await identityService.StoreRefreshTokenAsync(user.Id, refreshToken, cancellationToken);

        return new AuthTokensResponse(accessToken.Value, refreshToken.Value);
    }
}
