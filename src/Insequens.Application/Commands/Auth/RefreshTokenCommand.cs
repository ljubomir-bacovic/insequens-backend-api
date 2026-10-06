using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record RefreshTokenCommand(string Token, string RefreshToken) : IRequest<AuthTokensResponse>;
