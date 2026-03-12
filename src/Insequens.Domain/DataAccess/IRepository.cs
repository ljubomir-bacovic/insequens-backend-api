using System.Linq.Expressions;

namespace Journal.Domain.DataAccess;

public interface IRepository<T>
    where T : class, IEntity
{
    void AddOrUpdate(T entity, bool? isNew = null);
    IQueryable<T> AsQueryable(params Expression<Func<T, object>>[]? includeExpressions);
    void AddOrUpdate(IEnumerable<T> entities, bool? isNew = null);
    T? Find(params object[] keyValues);
    Task<T?> FindAsync(params object[] keyValues);

    void Remove(T entity);
    void Remove(IEnumerable<T> entities);
    void Clone(T oldEntity, ref T newEntity);
}