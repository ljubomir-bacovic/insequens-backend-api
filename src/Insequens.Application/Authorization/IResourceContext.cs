using Insequens.Domain;

namespace Insequens.Application.Authorization;

/// <summary>The resource the current request was authorized for, already loaded and tracked.</summary>
public interface IResourceContext<out TEntity>
    where TEntity : class, IOwnedEntity
{
    TEntity Resource { get; }
}
