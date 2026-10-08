using Insequens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Application.Abstractions;

/// <summary>
/// The persistence surface handlers work with: one <see cref="DbSet{TEntity}"/> per aggregate and a single
/// <see cref="SaveChangesAsync"/> at the end of a command. Audit fields are set by an interceptor on save.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<ToDoItem> ToDoItems { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    /// <summary>The set for any entity, for generic code such as ownership policies.</summary>
    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
