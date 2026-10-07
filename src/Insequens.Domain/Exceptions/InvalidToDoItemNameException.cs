namespace Insequens.Domain.Exceptions;

public sealed class InvalidToDoItemNameException(string message) : DomainException(message);
