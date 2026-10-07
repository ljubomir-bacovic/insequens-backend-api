using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

/// <param name="IpAddress">The client address, recorded on the new refresh token; never logged.</param>
public record RefreshTokenCommand(string Token, string RefreshToken, string? IpAddress = null) : IRequest<AuthTokensResponse>;
