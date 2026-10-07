using System.Security.Claims;
using Insequens.Application.Abstractions.Identity;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

internal static class AuthTokenIssuer
{
    /// <summary>An access token for <paramref name="user"/> in session <paramref name="sessionId"/>, paired with its refresh token.</summary>
    public static AuthTokensResponse CreateResponse(
        ITokenService tokenService,
        AuthUser user,
        Guid sessionId,
        IssuedRefreshToken refreshToken)
    {
        var accessToken = tokenService.CreateAccessToken(
            user.Id,
            [
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(AuthClaimTypes.SessionId, sessionId.ToString()),
            ]);

        return new AuthTokensResponse(accessToken.Value, refreshToken.Value);
    }
}
