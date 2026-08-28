using Crm.Domain.Common;

namespace Crm.Domain.Events;

public sealed record FieldChangedEvent(string EntityName, Guid RecordId, string FieldName, object? OldValue, object? NewValue) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public string EventType => "FieldChanged";
}
