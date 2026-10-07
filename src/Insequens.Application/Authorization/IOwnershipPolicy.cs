using Insequens.Domain;

namespace Insequens.Application.Authorization;

/// <summary>Decides whether a user may act on a resource, and loads it if so.</summary>
public interface IOwnershipPolicy<TEntity>
    where TEntity : class, IOwnedEntity
{
    /// <summary>Returns the tracked entity, or null when it does not exist or belongs to someone else.</summary>
    Task<TEntity?> FindOwnedAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken);
}
