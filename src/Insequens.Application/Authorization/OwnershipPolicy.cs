using Insequens.Application.Abstractions;
using Insequens.Domain;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Authorization;

/// <summary>Owner-only access in a single query. Membership-aware policies replace it per entity later.</summary>
public sealed class OwnershipPolicy<TEntity>(IApplicationDbContext dbContext) : IOwnershipPolicy<TEntity>
    where TEntity : class, IOwnedEntity
{
    public Task<TEntity?> FindOwnedAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>()
            .SingleOrDefaultAsync(entity => entity.Id == resourceId && entity.UserId == userId, cancellationToken);
}
