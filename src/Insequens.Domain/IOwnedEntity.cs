namespace Insequens.Domain;

/// <summary>An entity that belongs to one user. Ownership policies authorize access by these two keys.</summary>
public interface IOwnedEntity
{
    Guid Id { get; }

    Guid UserId { get; }
}
