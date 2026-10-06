using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokensResponse>;
