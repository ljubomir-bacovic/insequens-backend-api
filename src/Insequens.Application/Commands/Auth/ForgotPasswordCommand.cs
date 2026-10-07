using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record ForgotPasswordCommand(string Email) : IRequest<AuthMessageResponse>;
