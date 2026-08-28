namespace Crm.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? ModifiedOn { get; set; }
    public Guid? ModifiedById { get; set; }
}
