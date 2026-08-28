namespace Crm.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
    string EventType { get; }
}
