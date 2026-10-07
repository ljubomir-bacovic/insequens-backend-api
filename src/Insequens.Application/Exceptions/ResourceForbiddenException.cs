namespace Insequens.Application.Exceptions;

/// <summary>
/// The caller can see the resource but lacks the role for this action (403). Ownership failures are a
/// <see cref="NotFoundException"/> instead.
/// </summary>
public sealed class ResourceForbiddenException(Guid id) : ResourceException($"Access denied for resource {id}.", id);
