using System.Runtime.Serialization;

namespace Journal.Core.Exceptions;

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

    protected ToDoItemNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
