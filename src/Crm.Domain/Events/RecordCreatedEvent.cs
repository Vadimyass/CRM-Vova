using Crm.Domain.Common;

namespace Crm.Domain.Events;

public sealed record RecordCreatedEvent(string EntityName, Guid RecordId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public string EventType => "RecordCreated";
}
