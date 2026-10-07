namespace Insequens.Application.Exceptions;

/// <summary>Base for failures about one specific resource, which every subclass must identify.</summary>
public abstract class ResourceException(string message, Guid id) : Exception(message)
{
    public Guid Id { get; } = id;
}
