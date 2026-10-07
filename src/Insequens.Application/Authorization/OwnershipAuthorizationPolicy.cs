using Microsoft.Extensions.DependencyInjection;

namespace Insequens.Application.Authorization;

/// <summary>
/// For requests implementing <see cref="IOwned{TEntity}"/>, loads the resource through its ownership policy into the
/// scoped <see cref="IResourceContext{TEntity}"/> and throws <see cref="Exceptions.NotFoundException"/> when the caller
/// may not see it. Other requests pass.
/// </summary>
public sealed class OwnershipAuthorizationPolicy<TRequest>(IServiceProvider serviceProvider) : IAuthorizationPolicy<TRequest>
    where TRequest : notnull
{
    private static readonly Type? ResourceContextType = typeof(TRequest)
        .GetInterfaces()
        .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IOwned<>))
        .Select(type => typeof(IResourceContext<>).MakeGenericType(type.GetGenericArguments()))
        .SingleOrDefault();

    public async Task AuthorizeAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (ResourceContextType is not null && request is IResourceRequest resourceRequest)
        {
            // The scoped context is the same instance the handler receives, so the entity is loaded once.
            var loader = (IResourceLoader)serviceProvider.GetRequiredService(ResourceContextType);
            await loader.LoadAsync(resourceRequest.ResourceId, resourceRequest.UserId, cancellationToken);
        }
    }
}
