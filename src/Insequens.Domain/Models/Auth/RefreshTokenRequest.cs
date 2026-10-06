namespace Insequens.Domain.Models.Auth;

public sealed record RefreshTokenRequest(string Token, string RefreshToken);
