namespace Journal.Domain;

public abstract class AuditableEntity : BaseEntity<Guid>
{
    public DateTime CreatedOn { get; set; }
    public DateTime UpdatedOn { get; set; }
    //public T CreatedBy { get; set; } = default!;
    //public T UpdatedBy { get; set; } = default!;
}
