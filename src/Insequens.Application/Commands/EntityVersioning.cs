using Insequens.Application.Abstractions;
using Insequens.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using ToDoItemEntity = Insequens.Domain.Entities.ToDoItem;

namespace Insequens.Application.Commands;

/// <remarks>Shared by the v1 and v2 task commands.</remarks>
internal static class EntityVersioning
{
    /// <summary>
    /// Saves a change to <paramref name="item"/> under optimistic concurrency. With an expected version (the
    /// request's <c>If-Match</c>), a stale one or a concurrent change is a <see cref="PreconditionFailedException"/>;
    /// without one, a concurrent change is a <see cref="ConcurrencyConflictException"/>.
    /// </summary>
    public static async Task SaveChangesAsync(
        this IApplicationDbContext dbContext,
        ToDoItemEntity item,
        byte[]? expectedVersion,
        CancellationToken cancellationToken)
    {
        if (expectedVersion is not null && !expectedVersion.AsSpan().SequenceEqual(item.RowVersion))
        {
            throw new PreconditionFailedException(nameof(ToDoItemEntity), item.Id);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw expectedVersion is null
                ? new ConcurrencyConflictException(nameof(ToDoItemEntity), item.Id)
                : new PreconditionFailedException(nameof(ToDoItemEntity), item.Id);
        }
    }
}
