using Insequens.Contracts.V1.Account;
using MediatR;

namespace Insequens.Application.Commands.Account;

public record DeleteAccountCommand(Guid UserId, string CurrentPassword) : IRequest<AccountDeletionResponse>;
