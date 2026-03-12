using Journal.Domain;
using Journal.Domain.Data;
using Journal.Domain.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Journal.Infrastructure.DataAccess;

public class DataContext : IDataContext
{
    private readonly JournalContext _context;
    private bool _disposed;

    public DataContext(JournalContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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

    public async Task SaveChangesAsync()
    {
        try
        {
            SetAuditableProperties();
            await _context.SaveChangesAsync();
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

        foreach (var entityEntry in entries)
        {
            var now = DateTime.Now;
            if (entityEntry.State == EntityState.Added)
            {
                ((AuditableEntity)entityEntry.Entity).CreatedOn = now;
            }

            ((AuditableEntity)entityEntry.Entity).UpdatedOn = now;
        }
    }
}
