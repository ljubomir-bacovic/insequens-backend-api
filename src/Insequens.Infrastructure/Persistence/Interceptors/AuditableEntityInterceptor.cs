using Insequens.Application.Abstractions;
using Insequens.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Insequens.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps <see cref="AuditableEntity"/> audit fields on every save: UTC timestamps from <see cref="TimeProvider"/>
/// and the acting user from <see cref="ICurrentUser"/>.
/// </summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedOn).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = userId;
                entry.Property(entity => entity.UpdatedOn).CurrentValue = now;
                entry.Property(entity => entity.UpdatedBy).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedOn).CurrentValue = now;
                entry.Property(entity => entity.UpdatedBy).CurrentValue = userId;
            }
        }
    }
}
