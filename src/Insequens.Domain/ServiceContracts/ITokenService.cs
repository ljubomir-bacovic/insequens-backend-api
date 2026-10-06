using System.Security.Claims;
using Insequens.Domain.Models.Auth;

namespace Insequens.Domain.ServiceContracts;

public interface ITokenService
{
    AccessToken CreateAccessToken(Guid userId, IEnumerable<Claim> claims);

    IssuedRefreshToken CreateRefreshToken();

    /// <summary>
    /// Validates the signature, issuer, audience and algorithm of an access token while ignoring its
    /// lifetime. Never throws for a bad token; returns <see cref="ExpiredAccessTokenResult.Invalid"/>.
    /// </summary>
    Task<ExpiredAccessTokenResult> ValidateExpiredAccessTokenAsync(string token);
}
