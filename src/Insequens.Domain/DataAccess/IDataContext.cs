namespace Journal.Domain.DataAccess;
public interface IDataContext : IDisposable
{
    void SaveChanges();

    Task SaveChangesAsync();

    IRepository<T> GetRepository<T>()
        where T : class, IEntity;
}