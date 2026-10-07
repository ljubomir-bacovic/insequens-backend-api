namespace Insequens.Domain.Exceptions;

/// <summary>Base for every violated domain invariant. The API maps it to 400 with the message as detail.</summary>
public abstract class DomainException(string message) : Exception(message);
