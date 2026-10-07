using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record RegisterUserCommand(string Email, string Password) : IRequest<AuthMessageResponse>;
