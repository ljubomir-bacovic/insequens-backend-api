namespace Journal.Domain;

public interface IEntity
{
    bool IsNew { get; }
}

public interface IEntity<T> : IEntity
{
    T Id { get; set; }
}
