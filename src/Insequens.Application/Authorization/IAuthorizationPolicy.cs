namespace Insequens.Application.Authorization;

/// <summary>
/// One rule a request must pass before its handler runs: a role, ownership of a resource, and later workspace
/// membership. <see cref="AuthorizationBehavior{TRequest,TResponse}"/> runs every registered policy in order; a
/// policy that does not apply to <typeparamref name="TRequest"/> returns without doing anything.
/// </summary>
public interface IAuthorizationPolicy<in TRequest>
    where TRequest : notnull
{
    /// <summary>
    /// Returns when the request is allowed. Throws <see cref="Exceptions.ForbiddenException"/> (403) when the caller
    /// lacks a role, or <see cref="Exceptions.NotFoundException"/> (404) when the caller may not see the resource.
    /// </summary>
    Task AuthorizeAsync(TRequest request, CancellationToken cancellationToken);
}
