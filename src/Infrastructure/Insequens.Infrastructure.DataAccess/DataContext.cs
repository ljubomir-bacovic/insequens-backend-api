using Insequens.Domain;
using Insequens.Domain.Data;
using Insequens.Domain.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Insequens.Infrastructure.DataAccess;

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
        try
        {
            SetAuditableProperties();
            _context.SaveChanges();
        }
        catch (DbException exception)
        {
            var message = exception.Message;
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SetAuditableProperties();
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            var message = exception.Message;
            throw;
        }
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
                .Entries()
                .Where(e => e.Entity is AuditableEntity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entityEntry in entries)
        {
            if (entityEntry.State == EntityState.Added)
            {
                ((AuditableEntity)entityEntry.Entity).CreatedOn = now;
            }

            ((AuditableEntity)entityEntry.Entity).UpdatedOn = now;
        }
    }
}
