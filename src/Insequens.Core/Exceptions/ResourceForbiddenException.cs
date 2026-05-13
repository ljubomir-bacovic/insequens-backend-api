using System.Runtime.Serialization;

namespace Insequens.Core.Exceptions;

[Serializable]
public class ResourceForbiddenException : Exception
{
    public Guid Id { get; }

    public ResourceForbiddenException()
    {
    }

    public ResourceForbiddenException(Guid id)
        : base($"Access denied for resource {id}.")
    {
        Id = id;
    }

    public ResourceForbiddenException(string? message) : base(message)
    {
    }

    public ResourceForbiddenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected ResourceForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
