namespace Crm.Domain.Common;

/// Record-level access is resolved from these two columns by EF global query filters.
public interface IOwnedRecord
{
    Guid? OwnerId { get; set; }
    Guid? OrgUnitId { get; set; }
}
