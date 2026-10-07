namespace Insequens.Application.Exceptions;

/// <summary>
/// The client sent the version it last read (<c>If-Match</c>) and the resource has changed since. The client
/// should read it again before retrying (412).
/// </summary>
public sealed class PreconditionFailedException(string resourceName, Guid id)
    : ResourceException($"{resourceName} {id} has changed since the given version.", id)
{
    public string ResourceName { get; } = resourceName;
}
