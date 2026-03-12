using Journal.Domain;
using Journal.Domain.DataAccess;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Journal.Infrastructure.DataAccess;

public class Repository<T> : IRepository<T>
    where T : class, IEntity

{
    private readonly DbContext _context;
    private readonly DbSet<T> _dataSet;

    public Repository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dataSet = context.Set<T>();
    }

    public IQueryable<T> AsQueryable(params Expression<Func<T, object>>[]? includeExpressions)
    {
        IQueryable<T> result = _dataSet;
        if (includeExpressions != null && includeExpressions.Any())
            result = includeExpressions.Aggregate(result, (query, expression) => query.Include(expression));

        return result.AsQueryable();
    }

    public void AddOrUpdate(IEnumerable<T> entities, bool? isNew = null)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        foreach (var entity in entities) AddOrUpdate(entity, isNew);
    }

    public T? Find(params object[] keyValues)
    {
        if (keyValues == null) throw new ArgumentNullException(nameof(keyValues));

        if (keyValues.Length == 0)
            throw new ArgumentException("Key values array shouldn't be empty", nameof(keyValues));

        return _dataSet.Find(keyValues);
    }

    public async Task<T?> FindAsync(params object[] keyValues)
    {
        if (keyValues == null) throw new ArgumentNullException(nameof(keyValues));

        if (keyValues.Length == 0)
            throw new ArgumentException("Key values array shouldn't be empty", nameof(keyValues));

        return await _dataSet.FindAsync(keyValues);
    }

    public void Remove(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        if (_context.Entry(entity).State == EntityState.Detached) _dataSet.Attach(entity);

        _dataSet.Remove(entity);
    }

    public void Remove(IEnumerable<T> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        foreach (var entity in entities) Remove(entity);
    }

    public void Clone(T oldEntity, ref T newEntity)
    {
        if (oldEntity == null) throw new ArgumentNullException(nameof(oldEntity));

        if (_context.Entry(newEntity).State == EntityState.Detached) _dataSet.Add(newEntity);

        var values = _context.Entry(oldEntity).CurrentValues.Clone();
        _context.Entry(newEntity).CurrentValues.SetValues(values);
    }

    public void AddOrUpdate(T entity, bool? isNew = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        if (_context.Entry(entity).State == EntityState.Detached) _dataSet.Add(entity);
    }
}