using Insequens.Contracts.V1.Auth;
using MediatR;

namespace Insequens.Application.Commands.Account;

/// <param name="UserId">From the link, so not yet trusted; parsed by the handler.</param>
public record ConfirmEmailChangeCommand(string UserId, string NewEmail, string Token) : IRequest<AuthMessageResponse>;
