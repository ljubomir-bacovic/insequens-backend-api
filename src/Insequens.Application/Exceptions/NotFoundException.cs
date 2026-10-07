namespace Insequens.Application.Exceptions;

/// <summary>
/// The resource does not exist or the caller may not see it. Both cases look the same to the client (404), so
/// resource IDs cannot be probed.
/// </summary>
public sealed class NotFoundException(string resourceName, Guid id)
    : ResourceException($"{resourceName} {id} was not found.", id)
{
    public string ResourceName { get; } = resourceName;
}
