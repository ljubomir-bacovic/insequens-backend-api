using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record RegisterUserCommand(string Email, string Password) : IRequest<AuthMessageResponse>;
