namespace Insequens.Application.Authorization;

/// <summary>Lets the non-generic <see cref="AuthorizationBehavior{TRequest,TResponse}"/> fill a typed resource context.</summary>
internal interface IResourceLoader
{
    Task LoadAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken);
}
