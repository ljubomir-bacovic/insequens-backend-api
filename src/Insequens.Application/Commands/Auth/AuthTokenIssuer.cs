using System.Security.Claims;
using Insequens.Application.Abstractions.Identity;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

internal static class AuthTokenIssuer
{
    /// <summary>
    /// An access token for <paramref name="user"/> in session <paramref name="sessionId"/>, carrying the user's roles,
    /// paired with its refresh token.
    /// </summary>
    public static async Task<AuthTokensResponse> CreateResponseAsync(
        ITokenService tokenService,
        IIdentityService identityService,
        AuthUser user,
        Guid sessionId,
        IssuedRefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        var roles = await identityService.GetRolesAsync(user.Id, cancellationToken);
        var accessToken = tokenService.CreateAccessToken(
            user.Id,
            [
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(AuthClaimTypes.SessionId, sessionId.ToString()),
                .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
            ]);

        return new AuthTokensResponse(accessToken.Value, refreshToken.Value);
    }
}
