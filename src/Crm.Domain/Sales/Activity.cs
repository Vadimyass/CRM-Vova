using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Domain.Sales;

public class Activity : AuditableEntity, ICustomFieldOwner, IOwnedRecord
{
    private Activity() { }

    public Activity(string title, ActivityType type)
    {
        Title = title;
        Type = type;
        Raise(new RecordCreatedEvent(nameof(Activity), Id));
    }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ActivityType Type { get; set; }
    public ActivityStatus Status { get; private set; } = ActivityStatus.NotStarted;
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedOn { get; private set; }

    /// Polymorphic link to the record the activity belongs to.
    public string? RelatedEntityName { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public Guid? OwnerId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Dictionary<string, object?> CustomData { get; set; } = [];

    public void Complete()
    {
        Status = ActivityStatus.Done;
        CompletedOn = DateTimeOffset.UtcNow;
    }
}
