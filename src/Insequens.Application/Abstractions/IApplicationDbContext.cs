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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
