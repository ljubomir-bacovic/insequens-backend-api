using Insequens.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Insequens.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps <see cref="AuditableEntity"/> timestamps in UTC from <see cref="TimeProvider"/> on every save.</summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
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

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedOn).CurrentValue = now;
                entry.Property(entity => entity.UpdatedOn).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedOn).CurrentValue = now;
            }
        }
    }
}
