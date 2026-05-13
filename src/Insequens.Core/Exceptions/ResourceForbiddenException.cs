namespace Insequens.Core.Exceptions;

public class ResourceForbiddenException : Exception
{
    public Guid Id { get; }

    public ResourceForbiddenException(Guid id)
        : base($"Access denied for resource {id}.")
    {
        Id = id;
    }
}
