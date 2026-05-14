namespace Insequens.Application.Commands;

/// <summary>
/// Marker interface for commands/queries that operate on a user-owned resource.
/// The OwnershipBehavior uses this to enforce access control automatically.
/// </summary>
public interface IOwned
{
    Guid UserId { get; }
    Guid ItemId { get; }
}
