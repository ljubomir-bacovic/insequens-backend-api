using Insequens.Application.Exceptions;
using Insequens.Domain;

namespace Insequens.Application.Authorization;

internal sealed class ResourceContext<TEntity>(IOwnershipPolicy<TEntity> policy) : IResourceContext<TEntity>, IResourceLoader
    where TEntity : class, IOwnedEntity
{
    private TEntity? _resource;

    public TEntity Resource => _resource
        ?? throw new InvalidOperationException(
            $"No {typeof(TEntity).Name} was authorized for this request. Does the request implement IOwned<{typeof(TEntity).Name}>?");

    public async Task LoadAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken)
    {
        _resource = await policy.FindOwnedAsync(resourceId, userId, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, resourceId);
    }
}
