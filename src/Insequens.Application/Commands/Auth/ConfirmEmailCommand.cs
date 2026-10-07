using Insequens.Domain.Models.Auth;
using MediatR;

namespace Insequens.Application.Commands.Auth;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<AuthMessageResponse>;
