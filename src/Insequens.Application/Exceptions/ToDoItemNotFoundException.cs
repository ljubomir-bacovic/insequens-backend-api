using System.Runtime.Serialization;

namespace Insequens.Application.Exceptions;

[Serializable]
public class ToDoItemNotFoundException : Exception
{
    public Guid Id { get; }

    public ToDoItemNotFoundException()
    {
    }

    public ToDoItemNotFoundException(Guid id)
    {
        Id = id;
    }

    public ToDoItemNotFoundException(string? message) : base(message)
    {
    }

    public ToDoItemNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    [Obsolete("Binary serialization is obsolete and should not be used.")]
    protected ToDoItemNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
