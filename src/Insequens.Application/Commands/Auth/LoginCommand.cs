using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokensResponse>;
