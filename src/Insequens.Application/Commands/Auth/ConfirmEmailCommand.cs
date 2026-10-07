using MediatR;
using Insequens.Contracts.V1.Auth;

namespace Insequens.Application.Commands.Auth;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<AuthMessageResponse>;
