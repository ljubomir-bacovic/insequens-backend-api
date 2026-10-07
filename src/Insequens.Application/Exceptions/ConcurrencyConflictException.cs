namespace Insequens.Application.Exceptions;

/// <summary>
/// Another request changed or deleted the resource between this request reading and saving it, and the client
/// sent no version to compare (409).
/// </summary>
public sealed class ConcurrencyConflictException(string resourceName, Guid id)
    : ResourceException($"{resourceName} {id} was changed by another request.", id)
{
    public string ResourceName { get; } = resourceName;
}
