namespace Insequens.Domain.DataAccess;
public interface IDataContext : IDisposable
{
    void SaveChanges();

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    IRepository<T> GetRepository<T>()
        where T : class, IEntity;
}
