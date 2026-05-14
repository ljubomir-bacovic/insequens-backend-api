namespace Insequens.Application.Commands;

/// <summary>
/// Marker interface for commands/queries that operate on a user-owned resource.
/// The OwnershipBehavior uses this to enforce access control automatically.
/// </summary>
public interface IOwned
{
    /// <summary>
    /// Gets the identifier of the user who owns the resource.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the identifier of the user-owned resource being accessed.
    /// </summary>
    Guid ItemId { get; }
}
