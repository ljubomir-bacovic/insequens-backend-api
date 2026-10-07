using Insequens.Domain;
using Insequens.Domain.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Insequens.Infrastructure.Persistence;

public class DataContext : IDataContext
{
    private readonly InsequensContext _context;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public DataContext(InsequensContext context, TimeProvider timeProvider)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void SaveChanges()
    {
        SetAuditableProperties();
        _context.SaveChanges();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditableProperties();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public IRepository<T> GetRepository<T>()
        where T : class, IEntity
    {
        return new Repository<T>(_context);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing) _context.Dispose();

            _disposed = true;
        }
    }

    private void SetAuditableProperties()
    {
        var entries = _context.ChangeTracker
                .Entries<AuditableEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                entityEntry.Entity.CreatedOn = now;
            }

            entityEntry.Entity.UpdatedOn = now;
        }
    }
}
