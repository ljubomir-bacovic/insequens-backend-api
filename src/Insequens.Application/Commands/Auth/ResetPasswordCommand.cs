using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<AuthMessageResponse>;
