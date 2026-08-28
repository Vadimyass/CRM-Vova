using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Domain.Sales;

public class Account : AuditableEntity, ICustomFieldOwner, IOwnedRecord
{
    private Account() { }

    public Account(string name)
    {
        Name = name;
        Raise(new RecordCreatedEvent(nameof(Account), Id));
    }

    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Inn { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public Guid? IndustryId { get; set; }
    public Guid? PrimaryContactId { get; set; }

    public Guid? OwnerId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Dictionary<string, object?> CustomData { get; set; } = [];

    public ICollection<Contact> Contacts { get; set; } = [];
}
