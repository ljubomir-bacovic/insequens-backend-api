namespace Insequens.Domain;

public abstract class AuditableEntity : BaseEntity<Guid>
{
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
}
