using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record RefreshTokenCommand(string Token, string RefreshToken) : IRequest<AuthTokensResponse>;
