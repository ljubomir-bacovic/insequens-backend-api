namespace Insequens.Domain;

/// <summary>
/// An entity whose creation and last change are recorded. The persistence layer sets these fields on save;
/// domain code never does.
/// </summary>
public abstract class AuditableEntity : BaseEntity<Guid>
{
    public DateTime CreatedOn { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedOn { get; private set; }
    public Guid? UpdatedBy { get; private set; }
}
