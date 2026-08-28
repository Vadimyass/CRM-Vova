using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Domain.Sales;

public class Opportunity : AuditableEntity, ICustomFieldOwner, IOwnedRecord
{
    private readonly List<StageHistoryItem> _stageHistory = [];

    private Opportunity() { }

    public Opportunity(string title, Guid initialStageId)
    {
        Title = title;
        StageId = initialStageId;
        StageEnteredOn = DateTimeOffset.UtcNow;
        _stageHistory.Add(new StageHistoryItem { OpportunityId = Id, ToStageId = initialStageId });
        Raise(new RecordCreatedEvent(nameof(Opportunity), Id));
    }

    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public DateOnly? CloseDate { get; set; }

    public Guid StageId { get; private set; }
    public DateTimeOffset StageEnteredOn { get; private set; }

    public Guid? AccountId { get; set; }
    public Guid? PrimaryContactId { get; set; }

    public Guid? OwnerId { get; set; }
    public Guid? OrgUnitId { get; set; }
    public Dictionary<string, object?> CustomData { get; set; } = [];

    public IReadOnlyList<StageHistoryItem> StageHistory => _stageHistory;

    public void MoveToStage(Guid stageId, Guid? changedById = null)
    {
        if (StageId == stageId)
        {
            return;
        }

        var previousStageId = StageId;
        var now = DateTimeOffset.UtcNow;

        _stageHistory.Add(new StageHistoryItem
        {
            OpportunityId = Id,
            FromStageId = previousStageId,
            ToStageId = stageId,
            ChangedOn = now,
            ChangedById = changedById,
            TimeInPreviousStage = now - StageEnteredOn
        });

        StageId = stageId;
        StageEnteredOn = now;

        Raise(new StageChangedEvent(Id, previousStageId, stageId));
    }
}
