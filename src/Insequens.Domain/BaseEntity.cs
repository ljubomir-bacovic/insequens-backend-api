namespace Insequens.Domain;

public abstract class BaseEntity<T> : IEntity<T>
{
    public T Id { get; set; } = default!;
}
