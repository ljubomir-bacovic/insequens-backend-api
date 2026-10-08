using MediatR;

namespace Insequens.Application.Commands.Tasks;

/// <summary>
/// Permanently deletes every task that has been in the trash longer than <c>TaskTrash:Retention</c>. Meant to run on
/// a schedule (INS-082); returns how many tasks were purged.
/// </summary>
public record PurgeDeletedTasksCommand : IRequest<int>;
