using Crm.Domain.Common;

namespace Crm.Domain.Events;

public sealed record StageChangedEvent(Guid OpportunityId, Guid? FromStageId, Guid ToStageId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public string EventType => "StageChanged";
}
