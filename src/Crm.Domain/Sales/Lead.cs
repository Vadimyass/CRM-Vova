using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Domain.Sales;

public class Lead : AuditableEntity, ICustomFieldOwner, IOwnedRecord
{
    private Lead() { }

    public Lead(string title)
    {
        Title = title;
        Raise(new RecordCreatedEvent(nameof(Lead), Id));
    }

    public string Title { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? EstimatedAmount { get; set; }
    public Guid? SourceId { get; set; }

    public LeadStatus Status { get; private set; } = LeadStatus.New;
    public string? DisqualifyReason { get; private set; }

    public Guid? AccountId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? OpportunityId { get; private set; }

    public Guid? OwnerId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Dictionary<string, object?> CustomData { get; set; } = [];

    public void ChangeStatus(LeadStatus status)
    {
        if (Status == status)
        {
            return;
        }

        var previous = Status;
        Status = status;
        Raise(new FieldChangedEvent(nameof(Lead), Id, nameof(Status), previous, status));
    }

    public void Qualify(Guid opportunityId)
    {
        OpportunityId = opportunityId;
        ChangeStatus(LeadStatus.Qualified);
    }

    public void Disqualify(string reason)
    {
        DisqualifyReason = reason;
        ChangeStatus(LeadStatus.Disqualified);
    }
}
