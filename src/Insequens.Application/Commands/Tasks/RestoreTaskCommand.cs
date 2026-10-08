using MediatR;

namespace Insequens.Application.Commands.Tasks;

/// <summary>
/// Takes a task out of the trash. Not <c>IOwned</c>: ownership policies see only tasks that are not deleted, so the
/// handler's owner filter, which includes the trash, is the authorization.
/// </summary>
/// <param name="ExpectedVersion">The version the client last read (<c>If-Match</c>); null skips the check.</param>
public record RestoreTaskCommand(Guid ItemId, Guid UserId, byte[]? ExpectedVersion = null) : IRequest;
