using MediatR;

namespace Insequens.Application.Commands.Account;

/// <summary>
/// Permanently deletes every account whose deletion grace period has passed, with all its data. Meant to run on a
/// schedule (INS-082); returns how many accounts were purged.
/// </summary>
public record PurgeDeletedAccountsCommand : IRequest<int>;
