using Insequens.Domain;

namespace Insequens.Application.Authorization;

/// <summary>
/// Marks a request on a <typeparamref name="TEntity"/> that only its owner may touch. The
/// <see cref="AuthorizationBehavior{TRequest,TResponse}"/> loads the entity once through
/// <see cref="IOwnershipPolicy{TEntity}"/> and the handler receives it from <see cref="IResourceContext{TEntity}"/>.
/// A missing or foreign resource is a 404, so callers cannot probe for IDs.
/// </summary>
public interface IOwned<TEntity> : IResourceRequest
    where TEntity : class, IOwnedEntity;
