namespace Insequens.Domain.Models.Auth;

public sealed record AuthTokensResponse(string Token, string RefreshToken);
