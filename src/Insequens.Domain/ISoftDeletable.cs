namespace Insequens.Domain;

/// <summary>
/// An entity that is moved to a trash instead of being deleted. A global query filter hides it; it can be restored
/// until it is purged. The persistence layer stamps <see cref="DeletedOn"/> and <see cref="DeletedBy"/> when
/// <see cref="IsDeleted"/> changes, as it does the audit fields.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTime? DeletedOn { get; }

    Guid? DeletedBy { get; }
}
