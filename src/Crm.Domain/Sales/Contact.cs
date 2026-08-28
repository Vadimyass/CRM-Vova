using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Domain.Sales;

public class Contact : AuditableEntity, ICustomFieldOwner, IOwnedRecord
{
    private Contact() { }

    public Contact(string fullName)
    {
        FullName = fullName;
        Raise(new RecordCreatedEvent(nameof(Contact), Id));
    }

    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }

    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }

    public Guid? OwnerId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Dictionary<string, object?> CustomData { get; set; } = [];
}
