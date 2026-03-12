namespace Journal.Domain;
public abstract class BaseEntity<T> : IEntity<T>
{
    public T Id { get; set; } = default!;

    public bool IsNew => Equals(Id, default(T));

}
