using Insequens.Contracts.V1.Auth;
using MediatR;

namespace Insequens.Application.Commands.Account;

public record RequestEmailChangeCommand(Guid UserId, string NewEmail, string CurrentPassword) : IRequest<AuthMessageResponse>;
