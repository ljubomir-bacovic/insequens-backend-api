namespace Insequens.Contracts.V1.Auth;

public sealed record RefreshTokenRequest(string Token, string RefreshToken);
