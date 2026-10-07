namespace Insequens.Domain.Exceptions;

public sealed class ToDoItemDescriptionTooLongException(int maximumLength)
    : DomainException($"Task description must not exceed {maximumLength} characters.");
