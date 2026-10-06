using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<AuthMessageResponse>;
